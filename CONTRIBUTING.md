# Contributing to NuVet

Thank you for your interest in contributing to NuVet! This document provides guidelines and information for contributors.

## Getting Started

1. **Fork the repository**
2. **Clone your fork**:
   ```bash
   git clone https://github.com/your-username/NuVet.git
   cd NuVet
   ```
3. **Set up the development environment**:
   ```bash
   dotnet restore
   dotnet build
   dotnet test
   ```

## Development Guidelines

### Code Style
- Follow standard C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Write unit tests for new functionality

### Pull Request Process
1. Create a feature branch from `main`
2. Make your changes with appropriate tests
3. Ensure all tests pass
4. Update documentation if needed
5. Submit a pull request with a clear description

### Testing
- Write unit tests for new features
- Ensure existing tests still pass
- Test CLI commands manually
- Include integration tests where appropriate

## Architecture

### Project Structure
- `NuVet.Core`: Core business logic and models
- `NuVet.CLI`: Command-line interface
- `NuVet.Tests`: Unit and integration tests
- `Demo/`: Example project for testing

### Key Components
- **Services**: Business logic interfaces and implementations
- **Models**: Domain objects and data structures
- **Commands**: CLI command implementations

## Reporting Issues

Please use GitHub Issues to report bugs or request features. Include:
- Clear description of the issue
- Steps to reproduce
- Expected vs actual behavior
- Environment details (.NET version, OS, etc.)

## License

By contributing, you agree that your contributions will be licensed under the MIT License.