using Semver;

namespace NuVet.Core.Models;

/// <summary>
/// Metadata information about a NuGet package
/// </summary>
public class PackageMetadata
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required List<string> Authors { get; init; }
    public required List<string> Tags { get; init; }
    public required Uri ProjectUrl { get; init; }
    public required Uri LicenseUrl { get; init; }
    public DateTime Published { get; init; }
    public long DownloadCount { get; init; }
    public bool IsPrerelease { get; init; }
    public SemVersion? LatestVersion { get; init; }
    public SemVersion? LatestStableVersion { get; init; }
    public List<PackageVersion> Versions { get; init; } = new();
}

/// <summary>
/// Information about a specific version of a package
/// </summary>
public class PackageVersion
{
    public required SemVersion Version { get; init; }
    public required DateTime Published { get; init; }
    public required long DownloadCount { get; init; }
    public required bool IsPrerelease { get; init; }
    public List<PackageDependency> Dependencies { get; init; } = new();
    public string? ReleaseNotes { get; init; }
}

/// <summary>
/// Represents a dependency of a NuGet package
/// </summary>
public class PackageDependency
{
    public required string Id { get; init; }
    public required string VersionRange { get; init; }
    public string? TargetFramework { get; init; }
    
    /// <summary>
    /// Parses the version range to determine if a specific version satisfies the dependency
    /// </summary>
    public bool IsSatisfiedBy(SemVersion version)
    {
        // Simple implementation - in practice, you'd want proper NuGet version range parsing
        return true; // TODO: Implement proper version range matching
    }
}

/// <summary>
/// Result of a package search operation
/// </summary>
public class PackageSearchResult
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required List<string> Authors { get; init; }
    public required List<string> Tags { get; init; }
    public required SemVersion LatestVersion { get; init; }
    public required long TotalDownloads { get; init; }
    public required bool IsVerified { get; init; }
}