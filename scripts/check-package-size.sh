#!/bin/bash

# Check npm tarball sizes before publishing.
# npm rejects large tarballs (413): a 180 MB tarball was accepted, 212 MB was rejected.
# Usage: ./scripts/check-package-size.sh [max_bytes]
# Exits 1 if a required package exceeds the limit; optional GPU packages only warn.

set -e -u

MAX_BYTES=${1:-188743680} # 180 MiB
PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd -P)"

REQUIRED_PACKAGES=(
    com.github.asus4.onnxruntime
    com.github.asus4.onnxruntime.unity
    com.github.asus4.onnxruntime-extensions
    com.github.asus4.onnxruntime-genai
)
OPTIONAL_PACKAGES=(
    com.github.asus4.onnxruntime.linux-x64-gpu
    com.github.asus4.onnxruntime.win-x64-gpu
)

function tarball_size() {
    (cd "$PROJECT_DIR/$1" && npm pack --dry-run --json 2>/dev/null \
        | node -e 'let s="";process.stdin.on("data",d=>s+=d).on("end",()=>console.log(JSON.parse(s)[0].size))')
}

STATUS=0
for PACKAGE in "${REQUIRED_PACKAGES[@]}"; do
    SIZE=$(tarball_size $PACKAGE)
    echo "$PACKAGE: $SIZE bytes"
    if [ "$SIZE" -gt "$MAX_BYTES" ]; then
        echo "::error::$PACKAGE tarball ($SIZE bytes) exceeds the npm limit ($MAX_BYTES bytes)"
        STATUS=1
    fi
done
for PACKAGE in "${OPTIONAL_PACKAGES[@]}"; do
    SIZE=$(tarball_size $PACKAGE)
    echo "$PACKAGE: $SIZE bytes"
    if [ "$SIZE" -gt "$MAX_BYTES" ]; then
        echo "::warning::$PACKAGE tarball ($SIZE bytes) exceeds the npm limit ($MAX_BYTES bytes)"
    fi
done

exit $STATUS
