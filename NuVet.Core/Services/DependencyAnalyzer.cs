using System.Xml.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;
using NuVet.Core.Models;
using NuVet.Core.Services;
using Semver;

namespace NuVet.Core.Services.Implementation;

/// <summary>
/// Implementation of IDependencyAnalyzer using MSBuild APIs
/// </summary>
public class DependencyAnalyzer : IDependencyAnalyzer
{
    private readonly ILogger<DependencyAnalyzer> _logger;
    private readonly INuGetService _nugetService;
    private static bool _msbuildRegistered = false;

    public DependencyAnalyzer(ILogger<DependencyAnalyzer> logger, INuGetService nugetService)
    {
        _logger = logger;
        _nugetService = nugetService;
        
        // Register MSBuild if not already registered
        if (!_msbuildRegistered)
        {
            try
            {
                MSBuildLocator.RegisterDefaults();
                _msbuildRegistered = true;
            }
            catch (InvalidOperationException)
            {
                // MSBuild already registered
                _msbuildRegistered = true;
            }
        }
    }

    public async Task<DependencyGraph> AnalyzeAsync(string solutionOrProjectPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing dependencies for {Path}", solutionOrProjectPath);
        
        var projects = new List<string>();
        
        if (Path.GetExtension(solutionOrProjectPath).Equals(".sln", StringComparison.OrdinalIgnoreCase))
        {
            // Analyze solution
            projects = await GetProjectsFromSolutionAsync(solutionOrProjectPath, cancellationToken);
        }
        else if (IsProjectFile(solutionOrProjectPath))
        {
            // Analyze single project
            projects.Add(solutionOrProjectPath);
        }
        else
        {
            // Analyze directory
            projects = await FindProjectsAsync(solutionOrProjectPath, cancellationToken);
        }

        return await AnalyzeProjectsAsync(projects, cancellationToken);
    }

