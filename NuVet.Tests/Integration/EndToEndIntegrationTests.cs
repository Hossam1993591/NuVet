using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NuVet.Core.Models;
using NuVet.Core.Services;
using NuVet.Core.Services.Implementation;
using FluentAssertions;
using Semver;
using System;

namespace NuVet.Tests.Integration;

public class EndToEndIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<IVulnerabilityService> _mockVulnerabilityService;
    private readonly Mock<IDependencyAnalyzer> _mockDependencyAnalyzer;

    public EndToEndIntegrationTests()
    {
        // Set up dependency injection container
        var services = new ServiceCollection();
        
        // Mock services for testing
        _mockVulnerabilityService = new Mock<IVulnerabilityService>();
        _mockDependencyAnalyzer = new Mock<IDependencyAnalyzer>();
        
        // Configure services
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(_mockVulnerabilityService.Object);
        services.AddSingleton(_mockDependencyAnalyzer.Object);
        services.AddScoped<IVulnerabilityScanner, VulnerabilityScanner>();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task FullScanWorkflow_WithVulnerablePackages_ReturnsCorrectResults()
    {
        // Arrange
        var solutionPath = "/test/TestApp.sln";
        SetupMockServices();
        
        var scanner = _serviceProvider.GetRequiredService<IVulnerabilityScanner>();

        // Act
        var scanResult = await scanner.ScanAsync(solutionPath);

        // Assert
        scanResult.Should().NotBeNull();
        scanResult.VulnerablePackages.Should().NotBeEmpty();
        scanResult.Summary.VulnerablePackages.Should().BeGreaterThan(0);
        scanResult.HasVulnerabilities.Should().BeTrue();
        scanResult.HasCriticalVulnerabilities.Should().BeTrue();
        
        // Verify the highest severity is detected correctly
        var highestSeverity = scanResult.GetHighestSeverity();
        highestSeverity.Should().Be(VulnerabilitySeverity.Critical);
        
        // Verify vulnerable packages by severity
        var criticalPackages = scanResult.GetVulnerablePackagesBySeverity(VulnerabilitySeverity.Critical);
        criticalPackages.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ScanWorkflow_WithNoVulnerabilities_ReturnsCleanResults()
    {
        // Arrange
        var solutionPath = "/test/SafeApp.sln";
        SetupCleanMockServices();
        
        var scanner = _serviceProvider.GetRequiredService<IVulnerabilityScanner>();

        // Act
        var scanResult = await scanner.ScanAsync(solutionPath);

        // Assert
        scanResult.Should().NotBeNull();
        scanResult.VulnerablePackages.Should().BeEmpty();
        scanResult.HasVulnerabilities.Should().BeFalse();
        scanResult.HasCriticalVulnerabilities.Should().BeFalse();
        scanResult.GetHighestSeverity().Should().Be(VulnerabilitySeverity.Unknown);
    }

    [Fact]
    public async Task ScanWorkflow_WithFilteredResults_RespectsMinimumSeverity()
    {
        // Arrange
        var solutionPath = "/test/TestApp.sln";
        SetupMockServices();
        
        var options = new ScanOptions
        {
            MinimumSeverity = VulnerabilitySeverity.High,
            IncludeTransitiveDependencies = true
        };
        
        var scanner = _serviceProvider.GetRequiredService<IVulnerabilityScanner>();

        // Act
        var scanResult = await scanner.ScanAsync(solutionPath, options);

        // Assert
        scanResult.Should().NotBeNull();
        
        // All vulnerabilities should be High or Critical
        foreach (var vulnPackage in scanResult.VulnerablePackages)
        {
            vulnPackage.HighestSeverity.Should().BeOneOf(VulnerabilitySeverity.High, VulnerabilitySeverity.Critical);
        }
    }

    [Fact]
    public async Task ScanWorkflow_WithExcludedPackages_FiltersCorrectly()
    {
        // Arrange
        var solutionPath = "/test/TestApp.sln";
        SetupMockServices();
        
        var options = new ScanOptions
        {
            ExcludePackages = new List<string> { "System.*", "Microsoft.*" }
        };
        
        var scanner = _serviceProvider.GetRequiredService<IVulnerabilityScanner>();

        // Act
        var scanResult = await scanner.ScanAsync(solutionPath, options);

        // Assert
        scanResult.Should().NotBeNull();
        
        // Ensure no excluded packages are in the results
        foreach (var vulnPackage in scanResult.VulnerablePackages)
        {
            vulnPackage.Package.Id.Should().NotStartWith("System.");
            vulnPackage.Package.Id.Should().NotStartWith("Microsoft.");
        }
    }

    [Fact] 
    public async Task DependencyGraphIntegration_BuildsCorrectStructure()
    {
        // Arrange
        var dependencyGraph = CreateComplexDependencyGraph();
        var scanner = _serviceProvider.GetRequiredService<IVulnerabilityScanner>();
        
        _mockVulnerabilityService
            .Setup(x => x.GetVulnerabilitiesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, List<Vulnerability>>());

        // Act
        var result = await scanner.ScanDependencyGraphAsync(dependencyGraph);

        // Assert
        result.Should().NotBeNull();
        result.ScannedProjects.Should().HaveCount(3);
        result.Summary.TotalPackages.Should().BeGreaterThan(0);
        
        // Verify unique packages are correctly identified
        var uniquePackages = dependencyGraph.GetUniquePackages();
        uniquePackages.Should().HaveCountLessThanOrEqualTo(dependencyGraph.AllPackages.Count);
    }

    [Fact]
    public async Task VulnerabilityDetection_WithMultipleVersions_DetectsCorrectlyAffected()
    {
        // Arrange
        var packages = new List<PackageReference>
        {
            CreatePackageReference("TestPackage", "1.0.0"),  // Vulnerable
            CreatePackageReference("TestPackage", "1.2.0"),  // Safe
            CreatePackageReference("OtherPackage", "2.0.0")  // Vulnerable
        };

        var vulnerabilities = new Dictionary<string, List<Vulnerability>>
        {
            {
                "TestPackage",
                new List<Vulnerability>
                {
                    CreateVulnerability("GHSA-001", "TestPackage", "1.1.0") // Affects 1.0.0 but not 1.2.0
                }
            },
            {
                "OtherPackage", 
                new List<Vulnerability>
                {
                    CreateVulnerability("GHSA-002", "OtherPackage", "2.1.0") // Affects 2.0.0
                }
            }
        };

        _mockVulnerabilityService
            .Setup(x => x.GetVulnerabilitiesAsync("TestPackage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(vulnerabilities["TestPackage"]);

        _mockVulnerabilityService
            .Setup(x => x.GetVulnerabilitiesAsync("OtherPackage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(vulnerabilities["OtherPackage"]);

        _mockVulnerabilityService
            .Setup(x => x.GetVulnerabilitiesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vulnerabilities);

        var scanner = _serviceProvider.GetRequiredService<IVulnerabilityScanner>();

        // Act
        var result = await scanner.ScanPackagesAsync(packages);

        // Assert
        result.Should().HaveCount(2); // Two vulnerable packages
        result.Should().Contain(vp => vp.Package.Id == "TestPackage" && vp.Package.Version.ToString() == "1.0.0");
        result.Should().Contain(vp => vp.Package.Id == "OtherPackage" && vp.Package.Version.ToString() == "2.0.0");
        result.Should().NotContain(vp => vp.Package.Id == "TestPackage" && vp.Package.Version.ToString() == "1.2.0");
    }

    [Fact]
    public void ScanOptions_Configuration_WorksCorrectly()
    {
        // Arrange & Act
        var defaultOptions = new ScanOptions();
        var customOptions = new ScanOptions
        {
            IncludeTransitiveDependencies = false,
            MinimumSeverity = VulnerabilitySeverity.Critical,
            UseLocalCache = false,
            CacheExpiration = TimeSpan.FromMinutes(30),
            ExcludePackages = new List<string> { "TestPackage" },
            IncludeOnlyProjects = new List<string> { "/src/" }
        };

        // Assert
        defaultOptions.IncludeTransitiveDependencies.Should().BeTrue();
        defaultOptions.MinimumSeverity.Should().Be(VulnerabilitySeverity.Low);
        
        customOptions.IncludeTransitiveDependencies.Should().BeFalse();
        customOptions.MinimumSeverity.Should().Be(VulnerabilitySeverity.Critical);
        customOptions.ExcludePackages.Should().Contain("TestPackage");
    }

    [Fact]
    public void UpdateOptions_Configuration_WorksCorrectly()
    {
        // Arrange & Act
        var defaultOptions = new UpdateOptions();
        var customOptions = new UpdateOptions
        {
            AutoApproveMinorUpdates = false,
            AutoApprovePatchUpdates = true,
            CreateBackup = false,
            ValidateAfterUpdate = false,
            RollbackOnFailure = false,
            MinimumSeverityToUpdate = VulnerabilitySeverity.High,
            ExcludePackages = new List<string> { "CriticalPackage" }
        };

        // Assert
        defaultOptions.AutoApproveMinorUpdates.Should().BeTrue();
        defaultOptions.CreateBackup.Should().BeTrue();
        
        customOptions.AutoApproveMinorUpdates.Should().BeFalse();
        customOptions.CreateBackup.Should().BeFalse();
        customOptions.ExcludePackages.Should().Contain("CriticalPackage");
    }

    private void SetupMockServices()
    {
        var dependencyGraph = CreateTestDependencyGraph();
        var vulnerabilities = CreateTestVulnerabilities();

        _mockDependencyAnalyzer
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dependencyGraph);

        // Setup single package vulnerability lookup
        _mockVulnerabilityService
            .Setup(x => x.GetVulnerabilitiesAsync("VulnerablePackage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(vulnerabilities["VulnerablePackage"]);

        _mockVulnerabilityService
            .Setup(x => x.GetVulnerabilitiesAsync("AnotherVulnerablePackage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(vulnerabilities["AnotherVulnerablePackage"]);

        _mockVulnerabilityService
            .Setup(x => x.GetVulnerabilitiesAsync("SafePackage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Vulnerability>());

        // Setup batch lookup as well
        _mockVulnerabilityService
            .Setup(x => x.GetVulnerabilitiesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vulnerabilities);
    }

    private void SetupCleanMockServices()
    {
        var dependencyGraph = CreateCleanDependencyGraph();

        _mockDependencyAnalyzer
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dependencyGraph);

        _mockVulnerabilityService
            .Setup(x => x.GetVulnerabilitiesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Vulnerability>());

        _mockVulnerabilityService
            .Setup(x => x.GetVulnerabilitiesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, List<Vulnerability>>());
    }

    private static DependencyGraph CreateTestDependencyGraph()
    {
        var projects = new List<ProjectInfo>
        {
            new ProjectInfo
            {
                Name = "TestApp",
                Path = "/test/TestApp.csproj",
                TargetFramework = "net8.0",
                Type = ProjectType.ConsoleApplication
            }
        };

        var packages = new List<PackageReference>
        {
            CreatePackageReference("VulnerablePackage", "1.0.0"),
            CreatePackageReference("AnotherVulnerablePackage", "2.0.0"),
            CreatePackageReference("SafePackage", "3.0.0")
        };

        return new DependencyGraph
        {
            RootPath = "/test",
            Projects = projects,
            AllPackages = packages
        };
    }

    private static DependencyGraph CreateCleanDependencyGraph()
    {
        var projects = new List<ProjectInfo>
        {
            new ProjectInfo
            {
                Name = "SafeApp",
                Path = "/test/SafeApp.csproj",
                TargetFramework = "net8.0",
                Type = ProjectType.ConsoleApplication
            }
        };

        var packages = new List<PackageReference>
        {
            CreatePackageReference("SafePackage1", "1.0.0"),
            CreatePackageReference("SafePackage2", "2.0.0")
        };

        return new DependencyGraph
        {
            RootPath = "/test",
            Projects = projects,
            AllPackages = packages
        };
    }

    private static DependencyGraph CreateComplexDependencyGraph()
    {
        var projects = new List<ProjectInfo>
        {
            new ProjectInfo { Name = "WebApi", Path = "/src/WebApi/WebApi.csproj", TargetFramework = "net8.0", Type = ProjectType.WebApplication },
            new ProjectInfo { Name = "Core", Path = "/src/Core/Core.csproj", TargetFramework = "net8.0", Type = ProjectType.ClassLibrary },
            new ProjectInfo { Name = "Tests", Path = "/test/Tests.csproj", TargetFramework = "net8.0", Type = ProjectType.TestProject }
        };

        var packages = new List<PackageReference>
        {
            CreatePackageReference("Microsoft.AspNetCore.App", "8.0.0", "/src/WebApi/WebApi.csproj"),
            CreatePackageReference("Newtonsoft.Json", "13.0.3", "/src/Core/Core.csproj"),
            CreatePackageReference("xunit", "2.4.2", "/test/Tests.csproj"),
            CreatePackageReference("Moq", "4.20.69", "/test/Tests.csproj")
        };

        return new DependencyGraph
        {
            RootPath = "/src",
            Projects = projects,
            AllPackages = packages
        };
    }

    private static Dictionary<string, List<Vulnerability>> CreateTestVulnerabilities()
    {
        return new Dictionary<string, List<Vulnerability>>
        {
            {
                "VulnerablePackage",
                new List<Vulnerability>
                {
                    CreateVulnerability("GHSA-001", "VulnerablePackage", "1.1.0", VulnerabilitySeverity.Critical),
                    CreateVulnerability("GHSA-002", "VulnerablePackage", "1.0.5", VulnerabilitySeverity.High)
                }
            },
            {
                "AnotherVulnerablePackage",
                new List<Vulnerability>
                {
                    CreateVulnerability("GHSA-003", "AnotherVulnerablePackage", "2.1.0", VulnerabilitySeverity.High)
                }
            }
        };
    }

    private static PackageReference CreatePackageReference(string id, string version, string projectPath = "/test/project.csproj")
    {
        return new PackageReference
        {
            Id = id,
            Version = SemVersion.Parse(version, SemVersionStyles.Strict),
            ProjectPath = projectPath
        };
    }

    private static Vulnerability CreateVulnerability(string id, string packageId, string patchedVersion, VulnerabilitySeverity severity = VulnerabilitySeverity.High)
    {
        return new Vulnerability
        {
            Id = id,
            Title = $"Test Vulnerability {id}",
            Description = "Test vulnerability description",
            Severity = severity,
            PackageId = packageId,
            AffectedVersions = new List<SemVersion> { SemVersion.Parse("1.0.0", SemVersionStyles.Strict) },
            PatchedVersions = new List<SemVersion> { SemVersion.Parse(patchedVersion, SemVersionStyles.Strict) },
            AdvisoryUrl = new Uri($"https://github.com/advisories/{id}"),
            PublishedAt = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}