# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-11-07

### Added
- Initial release of NuVet - .NET Vulnerability Scanner
- Core vulnerability scanning functionality for .NET projects and solutions
- Support for multiple vulnerability data sources:
  - GitHub Security Advisories
  - NuGet.org vulnerability database
- Comprehensive dependency analysis with support for transitive dependencies
- VulnerabilityScanner service with configurable scan options
- VulnerabilityService for fetching and caching vulnerability data
- DependencyAnalyzer for project and solution analysis
- NuGetService for NuGet package operations
- PackageUpdater for upgrade recommendations
- Support for multiple severity levels (Critical, High, Moderate, Low, Unknown)
- Detailed vulnerability models with version range support
- Intelligent caching system for performance optimization
- Comprehensive test suite with 146 unit tests and 36% code coverage
- MIT license for open source distribution

### Features
- **Project Analysis**: Scan individual .csproj files
- **Solution Analysis**: Scan entire .sln solutions
- **Transitive Dependencies**: Analyze indirect package dependencies
- **Version Range Support**: Precise vulnerability matching using semantic versioning
- **Parallel Processing**: Efficient concurrent scanning
- **Error Handling**: Graceful degradation and detailed error reporting
- **Extensible Architecture**: Modular design for easy extension

### Technical Details
- Built on .NET 8.0 with latest language features
- Uses modern async/await patterns throughout
- Implements dependency injection for testability
- Comprehensive logging using Microsoft.Extensions.Logging
- HTTP client integration for external API calls
- JSON serialization for vulnerability data processing
- Semantic versioning support using Semver library
- MSBuild integration for project file analysis

### Documentation
- Comprehensive README with usage examples
- XML documentation for all public APIs
- Code comments and architectural documentation
- MIT license terms

## [Unreleased]

### Analysis - 2025-11-19
- **Test Coverage Assessment**: Current coverage at 35.5% (484/1360 lines)
- **Test Suite Status**: 146 tests passing successfully
- **Coverage Gaps Identified**:
  - DependencyAnalyzer: 0% coverage - needs integration tests
  - NuGetService: 0% coverage - needs API interaction tests
  - PackageUpdater: 0% coverage - needs update workflow tests
- **Models Coverage**: 91-100% coverage across all model classes ✅
- **VulnerabilityScanner**: 96.7% coverage ✅
- **VulnerabilityService**: 64.4% coverage

### Planned
- **Q1 2026 Priority**: Increase test coverage to 80%+
  - Add integration tests for DependencyAnalyzer (MSBuild project analysis)
  - Add NuGetService tests (with mocked NuGet API responses)
  - Add PackageUpdater tests (backup, restore, update workflows)
- CLI tool for command-line usage
- SARIF output format for security tools integration
- Configuration file support
- Continuous monitoring capabilities
- Performance optimizations
- Additional output formats (JSON, XML, HTML)
- Integration with CI/CD platforms
- Plugin system for custom vulnerability sources

---

## Version History

- **v1.0.0**: Initial stable release with core vulnerability scanning features