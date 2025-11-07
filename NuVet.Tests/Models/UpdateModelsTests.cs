using NuVet.Core.Models;
using Semver;
using FluentAssertions;

namespace NuVet.Tests.Models;

public class UpdateModelsTests
{
    #region PackageUpdate Tests
    
    [Fact]
    public void PackageUpdate_IsMajorVersionUpdate_ReturnsTrueForMajorVersionChange()
    {
        // Arrange
        var currentPackage = new PackageReference
        {
            Id = "TestPackage",
            Version = SemVersion.Parse("1.0.0", SemVersionStyles.Strict),
            ProjectPath = "/test/project.csproj"
        };
        
        var update = new PackageUpdate
        {
            CurrentPackage = currentPackage,
            TargetVersion = SemVersion.Parse("2.0.0", SemVersionStyles.Strict),
            AffectedProjects = new List<string> { "/test/project.csproj" },
            VulnerabilitiesFixed = new List<Vulnerability>(),
            UpdateType = UpdateType.Security
        };

        // Act & Assert
        update.IsMajorVersionUpdate.Should().BeTrue();
        update.IsMinorVersionUpdate.Should().BeFalse();
        update.IsPatchVersionUpdate.Should().BeFalse();
    }

    [Fact]
    public void PackageUpdate_IsMinorVersionUpdate_ReturnsTrueForMinorVersionChange()
    {
        // Arrange
        var currentPackage = new PackageReference
        {
            Id = "TestPackage",
            Version = SemVersion.Parse("1.0.0", SemVersionStyles.Strict),
            ProjectPath = "/test/project.csproj"
        };
        
        var update = new PackageUpdate
        {
            CurrentPackage = currentPackage,
            TargetVersion = SemVersion.Parse("1.1.0", SemVersionStyles.Strict),
            AffectedProjects = new List<string> { "/test/project.csproj" },
            VulnerabilitiesFixed = new List<Vulnerability>(),
            UpdateType = UpdateType.Feature
        };

        // Act & Assert
        update.IsMinorVersionUpdate.Should().BeTrue();
        update.IsMajorVersionUpdate.Should().BeFalse();
        update.IsPatchVersionUpdate.Should().BeFalse();
    }

    [Fact]
    public void PackageUpdate_IsPatchVersionUpdate_ReturnsTrueForPatchVersionChange()
    {
        // Arrange
        var currentPackage = new PackageReference
        {
            Id = "TestPackage",
            Version = SemVersion.Parse("1.0.0", SemVersionStyles.Strict),
            ProjectPath = "/test/project.csproj"
        };
        
        var update = new PackageUpdate
        {
            CurrentPackage = currentPackage,
            TargetVersion = SemVersion.Parse("1.0.1", SemVersionStyles.Strict),
            AffectedProjects = new List<string> { "/test/project.csproj" },
            VulnerabilitiesFixed = new List<Vulnerability>(),
            UpdateType = UpdateType.BugFix
        };

        // Act & Assert
        update.IsPatchVersionUpdate.Should().BeTrue();
        update.IsMajorVersionUpdate.Should().BeFalse();
        update.IsMinorVersionUpdate.Should().BeFalse();
    }

    #endregion

    #region UpdateResult Tests

    [Fact]
    public void UpdateResult_IsSuccessful_ReturnsTrueWhenStatusIsSuccess()
    {
        // Arrange
        var update = CreateTestPackageUpdate();
        var result = new UpdateResult
        {
            Update = update,
            Status = UpdateStatus.Success,
            Duration = TimeSpan.FromSeconds(5)
        };

        // Act & Assert
        result.IsSuccessful.Should().BeTrue();
        result.RequiresRollback.Should().BeFalse();
    }

