using NuVet.Core.Models;
using Semver;
using FluentAssertions;

namespace NuVet.Tests.Models;

public class DependencyGraphTests
{
    [Fact]
    public void DependencyGraph_GetUniquePackages_ReturnsDistinctPackages()
    {
        // Arrange
        var package1v1 = CreatePackageReference("TestPackage", "1.0.0", "/project1.csproj");
        var package1v2 = CreatePackageReference("TestPackage", "1.1.0", "/project2.csproj");
        var package2v1 = CreatePackageReference("OtherPackage", "2.0.0", "/project1.csproj");
        var package1v1Duplicate = CreatePackageReference("TestPackage", "1.0.0", "/project3.csproj");

        var dependencyGraph = new DependencyGraph
        {
            RootPath = "/solution",
            Projects = new List<ProjectInfo>(),
            AllPackages = new List<PackageReference> { package1v1, package1v2, package2v1, package1v1Duplicate }
        };

        // Act
        var uniquePackages = dependencyGraph.GetUniquePackages().ToList();

        // Assert
        uniquePackages.Should().HaveCount(3); // TestPackage 1.0.0, TestPackage 1.1.0, OtherPackage 2.0.0
        uniquePackages.Should().ContainSingle(p => p.Id == "TestPackage" && p.Version.ToString() == "1.0.0");
        uniquePackages.Should().ContainSingle(p => p.Id == "TestPackage" && p.Version.ToString() == "1.1.0");
        uniquePackages.Should().ContainSingle(p => p.Id == "OtherPackage" && p.Version.ToString() == "2.0.0");
    }

    [Fact]
    public void DependencyGraph_GetDirectDependencies_ReturnsOnlyDirectDependencies()
    {
        // Arrange
        var directDep = CreatePackageReference("DirectPackage", "1.0.0", "/project1.csproj", true);
        var transitiveDep = CreatePackageReference("TransitivePackage", "1.0.0", "/project1.csproj", false);
        var otherProjectDep = CreatePackageReference("OtherPackage", "1.0.0", "/project2.csproj", true);

        var dependencyGraph = new DependencyGraph
        {
            RootPath = "/solution",
            Projects = new List<ProjectInfo>(),
            AllPackages = new List<PackageReference> { directDep, transitiveDep, otherProjectDep }
        };

        // Act
        var directDependencies = dependencyGraph.GetDirectDependencies("/project1.csproj").ToList();

        // Assert
        directDependencies.Should().HaveCount(1);
        directDependencies[0].Id.Should().Be("DirectPackage");
        directDependencies[0].IsDirectDependency.Should().BeTrue();
    }

    [Fact]
    public void DependencyGraph_GetTransitiveDependencies_ReturnsAllTransitiveDependencies()
    {
        // Arrange
        var dependency1 = CreatePackageReference("Dependency1", "1.0.0", "/project1.csproj");
        var dependency2 = CreatePackageReference("Dependency2", "1.0.0", "/project1.csproj");
        var subDependency = CreatePackageReference("SubDependency", "1.0.0", "/project1.csproj");

        // Set up dependency chain: mainPackage -> dependency1 -> subDependency
        //                                      -> dependency2
        dependency1.Dependencies.Add(subDependency);
        
        var mainPackage = CreatePackageReference("MainPackage", "1.0.0", "/project1.csproj");
        mainPackage.Dependencies.Add(dependency1);
        mainPackage.Dependencies.Add(dependency2);

        var dependencyGraph = new DependencyGraph
        {
            RootPath = "/solution",
            Projects = new List<ProjectInfo>(),
            AllPackages = new List<PackageReference> { mainPackage, dependency1, dependency2, subDependency }
        };

        // Act
        var transitiveDeps = dependencyGraph.GetTransitiveDependencies(mainPackage).ToList();

        // Assert
        transitiveDeps.Should().HaveCount(3); // dependency1, dependency2, subDependency
        transitiveDeps.Should().Contain(dependency1);
        transitiveDeps.Should().Contain(dependency2);
        transitiveDeps.Should().Contain(subDependency);
    }

    [Fact]
    public void DependencyGraph_GetTransitiveDependencies_HandlesCircularDependencies()
    {
        // Arrange
        var package1 = CreatePackageReference("Package1", "1.0.0", "/project1.csproj");
        var package2 = CreatePackageReference("Package2", "1.0.0", "/project1.csproj");

        // Create circular dependency: package1 -> package2 -> package1
        package1.Dependencies.Add(package2);
        package2.Dependencies.Add(package1);

        var dependencyGraph = new DependencyGraph
        {
            RootPath = "/solution",
            Projects = new List<ProjectInfo>(),
            AllPackages = new List<PackageReference> { package1, package2 }
        };

        // Act
        var transitiveDeps = dependencyGraph.GetTransitiveDependencies(package1).ToList();

        // Assert - Should handle circular dependency gracefully without infinite loop
        transitiveDeps.Should().HaveCount(2); // Both packages should be included exactly once
        transitiveDeps.Should().Contain(package1);
        transitiveDeps.Should().Contain(package2);
    }

