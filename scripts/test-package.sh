#!/bin/bash

# Bash script to test NuGet package locally
# This script helps verify the package before publishing

VERSION=${1:-"1.0.0-test"}
CONFIGURATION=${2:-"Release"}

echo "Testing NuGet package creation for Raziee.SharedKernel"
echo "Version: $VERSION"
echo "Configuration: $CONFIGURATION"

# Clean previous builds
echo "Cleaning previous builds..."
dotnet clean --configuration $CONFIGURATION

# Restore dependencies
echo "Restoring dependencies..."
dotnet restore

# Build solution
echo "Building solution..."
dotnet build --configuration $CONFIGURATION --no-restore

# Run tests
echo "Running tests..."
dotnet test --configuration $CONFIGURATION --no-build --verbosity normal

# Update version in project file
echo "Updating version to $VERSION..."
PROJECT_FILE="src/Raziee.SharedKernel/Raziee.SharedKernel.csproj"
sed -i.bak "s/<Version>.*<\/Version>/<Version>$VERSION<\/Version>/g" "$PROJECT_FILE"

# Create artifacts directory
ARTIFACTS_DIR="artifacts"
if [ -d "$ARTIFACTS_DIR" ]; then
    rm -rf "$ARTIFACTS_DIR"
fi
mkdir "$ARTIFACTS_DIR"

# Pack the NuGet package
echo "Creating NuGet package..."
dotnet pack src/Raziee.SharedKernel/Raziee.SharedKernel.csproj --configuration $CONFIGURATION --no-build --output "$ARTIFACTS_DIR"

# List created packages
echo "Created packages:"
ls -la "$ARTIFACTS_DIR"/*.nupkg

# Test package installation
echo "Testing package installation..."
TEST_PROJECT_DIR="test-package-installation"
if [ -d "$TEST_PROJECT_DIR" ]; then
    rm -rf "$TEST_PROJECT_DIR"
fi

mkdir "$TEST_PROJECT_DIR"
cd "$TEST_PROJECT_DIR"

# Create a test project
dotnet new console -n TestApp
cd TestApp

# Install the local package
PACKAGE_PATH="../../artifacts/Raziee.SharedKernel.$VERSION.nupkg"
dotnet add package "$PACKAGE_PATH" --source ../..

# Build the test project
echo "Building test project with installed package..."
dotnet build

if [ $? -eq 0 ]; then
    echo "✅ Package test successful!"
    echo "The package is ready for publishing."
else
    echo "❌ Package test failed!"
    echo "Please check the errors above."
fi

# Cleanup
cd ../..
rm -rf "$TEST_PROJECT_DIR"

echo "Test completed. Check the artifacts directory for the created package."
