using FluentAssertions;
using NuVet.Core.Models;
using Semver;

namespace NuVet.Tests.Models;

public class ModelTests
{
    [Fact]
    public void ProjectInfo_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var projectInfo = new ProjectInfo
        {
            Name = "TestProject",
            Path = "/path/to/test.csproj",
            TargetFramework = "net8.0",
            Type = ProjectType.ClassLibrary,
            OutputType = "Library"
        };

        // Assert
        projectInfo.Name.Should().Be("TestProject");
        projectInfo.Path.Should().Be("/path/to/test.csproj");
        projectInfo.TargetFramework.Should().Be("net8.0");
        projectInfo.Type.Should().Be(ProjectType.ClassLibrary);
        projectInfo.OutputType.Should().Be("Library");
    }

    [Fact]
    public void DependencyGraph_ShouldInitializeCorrectly()
    {
        // Arrange
        var projects = new List<ProjectInfo>
        {
            new ProjectInfo
            {
                Name = "Project1",
                Path = "/path/project1.csproj",
                TargetFramework = "net8.0",
                Type = ProjectType.ClassLibrary
            }
        };

        var packages = new List<PackageReference>
        {
            new PackageReference
            {
                Id = "Newtonsoft.Json",
                Version = SemVersion.Parse("13.0.3"),
                ProjectPath = "/path/project1.csproj"
            }
        };

        // Act
        var dependencyGraph = new DependencyGraph
        {
            RootPath = "/path",
            Projects = projects,
            AllPackages = packages
        };

        // Assert
        dependencyGraph.RootPath.Should().Be("/path");
        dependencyGraph.Projects.Should().HaveCount(1);
        dependencyGraph.AllPackages.Should().HaveCount(1);
        dependencyGraph.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void DependencyGraph_GetUniquePackages_ShouldReturnDistinctPackages()
    {
        // Arrange
        var packages = new List<PackageReference>
        {
            new PackageReference
            {
                Id = "Package1",
                Version = SemVersion.Parse("1.0.0"),
                ProjectPath = "/path/project1.csproj"
            },
            new PackageReference
            {
                Id = "Package1",
                Version = SemVersion.Parse("1.0.0"),
                ProjectPath = "/path/project2.csproj"
            },
            new PackageReference
            {
                Id = "Package2",
                Version = SemVersion.Parse("2.0.0"),
                ProjectPath = "/path/project1.csproj"
            }
        };

        var dependencyGraph = new DependencyGraph
        {
            RootPath = "/path",
            Projects = new List<ProjectInfo>(),
            AllPackages = packages
        };

        // Act
        var uniquePackages = dependencyGraph.GetUniquePackages().ToList();

        // Assert
        uniquePackages.Should().HaveCount(2);
    }

    [Fact]
    public void Vulnerability_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var vulnerability = new Vulnerability
        {
            Id = "CVE-2023-1234",
            Title = "Test Vulnerability",
            Description = "A test vulnerability description",
            Severity = VulnerabilitySeverity.High,
            PackageId = "Test.Package",
            AffectedVersions = new List<SemVersion> { SemVersion.Parse("1.0.0") },
            PatchedVersions = new List<SemVersion> { SemVersion.Parse("1.1.0") },
            AdvisoryUrl = new Uri("https://example.com/advisory"),
            PublishedAt = DateTime.UtcNow
        };

        // Assert
        vulnerability.Id.Should().Be("CVE-2023-1234");
        vulnerability.Title.Should().Be("Test Vulnerability");
        vulnerability.Severity.Should().Be(VulnerabilitySeverity.High);
        vulnerability.PackageId.Should().Be("Test.Package");
    }

    [Fact]
    public void Vulnerability_IsVersionAffected_ShouldReturnCorrectResult()
    {
        // Arrange
        var vulnerability = new Vulnerability
        {
            Id = "CVE-2023-1234",
            Title = "Test Vulnerability",
            Description = "A test vulnerability description",
            Severity = VulnerabilitySeverity.High,
            PackageId = "Test.Package",
            AffectedVersions = new List<SemVersion> { SemVersion.Parse("1.0.0") },
            PatchedVersions = new List<SemVersion> { SemVersion.Parse("1.1.0") },
            AdvisoryUrl = new Uri("https://example.com/advisory")
        };

        // Act & Assert
        vulnerability.IsVersionAffected(SemVersion.Parse("1.0.0")).Should().BeTrue();
        vulnerability.IsVersionAffected(SemVersion.Parse("1.1.0")).Should().BeFalse();
    }

    [Fact]
    public void VulnerablePackage_ShouldInitializeCorrectly()
    {
        // Arrange
        var package = new PackageReference
        {
            Id = "Test.Package",
            Version = SemVersion.Parse("1.0.0"),
            ProjectPath = "/path/project.csproj"
        };

        var vulnerabilities = new List<Vulnerability>
        {
            new Vulnerability
            {
                Id = "CVE-2023-1234",
                Title = "Test Vulnerability",
                Description = "A test vulnerability",
                Severity = VulnerabilitySeverity.High,
                PackageId = "Test.Package",
                AffectedVersions = new List<SemVersion> { SemVersion.Parse("1.0.0") },
                PatchedVersions = new List<SemVersion> { SemVersion.Parse("1.1.0") },
                AdvisoryUrl = new Uri("https://example.com/advisory")
            }
        };

        // Act
        var vulnerablePackage = new VulnerablePackage
        {
            Package = package,
            Vulnerabilities = vulnerabilities,
            AffectedProjects = new List<string> { "/path/project.csproj" }
        };

        // Assert
        vulnerablePackage.Package.Should().Be(package);
        vulnerablePackage.Vulnerabilities.Should().HaveCount(1);
        vulnerablePackage.HighestSeverity.Should().Be(VulnerabilitySeverity.High);
    }

    [Fact]
    public void ScanResult_ShouldInitializeCorrectly()
    {
        // Arrange
        var vulnerablePackages = new List<VulnerablePackage>();
        var projects = new List<ProjectInfo>
        {
            new ProjectInfo
            {
                Name = "TestProject",
                Path = "/path/test.csproj",
                TargetFramework = "net8.0",
                Type = ProjectType.ClassLibrary
            }
        };

        var summary = new ScanSummary
        {
            TotalProjects = 1,
            TotalPackages = 5,
            VulnerablePackages = 0,
            CriticalVulnerabilities = 0,
            HighVulnerabilities = 0,
            ModerateVulnerabilities = 0,
            LowVulnerabilities = 0,
            UnknownVulnerabilities = 0
        };

        // Act
        var scanResult = new ScanResult
        {
            SolutionPath = "/path/solution.sln",
            ScanDate = DateTime.UtcNow,
            VulnerablePackages = vulnerablePackages,
            ScannedProjects = projects,
            Summary = summary,
            ScanDuration = TimeSpan.FromSeconds(30)
        };

        // Assert
        scanResult.SolutionPath.Should().Be("/path/solution.sln");
        scanResult.VulnerablePackages.Should().BeEmpty();
        scanResult.ScannedProjects.Should().HaveCount(1);
        scanResult.HasVulnerabilities.Should().BeFalse();
    }

    [Fact]
    public void PackageUpdate_ShouldInitializeCorrectly()
    {
        // Arrange
        var currentPackage = new PackageReference
        {
            Id = "Test.Package",
            Version = SemVersion.Parse("1.0.0"),
            ProjectPath = "/path/project.csproj"
        };

        // Act
        var packageUpdate = new PackageUpdate
        {
            CurrentPackage = currentPackage,
            TargetVersion = SemVersion.Parse("1.1.0"),
            AffectedProjects = new List<string> { "/path/project.csproj" },
            VulnerabilitiesFixed = new List<Vulnerability>(),
            UpdateType = UpdateType.Security,
            UpdateReason = "Security vulnerability fix"
        };

        // Assert
        packageUpdate.CurrentPackage.Should().Be(currentPackage);
        packageUpdate.TargetVersion.Should().Be(SemVersion.Parse("1.1.0"));
        packageUpdate.UpdateType.Should().Be(UpdateType.Security);
        packageUpdate.IsMajorVersionUpdate.Should().BeFalse();
        packageUpdate.IsMinorVersionUpdate.Should().BeTrue();
        packageUpdate.IsPatchVersionUpdate.Should().BeFalse();
    }

    [Fact]
    public void UpdateResult_ShouldInitializeCorrectly()
    {
        // Arrange
        var packageUpdate = new PackageUpdate
        {
            CurrentPackage = new PackageReference
            {
                Id = "Test.Package",
                Version = SemVersion.Parse("1.0.0"),
                ProjectPath = "/path/project.csproj"
            },
            TargetVersion = SemVersion.Parse("1.1.0"),
            AffectedProjects = new List<string> { "/path/project.csproj" },
            VulnerabilitiesFixed = new List<Vulnerability>()
        };

        // Act
        var updateResult = new UpdateResult
        {
            Update = packageUpdate,
            Status = UpdateStatus.Success,
            Duration = TimeSpan.FromSeconds(15)
        };

        // Assert
        updateResult.Update.Should().Be(packageUpdate);
        updateResult.Status.Should().Be(UpdateStatus.Success);
        updateResult.IsSuccessful.Should().BeTrue();
        updateResult.RequiresRollback.Should().BeFalse();
    }

    [Fact]
    public void UpdateOptions_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var updateOptions = new UpdateOptions();

        // Assert
        updateOptions.AutoApproveMinorUpdates.Should().BeTrue();
        updateOptions.AutoApprovePatchUpdates.Should().BeTrue();
        updateOptions.CreateBackup.Should().BeTrue();
        updateOptions.ValidateAfterUpdate.Should().BeTrue();
        updateOptions.RollbackOnFailure.Should().BeTrue();
        updateOptions.MinimumSeverityToUpdate.Should().Be(VulnerabilitySeverity.Low);
    }

    [Fact]
    public void ValidationResult_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var validationResult = new ValidationResult
        {
            IsValid = true,
            Errors = new List<string>(),
            Warnings = new List<string> { "Some warning" },
            ValidationDuration = TimeSpan.FromSeconds(10)
        };

        // Assert
        validationResult.IsValid.Should().BeTrue();
        validationResult.Errors.Should().BeEmpty();
        validationResult.Warnings.Should().HaveCount(1);
        validationResult.ValidationDuration.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void PackageMetadata_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var packageMetadata = new PackageMetadata
        {
            Id = "Test.Package",
            Title = "Test Package",
            Description = "A test package description",
            Authors = new List<string> { "Test Author" },
            Tags = new List<string> { "test", "package" },
            ProjectUrl = new Uri("https://github.com/test/package"),
            LicenseUrl = new Uri("https://opensource.org/licenses/MIT"),
            Published = DateTime.UtcNow,
            DownloadCount = 1000,
            IsPrerelease = false
        };

        // Assert
        packageMetadata.Id.Should().Be("Test.Package");
        packageMetadata.Title.Should().Be("Test Package");
        packageMetadata.Authors.Should().Contain("Test Author");
        packageMetadata.Tags.Should().Contain("test");
        packageMetadata.IsPrerelease.Should().BeFalse();
    }

    [Fact]
    public void BackupFile_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var backupFile = new BackupFile
        {
            OriginalPath = "/path/project.csproj",
            Content = "<Project>...</Project>",
            BackedUpAt = DateTime.UtcNow
        };

        // Assert
        backupFile.OriginalPath.Should().Be("/path/project.csproj");
        backupFile.Content.Should().Be("<Project>...</Project>");
        backupFile.BackedUpAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}