using NuVet.Core.Models;
using NuVet.Core.Services;
using FluentAssertions;

namespace NuVet.Tests.Services;

public class ScanOptionsTests
{
    [Fact]
    public void ScanOptions_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new ScanOptions();

        // Assert
        options.IncludeTransitiveDependencies.Should().BeTrue();
        options.MinimumSeverity.Should().Be(VulnerabilitySeverity.Low);
        options.UseLocalCache.Should().BeTrue();
        options.CacheExpiration.Should().Be(TimeSpan.FromHours(24));
        options.ExcludePackages.Should().NotBeNull().And.BeEmpty();
        options.IncludeOnlyProjects.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ScanOptions_CanBeConfigured()
    {
        // Arrange & Act
        var options = new ScanOptions
        {
            IncludeTransitiveDependencies = false,
            MinimumSeverity = VulnerabilitySeverity.High,
            UseLocalCache = false,
            CacheExpiration = TimeSpan.FromHours(1),
            ExcludePackages = new List<string> { "System.*", "Microsoft.*" },
            IncludeOnlyProjects = new List<string> { "/src/Core/", "/src/Api/" }
        };

        // Assert
        options.IncludeTransitiveDependencies.Should().BeFalse();
        options.MinimumSeverity.Should().Be(VulnerabilitySeverity.High);
        options.UseLocalCache.Should().BeFalse();
        options.CacheExpiration.Should().Be(TimeSpan.FromHours(1));
        options.ExcludePackages.Should().Contain("System.*");
        options.IncludeOnlyProjects.Should().Contain("/src/Core/");
    }
}

public class UpdateOptionsTests
{
    [Fact]
    public void UpdateOptions_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new UpdateOptions();

        // Assert
        options.AutoApproveMinorUpdates.Should().BeTrue();
        options.AutoApprovePatchUpdates.Should().BeTrue();
        options.CreateBackup.Should().BeTrue();
        options.ValidateAfterUpdate.Should().BeTrue();
        options.RollbackOnFailure.Should().BeTrue();
        options.MinimumSeverityToUpdate.Should().Be(VulnerabilitySeverity.Low);
        options.ExcludePackages.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void UpdateOptions_CanBeConfigured()
    {
        // Arrange & Act
        var options = new UpdateOptions
        {
            AutoApproveMinorUpdates = false,
            AutoApprovePatchUpdates = true,
            CreateBackup = false,
            ValidateAfterUpdate = false,
            RollbackOnFailure = false,
            MinimumSeverityToUpdate = VulnerabilitySeverity.Critical,
            ExcludePackages = new List<string> { "LegacyPackage", "InternalTool" }
        };

        // Assert
        options.AutoApproveMinorUpdates.Should().BeFalse();
        options.AutoApprovePatchUpdates.Should().BeTrue();
        options.CreateBackup.Should().BeFalse();
        options.ValidateAfterUpdate.Should().BeFalse();
        options.RollbackOnFailure.Should().BeFalse();
        options.MinimumSeverityToUpdate.Should().Be(VulnerabilitySeverity.Critical);
        options.ExcludePackages.Should().Contain("LegacyPackage");
    }
}

public class ValidationResultTests
{
    [Fact]
    public void ValidationResult_CanBeCreatedAsValid()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            IsValid = true,
            Errors = new List<string>(),
            Warnings = new List<string> { "Package version mismatch detected" },
            ValidationDuration = TimeSpan.FromSeconds(30)
        };

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().Contain("Package version mismatch detected");
        result.ValidationDuration.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void ValidationResult_CanBeCreatedAsInvalid()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            IsValid = false,
            Errors = new List<string> { "Build failed", "Missing dependency" },
            Warnings = new List<string>(),
            ValidationDuration = TimeSpan.FromMinutes(2)
        };

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Build failed");
        result.Errors.Should().Contain("Missing dependency");
        result.Warnings.Should().BeEmpty();
        result.ValidationDuration.Should().Be(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void ValidationResult_CanHaveBothErrorsAndWarnings()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            IsValid = false,
            Errors = new List<string> { "Critical error" },
            Warnings = new List<string> { "Deprecated API usage", "Performance warning" },
            ValidationDuration = TimeSpan.FromMinutes(1)
        };

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Warnings.Should().HaveCount(2);
    }
}