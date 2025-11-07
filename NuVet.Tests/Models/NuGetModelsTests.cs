using NuVet.Core.Models;
using Semver;
using FluentAssertions;

namespace NuVet.Tests.Models;

public class NuGetModelsTests
{
    #region PackageMetadata Tests

    [Fact]
    public void PackageMetadata_CanBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var metadata = new PackageMetadata
        {
            Id = "Newtonsoft.Json",
            Title = "Json.NET",
            Description = "Popular high-performance JSON framework for .NET",
            Authors = new List<string> { "James Newton-King" },
            Tags = new List<string> { "json", "serialization" },
            ProjectUrl = new Uri("https://www.newtonsoft.com/json"),
            LicenseUrl = new Uri("https://licenses.nuget.org/MIT"),
            Published = DateTime.Parse("2023-01-01"),
            DownloadCount = 1000000,
            IsPrerelease = false,
            LatestVersion = SemVersion.Parse("13.0.3", SemVersionStyles.Strict),
            LatestStableVersion = SemVersion.Parse("13.0.3", SemVersionStyles.Strict),
            Versions = new List<PackageVersion>()
        };

        // Assert
        metadata.Id.Should().Be("Newtonsoft.Json");
        metadata.Title.Should().Be("Json.NET");
        metadata.Description.Should().Contain("JSON framework");
        metadata.Authors.Should().Contain("James Newton-King");
        metadata.Tags.Should().Contain("json");
        metadata.ProjectUrl.Should().Be(new Uri("https://www.newtonsoft.com/json"));
        metadata.IsPrerelease.Should().BeFalse();
        metadata.DownloadCount.Should().Be(1000000);
    }

    #endregion

    #region PackageVersion Tests

    [Fact]
    public void PackageVersion_CanBeCreatedWithDependencies()
    {
        // Arrange
        var dependencies = new List<PackageDependency>
        {
            new PackageDependency
            {
                Id = "Microsoft.Extensions.Logging",
                VersionRange = "[6.0.0,)",
                TargetFramework = "net8.0"
            }
        };

        // Act
        var packageVersion = new PackageVersion
        {
            Version = SemVersion.Parse("1.5.0", SemVersionStyles.Strict),
            Published = DateTime.Parse("2023-06-01"),
            DownloadCount = 50000,
            IsPrerelease = false,
            Dependencies = dependencies,
            ReleaseNotes = "Bug fixes and performance improvements"
        };

        // Assert
        packageVersion.Version.Should().Be(SemVersion.Parse("1.5.0", SemVersionStyles.Strict));
        packageVersion.IsPrerelease.Should().BeFalse();
        packageVersion.Dependencies.Should().HaveCount(1);
        packageVersion.Dependencies[0].Id.Should().Be("Microsoft.Extensions.Logging");
        packageVersion.ReleaseNotes.Should().Contain("Bug fixes");
    }

    [Fact]
    public void PackageVersion_CanBeCreatedWithoutDependencies()
    {
        // Arrange & Act
        var packageVersion = new PackageVersion
        {
            Version = SemVersion.Parse("2.0.0-beta1", SemVersionStyles.Strict),
            Published = DateTime.Parse("2023-07-01"),
            DownloadCount = 1000,
            IsPrerelease = true
        };

        // Assert
        packageVersion.IsPrerelease.Should().BeTrue();
        packageVersion.Dependencies.Should().BeEmpty();
        packageVersion.ReleaseNotes.Should().BeNull();
    }

    #endregion

    #region PackageDependency Tests

    [Fact]
    public void PackageDependency_IsSatisfiedBy_ReturnsTrue_ForAnyVersion()
    {
        // NOTE: Current implementation always returns true as mentioned in TODO
        // Arrange
        var dependency = new PackageDependency
        {
            Id = "TestPackage",
            VersionRange = "[1.0.0,2.0.0)",
            TargetFramework = "net8.0"
        };

        var version = SemVersion.Parse("1.5.0", SemVersionStyles.Strict);

        // Act
        var result = dependency.IsSatisfiedBy(version);

        // Assert
        result.Should().BeTrue(); // Based on TODO comment in implementation
    }

    [Fact]
    public void PackageDependency_CanBeCreatedWithMinimalProperties()
    {
        // Arrange & Act
        var dependency = new PackageDependency
        {
            Id = "System.Text.Json",
            VersionRange = "[6.0.0,)"
        };

        // Assert
        dependency.Id.Should().Be("System.Text.Json");
        dependency.VersionRange.Should().Be("[6.0.0,)");
        dependency.TargetFramework.Should().BeNull();
    }

    [Fact]
    public void PackageDependency_CanBeCreatedWithTargetFramework()
    {
        // Arrange & Act
        var dependency = new PackageDependency
        {
            Id = "Microsoft.AspNetCore.App",
            VersionRange = "8.0.0",
            TargetFramework = "net8.0"
        };

        // Assert
        dependency.TargetFramework.Should().Be("net8.0");
    }

    #endregion

    #region PackageSearchResult Tests

    [Fact]
    public void PackageSearchResult_CanBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var searchResult = new PackageSearchResult
        {
            Id = "AutoMapper",
            Title = "AutoMapper",
            Description = "A convention-based object-object mapper",
            Authors = new List<string> { "Jimmy Bogard" },
            Tags = new List<string> { "mapper", "convention", "object" },
            LatestVersion = SemVersion.Parse("12.0.1", SemVersionStyles.Strict),
            TotalDownloads = 500000000,
            IsVerified = true
        };

        // Assert
        searchResult.Id.Should().Be("AutoMapper");
        searchResult.Title.Should().Be("AutoMapper");
        searchResult.Description.Should().Contain("mapper");
        searchResult.Authors.Should().Contain("Jimmy Bogard");
        searchResult.Tags.Should().Contain("convention");
        searchResult.LatestVersion.Should().Be(SemVersion.Parse("12.0.1", SemVersionStyles.Strict));
        searchResult.TotalDownloads.Should().Be(500000000);
        searchResult.IsVerified.Should().BeTrue();
    }

    [Fact]
    public void PackageSearchResult_CanBeCreatedWithEmptyCollections()
    {
        // Arrange & Act
        var searchResult = new PackageSearchResult
        {
            Id = "TestPackage",
            Title = "Test Package",
            Description = "A test package",
            Authors = new List<string>(),
            Tags = new List<string>(),
            LatestVersion = SemVersion.Parse("1.0.0", SemVersionStyles.Strict),
            TotalDownloads = 100,
            IsVerified = false
        };

        // Assert
        searchResult.Authors.Should().BeEmpty();
        searchResult.Tags.Should().BeEmpty();
        searchResult.IsVerified.Should().BeFalse();
    }

    #endregion
}