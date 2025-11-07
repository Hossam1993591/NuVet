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

### Planned
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