    public async Task<DependencyGraph> AnalyzeProjectsAsync(IEnumerable<string> projectPaths, CancellationToken cancellationToken = default)
    {
        var projects = new List<ProjectInfo>();
        var allPackages = new List<PackageReference>();
        
        foreach (var projectPath in projectPaths)
        {
            try
            {
                _logger.LogDebug("Analyzing project {ProjectPath}", projectPath);
                
                var projectInfo = await AnalyzeProjectAsync(projectPath, cancellationToken);
                if (projectInfo != null)
                {
                    projects.Add(projectInfo);
                    allPackages.AddRange(projectInfo.PackageReferences);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing project {ProjectPath}", projectPath);
            }
        }

        // Build transitive dependencies
        await BuildTransitiveDependenciesAsync(allPackages, cancellationToken);

        var rootPath = projects.Any() ? 
            GetCommonPath(projects.Select(p => p.Path)) : 
            Environment.CurrentDirectory;

        return new DependencyGraph
        {
            RootPath = rootPath,
            Projects = projects,
            AllPackages = allPackages,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<List<string>> FindProjectsAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        var projects = new List<string>();
        
        try
        {
            var projectExtensions = new[] { "*.csproj", "*.vbproj", "*.fsproj" };
            
            foreach (var extension in projectExtensions)
            {
                var foundProjects = Directory.GetFiles(rootPath, extension, SearchOption.AllDirectories);
                projects.AddRange(foundProjects);
            }
            
            _logger.LogDebug("Found {Count} projects in {RootPath}", projects.Count, rootPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding projects in {RootPath}", rootPath);
        }
        
        return projects;
    }

    public async Task<List<PackageReference>> GetPackageReferencesAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var projectInfo = await AnalyzeProjectAsync(projectPath, cancellationToken);
            return projectInfo?.PackageReferences ?? new List<PackageReference>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting package references for {ProjectPath}", projectPath);
            return new List<PackageReference>();
        }
    }

    private async Task<ProjectInfo?> AnalyzeProjectAsync(string projectPath, CancellationToken cancellationToken)
    {
        try
        {
            using var projectCollection = new ProjectCollection();
            var project = projectCollection.LoadProject(projectPath);
            
            var projectInfo = new ProjectInfo
            {
                Name = Path.GetFileNameWithoutExtension(projectPath),
                Path = projectPath,
                TargetFramework = project.GetPropertyValue("TargetFramework") ?? "net8.0",
                Type = DetermineProjectType(project),
                OutputType = project.GetPropertyValue("OutputType"),
                TargetFrameworks = GetTargetFrameworks(project),
                PackageReferences = new List<PackageReference>()
            };

            // Get PackageReference items
            var packageReferences = project.GetItems("PackageReference");
            foreach (var packageRef in packageReferences)
            {
                var packageId = packageRef.EvaluatedInclude;
                var version = packageRef.GetMetadataValue("Version");
                
                if (!string.IsNullOrEmpty(packageId) && !string.IsNullOrEmpty(version))
                {
                                            if (SemVersion.TryParse(version, SemVersionStyles.Strict, out var semVersion))
                    {
                        var packageReference = new PackageReference
                        {
                            Id = packageId,
                            Version = semVersion,
                            ProjectPath = projectPath,
                            TargetFramework = projectInfo.TargetFramework,
                            IsDirectDependency = true,
                            Source = PackageReferenceSource.PackageReference
                        };
                        
                        projectInfo.PackageReferences.Add(packageReference);
                    }
                }
            }

            // Also check for packages.config (legacy format)
            var packagesConfigPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "packages.config");
            if (File.Exists(packagesConfigPath))
            {
                var packagesConfigRefs = await ParsePackagesConfigAsync(packagesConfigPath, projectPath);
                projectInfo.PackageReferences.AddRange(packagesConfigRefs);
            }

            _logger.LogDebug("Project {ProjectName} has {PackageCount} package references", 
                projectInfo.Name, projectInfo.PackageReferences.Count);

            return projectInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing project {ProjectPath}", projectPath);
            return null;
        }
    }

