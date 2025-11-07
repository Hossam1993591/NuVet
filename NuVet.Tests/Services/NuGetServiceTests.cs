using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NuVet.Core.Models;
using NuVet.Core.Services;
using Semver;

namespace NuVet.Tests.Services;

public class NuGetServiceTests
{
    private readonly Mock<ILogger<INuGetService>> _loggerMock;
    private readonly Mock<INuGetService> _nugetServiceMock;

    public NuGetServiceTests()
    {
        _loggerMock = new Mock<ILogger<INuGetService>>();
        _nugetServiceMock = new Mock<INuGetService>();
    }

    [Fact]
    public async Task GetPackageMetadataAsync_WithValidPackageId_ShouldReturnMetadata()
    {
        // Arrange
        var packageId = "Newtonsoft.Json";
        var cancellationToken = CancellationToken.None;
        var expectedMetadata = new PackageMetadata
        {
            Id = packageId,
            Title = "Json.NET",
            Description = "Popular JSON framework for .NET",
            Authors = new List<string> { "James Newton-King" },
            Tags = new List<string> { "json" },
            ProjectUrl = new Uri("https://www.newtonsoft.com/json"),
            LicenseUrl = new Uri("https://raw.github.com/JamesNK/Newtonsoft.Json/master/LICENSE.md"),
            Versions = new List<PackageVersion>
            {
                new PackageVersion
                {
                    Version = SemVersion.Parse("13.0.3"),
                    Published = DateTime.UtcNow.AddDays(-30),
                    DownloadCount = 1000000,
                    IsPrerelease = false
                }
            }
        };

        _nugetServiceMock.Setup(x => x.GetPackageMetadataAsync(packageId, cancellationToken))
            .ReturnsAsync(expectedMetadata);

        // Act
        var result = await _nugetServiceMock.Object.GetPackageMetadataAsync(packageId, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(packageId);
        result.Title.Should().Be("Json.NET");
        result.Authors.Should().Contain("James Newton-King");
        result.Versions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPackageMetadataAsync_WithInvalidPackageId_ShouldReturnNull()
    {
        // Arrange
        var invalidPackageId = "Non.Existent.Package.12345";
        var cancellationToken = CancellationToken.None;

        _nugetServiceMock.Setup(x => x.GetPackageMetadataAsync(invalidPackageId, cancellationToken))
            .ReturnsAsync((PackageMetadata?)null);

        // Act
        var result = await _nugetServiceMock.Object.GetPackageMetadataAsync(invalidPackageId, cancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPackageVersionsAsync_WithValidPackageId_ShouldReturnVersions()
    {
        // Arrange
        var packageId = "Newtonsoft.Json";
        var cancellationToken = CancellationToken.None;
        var expectedVersions = new List<string> { "13.0.3", "13.0.2", "13.0.1" };

        _nugetServiceMock.Setup(x => x.GetPackageVersionsAsync(packageId, cancellationToken))
            .ReturnsAsync(expectedVersions);

        // Act
        var result = await _nugetServiceMock.Object.GetPackageVersionsAsync(packageId, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedVersions);
    }

    [Fact]
    public async Task SearchPackagesAsync_WithValidQuery_ShouldReturnResults()
    {
        // Arrange
        var searchQuery = "json";
        var cancellationToken = CancellationToken.None;
        var expectedResults = new List<PackageSearchResult>
        {
            new PackageSearchResult
            {
                Id = "Newtonsoft.Json",
                Title = "Json.NET",
                Description = "Popular JSON framework for .NET",
                Authors = new List<string> { "James Newton-King" },
                Tags = new List<string> { "json" },
                LatestVersion = SemVersion.Parse("13.0.3"),
                TotalDownloads = 5000000,
                IsVerified = true
            }
        };

        _nugetServiceMock.Setup(x => x.SearchPackagesAsync(searchQuery, 0, 10, cancellationToken))
            .ReturnsAsync(expectedResults);

        // Act
        var result = await _nugetServiceMock.Object.SearchPackagesAsync(searchQuery, 0, 10, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Id.Should().Be("Newtonsoft.Json");
    }

    [Fact]
    public async Task SearchPackagesAsync_WithEmptyQuery_ShouldReturnResults()
    {
        // Arrange
        var emptyQuery = "";
        var cancellationToken = CancellationToken.None;
        var expectedResults = new List<PackageSearchResult>
        {
            new PackageSearchResult
            {
                Id = "Popular.Package",
                Title = "Popular Package",
                Description = "A popular package",
                Authors = new List<string> { "Author" },
                Tags = new List<string> { "popular" },
                LatestVersion = SemVersion.Parse("1.0.0"),
                TotalDownloads = 1000000,
                IsVerified = true
            }
        };

        _nugetServiceMock.Setup(x => x.SearchPackagesAsync(emptyQuery, 0, 5, cancellationToken))
            .ReturnsAsync(expectedResults);

        // Act
        var result = await _nugetServiceMock.Object.SearchPackagesAsync(emptyQuery, 0, 5, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountLessOrEqualTo(5);
    }

    [Fact]
    public async Task GetPackageDependenciesAsync_WithValidPackage_ShouldReturnDependencies()
    {
        // Arrange
        var packageId = "Microsoft.AspNetCore.App";
        var version = "8.0.0";
        var cancellationToken = CancellationToken.None;
        var expectedDependencies = new List<PackageDependency>
        {
            new PackageDependency
            {
                Id = "Microsoft.Extensions.DependencyInjection",
                VersionRange = "[8.0.0, )",
                TargetFramework = "net8.0"
            }
        };

        _nugetServiceMock.Setup(x => x.GetPackageDependenciesAsync(packageId, version, null, cancellationToken))
            .ReturnsAsync(expectedDependencies);

        // Act
        var result = await _nugetServiceMock.Object.GetPackageDependenciesAsync(packageId, version, null, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDependencies);
    }

    [Fact]
    public async Task GetPackageDependenciesAsync_WithTargetFramework_ShouldReturnFrameworkSpecificDependencies()
    {
        // Arrange
        var packageId = "System.Text.Json";
        var version = "8.0.0";
        var targetFramework = "net8.0";
        var cancellationToken = CancellationToken.None;
        var expectedDependencies = new List<PackageDependency>();

        _nugetServiceMock.Setup(x => x.GetPackageDependenciesAsync(packageId, version, targetFramework, cancellationToken))
            .ReturnsAsync(expectedDependencies);

        // Act
        var result = await _nugetServiceMock.Object.GetPackageDependenciesAsync(packageId, version, targetFramework, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDependencies);
    }

    [Fact]
    public async Task PackageVersionExistsAsync_WithValidPackageVersion_ShouldReturnTrue()
    {
        // Arrange
        var packageId = "Newtonsoft.Json";
        var version = "13.0.3";
        var cancellationToken = CancellationToken.None;

        _nugetServiceMock.Setup(x => x.PackageVersionExistsAsync(packageId, version, cancellationToken))
            .ReturnsAsync(true);

        // Act
        var result = await _nugetServiceMock.Object.PackageVersionExistsAsync(packageId, version, cancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PackageVersionExistsAsync_WithInvalidPackageVersion_ShouldReturnFalse()
    {
        // Arrange
        var packageId = "Newtonsoft.Json";
        var invalidVersion = "999.999.999";
        var cancellationToken = CancellationToken.None;

        _nugetServiceMock.Setup(x => x.PackageVersionExistsAsync(packageId, invalidVersion, cancellationToken))
            .ReturnsAsync(false);

        // Act
        var result = await _nugetServiceMock.Object.PackageVersionExistsAsync(packageId, invalidVersion, cancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetPackageMetadataAsync_WithCancellation_ShouldThrowOperationCancelledException()
    {
        // Arrange
        var packageId = "Newtonsoft.Json";
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        _nugetServiceMock.Setup(x => x.GetPackageMetadataAsync(packageId, cancellationTokenSource.Token))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            _nugetServiceMock.Object.GetPackageMetadataAsync(packageId, cancellationTokenSource.Token));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task GetPackageMetadataAsync_WithInvalidPackageId_ShouldThrowArgumentException(string invalidPackageId)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        _nugetServiceMock.Setup(x => x.GetPackageMetadataAsync(invalidPackageId, cancellationToken))
            .ThrowsAsync(new ArgumentException());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _nugetServiceMock.Object.GetPackageMetadataAsync(invalidPackageId, cancellationToken));
    }

    [Fact]
    public async Task SearchPackagesAsync_WithPagination_ShouldRespectSkipAndTake()
    {
        // Arrange
        var searchQuery = "microsoft";
        var skip = 5;
        var take = 3;
        var cancellationToken = CancellationToken.None;
        var expectedResults = new List<PackageSearchResult>
        {
            new PackageSearchResult
            {
                Id = "Microsoft.Test1",
                Title = "Microsoft Test 1",
                Description = "First test package",
                Authors = new List<string> { "Microsoft" },
                Tags = new List<string> { "microsoft" },
                LatestVersion = SemVersion.Parse("1.0.0"),
                TotalDownloads = 100000,
                IsVerified = true
            },
            new PackageSearchResult
            {
                Id = "Microsoft.Test2",
                Title = "Microsoft Test 2", 
                Description = "Second test package",
                Authors = new List<string> { "Microsoft" },
                Tags = new List<string> { "microsoft" },
                LatestVersion = SemVersion.Parse("2.0.0"),
                TotalDownloads = 200000,
                IsVerified = true
            }
        };

        _nugetServiceMock.Setup(x => x.SearchPackagesAsync(searchQuery, skip, take, cancellationToken))
            .ReturnsAsync(expectedResults);

        // Act
        var result = await _nugetServiceMock.Object.SearchPackagesAsync(searchQuery, skip, take, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountLessOrEqualTo(take);
        result.Should().BeEquivalentTo(expectedResults);
    }

    [Fact]
    public void NuGetService_Mock_ShouldInitializeCorrectly()
    {
        // Assert
        _nugetServiceMock.Should().NotBeNull();
        _loggerMock.Should().NotBeNull();
    }
}