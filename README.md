# NuVet - .NET Vulnerability Scanner

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![NuGet](https://img.shields.io/nuget/v/NuVet.Core.svg)](https://www.nuget.org/packages/NuVet.Core/)
[![Build Status](https://img.shields.io/github/actions/workflow/status/nuvet/nuvet/ci.yml?branch=main)](https://github.com/nuvet/nuvet/actions)
[![Coverage](https://img.shields.io/badge/coverage-36%25-yellow.svg)](https://github.com/nuvet/nuvet/actions)

A comprehensive .NET vulnerability scanner for NuGet packages that helps identify security vulnerabilities in your dependencies.

## Features

- 🔍 **Comprehensive Scanning**: Analyzes .NET projects and solutions for vulnerable NuGet packages
- 🛡️ **Multiple Data Sources**: Integrates with GitHub Security Advisories and NuGet vulnerability data
- � **Detailed Reporting**: Provides detailed vulnerability reports with severity levels and remediation guidance
- ⚡ **Fast Performance**: Efficient scanning with intelligent caching and parallel processing
- 🔧 **Easy Integration**: Simple CLI interface and programmatic API
- 📈 **Continuous Monitoring**: Supports integration with CI/CD pipelines
- **Multiple Data Sources**: Integrates with GitHub Security Advisories and NuGet vulnerability databases
- **Dependency Analysis**: Build complete dependency graphs including transitive dependencies
- **Safe Updates**: Automatically update vulnerable packages with compatibility testing
- **Rollback Support**: Create backups before updates and rollback on failure
- **Breaking Change Detection**: Identify potential breaking changes in package updates
- **CI/CD Integration**: JSON output and exit codes for easy integration with build pipelines
- **Rich CLI Experience**: Beautiful, interactive command-line interface with progress indicators

## 📦 Installation

### Prerequisites
- .NET 8.0 SDK or later
- Windows, macOS, or Linux

### Install from Source
```bash
git clone https://github.com/wesleyscholl/NuVet.git
cd NuVet
dotnet build
dotnet pack
dotnet tool install --global --add-source ./NuVet.CLI/bin/Debug nuvet
```

### Install as .NET Tool (Coming Soon)
```bash
dotnet tool install --global nuvet
```

## 🔧 Usage

### Basic Commands

#### Scan for Vulnerabilities
```bash
# Scan current directory
nuvet scan .

# Scan specific solution
nuvet scan MyApp.sln

# Scan with minimum severity filter
nuvet scan . --min-severity High

# Output results to JSON file
nuvet scan . --output results.json --json

# Exclude specific packages
nuvet scan . --exclude "Microsoft.*,System.*"
```

#### Update Vulnerable Packages
```bash
# Update all vulnerable packages
nuvet update .

# Dry run to see what would be updated
nuvet update . --dry-run

# Auto-approve all updates
nuvet update . --auto-approve

# Update only critical and high severity vulnerabilities
nuvet update . --min-severity High

# Skip backup creation (not recommended)
nuvet update . --no-backup
```

#### Analyze Dependencies
```bash
# Analyze project dependencies
nuvet analyze .

# Show transitive dependencies
nuvet analyze . --show-transitive

# Display as tree format
nuvet analyze . --tree

# Save dependency graph to file
nuvet analyze . --output deps.json
```

### Command Options

#### Scan Command
```
nuvet scan <path> [options]

Arguments:
  <path>                  Path to solution, project, or directory to scan

Options:
  --output <file>         Output file path for results (JSON format)
  --min-severity <level>  Minimum severity level to report [Low|Moderate|High|Critical]
  --include-transitive    Include transitive dependencies (default: true)
  --exclude <packages>    Package names to exclude from scan
  --json                  Output results in JSON format
  --verbose              Enable verbose logging
```

#### Update Command
```
nuvet update <path> [options]

Arguments:
  <path>                  Path to solution, project, or directory to update

Options:
  --dry-run              Show what would be updated without making changes
  --auto-approve         Automatically approve all updates
  --min-severity <level> Minimum severity to update [Low|Moderate|High|Critical]
  --exclude <packages>   Package names to exclude from updates
  --no-backup           Skip creating backups before updates
  --no-validation       Skip build validation after updates
  --verbose             Enable verbose logging
```

#### Analyze Command
```
nuvet analyze <path> [options]

Arguments:
  <path>                  Path to solution, project, or directory to analyze

Options:
  --output <file>         Output file path for dependency graph (JSON format)
  --show-transitive      Show transitive dependencies
  --tree                 Display dependencies in tree format
  --verbose              Enable verbose logging
```

## 🛡️ Security Features

### Vulnerability Sources
- **GitHub Security Advisories**: Comprehensive vulnerability database
- **NuGet.org Vulnerability Database**: Official NuGet security advisories
- **Manual Override Support**: Configure custom vulnerability sources

### Update Safety
- **Automatic Backups**: All project files are backed up before updates
- **Build Validation**: Verify projects still build after updates
- **Rollback on Failure**: Automatically restore backups if updates fail
- **Breaking Change Detection**: Identify potential breaking changes
- **Semantic Versioning Awareness**: Respect SemVer update policies

### CI/CD Integration
```yaml
# GitHub Actions example
- name: Scan for vulnerabilities
  run: nuvet scan . --json --output vulns.json
  
- name: Check for critical vulnerabilities
  run: |
    if nuvet scan . --min-severity Critical --json | jq '.vulnCount > 0'; then
      echo "Critical vulnerabilities found!"
      exit 1
    fi
```

## 📊 Output Examples

### Scan Results
```
────────────────── NuVet - Vulnerability Scanner ──────────────────

Scanning: ./MyApp.sln
Min Severity: Low
Include Transitive: True

┌───────────────────────┬───────┐
│ Metric                │ Count │
├───────────────────────┼───────┤
│ Projects Scanned      │ 5     │
│ Packages Analyzed     │ 127   │
│ Vulnerable Packages   │ 3     │
│ Total Vulnerabilities │ 8     │
└───────────────────────┴───────┘

┌──────────┬───────┐
│ Severity │ Count │
├──────────┼───────┤
│ Critical │ 2     │
│ High     │ 3     │
│ Moderate │ 2     │
│ Low      │ 1     │
└──────────┴───────┘

─────────────────────── Vulnerable Packages ───────────────────────

Newtonsoft.Json 12.0.3
  Highest Severity: High
  Affected Projects: 3
  Suggested Updates: 13.0.1, 13.0.2, 13.0.3
    • High: Improper Handling of Exceptional Conditions
    • Moderate: Uncontrolled Resource Consumption
```

### JSON Output
```json
{
  "solutionPath": "./MyApp.sln",
  "scanDate": "2025-01-15T10:30:00Z",
  "summary": {
    "totalProjects": 5,
    "totalPackages": 127,
    "vulnerablePackages": 3,
    "criticalVulnerabilities": 2,
    "highVulnerabilities": 3,
    "moderateVulnerabilities": 2,
    "lowVulnerabilities": 1
  },
  "vulnerablePackages": [...]
}
```

## 🏗️ Architecture

### Core Components
- **NuVet.Core**: Core domain models and services
- **NuVet.CLI**: Command-line interface
- **NuVet.Tests**: Comprehensive test suite

### Key Services
- **VulnerabilityScanner**: Orchestrates vulnerability detection
- **DependencyAnalyzer**: Builds dependency graphs from project files
- **VulnerabilityService**: Integrates with vulnerability databases
- **PackageUpdater**: Safely updates packages with rollback support
- **NuGetService**: Interfaces with NuGet repositories

### Technology Stack
- **.NET 8.0**: Modern .NET platform
- **System.CommandLine**: Advanced CLI framework
- **Spectre.Console**: Rich console UI
- **NuGet.Protocol**: Official NuGet API client
- **MSBuild APIs**: Project file parsing
- **Semantic Versioning**: Version comparison and analysis

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Development Setup
```bash
git clone https://github.com/wesleyscholl/NuVet.git
cd NuVet
dotnet restore
dotnet build
dotnet test
```

### Running Tests
```bash
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- GitHub Security Advisories for vulnerability data
- NuGet team for the excellent NuGet.Protocol APIs
- Spectre.Console for the beautiful CLI experience
- All contributors and users of this project

## 🔗 Related Projects

- [dotnet-outdated](https://github.com/dotnet-outdated/dotnet-outdated) - Find outdated packages
- [Snyk](https://snyk.io/) - Commercial vulnerability scanning
- [OWASP Dependency Check](https://owasp.org/www-project-dependency-check/) - Generic dependency scanning

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/wesleyscholl/NuVet/issues)
- **Discussions**: [GitHub Discussions](https://github.com/wesleyscholl/NuVet/discussions)
- **Documentation**: [Wiki](https://github.com/wesleyscholl/NuVet/wiki)

---

**Made with ❤️ for the .NET community**