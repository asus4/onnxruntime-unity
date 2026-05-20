#!/usr/bin/env bash
# Build an empty stub nupkg for Microsoft.ML.OnnxRuntime.Managed.
#
# Why: NuGetForUnity does not support transitive-dependency exclusion. When a
# user installs Microsoft.ML.OnnxRuntime via NuGetForUnity it tries to also
# pull Microsoft.ML.OnnxRuntime.Managed, which would conflict with the Unity
# fork shipped by this UPM package. By placing an empty stub nupkg in a local
# file feed and pointing packageSourceMapping at it (see Templates~/NuGet.config),
# the official Managed package is never expanded.
#
# Usage:
#   scripts/build-stub-managed.sh <version>
# Example:
#   scripts/build-stub-managed.sh 1.26.0
#
# Output: com.github.asus4.onnxruntime/NuGetStubs~/Microsoft.ML.OnnxRuntime.Managed.<version>.nupkg
#
# Requires: zip (preinstalled on macOS / Linux).
set -euo pipefail

if [[ $# -lt 1 ]]; then
    echo "Usage: $0 <version>" >&2
    exit 1
fi

VERSION="$1"
PACKAGE_ID="Microsoft.ML.OnnxRuntime.Managed"
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT_DIR="${REPO_ROOT}/com.github.asus4.onnxruntime/NuGetStubs~"
OUT_FILE="${OUT_DIR}/${PACKAGE_ID}.${VERSION}.nupkg"

mkdir -p "${OUT_DIR}"

WORK_DIR="$(mktemp -d)"
trap 'rm -rf "${WORK_DIR}"' EXIT

# .nuspec — empty dependencies, mark as stub in description.
cat > "${WORK_DIR}/${PACKAGE_ID}.nuspec" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
  <metadata>
    <id>${PACKAGE_ID}</id>
    <version>${VERSION}</version>
    <authors>asus4</authors>
    <owners>asus4</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Stub package for com.github.asus4.onnxruntime UPM. The actual managed C# code is shipped by the UPM package; this stub exists only to satisfy the Microsoft.ML.OnnxRuntime native NuGet dependency without expanding the official managed assembly.</description>
    <projectUrl>https://github.com/asus4/onnxruntime-unity</projectUrl>
    <tags>stub onnxruntime unity</tags>
    <dependencies />
  </metadata>
</package>
EOF

# Empty lib placeholder for netstandard2.0 (NuGet convention).
mkdir -p "${WORK_DIR}/lib/netstandard2.0"
: > "${WORK_DIR}/lib/netstandard2.0/_._"

# A .nupkg is a zip with these contents at the root.
rm -f "${OUT_FILE}"
(cd "${WORK_DIR}" && zip -qr "${OUT_FILE}" "${PACKAGE_ID}.nuspec" lib)

echo "Wrote ${OUT_FILE}"
