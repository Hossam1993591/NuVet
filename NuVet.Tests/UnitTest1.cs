using NuVet.Core.Models;
using Semver;

namespace NuVet.Tests;

public class PackageReferenceTests
{
    [Fact]
    public void PackageReference_Equality_WorksCorrectly()
    {
        // Arrange
        var package1 = new PackageReference
        {
            Id = "TestPackage",
            Version = SemVersion.Parse("1.0.0", SemVersionStyles.Strict),
            ProjectPath = "/test/project.csproj"
        };

        var package2 = new PackageReference
        {
            Id = "TestPackage",
            Version = SemVersion.Parse("1.0.0", SemVersionStyles.Strict),
            ProjectPath = "/different/project.csproj"
        };

        // Act & Assert
        Assert.Equal(package1, package2);
        Assert.Equal(package1.GetHashCode(), package2.GetHashCode());
    }

    [Fact]
    public void PackageReference_ToString_ReturnsCorrectFormat()
    {
        // Arrange
        var package = new PackageReference
        {
            Id = "TestPackage",
            Version = SemVersion.Parse("1.2.3", SemVersionStyles.Strict),
            ProjectPath = "/test/project.csproj"
        };

        // Act
        var result = package.ToString();

        // Assert
        Assert.Equal("TestPackage 1.2.3", result);
    }
}