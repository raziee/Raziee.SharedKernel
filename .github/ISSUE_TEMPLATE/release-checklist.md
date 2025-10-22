---
name: Release Checklist
about: Checklist for creating a new release
title: 'Release v[VERSION]'
labels: 'release'
assignees: ''

---

# Release Checklist for v[VERSION]

## Pre-Release

- [ ] All tests are passing
- [ ] Code review completed
- [ ] Documentation updated
- [ ] Breaking changes documented
- [ ] Migration guide created (if needed)
- [ ] Performance impact assessed
- [ ] Security review completed

## Release Process

- [ ] Version number updated in project file
- [ ] CHANGELOG.md updated
- [ ] Release notes prepared
- [ ] Tag created: `git tag v[VERSION]`
- [ ] Tag pushed: `git push origin v[VERSION]`
- [ ] GitHub Actions workflow triggered
- [ ] NuGet package published successfully
- [ ] GitHub release created

## Post-Release

- [ ] Package installation tested
- [ ] Documentation links verified
- [ ] Community notified (if applicable)
- [ ] Monitoring setup for package downloads
- [ ] Issue tracking for user feedback

## Release Notes Template

```markdown
## What's New in v[VERSION]

### 🚀 New Features
- Feature 1
- Feature 2

### 🐛 Bug Fixes
- Fix 1
- Fix 2

### 🔧 Improvements
- Improvement 1
- Improvement 2

### 📚 Documentation
- Updated documentation for new features
- Added migration guide

### 🏗️ Breaking Changes
- Breaking change 1 (with migration guide)
- Breaking change 2 (with migration guide)

## Installation

```bash
dotnet add package Raziee.SharedKernel
```

## Migration Guide

[Link to migration guide if applicable]

## Full Changelog

[Link to full changelog]
```
