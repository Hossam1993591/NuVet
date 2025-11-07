using NuVet.Core.Models;

namespace NuVet.Core.Services;

/// <summary>
/// Service for interacting with vulnerability databases and NuGet security advisories
/// </summary>
public interface IVulnerabilityService
{
    /// <summary>
    /// Gets vulnerability information for a specific package
    /// </summary>
    Task<List<Vulnerability>> GetVulnerabilitiesAsync(string packageId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets vulnerability information for multiple packages
    /// </summary>
    Task<Dictionary<string, List<Vulnerability>>> GetVulnerabilitiesAsync(
        IEnumerable<string> packageIds, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a specific package version has known vulnerabilities
    /// </summary>
    Task<List<Vulnerability>> CheckPackageVersionAsync(
        string packageId, 
        string version, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the latest vulnerability database update timestamp
    /// </summary>
    Task<DateTime?> GetLastUpdateAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the local vulnerability database cache
    /// </summary>
    Task RefreshVulnerabilityDatabaseAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for interacting with NuGet package repositories
/// </summary>
public interface INuGetService
{
    /// <summary>
    /// Gets package metadata from NuGet repositories
    /// </summary>
    Task<PackageMetadata?> GetPackageMetadataAsync(
        string packageId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all available versions for a package
    /// </summary>
    Task<List<string>> GetPackageVersionsAsync(
        string packageId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches for packages by name or description
    /// </summary>
    Task<List<PackageSearchResult>> SearchPackagesAsync(
        string searchQuery, 
        int skip = 0, 
        int take = 20, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets package dependencies for a specific version
    /// </summary>
    Task<List<PackageDependency>> GetPackageDependenciesAsync(
        string packageId, 
        string version, 
        string? targetFramework = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a package version exists in the repository
    /// </summary>
    Task<bool> PackageVersionExistsAsync(
        string packageId, 
        string version, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for analyzing project dependencies
/// </summary>
public interface IDependencyAnalyzer
{
    /// <summary>
    /// Analyzes a solution or project and builds a dependency graph
    /// </summary>
    Task<DependencyGraph> AnalyzeAsync(
        string solutionOrProjectPath, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Analyzes multiple projects and builds a combined dependency graph
    /// </summary>
    Task<DependencyGraph> AnalyzeProjectsAsync(
        IEnumerable<string> projectPaths, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds all .NET projects in a directory tree
    /// </summary>
    Task<List<string>> FindProjectsAsync(
        string rootPath, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets package references from a specific project file
    /// </summary>
    Task<List<PackageReference>> GetPackageReferencesAsync(
        string projectPath, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for scanning projects for vulnerabilities
/// </summary>
public interface IVulnerabilityScanner
{
    /// <summary>
    /// Scans a solution or project for vulnerable packages
    /// </summary>
    Task<ScanResult> ScanAsync(
        string solutionOrProjectPath, 
        ScanOptions? options = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Scans a dependency graph for vulnerabilities
    /// </summary>
    Task<ScanResult> ScanDependencyGraphAsync(
        DependencyGraph dependencyGraph, 
        ScanOptions? options = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Scans specific packages for vulnerabilities
    /// </summary>
    Task<List<VulnerablePackage>> ScanPackagesAsync(
        IEnumerable<PackageReference> packages, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for updating packages
/// </summary>
public interface IPackageUpdater
{
    /// <summary>
    /// Updates vulnerable packages to safe versions
    /// </summary>
    Task<List<UpdateResult>> UpdateVulnerablePackagesAsync(
        ScanResult scanResult, 
        UpdateOptions? options = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates a specific package in specified projects
    /// </summary>
    Task<UpdateResult> UpdatePackageAsync(
        PackageUpdate update, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a backup of project files before updates
    /// </summary>
    Task<UpdateBackup> CreateBackupAsync(
        IEnumerable<string> projectPaths, 
        string description,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Restores project files from a backup
    /// </summary>
    Task<bool> RestoreBackupAsync(
        UpdateBackup backup, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates that projects can still build after updates
    /// </summary>
    Task<ValidationResult> ValidateUpdatesAsync(
        IEnumerable<string> projectPaths, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration options for vulnerability scanning
/// </summary>
public class ScanOptions
{
    public bool IncludeTransitiveDependencies { get; set; } = true;
    public VulnerabilitySeverity MinimumSeverity { get; set; } = VulnerabilitySeverity.Low;
    public bool UseLocalCache { get; set; } = true;
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromHours(24);
    public List<string> ExcludePackages { get; set; } = new();
    public List<string> IncludeOnlyProjects { get; set; } = new();
}

/// <summary>
/// Configuration options for package updates
/// </summary>
public class UpdateOptions
{
    public bool AutoApproveMinorUpdates { get; set; } = true;
    public bool AutoApprovePatchUpdates { get; set; } = true;
    public bool CreateBackup { get; set; } = true;
    public bool ValidateAfterUpdate { get; set; } = true;
    public bool RollbackOnFailure { get; set; } = true;
    public VulnerabilitySeverity MinimumSeverityToUpdate { get; set; } = VulnerabilitySeverity.Low;
    public List<string> ExcludePackages { get; set; } = new();
}

/// <summary>
/// Result of project validation after updates
/// </summary>
public class ValidationResult
{
    public required bool IsValid { get; init; }
    public required List<string> Errors { get; init; }
    public required List<string> Warnings { get; init; }
    public required TimeSpan ValidationDuration { get; init; }
}