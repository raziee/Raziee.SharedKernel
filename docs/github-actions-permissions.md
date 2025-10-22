# GitHub Actions Permissions Guide

## Overview

This document explains the permissions required for the GitHub Actions workflows in this repository and how to configure them properly.

## Required Permissions

### For Build and Test Workflow (`.github/workflows/dotnet.yml`)

```yaml
permissions:
  contents: read      # Read repository contents
  packages: read      # Read packages (for dependencies)
  security-events: write  # Write security scan results
```

### For Publish Workflow (`.github/workflows/publish-nuget.yml`)

```yaml
permissions:
  contents: write     # Write to repository (create releases)
  packages: write     # Write packages (publish to NuGet)
  id-token: write     # Generate ID tokens for authentication
```

## Setting Up Permissions

### 1. Repository Settings

1. Go to your repository on GitHub
2. Navigate to **Settings** → **Actions** → **General**
3. Under **Workflow permissions**, select:
   - ✅ **Read and write permissions**
   - ✅ **Allow GitHub Actions to create and approve pull requests**

### 2. Required Secrets

Make sure you have the following secrets configured:

#### NUGET_API_KEY
- **Purpose**: Authenticate with NuGet.org for package publishing
- **How to get**: 
  1. Go to [nuget.org](https://www.nuget.org/)
  2. Sign in to your account
  3. Go to **Account Settings** → **API Keys**
  4. Click **Create** to generate a new API key
  5. Copy the generated key (you won't see it again)

#### GITHUB_TOKEN (Automatic)
- **Purpose**: Automatically provided by GitHub Actions
- **Permissions**: Based on the workflow permissions defined above
- **Note**: No manual setup required

### 3. Repository Permissions

Ensure your repository has the following settings:

1. **Actions**: Enabled
2. **Packages**: Enabled (for publishing NuGet packages)
3. **Releases**: Enabled (for creating GitHub releases)

## Troubleshooting Common Issues

### "Resource not accessible by integration"

This error occurs when the workflow doesn't have sufficient permissions. Solutions:

1. **Check workflow permissions**: Ensure the `permissions` section is correctly defined
2. **Repository settings**: Verify that Actions have write permissions
3. **Token scope**: Ensure the GITHUB_TOKEN has the required scopes

### "Permission denied" errors

1. **Check repository settings**: Go to Settings → Actions → General
2. **Verify workflow permissions**: Ensure the workflow has the required permissions
3. **Check branch protection**: Ensure the workflow can write to the repository

### "API key invalid" errors

1. **Verify NUGET_API_KEY**: Check that the secret is correctly set
2. **Check API key permissions**: Ensure the key has publish permissions on NuGet.org
3. **Regenerate key**: If needed, create a new API key

## Security Considerations

### Principle of Least Privilege

The workflows are configured with minimal required permissions:

- **Build workflow**: Only read access to contents and packages
- **Publish workflow**: Write access only when publishing releases

### Token Security

- **GITHUB_TOKEN**: Automatically scoped to the repository
- **NUGET_API_KEY**: Store securely in repository secrets
- **No hardcoded credentials**: All sensitive data is stored in secrets

## Workflow Permissions Explained

### contents: read/write
- **read**: Access repository files for building and testing
- **write**: Create releases and update repository content

### packages: read/write
- **read**: Download NuGet packages for building
- **write**: Publish packages to NuGet.org

### security-events: write
- **write**: Report security scan results to GitHub Security tab

### id-token: write
- **write**: Generate OIDC tokens for authentication with external services

## Verification

To verify that permissions are working correctly:

1. **Run the build workflow**: Should complete without permission errors
2. **Check security scans**: Should appear in the Security tab
3. **Test package creation**: Should generate packages without errors
4. **Verify release creation**: Should create GitHub releases successfully

## Support

If you encounter permission issues:

1. Check the GitHub Actions logs for specific error messages
2. Verify repository settings and secrets
3. Ensure the workflow has the required permissions
4. Check GitHub's documentation for the latest permission requirements

## References

- [GitHub Actions Permissions](https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions#permissions)
- [NuGet API Keys](https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package)
- [GitHub Actions Security](https://docs.github.com/en/actions/security-guides/security-hardening-for-github-actions)
