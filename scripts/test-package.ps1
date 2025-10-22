# PowerShell script to test NuGet package locally
# This script helps verify the package before publishing

param(
    [string]$Version = "1.0.0-test",
    [string]$Configuration = "Release"
)

Write-Host "Testing NuGet package creation for Raziee.SharedKernel" -ForegroundColor Green
Write-Host "Version: $Version" -ForegroundColor Yellow
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow

# Clean previous builds
Write-Host "`nCleaning previous builds..." -ForegroundColor Blue
dotnet clean --configuration $Configuration

# Restore dependencies
Write-Host "`nRestoring dependencies..." -ForegroundColor Blue
dotnet restore

# Build solution
Write-Host "`nBuilding solution..." -ForegroundColor Blue
dotnet build --configuration $Configuration --no-restore

# Run tests
Write-Host "`nRunning tests..." -ForegroundColor Blue
dotnet test --configuration $Configuration --no-build --verbosity normal

# Update version in project file
Write-Host "`nUpdating version to $Version..." -ForegroundColor Blue
$projectFile = "src/Raziee.SharedKernel/Raziee.SharedKernel.csproj"
$content = Get-Content $projectFile -Raw
$content = $content -replace '<Version>.*</Version>', "<Version>$Version</Version>"
Set-Content $projectFile -Value $content

# Create artifacts directory
$artifactsDir = "artifacts"
if (Test-Path $artifactsDir) {
    Remove-Item $artifactsDir -Recurse -Force
}
New-Item -ItemType Directory -Path $artifactsDir

# Pack the NuGet package
Write-Host "`nCreating NuGet package..." -ForegroundColor Blue
dotnet pack src/Raziee.SharedKernel/Raziee.SharedKernel.csproj --configuration $Configuration --no-build --output $artifactsDir

# List created packages
Write-Host "`nCreated packages:" -ForegroundColor Green
Get-ChildItem $artifactsDir -Filter "*.nupkg" | ForEach-Object {
    Write-Host "  - $($_.Name)" -ForegroundColor White
}

# Test package installation
Write-Host "`nTesting package installation..." -ForegroundColor Blue
$testProjectDir = "test-package-installation"
if (Test-Path $testProjectDir) {
    Remove-Item $testProjectDir -Recurse -Force
}

New-Item -ItemType Directory -Path $testProjectDir
Set-Location $testProjectDir

# Create a test project
dotnet new console -n TestApp
Set-Location TestApp

# Install the local package
$packagePath = Join-Path (Get-Location).Parent.Parent "artifacts" "Raziee.SharedKernel.$Version.nupkg"
dotnet add package $packagePath --source (Get-Location).Parent.Parent

# Build the test project
Write-Host "`nBuilding test project with installed package..." -ForegroundColor Blue
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Package test successful!" -ForegroundColor Green
    Write-Host "The package is ready for publishing." -ForegroundColor Green
} else {
    Write-Host "`n❌ Package test failed!" -ForegroundColor Red
    Write-Host "Please check the errors above." -ForegroundColor Red
}

# Cleanup
Set-Location (Get-Location).Parent.Parent.Parent
Remove-Item $testProjectDir -Recurse -Force

Write-Host "`nTest completed. Check the artifacts directory for the created package." -ForegroundColor Cyan
