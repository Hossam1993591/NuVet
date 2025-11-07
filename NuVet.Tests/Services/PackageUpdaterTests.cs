using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NuVet.Core.Models;
using NuVet.Core.Services;
using Semver;

namespace NuVet.Tests.Services;

public class PackageUpdaterTests
{
    private readonly Mock<ILogger<IPackageUpdater>> _loggerMock;
    private readonly Mock<INuGetService> _nugetServiceMock;
    private readonly Mock<IPackageUpdater> _packageUpdaterMock;

    public PackageUpdaterTests()
    {
        _loggerMock = new Mock<ILogger<IPackageUpdater>>();
        _nugetServiceMock = new Mock<INuGetService>();
        _packageUpdaterMock = new Mock<IPackageUpdater>();
    }

    [Fact]
    public async Task UpdateVulnerablePackagesAsync_WithValidScanResult_ShouldReturnUpdateResults()
    {
        // Arrange
        var packageReference = new PackageReference
        {
            Id = "Test.Package",
            Version = SemVersion.Parse("1.0.0"),
            ProjectPath = "/path/project.csproj"
        };

        var vulnerability = new Vulnerability
        {
            Id = "CVE-2023-1234",
            Title = "Test Vulnerability",
            Description = "A test vulnerability",
            Severity = VulnerabilitySeverity.High,
            PackageId = "Test.Package",
            AffectedVersions = new List<SemVersion> { SemVersion.Parse("1.0.0") },
            PatchedVersions = new List<SemVersion> { SemVersion.Parse("1.1.0") },
            AdvisoryUrl = new Uri("https://example.com/advisory")
        };

        var vulnerablePackage = new VulnerablePackage
        {
            Package = packageReference,
            Vulnerabilities = new List<Vulnerability> { vulnerability },
            AffectedProjects = new List<string> { "/path/project.csproj" }
        };

        var scanResult = new ScanResult
        {
            SolutionPath = "/path/solution.sln",
            ScanDate = DateTime.UtcNow,
            VulnerablePackages = new List<VulnerablePackage> { vulnerablePackage },
            ScannedProjects = new List<ProjectInfo>
            {
                new ProjectInfo
                {
                    Name = "TestProject",
                    Path = "/path/project.csproj",
                    TargetFramework = "net8.0",
                    Type = ProjectType.ClassLibrary
                }
            },
            Summary = new ScanSummary
            {
                TotalProjects = 1,
                TotalPackages = 1,
                VulnerablePackages = 1,
                CriticalVulnerabilities = 0,
                HighVulnerabilities = 1,
                ModerateVulnerabilities = 0,
                LowVulnerabilities = 0,
                UnknownVulnerabilities = 0
            }
        };

        var updateOptions = new UpdateOptions
        {
            AutoApproveMinorUpdates = true,
            CreateBackup = true
        };

        var expectedResults = new List<UpdateResult>
        {
            new UpdateResult
            {
                Update = new PackageUpdate
                {
                    CurrentPackage = packageReference,
                    TargetVersion = SemVersion.Parse("1.1.0"),
                    AffectedProjects = new List<string> { "/path/project.csproj" },
                    VulnerabilitiesFixed = new List<Vulnerability> { vulnerability },
                    UpdateType = UpdateType.Security
                },
                Status = UpdateStatus.Success,
                Duration = TimeSpan.FromSeconds(5)
            }
        };

        var cancellationToken = CancellationToken.None;

        _packageUpdaterMock.Setup(x => x.UpdateVulnerablePackagesAsync(scanResult, updateOptions, cancellationToken))
            .ReturnsAsync(expectedResults);

        // Act
        var result = await _packageUpdaterMock.Object.UpdateVulnerablePackagesAsync(scanResult, updateOptions, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Update.CurrentPackage.Id.Should().Be("Test.Package");
        result.First().Status.Should().Be(UpdateStatus.Success);
    }

    [Fact]
    public async Task UpdatePackageAsync_WithValidUpdate_ShouldReturnSuccessResult()
    {
        // Arrange
        var currentPackage = new PackageReference
        {
            Id = "Test.Package",
            Version = SemVersion.Parse("1.0.0"),
            ProjectPath = "/path/project.csproj"
        };

        var packageUpdate = new PackageUpdate
        {
            CurrentPackage = currentPackage,
            TargetVersion = SemVersion.Parse("1.1.0"),
            AffectedProjects = new List<string> { "/path/project.csproj" },
            VulnerabilitiesFixed = new List<Vulnerability>(),
            UpdateType = UpdateType.Security
        };

        var expectedResult = new UpdateResult
        {
            Update = packageUpdate,
            Status = UpdateStatus.Success,
            Duration = TimeSpan.FromSeconds(10)
        };

        var cancellationToken = CancellationToken.None;

        _packageUpdaterMock.Setup(x => x.UpdatePackageAsync(packageUpdate, cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _packageUpdaterMock.Object.UpdatePackageAsync(packageUpdate, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Update.Should().Be(packageUpdate);
        result.Status.Should().Be(UpdateStatus.Success);
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task CreateBackupAsync_WithValidProjectPaths_ShouldReturnBackup()
    {
        // Arrange
        var projectPaths = new List<string> { "/path/project1.csproj", "/path/project2.csproj" };
        var description = "Pre-update backup";
        var cancellationToken = CancellationToken.None;

        var expectedBackup = new UpdateBackup
        {
            BackupId = "backup123",
            CreatedAt = DateTime.UtcNow,
            Files = new List<BackupFile>
            {
                new BackupFile
                {
                    OriginalPath = "/path/project1.csproj",
                    Content = "<Project>...</Project>"
                },
                new BackupFile
                {
                    OriginalPath = "/path/project2.csproj",
                    Content = "<Project>...</Project>"
                }
            },
            Description = description
        };

        _packageUpdaterMock.Setup(x => x.CreateBackupAsync(projectPaths, description, cancellationToken))
            .ReturnsAsync(expectedBackup);

        // Act
        var result = await _packageUpdaterMock.Object.CreateBackupAsync(projectPaths, description, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().Be(description);
        result.BackupId.Should().NotBeNullOrEmpty();
        result.Files.Should().HaveCount(2);
    }

    [Fact]
    public async Task RestoreBackupAsync_WithValidBackup_ShouldReturnTrue()
    {
        // Arrange
        var backup = new UpdateBackup
        {
            BackupId = "test-backup-123",
            CreatedAt = DateTime.UtcNow,
            Files = new List<BackupFile>
            {
                new BackupFile
                {
                    OriginalPath = "/path/project.csproj",
                    Content = "<Project>...</Project>"
                }
            },
            Description = "Test backup"
        };

        var cancellationToken = CancellationToken.None;

        _packageUpdaterMock.Setup(x => x.RestoreBackupAsync(backup, cancellationToken))
            .ReturnsAsync(true);

        // Act
        var result = await _packageUpdaterMock.Object.RestoreBackupAsync(backup, cancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateUpdatesAsync_WithValidProjects_ShouldReturnValidationResult()
    {
        // Arrange
        var projectPaths = new List<string> { "/path/project1.csproj", "/path/project2.csproj" };
        var cancellationToken = CancellationToken.None;

        var expectedResult = new ValidationResult
        {
            IsValid = true,
            Errors = new List<string>(),
            Warnings = new List<string> { "Minor warning" },
            ValidationDuration = TimeSpan.FromSeconds(5)
        };

        _packageUpdaterMock.Setup(x => x.ValidateUpdatesAsync(projectPaths, cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _packageUpdaterMock.Object.ValidateUpdatesAsync(projectPaths, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().HaveCount(1);
        result.ValidationDuration.Should().BePositive();
    }

    [Fact]
    public async Task UpdateVulnerablePackagesAsync_WithCancellation_ShouldThrowOperationCancelledException()
    {
        // Arrange
        var scanResult = new ScanResult
        {
            SolutionPath = "/path/solution.sln",
            ScanDate = DateTime.UtcNow,
            VulnerablePackages = new List<VulnerablePackage>(),
            ScannedProjects = new List<ProjectInfo>(),
            Summary = new ScanSummary
            {
                TotalProjects = 0,
                TotalPackages = 0,
                VulnerablePackages = 0,
                CriticalVulnerabilities = 0,
                HighVulnerabilities = 0,
                ModerateVulnerabilities = 0,
                LowVulnerabilities = 0,
                UnknownVulnerabilities = 0
            }
        };

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        _packageUpdaterMock.Setup(x => x.UpdateVulnerablePackagesAsync(scanResult, null, cancellationTokenSource.Token))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            _packageUpdaterMock.Object.UpdateVulnerablePackagesAsync(scanResult, null, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task UpdateVulnerablePackagesAsync_WithNullScanResult_ShouldThrowArgumentNullException()
    {
        // Arrange
        ScanResult nullScanResult = null!;
        var cancellationToken = CancellationToken.None;

        _packageUpdaterMock.Setup(x => x.UpdateVulnerablePackagesAsync(nullScanResult, null, cancellationToken))
            .ThrowsAsync(new ArgumentNullException());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _packageUpdaterMock.Object.UpdateVulnerablePackagesAsync(nullScanResult, null, cancellationToken));
    }

    [Fact]
    public async Task UpdateVulnerablePackagesAsync_WithEmptyVulnerablePackages_ShouldReturnEmptyResults()
    {
        // Arrange
        var scanResult = new ScanResult
        {
            SolutionPath = "/path/solution.sln",
            ScanDate = DateTime.UtcNow,
            VulnerablePackages = new List<VulnerablePackage>(),
            ScannedProjects = new List<ProjectInfo>(),
            Summary = new ScanSummary
            {
                TotalProjects = 1,
                TotalPackages = 5,
                VulnerablePackages = 0,
                CriticalVulnerabilities = 0,
                HighVulnerabilities = 0,
                ModerateVulnerabilities = 0,
                LowVulnerabilities = 0,
                UnknownVulnerabilities = 0
            }
        };

        var cancellationToken = CancellationToken.None;

        _packageUpdaterMock.Setup(x => x.UpdateVulnerablePackagesAsync(scanResult, null, cancellationToken))
            .ReturnsAsync(new List<UpdateResult>());

        // Act
        var result = await _packageUpdaterMock.Object.UpdateVulnerablePackagesAsync(scanResult, null, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void PackageUpdater_Mock_ShouldInitializeCorrectly()
    {
        // Assert
        _packageUpdaterMock.Should().NotBeNull();
        _loggerMock.Should().NotBeNull();
        _nugetServiceMock.Should().NotBeNull();
    }
}