    private async Task<List<PackageReference>> ParsePackagesConfigAsync(string packagesConfigPath, string projectPath)
    {
        var packageReferences = new List<PackageReference>();
        
        try
        {
            var doc = XDocument.Load(packagesConfigPath);
            var packages = doc.Root?.Elements("package");
            
            if (packages != null)
            {
                foreach (var package in packages)
                {
                    var id = package.Attribute("id")?.Value;
                    var version = package.Attribute("version")?.Value;
                    var targetFramework = package.Attribute("targetFramework")?.Value;
                    
                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(version))
                    {
                        if (SemVersion.TryParse(version, SemVersionStyles.Strict, out var semVersion))
                        {
                            packageReferences.Add(new PackageReference
                            {
                                Id = id,
                                Version = semVersion,
                                ProjectPath = projectPath,
                                TargetFramework = targetFramework,
                                IsDirectDependency = true,
                                Source = PackageReferenceSource.PackagesConfig
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing packages.config at {Path}", packagesConfigPath);
        }
        
        return packageReferences;
    }

    private async Task BuildTransitiveDependenciesAsync(List<PackageReference> packages, CancellationToken cancellationToken)
    {
        var packageMap = packages.GroupBy(p => $"{p.Id}_{p.Version}").ToDictionary(g => g.Key, g => g.First());
        
        foreach (var package in packages.ToList())
        {
            try
            {
                var dependencies = await _nugetService.GetPackageDependenciesAsync(
                    package.Id, 
                    package.Version.ToString(), 
                    package.TargetFramework,
                    cancellationToken);

                foreach (var dependency in dependencies)
                {
                    // For simplicity, we'll add transitive dependencies as separate PackageReference objects
                    // In a more sophisticated implementation, you'd build a proper dependency tree
                    if (SemVersion.TryParse(dependency.VersionRange, SemVersionStyles.Strict, out var depVersion))
                    {
                        var key = $"{dependency.Id}_{depVersion}";
                        if (!packageMap.ContainsKey(key))
                        {
                            var transitiveDep = new PackageReference
                            {
                                Id = dependency.Id,
                                Version = depVersion,
                                ProjectPath = package.ProjectPath,
                                TargetFramework = dependency.TargetFramework ?? package.TargetFramework,
                                IsDirectDependency = false,
                                Source = package.Source
                            };
                            
                            package.Dependencies.Add(transitiveDep);
                            packages.Add(transitiveDep);
                            packageMap[key] = transitiveDep;
                        }
                        else
                        {
                            package.Dependencies.Add(packageMap[key]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not resolve dependencies for {PackageId} {Version}", 
                    package.Id, package.Version);
            }
        }
    }

    private async Task<List<string>> GetProjectsFromSolutionAsync(string solutionPath, CancellationToken cancellationToken)
    {
        var projects = new List<string>();
        
        try
        {
            var solution = SolutionFile.Parse(solutionPath);
            var solutionDir = Path.GetDirectoryName(solutionPath);
            
            foreach (var project in solution.ProjectsInOrder)
            {
                if (project.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
                {
                    var projectPath = Path.Combine(solutionDir!, project.RelativePath);
                    if (File.Exists(projectPath))
                    {
                        projects.Add(projectPath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing solution {SolutionPath}", solutionPath);
        }
        
        return projects;
    }

    private static ProjectType DetermineProjectType(Project project)
    {
        var outputType = project.GetPropertyValue("OutputType")?.ToLowerInvariant();
        var sdk = project.GetPropertyValue("Sdk")?.ToLowerInvariant();
        
        return outputType switch
        {
            "exe" => ProjectType.ConsoleApplication,
            "winexe" => ProjectType.WindowsApplication,
            "library" => ProjectType.ClassLibrary,
            _ => sdk switch
            {
                var s when s?.Contains("web") == true => ProjectType.WebApplication,
                var s when s?.Contains("test") == true => ProjectType.TestProject,
                _ => ProjectType.Other
            }
        };
    }

    private static List<string> GetTargetFrameworks(Project project)
    {
        var targetFrameworks = project.GetPropertyValue("TargetFrameworks");
        if (!string.IsNullOrEmpty(targetFrameworks))
        {
            return targetFrameworks.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        
        var targetFramework = project.GetPropertyValue("TargetFramework");
        return !string.IsNullOrEmpty(targetFramework) ? new List<string> { targetFramework } : new List<string>();
    }

    private static bool IsProjectFile(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension is ".csproj" or ".vbproj" or ".fsproj";
    }

    private static string GetCommonPath(IEnumerable<string> paths)
    {
        var pathList = paths.ToList();
        if (!pathList.Any()) return Environment.CurrentDirectory;
        if (pathList.Count == 1) return Path.GetDirectoryName(pathList[0]) ?? Environment.CurrentDirectory;
        
        var commonPath = pathList[0];
        foreach (var path in pathList.Skip(1))
        {
            commonPath = GetCommonPath(commonPath, path);
        }
        
        return Path.GetDirectoryName(commonPath) ?? Environment.CurrentDirectory;
    }

    private static string GetCommonPath(string path1, string path2)
    {
        var parts1 = path1.Split(Path.DirectorySeparatorChar);
        var parts2 = path2.Split(Path.DirectorySeparatorChar);
        
        var commonParts = new List<string>();
        var minLength = Math.Min(parts1.Length, parts2.Length);
        
        for (int i = 0; i < minLength; i++)
        {
            if (parts1[i].Equals(parts2[i], StringComparison.OrdinalIgnoreCase))
            {
                commonParts.Add(parts1[i]);
            }
            else
            {
                break;
            }
        }
        
        return string.Join(Path.DirectorySeparatorChar, commonParts);
    }
}