    [Fact]
    public void DependencyGraph_FindDependents_ReturnsPackagesThatDependOnTarget()
    {
        // Arrange
        var targetPackage = CreatePackageReference("TargetPackage", "1.0.0", "/project1.csproj");
        var dependent1 = CreatePackageReference("Dependent1", "1.0.0", "/project1.csproj");
        var dependent2 = CreatePackageReference("Dependent2", "1.0.0", "/project2.csproj");
        var independent = CreatePackageReference("Independent", "1.0.0", "/project3.csproj");

        // Set up dependencies
        dependent1.Dependencies.Add(targetPackage);
        dependent2.Dependencies.Add(targetPackage);
        // independent doesn't depend on targetPackage

        var dependencyGraph = new DependencyGraph
        {
            RootPath = "/solution",
            Projects = new List<ProjectInfo>(),
            AllPackages = new List<PackageReference> { targetPackage, dependent1, dependent2, independent }
        };

        // Act
        var dependents = dependencyGraph.FindDependents("TargetPackage").ToList();

        // Assert
        dependents.Should().HaveCount(2);
        dependents.Should().Contain(dependent1);
        dependents.Should().Contain(dependent2);
        dependents.Should().NotContain(independent);
        dependents.Should().NotContain(targetPackage);
    }

    [Fact]
    public void DependencyGraph_FindDependents_IsCaseInsensitive()
    {
        // Arrange
        var targetPackage = CreatePackageReference("targetpackage", "1.0.0", "/project1.csproj");
        var dependent = CreatePackageReference("Dependent", "1.0.0", "/project1.csproj");
        dependent.Dependencies.Add(targetPackage);

        var dependencyGraph = new DependencyGraph
        {
            RootPath = "/solution",
            Projects = new List<ProjectInfo>(),
            AllPackages = new List<PackageReference> { targetPackage, dependent }
        };

        // Act
        var dependents = dependencyGraph.FindDependents("TARGETPACKAGE").ToList();

        // Assert
        dependents.Should().HaveCount(1);
        dependents[0].Should().Be(dependent);
    }

    [Fact]
    public void DependencyGraph_CreatedAt_IsSetToCurrentTime()
    {
        // Arrange & Act
        var dependencyGraph = new DependencyGraph
        {
            RootPath = "/solution",
            Projects = new List<ProjectInfo>(),
            AllPackages = new List<PackageReference>()
        };

        // Assert
        dependencyGraph.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #region ProjectInfo Tests

    [Fact]
    public void ProjectInfo_CanBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var projectInfo = new ProjectInfo
        {
            Name = "MyApp",
            Path = "/src/MyApp/MyApp.csproj",
            TargetFramework = "net8.0",
            Type = ProjectType.ConsoleApplication,
            OutputType = "Exe",
            TargetFrameworks = new List<string> { "net8.0", "net6.0" },
            PackageReferences = new List<PackageReference>
            {
                CreatePackageReference("Microsoft.Extensions.Hosting", "8.0.0", "/src/MyApp/MyApp.csproj")
            }
        };

        // Assert
        projectInfo.Name.Should().Be("MyApp");
        projectInfo.Path.Should().Be("/src/MyApp/MyApp.csproj");
        projectInfo.TargetFramework.Should().Be("net8.0");
        projectInfo.Type.Should().Be(ProjectType.ConsoleApplication);
        projectInfo.OutputType.Should().Be("Exe");
        projectInfo.TargetFrameworks.Should().Contain("net8.0");
        projectInfo.TargetFrameworks.Should().Contain("net6.0");
        projectInfo.PackageReferences.Should().HaveCount(1);
    }

    [Fact]
    public void ProjectInfo_CanBeCreatedWithMinimalProperties()
    {
        // Arrange & Act
        var projectInfo = new ProjectInfo
        {
            Name = "TestLib",
            Path = "/src/TestLib/TestLib.csproj",
            TargetFramework = "netstandard2.1",
            Type = ProjectType.ClassLibrary
        };

        // Assert
        projectInfo.OutputType.Should().BeNull();
        projectInfo.TargetFrameworks.Should().BeEmpty();
        projectInfo.PackageReferences.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private static PackageReference CreatePackageReference(string id, string version, string projectPath, bool isDirectDependency = true)
    {
        return new PackageReference
        {
            Id = id,
            Version = SemVersion.Parse(version, SemVersionStyles.Strict),
            ProjectPath = projectPath,
            IsDirectDependency = isDirectDependency,
            Dependencies = new List<PackageReference>()
        };
    }

    #endregion
}