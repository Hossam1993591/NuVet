using Semver;

namespace NuVet.Core.Models;

/// <summary>
/// Represents a NuGet package reference found in a project
/// </summary>
public class PackageReference
{
    public required string Id { get; init; }
    public required SemVersion Version { get; init; }
    public required string ProjectPath { get; init; }
    public string? TargetFramework { get; init; }
    public bool IsDirectDependency { get; init; } = true;
    public List<PackageReference> Dependencies { get; init; } = new();
    
    /// <summary>
    /// The source where this package reference was found (e.g., packages.config, PackageReference, etc.)
    /// </summary>
    public PackageReferenceSource Source { get; init; } = PackageReferenceSource.PackageReference;
    
    public override string ToString()
    {
        return $"{Id} {Version}";
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not PackageReference other) return false;
        return Id.Equals(other.Id, StringComparison.OrdinalIgnoreCase) && 
               Version.Equals(other.Version);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Id.ToLowerInvariant(), Version);
    }
}

public enum PackageReferenceSource
{
    PackageReference,
    PackagesConfig,
    ProjectJson,
    CentralPackageManagement
}