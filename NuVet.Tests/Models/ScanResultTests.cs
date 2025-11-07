using NuVet.Core.Models;
using Semver;
using FluentAssertions;

namespace NuVet.Tests.Models;

public class ScanResultTests
{
    #region ScanResult Tests

    [Fact]
    public void ScanResult_GetVulnerablePackagesBySeverity_ReturnsCorrectPackages()
    {
        // Arrange
        var scanResult = CreateTestScanResult();

        // Act
        var criticalPackages = scanResult.GetVulnerablePackagesBySeverity(VulnerabilitySeverity.Critical).ToList();
        var highPackages = scanResult.GetVulnerablePackagesBySeverity(VulnerabilitySeverity.High).ToList();
        var lowPackages = scanResult.GetVulnerablePackagesBySeverity(VulnerabilitySeverity.Low).ToList();

        // Assert
        criticalPackages.Should().HaveCount(1);
        criticalPackages[0].Package.Id.Should().Be("CriticalPackage");
        
        highPackages.Should().HaveCount(1);
        highPackages[0].Package.Id.Should().Be("HighSeverityPackage");
        
        lowPackages.Should().BeEmpty();
    }

    [Fact]
    public void ScanResult_GetHighestSeverity_ReturnsCorrectSeverity()
    {
        // Arrange
        var scanResult = CreateTestScanResult();

        // Act
        var highestSeverity = scanResult.GetHighestSeverity();

        // Assert
        highestSeverity.Should().Be(VulnerabilitySeverity.Critical);
    }

    [Fact]
    public void ScanResult_GetHighestSeverity_ReturnsUnknownWhenNoVulnerabilities()
    {
        // Arrange
        var scanResult = new ScanResult
        {
            SolutionPath = "/test/solution.sln",
            ScanDate = DateTime.UtcNow,
            VulnerablePackages = new List<VulnerablePackage>(),
            ScannedProjects = new List<ProjectInfo>(),
            Summary = CreateTestSummary(0, 0, 0, 0, 0, 0)
        };

        // Act
        var highestSeverity = scanResult.GetHighestSeverity();

        // Assert
        highestSeverity.Should().Be(VulnerabilitySeverity.Unknown);
    }

    [Fact]
    public void ScanResult_HasVulnerabilities_ReturnsTrue_WhenVulnerabilitiesExist()
    {
        // Arrange
        var scanResult = CreateTestScanResult();

        // Act & Assert
        scanResult.HasVulnerabilities.Should().BeTrue();
    }

    [Fact]
    public void ScanResult_HasVulnerabilities_ReturnsFalse_WhenNoVulnerabilities()
    {
        // Arrange
        var scanResult = new ScanResult
        {
            SolutionPath = "/test/solution.sln",
            ScanDate = DateTime.UtcNow,
            VulnerablePackages = new List<VulnerablePackage>(),
            ScannedProjects = new List<ProjectInfo>(),
            Summary = CreateTestSummary(0, 0, 0, 0, 0, 0)
        };

        // Act & Assert
        scanResult.HasVulnerabilities.Should().BeFalse();
    }

    [Fact]
    public void ScanResult_HasCriticalVulnerabilities_ReturnsTrue_WhenCriticalVulnerabilitiesExist()
    {
        // Arrange
        var scanResult = CreateTestScanResult();

        // Act & Assert
        scanResult.HasCriticalVulnerabilities.Should().BeTrue();
    }

    [Fact]
    public void ScanResult_HasCriticalVulnerabilities_ReturnsFalse_WhenNoCriticalVulnerabilities()
    {
        // Arrange
        var highSeverityVulnerability = CreateVulnerability("GHSA-high-001", VulnerabilitySeverity.High);
        var vulnerablePackage = CreateVulnerablePackage("HighPackage", highSeverityVulnerability);
        
        var scanResult = new ScanResult
        {
            SolutionPath = "/test/solution.sln",
            ScanDate = DateTime.UtcNow,
            VulnerablePackages = new List<VulnerablePackage> { vulnerablePackage },
            ScannedProjects = new List<ProjectInfo>(),
            Summary = CreateTestSummary(1, 1, 0, 1, 0, 0)
        };

        // Act & Assert
        scanResult.HasCriticalVulnerabilities.Should().BeFalse();
    }

