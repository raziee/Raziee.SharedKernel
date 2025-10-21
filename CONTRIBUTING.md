# Contributing to Raziee.SharedKernel

Thank you for your interest in contributing to Raziee.SharedKernel! This document provides guidelines and information for contributors.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Contributing Guidelines](#contributing-guidelines)
- [Pull Request Process](#pull-request-process)
- [Issue Reporting](#issue-reporting)
- [Coding Standards](#coding-standards)
- [Testing](#testing)
- [Documentation](#documentation)

## Code of Conduct

This project follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). By participating, you agree to uphold this code.

## Getting Started

1. Fork the repository
2. Clone your fork locally
3. Create a new branch for your feature or bugfix
4. Make your changes
5. Add tests for your changes
6. Ensure all tests pass
7. Submit a pull request

## Development Setup

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code
- Git

### Setup Steps

1. Clone the repository:
   ```bash
   git clone https://github.com/raziee/Raziee.SharedKernel.git
   cd Raziee.SharedKernel
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Build the solution:
   ```bash
   dotnet build
   ```

4. Run tests:
   ```bash
   dotnet test
   ```

## Contributing Guidelines

### Types of Contributions

We welcome the following types of contributions:

- **Bug fixes**: Fix issues in existing code
- **New features**: Add new functionality
- **Documentation**: Improve or add documentation
- **Tests**: Add or improve test coverage
- **Performance**: Optimize existing code
- **Refactoring**: Improve code quality without changing functionality

### Before You Start

1. Check existing issues and pull requests to avoid duplicates
2. Discuss major changes in an issue before implementing
3. Ensure your changes align with the project's goals and architecture

## Pull Request Process

### Before Submitting

1. **Fork and clone** the repository
2. **Create a feature branch** from `main`
3. **Make your changes** following the coding standards
4. **Add tests** for new functionality
5. **Update documentation** if needed
6. **Ensure all tests pass** and code builds successfully
7. **Run code analysis** to check for issues

### Pull Request Guidelines

1. **Use descriptive titles** that clearly explain what the PR does
2. **Provide detailed descriptions** of changes made
3. **Reference related issues** using keywords like "Fixes #123"
4. **Keep PRs focused** - one feature or bugfix per PR
5. **Include screenshots** for UI changes
6. **Update CHANGELOG.md** for significant changes

### PR Template

```markdown
## Description
Brief description of changes made.

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update
- [ ] Performance improvement
- [ ] Code refactoring

## Testing
- [ ] Tests pass locally
- [ ] New tests added for new functionality
- [ ] All existing tests pass

## Checklist
- [ ] Code follows the project's coding standards
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] CHANGELOG.md updated (if applicable)
```

## Issue Reporting

### Before Creating an Issue

1. **Search existing issues** to avoid duplicates
2. **Check if it's already fixed** in the latest version
3. **Gather information** about your environment and the issue

### Issue Template

```markdown
## Bug Report / Feature Request

### Description
Clear and concise description of the issue or feature request.

### Steps to Reproduce (for bugs)
1. Go to '...'
2. Click on '....'
3. Scroll down to '....'
4. See error

### Expected Behavior
What you expected to happen.

### Actual Behavior
What actually happened.

### Environment
- OS: [e.g., Windows 10, macOS 12, Ubuntu 20.04]
- .NET Version: [e.g., .NET 8.0]
- Raziee.SharedKernel Version: [e.g., 1.0.0]

### Additional Context
Any other context about the problem here.
```

## Coding Standards

### General Guidelines

- **Follow C# coding conventions** as defined in the project's `.editorconfig`
- **Use meaningful names** for variables, methods, and classes
- **Write self-documenting code** with clear intent
- **Keep methods small** and focused on a single responsibility
- **Use appropriate access modifiers** (public, private, protected, internal)

### Code Style

- **Use PascalCase** for public members
- **Use camelCase** for private fields and local variables
- **Use meaningful names** that describe purpose
- **Avoid abbreviations** unless they're widely understood
- **Use const** for compile-time constants
- **Use readonly** for immutable fields

### Documentation

- **Add XML documentation** for all public APIs
- **Use meaningful parameter names** in documentation
- **Include examples** for complex APIs
- **Document exceptions** that methods can throw

### Example

```csharp
/// <summary>
/// Represents a user in the system.
/// </summary>
public class User : AggregateRoot<Guid>
{
    /// <summary>
    /// Gets the user's email address.
    /// </summary>
    public string Email { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the user</param>
    /// <param name="email">The user's email address</param>
    /// <exception cref="ArgumentNullException">Thrown when email is null or empty</exception>
    public User(Guid id, string email) : base(id)
    {
        Email = email ?? throw new ArgumentNullException(nameof(email));
    }
}
```

## Testing

### Test Requirements

- **All new code must have tests**
- **Maintain or improve test coverage**
- **Tests should be fast and reliable**
- **Use descriptive test names** that explain what is being tested

### Test Structure

```csharp
[Fact]
public void MethodName_WithCondition_ShouldReturnExpectedResult()
{
    // Arrange
    var input = "test";
    var expected = "TEST";

    // Act
    var result = MethodUnderTest(input);

    // Assert
    result.Should().Be(expected);
}
```

### Test Categories

- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test component interactions
- **Performance Tests**: Test performance characteristics

## Documentation

### Documentation Standards

- **Keep documentation up to date** with code changes
- **Use clear and concise language**
- **Include code examples** where helpful
- **Use proper markdown formatting**

### Types of Documentation

- **API Documentation**: XML comments for public APIs
- **README**: Project overview and quick start
- **Architecture**: Design decisions and patterns
- **Examples**: Code samples and use cases

## Release Process

### Versioning

We follow [Semantic Versioning](https://semver.org/):
- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

### Release Checklist

- [ ] All tests pass
- [ ] Documentation updated
- [ ] CHANGELOG.md updated
- [ ] Version numbers updated
- [ ] Release notes prepared

## Getting Help

If you need help or have questions:

1. **Check the documentation** first
2. **Search existing issues** for similar problems
3. **Create a new issue** with detailed information
4. **Join discussions** in the GitHub Discussions section

## Recognition

Contributors will be recognized in:
- **CONTRIBUTORS.md** file
- **Release notes** for significant contributions
- **GitHub contributors** page

Thank you for contributing to Raziee.SharedKernel! 🚀
