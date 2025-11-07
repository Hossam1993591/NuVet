using Microsoft.Extensions.Logging;
using Moq;
using NuVet.Core.Models;
using NuVet.Core.Services;
using FluentAssertions;
using Semver;

namespace NuVet.Tests.Services;

public class ServiceInterfaceTests
{
    [Fact]
    public void IVulnerabilityService_HasCorrectMethods()
    {
        // This test verifies that the interface contract is maintained
        var serviceType = typeof(IVulnerabilityService);
        
        serviceType.IsInterface.Should().BeTrue();
        serviceType.GetMethods().Should().HaveCountGreaterThan(3);
        
        var getVulnerabilitiesMethod = serviceType.GetMethod("GetVulnerabilitiesAsync", new[] { typeof(string), typeof(CancellationToken) });
        getVulnerabilitiesMethod.Should().NotBeNull();
    }

    [Fact]
    public void INuGetService_HasCorrectMethods()
    {
        var serviceType = typeof(INuGetService);
        
        serviceType.IsInterface.Should().BeTrue();
        serviceType.GetMethods().Should().HaveCountGreaterThan(4);
        
        var getPackageMetadataMethod = serviceType.GetMethod("GetPackageMetadataAsync");
        getPackageMetadataMethod.Should().NotBeNull();
    }

    [Fact]
    public void IDependencyAnalyzer_HasCorrectMethods()
    {
        var serviceType = typeof(IDependencyAnalyzer);
        
        serviceType.IsInterface.Should().BeTrue();
        serviceType.GetMethods().Should().HaveCountGreaterThan(3);
        
        var analyzeMethod = serviceType.GetMethod("AnalyzeAsync", new[] { typeof(string), typeof(CancellationToken) });
        analyzeMethod.Should().NotBeNull();
    }

    [Fact]
    public void IPackageUpdater_HasCorrectMethods()
    {
        var serviceType = typeof(IPackageUpdater);
        
        serviceType.IsInterface.Should().BeTrue();
        serviceType.GetMethods().Should().HaveCountGreaterThan(3);
        
        var updateMethod = serviceType.GetMethod("UpdateVulnerablePackagesAsync");
        updateMethod.Should().NotBeNull();
    }
}

