namespace NuVet.Core.Models;

/// <summary>
/// Represents the dependency graph of a .NET solution or project
/// </summary>
public class DependencyGraph
{
    public required string RootPath { get; init; }
    public required List<ProjectInfo> Projects { get; init; }
    public required List<PackageReference> AllPackages { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets all unique packages across all projects
    /// </summary>
    public IEnumerable<PackageReference> GetUniquePackages()
    {
        return AllPackages.DistinctBy(p => new { p.Id, p.Version });
    }
    
    /// <summary>
    /// Gets all direct dependencies for a specific project
    /// </summary>
    public IEnumerable<PackageReference> GetDirectDependencies(string projectPath)
    {
        return AllPackages.Where(p => p.ProjectPath == projectPath && p.IsDirectDependency);
    }
    
    /// <summary>
    /// Gets all transitive dependencies for a specific package
    /// </summary>
    public IEnumerable<PackageReference> GetTransitiveDependencies(PackageReference package)
    {
        var visited = new HashSet<string>();
        var result = new List<PackageReference>();
        
        CollectTransitiveDependencies(package, visited, result);
        
        return result;
    }
    
    private void CollectTransitiveDependencies(PackageReference package, HashSet<string> visited, List<PackageReference> result)
    {
        var key = $"{package.Id}_{package.Version}";
        if (visited.Contains(key)) return;
        
        visited.Add(key);
        
        foreach (var dependency in package.Dependencies)
        {
            result.Add(dependency);
            CollectTransitiveDependencies(dependency, visited, result);
        }
    }
    
    /// <summary>
    /// Finds packages that depend on the specified package
    /// </summary>
    public IEnumerable<PackageReference> FindDependents(string packageId)
    {
        return AllPackages.Where(p => p.Dependencies.Any(d => 
            d.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase)));
    }
}

/// <summary>
/// Information about a .NET project
/// </summary>
public class ProjectInfo
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required string TargetFramework { get; init; }
    public required ProjectType Type { get; init; }
    public string? OutputType { get; init; }
    public List<string> TargetFrameworks { get; init; } = new();
    public List<PackageReference> PackageReferences { get; init; } = new();
}

public enum ProjectType
{
    ClassLibrary,
    ConsoleApplication,
    WebApplication,
    TestProject,
    WindowsApplication,
    Other
}