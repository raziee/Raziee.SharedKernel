# NuGet Package Publishing Guide

This guide explains how to publish the Raziee.SharedKernel package to NuGet.org using GitHub Actions.

## Prerequisites

1. **NuGet.org Account**: Create an account at [nuget.org](https://www.nuget.org/)
2. **API Key**: Generate an API key from your NuGet.org account
3. **GitHub Repository**: Ensure your repository is properly configured

## GitHub Secrets Setup

To enable automatic publishing, you need to configure the following secrets in your GitHub repository:

### Required Secrets

1. **NUGET_API_KEY**
   - Go to your GitHub repository
   - Navigate to Settings → Secrets and variables → Actions
   - Click "New repository secret"
   - Name: `NUGET_API_KEY`
   - Value: Your NuGet.org API key

### How to Get NuGet API Key

1. Log in to [nuget.org](https://www.nuget.org/)
2. Go to your account settings
3. Navigate to "API Keys" section
4. Click "Create" to generate a new API key
5. Copy the generated key (you won't be able to see it again)

## Publishing Process

### Automatic Publishing

The package is automatically published when you create a Git tag that starts with `v`:

```bash
# Create and push a tag
git tag v1.0.0
git push origin v1.0.0
```

### Manual Publishing

You can also trigger the workflow manually:

1. Go to the "Actions" tab in your GitHub repository
2. Select "Build, Test and Publish NuGet Package"
3. Click "Run workflow"
4. Choose the branch and click "Run workflow"

## Workflow Details

The publishing workflow includes:

1. **Build and Test**: Runs on multiple operating systems
2. **Security Scan**: Checks for vulnerabilities
3. **Package Creation**: Creates the NuGet package
4. **Publishing**: Publishes to NuGet.org
5. **Release Creation**: Creates a GitHub release

## Version Management

### Semantic Versioning

The package follows [Semantic Versioning](https://semver.org/):

- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

### Version Updates

To update the version:

1. Update the version in `src/Raziee.SharedKernel/Raziee.SharedKernel.csproj`
2. Create a new tag: `git tag v1.1.0`
3. Push the tag: `git push origin v1.1.0`

## Package Information

### Current Package Details

- **Package ID**: `Raziee.SharedKernel`
- **Authors**: Raziee
- **License**: MIT
- **Repository**: https://github.com/raziee/Raziee.SharedKernel
- **Documentation**: https://github.com/raziee/Raziee.SharedKernel/blob/main/docs/

### Package Contents

The package includes:

- Core DDD abstractions
- CQRS pattern implementation
- Repository pattern
- Domain events
- Specification pattern
- Unit of Work pattern
- Multi-tenancy support
- Modular monolith architecture

## Installation

Users can install the package using:

```bash
dotnet add package Raziee.SharedKernel
```

Or via Package Manager Console:

```powershell
Install-Package Raziee.SharedKernel
```

## Troubleshooting

### Common Issues

1. **API Key Issues**
   - Ensure the API key is correctly set in GitHub secrets
   - Verify the API key has publish permissions on NuGet.org

2. **Version Conflicts**
   - Check if the version already exists on NuGet.org
   - Ensure version follows semantic versioning

3. **Build Failures**
   - Check the GitHub Actions logs for specific errors
   - Ensure all tests are passing before publishing

### Support

For issues related to publishing:

1. Check the GitHub Actions logs
2. Verify all secrets are correctly configured
3. Ensure the repository has proper permissions

## Best Practices

1. **Always test locally** before creating a release tag
2. **Use semantic versioning** for version numbers
3. **Update documentation** with each release
4. **Keep release notes** up to date
5. **Monitor package downloads** and user feedback

## Release Checklist

Before creating a new release:

- [ ] All tests are passing
- [ ] Documentation is updated
- [ ] Version number is incremented
- [ ] Release notes are prepared
- [ ] Local testing is completed
- [ ] GitHub secrets are configured
- [ ] NuGet.org API key is valid
