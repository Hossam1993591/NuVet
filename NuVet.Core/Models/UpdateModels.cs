using Semver;

namespace NuVet.Core.Models;

/// <summary>
/// Represents an update operation for a package
/// </summary>
public class PackageUpdate
{
    public required PackageReference CurrentPackage { get; init; }
    public required SemVersion TargetVersion { get; init; }
    public required List<string> AffectedProjects { get; init; }
    public required List<Vulnerability> VulnerabilitiesFixed { get; init; }
    public UpdateType UpdateType { get; init; }
    public string? UpdateReason { get; init; }
    public List<BreakingChange> PotentialBreakingChanges { get; init; } = new();
    public bool RequiresManualReview { get; init; }
    
    /// <summary>
    /// Determines if this is a major version update
    /// </summary>
    public bool IsMajorVersionUpdate => CurrentPackage.Version.Major != TargetVersion.Major;
    
    /// <summary>
    /// Determines if this is a minor version update
    /// </summary>
    public bool IsMinorVersionUpdate => CurrentPackage.Version.Minor != TargetVersion.Minor && !IsMajorVersionUpdate;
    
    /// <summary>
    /// Determines if this is a patch version update
    /// </summary>
    public bool IsPatchVersionUpdate => CurrentPackage.Version.Patch != TargetVersion.Patch && 
                                       !IsMajorVersionUpdate && !IsMinorVersionUpdate;
}

/// <summary>
/// Represents the result of a package update operation
/// </summary>
public class UpdateResult
{
    public required PackageUpdate Update { get; init; }
    public required UpdateStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public List<string> Warnings { get; init; } = new();
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    public TimeSpan Duration { get; init; }
    public UpdateBackup? Backup { get; init; }
    
    public bool IsSuccessful => Status == UpdateStatus.Success;
    public bool RequiresRollback => Status == UpdateStatus.Failed && Backup != null;
}

/// <summary>
/// Represents a backup of project state before an update
/// </summary>
public class UpdateBackup
{
    public required string BackupId { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required List<BackupFile> Files { get; init; }
    public required string Description { get; init; }
    
    public static UpdateBackup Create(string description, IEnumerable<string> filePaths)
    {
        var backupId = Guid.NewGuid().ToString("N")[..8];
        var files = filePaths.Select(path => new BackupFile 
        { 
            OriginalPath = path, 
            Content = File.ReadAllText(path) 
        }).ToList();
        
        return new UpdateBackup
        {
            BackupId = backupId,
            CreatedAt = DateTime.UtcNow,
            Files = files,
            Description = description
        };
    }
}

/// <summary>
/// Represents a backed up file
/// </summary>
public class BackupFile
{
    public required string OriginalPath { get; init; }
    public required string Content { get; init; }
    public DateTime BackedUpAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a potential breaking change in an update
/// </summary>
public class BreakingChange
{
    public required string Type { get; init; }
    public required string Description { get; init; }
    public required BreakingChangeSeverity Severity { get; init; }
    public string? Mitigation { get; init; }
    public string? AffectedApi { get; init; }
}

public enum UpdateType
{
    Security,
    BugFix,
    Feature,
    Major,
    Manual
}

public enum UpdateStatus
{
    Success,
    Failed,
    Skipped,
    RequiresManualIntervention
}

public enum BreakingChangeSeverity
{
    Low,
    Medium,
    High,
    Critical
}