public class MockedServiceTests
{
    [Fact]
    public async Task VulnerabilityService_MockedGetVulnerabilities_ReturnsExpectedData()
    {
        // Arrange
        var mockService = new Mock<IVulnerabilityService>();
        var expectedVulns = new List<Vulnerability>
        {
            CreateTestVulnerability("GHSA-test-001", VulnerabilitySeverity.High)
        };
        
        mockService
            .Setup(x => x.GetVulnerabilitiesAsync("TestPackage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedVulns);

        // Act
        var result = await mockService.Object.GetVulnerabilitiesAsync("TestPackage", CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("GHSA-test-001");
        result[0].Severity.Should().Be(VulnerabilitySeverity.High);
        
        mockService.Verify(x => x.GetVulnerabilitiesAsync("TestPackage", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NuGetService_MockedGetPackageMetadata_ReturnsExpectedData()
    {
        // Arrange
        var mockService = new Mock<INuGetService>();
        var expectedMetadata = new PackageMetadata
        {
            Id = "TestPackage",
            Title = "Test Package",
            Description = "A test package",
            Authors = new List<string> { "Test Author" },
            Tags = new List<string> { "test" },
            ProjectUrl = new Uri("https://test.com"),
            LicenseUrl = new Uri("https://test.com/license"),
            DownloadCount = 1000
        };
        
        mockService
            .Setup(x => x.GetPackageMetadataAsync("TestPackage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetadata);

        // Act
        var result = await mockService.Object.GetPackageMetadataAsync("TestPackage", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("TestPackage");
        result.Title.Should().Be("Test Package");
        result.Authors.Should().Contain("Test Author");
        
        mockService.Verify(x => x.GetPackageMetadataAsync("TestPackage", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DependencyAnalyzer_MockedAnalyze_ReturnsExpectedData()
    {
        // Arrange
        var mockAnalyzer = new Mock<IDependencyAnalyzer>();
        var expectedGraph = CreateTestDependencyGraph();
        
        mockAnalyzer
            .Setup(x => x.AnalyzeAsync("/test/solution.sln", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedGraph);

        // Act
        var result = await mockAnalyzer.Object.AnalyzeAsync("/test/solution.sln", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RootPath.Should().Be("/test");
        result.Projects.Should().HaveCount(1);
        result.AllPackages.Should().HaveCount(2);
        
        mockAnalyzer.Verify(x => x.AnalyzeAsync("/test/solution.sln", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PackageUpdater_MockedUpdatePackages_ReturnsExpectedResults()
    {
        // Arrange
        var mockUpdater = new Mock<IPackageUpdater>();
        var scanResult = CreateTestScanResult();
        var expectedResults = new List<UpdateResult>
        {
            new UpdateResult
            {
                Update = CreateTestPackageUpdate(),
                Status = UpdateStatus.Success,
                Duration = TimeSpan.FromSeconds(5)
            }
        };
        
        mockUpdater
            .Setup(x => x.UpdateVulnerablePackagesAsync(scanResult, It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResults);

        // Act
        var result = await mockUpdater.Object.UpdateVulnerablePackagesAsync(scanResult, null, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Status.Should().Be(UpdateStatus.Success);
        result[0].IsSuccessful.Should().BeTrue();
        
        mockUpdater.Verify(x => x.UpdateVulnerablePackagesAsync(
            It.IsAny<ScanResult>(), 
            It.IsAny<UpdateOptions>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Vulnerability CreateTestVulnerability(string id, VulnerabilitySeverity severity)
    {
        return new Vulnerability
        {
            Id = id,
            Title = $"Test Vulnerability {id}",
            Description = "Test vulnerability description",
            Severity = severity,
            PackageId = "TestPackage",
            AffectedVersions = new List<SemVersion> { SemVersion.Parse("1.0.0", SemVersionStyles.Strict) },
            PatchedVersions = new List<SemVersion> { SemVersion.Parse("1.1.0", SemVersionStyles.Strict) },
            AdvisoryUrl = new Uri($"https://github.com/advisories/{id}"),
            PublishedAt = DateTime.UtcNow
        };
    }

    private static DependencyGraph CreateTestDependencyGraph()
    {
        var projects = new List<ProjectInfo>
        {
            new ProjectInfo
            {
                Name = "TestProject",
                Path = "/test/project.csproj",
                TargetFramework = "net8.0",
                Type = ProjectType.ConsoleApplication
            }
        };

        var packages = new List<PackageReference>
        {
            new PackageReference
            {
                Id = "VulnerablePackage",
                Version = SemVersion.Parse("1.0.0", SemVersionStyles.Strict),
                ProjectPath = "/test/project.csproj"
            },
            new PackageReference
            {
                Id = "SafePackage", 
                Version = SemVersion.Parse("2.0.0", SemVersionStyles.Strict),
                ProjectPath = "/test/project.csproj"
            }
        };

        return new DependencyGraph
        {
            RootPath = "/test",
            Projects = projects,
            AllPackages = packages
        };
    }

    private static ScanResult CreateTestScanResult()
    {
        var vulnerablePackage = new VulnerablePackage
        {
            Package = new PackageReference
            {
                Id = "VulnerablePackage",
                Version = SemVersion.Parse("1.0.0", SemVersionStyles.Strict),
                ProjectPath = "/test/project.csproj"
            },
            Vulnerabilities = new List<Vulnerability>
            {
                CreateTestVulnerability("GHSA-001", VulnerabilitySeverity.High)
            },
            AffectedProjects = new List<string> { "/test/project.csproj" }
        };

        return new ScanResult
        {
            SolutionPath = "/test/solution.sln",
            ScanDate = DateTime.UtcNow,
            VulnerablePackages = new List<VulnerablePackage> { vulnerablePackage },
            ScannedProjects = new List<ProjectInfo>(),
            Summary = new ScanSummary
            {
                TotalProjects = 1,
                TotalPackages = 5,
                VulnerablePackages = 1,
                CriticalVulnerabilities = 0,
                HighVulnerabilities = 1,
                ModerateVulnerabilities = 0,
                LowVulnerabilities = 0,
                UnknownVulnerabilities = 0
            }
        };
    }

    private static PackageUpdate CreateTestPackageUpdate()
    {
        return new PackageUpdate
        {
            CurrentPackage = new PackageReference
            {
                Id = "TestPackage",
                Version = SemVersion.Parse("1.0.0", SemVersionStyles.Strict),
                ProjectPath = "/test/project.csproj"
            },
            TargetVersion = SemVersion.Parse("1.1.0", SemVersionStyles.Strict),
            AffectedProjects = new List<string> { "/test/project.csproj" },
            VulnerabilitiesFixed = new List<Vulnerability>(),
            UpdateType = UpdateType.Security
        };
    }
}