using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using NuVet.Core.Models;
using NuVet.Core.Services;
using Semver;

namespace NuVet.Core.Services.Implementation;

/// <summary>
/// Implementation of IPackageUpdater that safely updates packages with rollback capabilities
/// </summary>
public class PackageUpdater : IPackageUpdater
{
    private readonly INuGetService _nugetService;
    private readonly ILogger<PackageUpdater> _logger;
    private readonly string _backupDirectory;

    public PackageUpdater(INuGetService nugetService, ILogger<PackageUpdater> logger)
    {
        _nugetService = nugetService;
        _logger = logger;
        _backupDirectory = Path.Combine(Path.GetTempPath(), "NuVet", "Backups");
        Directory.CreateDirectory(_backupDirectory);
    }

    public async Task<List<UpdateResult>> UpdateVulnerablePackagesAsync(
        ScanResult scanResult,
        UpdateOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new UpdateOptions();
        var results = new List<UpdateResult>();

        _logger.LogInformation("Starting update of {Count} vulnerable packages", scanResult.VulnerablePackages.Count);

        // Create backup if requested
        UpdateBackup? globalBackup = null;
        if (options.CreateBackup)
        {
            var allProjectPaths = scanResult.VulnerablePackages
                .SelectMany(vp => vp.AffectedProjects)
                .Distinct()
                .ToList();
            
            globalBackup = await CreateBackupAsync(allProjectPaths, "Pre-vulnerability-update backup", cancellationToken);
            _logger.LogInformation("Created backup {BackupId} for {ProjectCount} projects", 
                globalBackup.BackupId, allProjectPaths.Count);
        }

        // Plan updates
        var updatePlans = await PlanUpdatesAsync(scanResult, options, cancellationToken);
        
        // Execute updates
        foreach (var update in updatePlans)
        {
            try
            {
                if (ShouldSkipUpdate(update, options))
                {
                    results.Add(new UpdateResult
                    {
                        Update = update,
                        Status = UpdateStatus.Skipped,
                        ErrorMessage = "Update skipped based on options"
                    });
                    continue;
                }

                var result = await UpdatePackageAsync(update, cancellationToken);
                results.Add(result);

                // If validation is enabled and update failed, attempt rollback
                if (options.ValidateAfterUpdate && result.Status == UpdateStatus.Failed && 
                    options.RollbackOnFailure && globalBackup != null)
                {
                    _logger.LogWarning("Update failed, attempting rollback for {PackageId}", update.CurrentPackage.Id);
                    await RestoreBackupAsync(globalBackup, cancellationToken);
                    
                    result = new UpdateResult
                    {
                        Update = result.Update,
                        Status = UpdateStatus.Failed,
                        ErrorMessage = $"{result.ErrorMessage} (Rolled back to backup {globalBackup.BackupId})",
                        Warnings = result.Warnings,
                        UpdatedAt = result.UpdatedAt,
                        Duration = result.Duration,
                        Backup = result.Backup
                    };
                }

                if (result.Status == UpdateStatus.Success)
                {
                    _logger.LogInformation("Successfully updated {PackageId} from {OldVersion} to {NewVersion}",
                        update.CurrentPackage.Id, update.CurrentPackage.Version, update.TargetVersion);
                }
                else
                {
                    _logger.LogError("Failed to update {PackageId}: {Error}",
                        update.CurrentPackage.Id, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating {PackageId}", update.CurrentPackage.Id);
                results.Add(new UpdateResult
                {
                    Update = update,
                    Status = UpdateStatus.Failed,
                    ErrorMessage = ex.Message
                });
            }
        }

        var successCount = results.Count(r => r.Status == UpdateStatus.Success);
        _logger.LogInformation("Update completed. {SuccessCount}/{TotalCount} packages updated successfully",
            successCount, results.Count);

        return results;
    }

    public async Task<UpdateResult> UpdatePackageAsync(PackageUpdate update, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        
        _logger.LogDebug("Updating {PackageId} from {OldVersion} to {NewVersion} in {ProjectCount} projects",
            update.CurrentPackage.Id, update.CurrentPackage.Version, update.TargetVersion, update.AffectedProjects.Count);

        try
        {
            // Verify target version exists
            var versionExists = await _nugetService.PackageVersionExistsAsync(
                update.CurrentPackage.Id, 
                update.TargetVersion.ToString(), 
                cancellationToken);

            if (!versionExists)
            {
                return new UpdateResult
                {
                    Update = update,
                    Status = UpdateStatus.Failed,
                    ErrorMessage = $"Target version {update.TargetVersion} does not exist for package {update.CurrentPackage.Id}",
                    Duration = DateTime.UtcNow - startTime
                };
            }

            // Create project-specific backup
            var backup = await CreateBackupAsync(update.AffectedProjects, 
                $"Backup before updating {update.CurrentPackage.Id}", cancellationToken);

            // Update each affected project
            var updateSuccessful = true;
            var errors = new List<string>();

            foreach (var projectPath in update.AffectedProjects)
            {
                try
                {
                    await UpdatePackageInProjectAsync(projectPath, update.CurrentPackage.Id, update.TargetVersion, cancellationToken);
                }
                catch (Exception ex)
                {
                    updateSuccessful = false;
                    errors.Add($"Failed to update {projectPath}: {ex.Message}");
                    _logger.LogError(ex, "Failed to update package in project {ProjectPath}", projectPath);
                }
            }

            if (updateSuccessful)
            {
                return new UpdateResult
                {
                    Update = update,
                    Status = UpdateStatus.Success,
                    Duration = DateTime.UtcNow - startTime,
                    Backup = backup
                };
            }
            else
            {
                // Restore backup on failure
                await RestoreBackupAsync(backup, cancellationToken);
                
                return new UpdateResult
                {
                    Update = update,
                    Status = UpdateStatus.Failed,
                    ErrorMessage = string.Join("; ", errors),
                    Duration = DateTime.UtcNow - startTime,
                    Backup = backup
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating package {PackageId}", update.CurrentPackage.Id);
            return new UpdateResult
            {
                Update = update,
                Status = UpdateStatus.Failed,
                ErrorMessage = ex.Message,
                Duration = DateTime.UtcNow - startTime
            };
        }
    }

    public async Task<UpdateBackup> CreateBackupAsync(
        IEnumerable<string> projectPaths,
        string description,
        CancellationToken cancellationToken = default)
    {
        var files = new List<BackupFile>();
        
        foreach (var projectPath in projectPaths)
        {
            if (File.Exists(projectPath))
            {
                var content = await File.ReadAllTextAsync(projectPath, cancellationToken);
                files.Add(new BackupFile
                {
                    OriginalPath = projectPath,
                    Content = content
                });
            }

            // Also backup packages.config if it exists
            var packagesConfigPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "packages.config");
            if (File.Exists(packagesConfigPath))
            {
                var content = await File.ReadAllTextAsync(packagesConfigPath, cancellationToken);
                files.Add(new BackupFile
                {
                    OriginalPath = packagesConfigPath,
                    Content = content
                });
            }
        }

        var backup = new UpdateBackup
        {
            BackupId = Guid.NewGuid().ToString("N")[..8],
            CreatedAt = DateTime.UtcNow,
            Files = files,
            Description = description
        };

        // Save backup to disk for persistence
        var backupFilePath = Path.Combine(_backupDirectory, $"{backup.BackupId}.json");
        var json = System.Text.Json.JsonSerializer.Serialize(backup, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        await File.WriteAllTextAsync(backupFilePath, json, cancellationToken);

        _logger.LogDebug("Created backup {BackupId} with {FileCount} files", backup.BackupId, files.Count);
        
        return backup;
    }

    public async Task<bool> RestoreBackupAsync(UpdateBackup backup, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Restoring backup {BackupId}", backup.BackupId);
            
            foreach (var file in backup.Files)
            {
                await File.WriteAllTextAsync(file.OriginalPath, file.Content, cancellationToken);
                _logger.LogDebug("Restored file {FilePath}", file.OriginalPath);
            }
            
            _logger.LogInformation("Successfully restored backup {BackupId}", backup.BackupId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring backup {BackupId}", backup.BackupId);
            return false;
        }
    }

    public async Task<ValidationResult> ValidateUpdatesAsync(
        IEnumerable<string> projectPaths,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var errors = new List<string>();
        var warnings = new List<string>();

        _logger.LogDebug("Validating updates for {ProjectCount} projects", projectPaths.Count());

        foreach (var projectPath in projectPaths)
        {
            try
            {
                // Try to restore packages
                var restoreResult = await RunDotNetCommandAsync("restore", projectPath, cancellationToken);
                if (restoreResult.ExitCode != 0)
                {
                    errors.Add($"Restore failed for {projectPath}: {restoreResult.Error}");
                    continue;
                }

                // Try to build
                var buildResult = await RunDotNetCommandAsync("build --no-restore", projectPath, cancellationToken);
                if (buildResult.ExitCode != 0)
                {
                    errors.Add($"Build failed for {projectPath}: {buildResult.Error}");
                }
                else if (!string.IsNullOrEmpty(buildResult.Output) && 
                         buildResult.Output.Contains("warning", StringComparison.OrdinalIgnoreCase))
                {
                    warnings.Add($"Build warnings for {projectPath}: {buildResult.Output}");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Validation error for {projectPath}: {ex.Message}");
            }
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors,
            Warnings = warnings,
            ValidationDuration = DateTime.UtcNow - startTime
        };
    }

    private async Task<List<PackageUpdate>> PlanUpdatesAsync(
        ScanResult scanResult,
        UpdateOptions options,
        CancellationToken cancellationToken)
    {
        var updates = new List<PackageUpdate>();

        foreach (var vulnerablePackage in scanResult.VulnerablePackages)
        {
            // Skip if package is excluded
            if (options.ExcludePackages.Any(exclude => 
                vulnerablePackage.Package.Id.Contains(exclude, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            // Skip if vulnerability severity is below threshold
            if (vulnerablePackage.HighestSeverity < options.MinimumSeverityToUpdate)
            {
                continue;
            }

            // Find the best target version
            var suggestedVersions = vulnerablePackage.GetSuggestedUpdateVersions();
            var targetVersion = suggestedVersions.FirstOrDefault();

            if (targetVersion == null)
            {
                _logger.LogWarning("No suitable update version found for {PackageId} {Version}",
                    vulnerablePackage.Package.Id, vulnerablePackage.Package.Version);
                continue;
            }

            var update = new PackageUpdate
            {
                CurrentPackage = vulnerablePackage.Package,
                TargetVersion = targetVersion,
                AffectedProjects = vulnerablePackage.AffectedProjects,
                VulnerabilitiesFixed = vulnerablePackage.Vulnerabilities,
                UpdateType = DetermineUpdateType(vulnerablePackage.Package.Version, targetVersion),
                UpdateReason = $"Fix {vulnerablePackage.Vulnerabilities.Count} vulnerabilities",
                RequiresManualReview = IsBreakingUpdate(vulnerablePackage.Package.Version, targetVersion)
            };

            updates.Add(update);
        }

        return updates;
    }

    private async Task UpdatePackageInProjectAsync(
        string projectPath,
        string packageId,
        SemVersion targetVersion,
        CancellationToken cancellationToken)
    {
        var projectDir = Path.GetDirectoryName(projectPath)!;
        var packagesConfigPath = Path.Combine(projectDir, "packages.config");

        if (File.Exists(packagesConfigPath))
        {
            // Update packages.config format
            await UpdatePackagesConfigAsync(packagesConfigPath, packageId, targetVersion, cancellationToken);
        }
        else
        {
            // Update PackageReference format
            await UpdatePackageReferenceAsync(projectPath, packageId, targetVersion, cancellationToken);
        }
    }

    private async Task UpdatePackageReferenceAsync(
        string projectPath,
        string packageId,
        SemVersion targetVersion,
        CancellationToken cancellationToken)
    {
        var doc = XDocument.Load(projectPath);
        var packageReferences = doc.Descendants("PackageReference")
            .Where(pr => pr.Attribute("Include")?.Value.Equals(packageId, StringComparison.OrdinalIgnoreCase) == true);

        foreach (var packageRef in packageReferences)
        {
            var versionAttr = packageRef.Attribute("Version");
            if (versionAttr != null)
            {
                versionAttr.Value = targetVersion.ToString();
            }
        }

        doc.Save(projectPath);
    }

    private async Task UpdatePackagesConfigAsync(
        string packagesConfigPath,
        string packageId,
        SemVersion targetVersion,
        CancellationToken cancellationToken)
    {
        var doc = XDocument.Load(packagesConfigPath);
        var packages = doc.Descendants("package")
            .Where(p => p.Attribute("id")?.Value.Equals(packageId, StringComparison.OrdinalIgnoreCase) == true);

        foreach (var package in packages)
        {
            var versionAttr = package.Attribute("version");
            if (versionAttr != null)
            {
                versionAttr.Value = targetVersion.ToString();
            }
        }

        doc.Save(packagesConfigPath);
    }

    private async Task<(int ExitCode, string Output, string Error)> RunDotNetCommandAsync(
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = Path.GetDirectoryName(workingDirectory),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(cancellationToken);

        var output = await outputTask;
        var error = await errorTask;

        return (process.ExitCode, output, error);
    }

    private static bool ShouldSkipUpdate(PackageUpdate update, UpdateOptions options)
    {
        // Skip major version updates unless explicitly allowed
        if (update.IsMajorVersionUpdate && !options.AutoApproveMinorUpdates)
        {
            return true;
        }

        // Skip minor version updates unless explicitly allowed
        if (update.IsMinorVersionUpdate && !options.AutoApproveMinorUpdates)
        {
            return true;
        }

        // Skip patch updates unless explicitly allowed
        if (update.IsPatchVersionUpdate && !options.AutoApprovePatchUpdates)
        {
            return true;
        }

        return false;
    }

    private static UpdateType DetermineUpdateType(SemVersion currentVersion, SemVersion targetVersion)
    {
        if (currentVersion.Major != targetVersion.Major)
            return UpdateType.Major;
        if (currentVersion.Minor != targetVersion.Minor)
            return UpdateType.Feature;
        if (currentVersion.Patch != targetVersion.Patch)
            return UpdateType.BugFix;
        
        return UpdateType.Security;
    }

    private static bool IsBreakingUpdate(SemVersion currentVersion, SemVersion targetVersion)
    {
        // Consider major version changes as potentially breaking
        return currentVersion.Major != targetVersion.Major;
    }
}