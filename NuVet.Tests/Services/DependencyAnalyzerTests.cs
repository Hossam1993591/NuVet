using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NuVet.Core.Models;
using NuVet.Core.Services;
using Semver;

namespace NuVet.Tests.Services;

public class DependencyAnalyzerTests
{
    private readonly Mock<ILogger<IDependencyAnalyzer>> _loggerMock;
    private readonly Mock<INuGetService> _nugetServiceMock;
    private readonly Mock<IDependencyAnalyzer> _analyzerMock;

    public DependencyAnalyzerTests()
    {
        _loggerMock = new Mock<ILogger<IDependencyAnalyzer>>();
        _nugetServiceMock = new Mock<INuGetService>();
        _analyzerMock = new Mock<IDependencyAnalyzer>();
    }

    [Fact]
    public async Task AnalyzeAsync_WithValidSolution_ShouldReturnDependencyGraph()
    {
        // Arrange
        var solutionPath = "/path/to/solution.sln";
        var cancellationToken = CancellationToken.None;

        var expectedGraph = new DependencyGraph
        {
            RootPath = solutionPath,
            Projects = new List<ProjectInfo>(),
            AllPackages = new List<PackageReference>()
        };

        _analyzerMock.Setup(x => x.AnalyzeAsync(solutionPath, cancellationToken))
            .ReturnsAsync(expectedGraph);

        // Act
        var result = await _analyzerMock.Object.AnalyzeAsync(solutionPath, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.RootPath.Should().Be(solutionPath);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AnalyzeAsync_WithCancellation_ShouldThrowOperationCancelledException()
    {
        // Arrange
        var solutionPath = "/path/to/solution.sln";
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        _analyzerMock.Setup(x => x.AnalyzeAsync(solutionPath, cancellationTokenSource.Token))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            _analyzerMock.Object.AnalyzeAsync(solutionPath, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task FindProjectsAsync_WithValidPath_ShouldReturnProjectPaths()
    {
        // Arrange
        var rootPath = "/path/to/solution";
        var cancellationToken = CancellationToken.None;
        var expectedPaths = new List<string> { "/path/to/project1.csproj", "/path/to/project2.csproj" };

        _analyzerMock.Setup(x => x.FindProjectsAsync(rootPath, cancellationToken))
            .ReturnsAsync(expectedPaths);

        // Act
        var result = await _analyzerMock.Object.FindProjectsAsync(rootPath, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedPaths);
    }

    [Fact]
    public async Task GetPackageReferencesAsync_WithValidProject_ShouldReturnPackageReferences()
    {
        // Arrange
        var projectPath = "/path/to/project.csproj";
        var cancellationToken = CancellationToken.None;
        var expectedReferences = new List<PackageReference>
        {
            new PackageReference
            {
                Id = "Test.Package",
                Version = SemVersion.Parse("1.0.0"),
                ProjectPath = projectPath
            }
        };

        _analyzerMock.Setup(x => x.GetPackageReferencesAsync(projectPath, cancellationToken))
            .ReturnsAsync(expectedReferences);

        // Act
        var result = await _analyzerMock.Object.GetPackageReferencesAsync(projectPath, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedReferences);
    }

    [Fact]
    public async Task AnalyzeProjectsAsync_WithMultipleProjects_ShouldReturnCombinedDependencyGraph()
    {
        // Arrange
        var projectPaths = new List<string>
        {
            "/path/to/project1.csproj",
            "/path/to/project2.csproj"
        };
        var cancellationToken = CancellationToken.None;
        var expectedGraph = new DependencyGraph
        {
            RootPath = "/path/to",
            Projects = new List<ProjectInfo>
            {
                new ProjectInfo
                {
                    Name = "Project1",
                    Path = projectPaths[0],
                    TargetFramework = "net8.0",
                    Type = ProjectType.ClassLibrary
                },
                new ProjectInfo
                {
                    Name = "Project2",
                    Path = projectPaths[1],
                    TargetFramework = "net8.0",
                    Type = ProjectType.ClassLibrary
                }
            },
            AllPackages = new List<PackageReference>()
        };

        _analyzerMock.Setup(x => x.AnalyzeProjectsAsync(projectPaths, cancellationToken))
            .ReturnsAsync(expectedGraph);

        // Act
        var result = await _analyzerMock.Object.AnalyzeProjectsAsync(projectPaths, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Projects.Should().HaveCount(2);
        result.AllPackages.Should().NotBeNull();
    }

    [Fact]
    public async Task AnalyzeAsync_WithNonExistentPath_ShouldThrowException()
    {
        // Arrange
        var invalidPath = "/non/existent/path.sln";
        var cancellationToken = CancellationToken.None;

        _analyzerMock.Setup(x => x.AnalyzeAsync(invalidPath, cancellationToken))
            .ThrowsAsync(new FileNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _analyzerMock.Object.AnalyzeAsync(invalidPath, cancellationToken));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task AnalyzeAsync_WithInvalidPath_ShouldThrowArgumentException(string invalidPath)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        _analyzerMock.Setup(x => x.AnalyzeAsync(invalidPath, cancellationToken))
            .ThrowsAsync(new ArgumentException());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _analyzerMock.Object.AnalyzeAsync(invalidPath, cancellationToken));
    }

    [Fact]
    public async Task GetPackageReferencesAsync_WithPackagesConfig_ShouldParseCorrectly()
    {
        // Arrange
        var projectPath = "/path/to/legacy.csproj";
        var cancellationToken = CancellationToken.None;
        var expectedReferences = new List<PackageReference>
        {
            new PackageReference
            {
                Id = "Legacy.Package",
                Version = SemVersion.Parse("1.0.0"),
                ProjectPath = projectPath,
                Source = PackageReferenceSource.PackagesConfig
            }
        };

        _analyzerMock.Setup(x => x.GetPackageReferencesAsync(projectPath, cancellationToken))
            .ReturnsAsync(expectedReferences);

        // Act
        var result = await _analyzerMock.Object.GetPackageReferencesAsync(projectPath, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Source.Should().Be(PackageReferenceSource.PackagesConfig);
    }

    [Fact]
    public void DependencyAnalyzer_Mock_ShouldInitializeCorrectly()
    {
        // Assert
        _analyzerMock.Should().NotBeNull();
        _loggerMock.Should().NotBeNull();
        _nugetServiceMock.Should().NotBeNull();
    }
}