    #endregion

    #region VulnerablePackage Tests

    [Fact]
    public void VulnerablePackage_HighestSeverity_ReturnsCorrectSeverity()
    {
        // Arrange
        var criticalVuln = CreateVulnerability("GHSA-critical-001", VulnerabilitySeverity.Critical);
        var highVuln = CreateVulnerability("GHSA-high-001", VulnerabilitySeverity.High);
        var moderateVuln = CreateVulnerability("GHSA-moderate-001", VulnerabilitySeverity.Moderate);

        var vulnerablePackage = new VulnerablePackage
        {
            Package = CreatePackageReference("TestPackage"),
            Vulnerabilities = new List<Vulnerability> { highVuln, criticalVuln, moderateVuln },
            AffectedProjects = new List<string> { "/test/project.csproj" }
        };

        // Act & Assert
        vulnerablePackage.HighestSeverity.Should().Be(VulnerabilitySeverity.Critical);
    }

    [Fact]
    public void VulnerablePackage_HighestSeverity_ReturnsUnknown_WhenNoVulnerabilities()
    {
        // Arrange
        var vulnerablePackage = new VulnerablePackage
        {
            Package = CreatePackageReference("TestPackage"),
            Vulnerabilities = new List<Vulnerability>(),
            AffectedProjects = new List<string> { "/test/project.csproj" }
        };

        // Act & Assert
        vulnerablePackage.HighestSeverity.Should().Be(VulnerabilitySeverity.Unknown);
    }

    [Fact]
    public void VulnerablePackage_GetSuggestedUpdateVersions_ReturnsVersionsThatFixAllVulnerabilities()
    {
        // Arrange
        var vuln1 = new Vulnerability
        {
            Id = "GHSA-001",
            Title = "Vulnerability 1",
            Description = "Test vulnerability",
            Severity = VulnerabilitySeverity.High,
            PackageId = "TestPackage",
            AffectedVersions = new List<SemVersion> { SemVersion.Parse("1.0.0", SemVersionStyles.Strict) },
            PatchedVersions = new List<SemVersion> 
            { 
                SemVersion.Parse("1.1.0", SemVersionStyles.Strict),
                SemVersion.Parse("1.2.0", SemVersionStyles.Strict)
            },
            AdvisoryUrl = new Uri("https://github.com/advisories/GHSA-001"),
            PublishedAt = DateTime.UtcNow
        };

        var vuln2 = new Vulnerability
        {
            Id = "GHSA-002",
            Title = "Vulnerability 2",
            Description = "Test vulnerability 2",
            Severity = VulnerabilitySeverity.Moderate,
            PackageId = "TestPackage",
            AffectedVersions = new List<SemVersion> { SemVersion.Parse("1.0.0", SemVersionStyles.Strict) },
            PatchedVersions = new List<SemVersion> 
            { 
                SemVersion.Parse("1.1.5", SemVersionStyles.Strict),
                SemVersion.Parse("1.2.0", SemVersionStyles.Strict)
            },
            AdvisoryUrl = new Uri("https://github.com/advisories/GHSA-002"),
            PublishedAt = DateTime.UtcNow
        };

        var vulnerablePackage = new VulnerablePackage
        {
            Package = CreatePackageReference("TestPackage"),
            Vulnerabilities = new List<Vulnerability> { vuln1, vuln2 },
            AffectedProjects = new List<string> { "/test/project.csproj" }
        };

        // Act
        var suggestedVersions = vulnerablePackage.GetSuggestedUpdateVersions();

        // Assert
        suggestedVersions.Should().NotBeEmpty();
        suggestedVersions.Should().Contain(SemVersion.Parse("1.2.0", SemVersionStyles.Strict));
    }

