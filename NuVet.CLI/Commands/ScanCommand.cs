using Microsoft.Extensions.Logging;
using NuVet.Core.Models;
using NuVet.Core.Services;
using Spectre.Console;
using System.CommandLine;

namespace NuVet.CLI.Commands;

/// <summary>
/// Command to scan projects for vulnerable packages
/// </summary>
public class ScanCommand
{
    private readonly IVulnerabilityScanner _scanner;
    private readonly ILogger<ScanCommand> _logger;

    public ScanCommand(IVulnerabilityScanner scanner, ILogger<ScanCommand> logger)
    {
        _scanner = scanner;
        _logger = logger;
    }

    public Command Create()
    {
        var pathArgument = new Argument<string>("path", "Path to solution, project, or directory to scan");
        var outputOption = new Option<string?>("--output", "Output file path for results (JSON format)");
        var severityOption = new Option<VulnerabilitySeverity>("--min-severity", () => VulnerabilitySeverity.Low, "Minimum severity level to report");
        var includeTransitiveOption = new Option<bool>("--include-transitive", () => true, "Include transitive dependencies");
        var excludePackagesOption = new Option<string[]>("--exclude", "Package names to exclude from scan");
        var jsonOption = new Option<bool>("--json", "Output results in JSON format");
        var verboseOption = new Option<bool>("--verbose", "Enable verbose logging");

        var command = new Command("scan", "Scan for vulnerable packages")
        {
            pathArgument,
            outputOption,
            severityOption,
            includeTransitiveOption,
            excludePackagesOption,
            jsonOption,
            verboseOption
        };

        command.SetHandler(async (path, output, minSeverity, includeTransitive, excludePackages, json, verbose) =>
        {
            if (verbose)
            {
                // This would ideally set log level to Debug, but we'll just note it for now
                AnsiConsole.WriteLine("[dim]Verbose logging enabled[/]");
            }

            var options = new ScanOptions
            {
                MinimumSeverity = minSeverity,
                IncludeTransitiveDependencies = includeTransitive,
                ExcludePackages = excludePackages?.ToList() ?? new List<string>()
            };

            await ExecuteScanAsync(path, output, options, json);
        }, pathArgument, outputOption, severityOption, includeTransitiveOption, excludePackagesOption, jsonOption, verboseOption);

        return command;
    }

    private async Task ExecuteScanAsync(string path, string? outputPath, ScanOptions options, bool jsonOutput)
    {
        try
        {
            AnsiConsole.Write(new Rule("[bold green]NuVet - Vulnerability Scanner[/]"));
            AnsiConsole.WriteLine();

            if (!Path.Exists(path))
            {
                AnsiConsole.WriteLine($"[red]Error:[/] Path '{path}' does not exist.");
                Environment.Exit(1);
                return;
            }

            AnsiConsole.WriteLine($"[blue]Scanning:[/] {path}");
            AnsiConsole.WriteLine($"[blue]Min Severity:[/] {options.MinimumSeverity}");
            AnsiConsole.WriteLine($"[blue]Include Transitive:[/] {options.IncludeTransitiveDependencies}");
            
            if (options.ExcludePackages.Any())
            {
                AnsiConsole.WriteLine($"[blue]Excluded Packages:[/] {string.Join(", ", options.ExcludePackages)}");
            }
            
            AnsiConsole.WriteLine();

            ScanResult result;
            
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Scanning for vulnerabilities...[/]");
                    task.MaxValue = 100;
                    
                    // Start scan
                    result = await _scanner.ScanAsync(path, options);
                    task.Value = 100;
                });

            result = await _scanner.ScanAsync(path, options);