    [Fact]
    public void UpdateResult_RequiresRollback_ReturnsTrueWhenFailedWithBackup()
    {
        // Arrange
        var update = CreateTestPackageUpdate();
        var backup = CreateTestBackup();
        
        var result = new UpdateResult
        {
            Update = update,
            Status = UpdateStatus.Failed,
            ErrorMessage = "Build failed",
            Duration = TimeSpan.FromSeconds(10),
            Backup = backup
        };

        // Act & Assert
        result.RequiresRollback.Should().BeTrue();
        result.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public void UpdateResult_RequiresRollback_ReturnsFalseWhenFailedWithoutBackup()
    {
        // Arrange
        var update = CreateTestPackageUpdate();
        
        var result = new UpdateResult
        {
            Update = update,
            Status = UpdateStatus.Failed,
            ErrorMessage = "Build failed",
            Duration = TimeSpan.FromSeconds(10)
        };

        // Act & Assert
        result.RequiresRollback.Should().BeFalse();
        result.IsSuccessful.Should().BeFalse();
    }

    #endregion

    #region UpdateBackup Tests

    [Fact]
    public void UpdateBackup_Create_CreatesBackupWithCorrectProperties()
    {
        // Arrange
        var description = "Pre-update backup";
        var filePaths = new[] { "/test/project1.csproj", "/test/project2.csproj" };
        
        // Create temporary files for testing
        var tempDir = Path.GetTempPath();
        var testFiles = filePaths.Select(path => 
        {
            var fileName = Path.GetFileName(path);
            var fullPath = Path.Combine(tempDir, fileName);
            File.WriteAllText(fullPath, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            return fullPath;
        }).ToArray();

        try
        {
            // Act
            var backup = UpdateBackup.Create(description, testFiles);

            // Assert
            backup.Description.Should().Be(description);
            backup.BackupId.Should().NotBeNullOrEmpty();
            backup.BackupId.Should().HaveLength(8);
            backup.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            backup.Files.Should().HaveCount(2);
            backup.Files.All(f => f.Content.Contains("Microsoft.NET.Sdk")).Should().BeTrue();
        }
        finally
        {
            // Cleanup
            foreach (var file in testFiles)
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
        }
    }

    #endregion

    #region BackupFile Tests

    [Fact]
    public void BackupFile_BackedUpAt_IsSetToCurrentTime()
    {
        // Arrange & Act
        var backupFile = new BackupFile
        {
            OriginalPath = "/test/project.csproj",
            Content = "<Project />"
        };

        // Assert
        backupFile.BackedUpAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region BreakingChange Tests

    [Fact]
    public void BreakingChange_CanBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var breakingChange = new BreakingChange
        {
            Type = "API Change",
            Description = "Method signature changed",
            Severity = BreakingChangeSeverity.High,
            Mitigation = "Update method calls",
            AffectedApi = "SomeClass.SomeMethod"
        };

        // Assert
        breakingChange.Type.Should().Be("API Change");
        breakingChange.Description.Should().Be("Method signature changed");
        breakingChange.Severity.Should().Be(BreakingChangeSeverity.High);
        breakingChange.Mitigation.Should().Be("Update method calls");
        breakingChange.AffectedApi.Should().Be("SomeClass.SomeMethod");
    }

    [Fact]
    public void BreakingChange_CanBeCreatedWithMinimalProperties()
    {
        // Arrange & Act
        var breakingChange = new BreakingChange
        {
            Type = "API Change",
            Description = "Method signature changed", 
            Severity = BreakingChangeSeverity.Medium
        };

        // Assert
        breakingChange.Type.Should().Be("API Change");
        breakingChange.Description.Should().Be("Method signature changed");
        breakingChange.Severity.Should().Be(BreakingChangeSeverity.Medium);
        breakingChange.Mitigation.Should().BeNull();
        breakingChange.AffectedApi.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private static PackageUpdate CreateTestPackageUpdate()
    {
        var currentPackage = new PackageReference
        {
            Id = "TestPackage",
            Version = SemVersion.Parse("1.0.0", SemVersionStyles.Strict),
            ProjectPath = "/test/project.csproj"
        };

        return new PackageUpdate
        {
            CurrentPackage = currentPackage,
            TargetVersion = SemVersion.Parse("1.1.0", SemVersionStyles.Strict),
            AffectedProjects = new List<string> { "/test/project.csproj" },
            VulnerabilitiesFixed = new List<Vulnerability>(),
            UpdateType = UpdateType.Security
        };
    }

    private static UpdateBackup CreateTestBackup()
    {
        return new UpdateBackup
        {
            BackupId = "test123",
            CreatedAt = DateTime.UtcNow,
            Files = new List<BackupFile>
            {
                new BackupFile
                {
                    OriginalPath = "/test/project.csproj",
                    Content = "<Project />"
                }
            },
            Description = "Test backup"
        };
    }

    #endregion
}