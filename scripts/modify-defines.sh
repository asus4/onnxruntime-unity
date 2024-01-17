#!/bin/bash -xe

PROJECT_DIR="$(cd "$(dirname "$0")" && pwd -P)"
DEFINES_FILE="$PROJECT_DIR/UnityDefines.cs"
SRC_ROOT="$PROJECT_DIR/com.github.asus4.onnxruntime/Runtime"

concat_defines() {
    cat "${DEFINES_FILE}" "$1" > "$1.tmp"
    mv "$1.tmp" "$1"
}

concat_defines "${SRC_ROOT}/AssemblyInfo.shared.cs"
concat_defines "${SRC_ROOT}/NativeMethods.shared.cs"
concat_defines "${SRC_ROOT}/SessionOptions.shared.cs"

echo "Done."
