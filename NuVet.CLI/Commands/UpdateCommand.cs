using Microsoft.Extensions.Logging;
using NuVet.Core.Models;
using NuVet.Core.Services;
using Spectre.Console;
using System.CommandLine;

namespace NuVet.CLI.Commands;

/// <summary>
/// Command to update vulnerable packages
/// </summary>
public class UpdateCommand
{
    private readonly IVulnerabilityScanner _scanner;
    private readonly IPackageUpdater _updater;
    private readonly ILogger<UpdateCommand> _logger;

    public UpdateCommand(IVulnerabilityScanner scanner, IPackageUpdater updater, ILogger<UpdateCommand> logger)
    {
        _scanner = scanner;
        _updater = updater;
        _logger = logger;
    }

    public Command Create()
    {
        var pathArgument = new Argument<string>("path", "Path to solution, project, or directory to update");
        var dryRunOption = new Option<bool>("--dry-run", "Show what would be updated without making changes");
        var autoApproveOption = new Option<bool>("--auto-approve", "Automatically approve all updates");
        var minSeverityOption = new Option<VulnerabilitySeverity>("--min-severity", () => VulnerabilitySeverity.Low, "Minimum severity to update");
        var excludePackagesOption = new Option<string[]>("--exclude", "Package names to exclude from updates");
        var noBackupOption = new Option<bool>("--no-backup", "Skip creating backups before updates");
        var noValidationOption = new Option<bool>("--no-validation", "Skip build validation after updates");
        var verboseOption = new Option<bool>("--verbose", "Enable verbose logging");

        var command = new Command("update", "Update vulnerable packages to safe versions")
        {
            pathArgument,
            dryRunOption,
            autoApproveOption,
            minSeverityOption,
            excludePackagesOption,
            noBackupOption,
            noValidationOption,
            verboseOption
        };

        command.SetHandler(async (path, dryRun, autoApprove, minSeverity, excludePackages, noBackup, noValidation, verbose) =>
        {
            var updateOptions = new UpdateOptions
            {
                MinimumSeverityToUpdate = minSeverity,
                ExcludePackages = excludePackages?.ToList() ?? new List<string>(),
                CreateBackup = !noBackup,
                ValidateAfterUpdate = !noValidation,
                AutoApproveMinorUpdates = autoApprove,
                AutoApprovePatchUpdates = autoApprove
            };

            await ExecuteUpdateAsync(path, dryRun, autoApprove, updateOptions, verbose);
        }, pathArgument, dryRunOption, autoApproveOption, minSeverityOption, excludePackagesOption, noBackupOption, noValidationOption, verboseOption);

        return command;
    }

    private async Task ExecuteUpdateAsync(string path, bool dryRun, bool autoApprove, UpdateOptions options, bool verbose)
    {
        try
        {
            AnsiConsole.Write(new Rule("[bold green]NuVet - Package Updater[/]"));
            AnsiConsole.WriteLine();

            if (!Path.Exists(path))
            {
                AnsiConsole.WriteLine($"[red]Error:[/] Path '{path}' does not exist.");
                Environment.Exit(1);
                return;
            }

            // First, scan for vulnerabilities
            AnsiConsole.WriteLine("[blue]Step 1:[/] Scanning for vulnerable packages...");
            
            var scanOptions = new ScanOptions
            {
                MinimumSeverity = options.MinimumSeverityToUpdate,
                ExcludePackages = options.ExcludePackages
            };

            var scanResult = await _scanner.ScanAsync(path, scanOptions);

            if (!scanResult.HasVulnerabilities)
            {
                AnsiConsole.WriteLine("[green]✓ No vulnerable packages found! Nothing to update.[/]");
                return;
            }

            // Show what will be updated
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[bold yellow]Packages to Update[/]"));
            AnsiConsole.WriteLine();

            var updateTable = new Table();
            updateTable.AddColumn("Package");
            updateTable.AddColumn("Current Version");
            updateTable.AddColumn("Suggested Version");
            updateTable.AddColumn("Severity");
            updateTable.AddColumn("Vulnerabilities Fixed");

            foreach (var vulnerablePackage in scanResult.VulnerablePackages)
            {
                var suggestedVersions = vulnerablePackage.GetSuggestedUpdateVersions();
                var targetVersion = suggestedVersions.FirstOrDefault()?.ToString() ?? "N/A";
                var severityColor = GetSeverityColor(vulnerablePackage.HighestSeverity);

                updateTable.AddRow(
                    vulnerablePackage.Package.Id,
                    vulnerablePackage.Package.Version.ToString(),
                    targetVersion,
                    $"[{severityColor}]{vulnerablePackage.HighestSeverity}[/]",
                    vulnerablePackage.Vulnerabilities.Count.ToString()
                );
            }

            AnsiConsole.Write(updateTable);
            AnsiConsole.WriteLine();

            if (dryRun)
            {
                AnsiConsole.WriteLine("[yellow]Dry run mode - no changes will be made.[/]");
                return;
            }

            // Confirm updates if not auto-approved
            if (!autoApprove)
            {
                if (!AnsiConsole.Confirm($"Update {scanResult.VulnerablePackages.Count} packages?"))
                {
                    AnsiConsole.WriteLine("[yellow]Update cancelled.[/]");
                    return;
                }
            }

            // Perform updates
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("[blue]Step 2:[/] Updating packages...");

            List<UpdateResult> updateResults;
            
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Updating packages...[/]");
                    task.MaxValue = scanResult.VulnerablePackages.Count;
                    
                    updateResults = await _updater.UpdateVulnerablePackagesAsync(scanResult, options);
                    task.Value = scanResult.VulnerablePackages.Count;
                });

