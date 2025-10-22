# Release Guide for Raziee.SharedKernel

## Quick Start

### 1. Set up GitHub Secrets

Go to your repository settings and add the following secret:
- **NUGET_API_KEY**: Your NuGet.org API key

### 2. Create a Release

```bash
# Update version in src/Raziee.SharedKernel/Raziee.SharedKernel.csproj
# Then create and push a tag
git tag v1.0.0
git push origin v1.0.0
```

### 3. Monitor the Process

1. Go to the "Actions" tab in GitHub
2. Watch the "Build, Test and Publish NuGet Package" workflow
3. The package will be automatically published to NuGet.org
4. A GitHub release will be created

## Package Information

- **Package ID**: `Raziee.SharedKernel`
- **Current Version**: 1.0.0
- **Target Framework**: .NET 8.0
- **License**: MIT
- **Repository**: https://github.com/raziee/Raziee.SharedKernel

## Installation

```bash
dotnet add package Raziee.SharedKernel
```

## Features Included

- ✅ Domain-Driven Design (DDD) abstractions
- ✅ CQRS pattern implementation
- ✅ Repository pattern with Entity Framework support
- ✅ Domain events and event dispatching
- ✅ Specification pattern
- ✅ Unit of Work pattern
- ✅ Multi-tenancy support
- ✅ Modular monolith architecture
- ✅ Comprehensive test coverage (78 tests)
- ✅ Full documentation

## Workflow Files Created

1. **`.github/workflows/publish-nuget.yml`** - Main publishing workflow
2. **`.github/workflows/dotnet.yml`** - Build and test workflow
3. **`.github/ISSUE_TEMPLATE/release-checklist.md`** - Release checklist
4. **`.github/pull_request_template.md`** - PR template

## Scripts for Local Testing

- **`scripts/test-package.ps1`** - PowerShell script for Windows
- **`scripts/test-package.sh`** - Bash script for Linux/macOS

## Documentation

- **`docs/nuget-publishing.md`** - Complete publishing guide
- **`docs/getting-started.md`** - Getting started guide
- **`docs/architecture.md`** - Architecture documentation

## Next Steps

1. **Configure GitHub Secrets** (NUGET_API_KEY)
2. **Test locally** using the provided scripts
3. **Create your first release** by pushing a tag
4. **Monitor the workflow** in GitHub Actions
5. **Verify the package** on NuGet.org

## Support

For issues or questions:
- Create an issue in the GitHub repository
- Check the documentation in the `docs/` folder
- Review the GitHub Actions logs for build issues

---

**Ready to publish!** 🚀

The package is now fully configured for automatic publishing to NuGet.org via GitHub Actions.
