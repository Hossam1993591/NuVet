using FluentAssertions;
using NuVet.Core.Models;
using Semver;

namespace NuVet.Tests.Models;

public class PackageReferenceTests
{
    [Fact]
    public void PackageReference_ShouldInitializeCorrectly()
    {
        // Arrange
        var id = "Newtonsoft.Json";
        var version = SemVersion.Parse("13.0.3");
        var projectPath = "/path/to/project.csproj";

        // Act
        var packageRef = new PackageReference 
        { 
            Id = id, 
            Version = version, 
            ProjectPath = projectPath,
            TargetFramework = "net8.0",
            IsDirectDependency = true
        };

        // Assert
        packageRef.Id.Should().Be(id);
        packageRef.Version.Should().Be(version);
        packageRef.ProjectPath.Should().Be(projectPath);
        packageRef.TargetFramework.Should().Be("net8.0");
        packageRef.IsDirectDependency.Should().BeTrue();
    }

    [Fact]
    public void PackageReference_ToString_ShouldReturnCorrectFormat()
    {
        // Arrange
        var packageRef = new PackageReference 
        { 
            Id = "Test.Package", 
            Version = SemVersion.Parse("1.0.0"), 
            ProjectPath = "/test/project.csproj"
        };

        // Act
        var result = packageRef.ToString();

        // Assert
        result.Should().Be("Test.Package 1.0.0");
    }

    [Fact]
    public void PackageReference_Equals_ShouldReturnTrueForSameIdAndVersion()
    {
        // Arrange
        var packageRef1 = new PackageReference 
        { 
            Id = "Test.Package", 
            Version = SemVersion.Parse("1.0.0"), 
            ProjectPath = "/test/project1.csproj"
        };
        var packageRef2 = new PackageReference 
        { 
            Id = "Test.Package", 
            Version = SemVersion.Parse("1.0.0"), 
            ProjectPath = "/test/project2.csproj"
        };

        // Act & Assert
        packageRef1.Equals(packageRef2).Should().BeTrue();
    }

    [Fact]
    public void PackageReference_Equals_ShouldReturnFalseForDifferentVersion()
    {
        // Arrange
        var packageRef1 = new PackageReference 
        { 
            Id = "Test.Package", 
            Version = SemVersion.Parse("1.0.0"), 
            ProjectPath = "/test/project.csproj"
        };
        var packageRef2 = new PackageReference 
        { 
            Id = "Test.Package", 
            Version = SemVersion.Parse("2.0.0"), 
            ProjectPath = "/test/project.csproj"
        };

        // Act & Assert
        packageRef1.Equals(packageRef2).Should().BeFalse();
    }

    [Fact]
    public void PackageReference_GetHashCode_ShouldBeSameForEqualPackages()
    {
        // Arrange
        var packageRef1 = new PackageReference 
        { 
            Id = "Test.Package", 
            Version = SemVersion.Parse("1.0.0"), 
            ProjectPath = "/test/project1.csproj"
        };
        var packageRef2 = new PackageReference 
        { 
            Id = "Test.Package", 
            Version = SemVersion.Parse("1.0.0"), 
            ProjectPath = "/test/project2.csproj"
        };

        // Act & Assert
        packageRef1.GetHashCode().Should().Be(packageRef2.GetHashCode());
    }

    [Theory]
    [InlineData(PackageReferenceSource.PackageReference)]
    [InlineData(PackageReferenceSource.PackagesConfig)]
    [InlineData(PackageReferenceSource.ProjectJson)]
    [InlineData(PackageReferenceSource.CentralPackageManagement)]
    public void PackageReference_Source_ShouldSetCorrectly(PackageReferenceSource source)
    {
        // Arrange & Act
        var packageRef = new PackageReference 
        { 
            Id = "Test.Package", 
            Version = SemVersion.Parse("1.0.0"), 
            ProjectPath = "/test/project.csproj",
            Source = source
        };

        // Assert
        packageRef.Source.Should().Be(source);
    }

    [Fact]
    public void PackageReference_Dependencies_ShouldInitializeAsEmptyList()
    {
        // Arrange & Act
        var packageRef = new PackageReference 
        { 
            Id = "Test.Package", 
            Version = SemVersion.Parse("1.0.0"), 
            ProjectPath = "/test/project.csproj"
        };

        // Assert
        packageRef.Dependencies.Should().NotBeNull();
        packageRef.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public void PackageReference_WithDependencies_ShouldAddCorrectly()
    {
        // Arrange
        var dependency = new PackageReference 
        { 
            Id = "Dependency.Package", 
            Version = SemVersion.Parse("2.1.0"), 
            ProjectPath = "/test/project.csproj",
            IsDirectDependency = false
        };

        var packageRef = new PackageReference 
        { 
            Id = "Test.Package", 
            Version = SemVersion.Parse("1.0.0"), 
            ProjectPath = "/test/project.csproj",
            Dependencies = new List<PackageReference> { dependency }
        };

        // Act & Assert
        packageRef.Dependencies.Should().HaveCount(1);
        packageRef.Dependencies.First().Should().Be(dependency);
    }
}