            updateResults = await _updater.UpdateVulnerablePackagesAsync(scanResult, options);

            // Show results
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[bold yellow]Update Results[/]"));
            AnsiConsole.WriteLine();

            var resultsTable = new Table();
            resultsTable.AddColumn("Package");
            resultsTable.AddColumn("Status");
            resultsTable.AddColumn("Details");

            var successCount = 0;
            var failureCount = 0;
            var skippedCount = 0;

            foreach (var result in updateResults)
            {
                var statusText = result.Status switch
                {
                    UpdateStatus.Success => "[green]Success[/]",
                    UpdateStatus.Failed => "[red]Failed[/]",
                    UpdateStatus.Skipped => "[yellow]Skipped[/]",
                    UpdateStatus.RequiresManualIntervention => "[orange3]Manual Review Required[/]",
                    _ => "[dim]Unknown[/]"
                };

                string details = result.Status switch
                {
                    UpdateStatus.Success => $"Updated to {result.Update.TargetVersion}",
                    UpdateStatus.Failed => result.ErrorMessage ?? "Unknown error",
                    UpdateStatus.Skipped => "Skipped based on options",
                    UpdateStatus.RequiresManualIntervention => "Breaking changes detected",
                    _ => ""
                };

                resultsTable.AddRow(
                    result.Update.CurrentPackage.Id,
                    statusText,
                    details
                );

                switch (result.Status)
                {
                    case UpdateStatus.Success:
                        successCount++;
                        break;
                    case UpdateStatus.Failed:
                        failureCount++;
                        break;
                    case UpdateStatus.Skipped:
                        skippedCount++;
                        break;
                }
            }

            AnsiConsole.Write(resultsTable);
            AnsiConsole.WriteLine();

            // Summary
            AnsiConsole.WriteLine($"[green]✓ Successful:[/] {successCount}");
            if (failureCount > 0)
                AnsiConsole.WriteLine($"[red]✗ Failed:[/] {failureCount}");
            if (skippedCount > 0)
                AnsiConsole.WriteLine($"[yellow]- Skipped:[/] {skippedCount}");

            if (failureCount > 0)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine("[red]Some updates failed. Check the error messages above and consider updating those packages manually.[/]");
                Environment.Exit(1);
            }
            else if (successCount > 0)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine("[green]All updates completed successfully![/]");
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during update execution");
            AnsiConsole.WriteLine($"[red]Error:[/] {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static string GetSeverityColor(VulnerabilitySeverity severity) => severity switch
    {
        VulnerabilitySeverity.Critical => "red",
        VulnerabilitySeverity.High => "orange3",
        VulnerabilitySeverity.Moderate => "yellow",
        VulnerabilitySeverity.Low => "blue",
        _ => "dim"
    };
}