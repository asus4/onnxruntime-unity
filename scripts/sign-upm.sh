#!/bin/bash
set -euo pipefail

# Sign UPM package
# Usage:
# sign_upm <package_folder>
sign_upm() {
    echo "Signing UPM package: $1"

    # Sync README.md
    cp ./README.md "$1/README.md"

    # Export UPM package tarball
    local package_dir=$(realpath "$1")
    local tarball_dir="$(realpath .)/packages/$1"

    upm pack "$package_dir" --organization-id "$UPM_ORG_ID" --destination "$tarball_dir"
}

# Extract version from UPM package filename
# Usage:
# extract_version <package_tgz_file>
extract_version() {
    local filename=$(basename "$1")
    echo "$filename" | sed -E "s/.*-([0-9]+\.[0-9]+\.[0-9]+.*)\.tgz/\1/"
}

# Sign all UPM packages
sign_upm "com.github.asus4.onnxruntime"
sign_upm "com.github.asus4.onnxruntime-extensions"
sign_upm "com.github.asus4.onnxruntime-genai"
sign_upm "com.github.asus4.onnxruntime.linux-x64-gpu"
sign_upm "com.github.asus4.onnxruntime.unity"
sign_upm "com.github.asus4.onnxruntime.win-x64-gpu"

# Make GitHub Release Draft
VERSION=$(extract_version packages/com.github.asus4.onnxruntime/com.github.asus4.onnxruntime-*.tgz)

echo "Creating GitHub Release Draft for version: $VERSION"
gh release create "v$VERSION" \
    --title "v$VERSION" \
    --draft \
    --generate-notes \
    packages/*/*.tgz

# Publish packages manually after checking the draft release
# npm publish packages/com.github.asus4.onnxruntime*/*.tgz --tag latest

echo "Done."
exit 0