            if (jsonOutput)
            {
                await OutputJsonResultsAsync(result, outputPath);
            }
            else
            {
                await OutputFormattedResultsAsync(result, outputPath);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scan execution");
            AnsiConsole.WriteLine($"[red]Error:[/] {ex.Message}");
            Environment.Exit(1);
        }
    }

    private async Task OutputFormattedResultsAsync(ScanResult result, string? outputPath)
    {
        AnsiConsole.Write(new Rule("[bold yellow]Scan Results[/]"));
        AnsiConsole.WriteLine();

        // Summary
        var summaryTable = new Table();
        summaryTable.AddColumn("Metric");
        summaryTable.AddColumn("Count");
        
        summaryTable.AddRow("Projects Scanned", result.Summary.TotalProjects.ToString());
        summaryTable.AddRow("Packages Analyzed", result.Summary.TotalPackages.ToString());
        summaryTable.AddRow("Vulnerable Packages", result.Summary.VulnerablePackages.ToString());
        summaryTable.AddRow("Total Vulnerabilities", result.Summary.TotalVulnerabilities.ToString());

        AnsiConsole.Write(summaryTable);
        AnsiConsole.WriteLine();

        // Severity breakdown
        if (result.Summary.TotalVulnerabilities > 0)
        {
            var severityTable = new Table();
            severityTable.AddColumn("Severity");
            severityTable.AddColumn("Count");
            
            if (result.Summary.CriticalVulnerabilities > 0)
                severityTable.AddRow("[red]Critical[/]", result.Summary.CriticalVulnerabilities.ToString());
            if (result.Summary.HighVulnerabilities > 0)
                severityTable.AddRow("[orange3]High[/]", result.Summary.HighVulnerabilities.ToString());
            if (result.Summary.ModerateVulnerabilities > 0)
                severityTable.AddRow("[yellow]Moderate[/]", result.Summary.ModerateVulnerabilities.ToString());
            if (result.Summary.LowVulnerabilities > 0)
                severityTable.AddRow("[blue]Low[/]", result.Summary.LowVulnerabilities.ToString());

            AnsiConsole.Write(severityTable);
            AnsiConsole.WriteLine();
        }

        // Vulnerable packages
        if (result.VulnerablePackages.Any())
        {
            AnsiConsole.Write(new Rule("[bold red]Vulnerable Packages[/]"));
            AnsiConsole.WriteLine();

            foreach (var vulnerablePackage in result.VulnerablePackages.OrderByDescending(vp => vp.HighestSeverity))
            {
                var color = GetSeverityColor(vulnerablePackage.HighestSeverity);
                
                AnsiConsole.WriteLine($"[bold]{vulnerablePackage.Package.Id}[/] [dim]{vulnerablePackage.Package.Version}[/]");
                AnsiConsole.WriteLine($"  [blue]Highest Severity:[/] [{color}]{vulnerablePackage.HighestSeverity}[/]");
                AnsiConsole.WriteLine($"  [blue]Affected Projects:[/] {vulnerablePackage.AffectedProjects.Count}");
                
                var suggestedVersions = vulnerablePackage.GetSuggestedUpdateVersions();
                if (suggestedVersions.Any())
                {
                    AnsiConsole.WriteLine($"  [blue]Suggested Updates:[/] {string.Join(", ", suggestedVersions.Take(3))}");
                }

                foreach (var vulnerability in vulnerablePackage.Vulnerabilities.Take(3))
                {
                    AnsiConsole.WriteLine($"    • [{GetSeverityColor(vulnerability.Severity)}]{vulnerability.Severity}[/]: {vulnerability.Title}");
                }

                if (vulnerablePackage.Vulnerabilities.Count > 3)
                {
                    AnsiConsole.WriteLine($"    [dim]... and {vulnerablePackage.Vulnerabilities.Count - 3} more[/]");
                }

                AnsiConsole.WriteLine();
            }
        }
        else
        {
            AnsiConsole.WriteLine("[green]✓ No vulnerabilities found![/]");
        }

        // Save to file if requested
        if (!string.IsNullOrEmpty(outputPath))
        {
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(outputPath, jsonContent);
            AnsiConsole.WriteLine($"[blue]Results saved to:[/] {outputPath}");
        }

        // Exit code based on results
        if (result.HasCriticalVulnerabilities)
        {
            Environment.Exit(2); // Critical vulnerabilities found
        }
        else if (result.HasVulnerabilities)
        {
            Environment.Exit(1); // Vulnerabilities found
        }
        // Exit 0 for no vulnerabilities
    }

    private async Task OutputJsonResultsAsync(ScanResult result, string? outputPath)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true 
        });

        if (!string.IsNullOrEmpty(outputPath))
        {
            await File.WriteAllTextAsync(outputPath, json);
            AnsiConsole.WriteLine($"Results saved to: {outputPath}");
        }
        else
        {
            Console.WriteLine(json);
        }

        // Exit code based on results
        if (result.HasCriticalVulnerabilities)
        {
            Environment.Exit(2);
        }
        else if (result.HasVulnerabilities)
        {
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