    [Fact]
    public void VulnerablePackage_GetSuggestedUpdateVersions_ReturnsEmpty_WhenNoPatchedVersions()
    {
        // Arrange
        var vuln = new Vulnerability
        {
            Id = "GHSA-001",
            Title = "Vulnerability 1",
            Description = "Test vulnerability",
            Severity = VulnerabilitySeverity.High,
            PackageId = "TestPackage",
            AffectedVersions = new List<SemVersion> { SemVersion.Parse("1.0.0", SemVersionStyles.Strict) },
            PatchedVersions = new List<SemVersion>(),
            AdvisoryUrl = new Uri("https://github.com/advisories/GHSA-001"),
            PublishedAt = DateTime.UtcNow
        };

        var vulnerablePackage = new VulnerablePackage
        {
            Package = CreatePackageReference("TestPackage"),
            Vulnerabilities = new List<Vulnerability> { vuln },
            AffectedProjects = new List<string> { "/test/project.csproj" }
        };

        // Act
        var suggestedVersions = vulnerablePackage.GetSuggestedUpdateVersions();

        // Assert
        suggestedVersions.Should().BeEmpty();
    }

    #endregion

    #region ScanSummary Tests

    [Fact]
    public void ScanSummary_TotalVulnerabilities_CalculatesCorrectSum()
    {
        // Arrange
        var summary = CreateTestSummary(5, 100, 3, 2, 1, 0);

        // Act & Assert
        summary.TotalVulnerabilities.Should().Be(3); // 2 + 1 + 0 + 0 + 0
    }

    [Fact]
    public void ScanSummary_TotalVulnerabilities_ReturnsZero_WhenNoVulnerabilities()
    {
        // Arrange
        var summary = CreateTestSummary(0, 50, 0, 0, 0, 0);

        // Act & Assert
        summary.TotalVulnerabilities.Should().Be(0);
    }

    #endregion

    #region Helper Methods

    private static ScanResult CreateTestScanResult()
    {
        var criticalVulnerability = CreateVulnerability("GHSA-critical-001", VulnerabilitySeverity.Critical);
        var highVulnerability = CreateVulnerability("GHSA-high-001", VulnerabilitySeverity.High);

        var criticalPackage = CreateVulnerablePackage("CriticalPackage", criticalVulnerability);
        var highPackage = CreateVulnerablePackage("HighSeverityPackage", highVulnerability);

        return new ScanResult
        {
            SolutionPath = "/test/solution.sln",
            ScanDate = DateTime.UtcNow,
            VulnerablePackages = new List<VulnerablePackage> { criticalPackage, highPackage },
            ScannedProjects = new List<ProjectInfo>(),
            Summary = CreateTestSummary(2, 10, 1, 1, 0, 0)
        };
    }

    private static VulnerablePackage CreateVulnerablePackage(string packageId, Vulnerability vulnerability)
    {
        return new VulnerablePackage
        {
            Package = CreatePackageReference(packageId),
            Vulnerabilities = new List<Vulnerability> { vulnerability },
            AffectedProjects = new List<string> { "/test/project.csproj" }
        };
    }

    private static Vulnerability CreateVulnerability(string id, VulnerabilitySeverity severity)
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

    private static PackageReference CreatePackageReference(string packageId)
    {
        return new PackageReference
        {
            Id = packageId,
            Version = SemVersion.Parse("1.0.0", SemVersionStyles.Strict),
            ProjectPath = "/test/project.csproj"
        };
    }

    private static ScanSummary CreateTestSummary(int totalProjects, int totalPackages, int vulnerablePackages, 
        int critical, int high, int moderate)
    {
        return new ScanSummary
        {
            TotalProjects = totalProjects,
            TotalPackages = totalPackages,
            VulnerablePackages = vulnerablePackages,
            CriticalVulnerabilities = critical,
            HighVulnerabilities = high,
            ModerateVulnerabilities = moderate,
            LowVulnerabilities = 0,
            UnknownVulnerabilities = 0
        };
    }

    #endregion
}