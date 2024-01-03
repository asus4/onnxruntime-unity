#!/bin/bash -xe

DEFINES_FILE="$(cd "$(dirname "$0")" && pwd -P)/UnityDefines.cs"
SRC_ROOT="$(cd "$(dirname "$0")/.." && pwd -P)/Packages/com.github.asus4.onnxruntime/Runtime"

concat_defines() {
    cat "${DEFINES_FILE}" "$1" > "$1.tmp"
    mv "$1.tmp" "$1"
}

concat_defines "${SRC_ROOT}/AssemblyInfo.shared.cs"
concat_defines "${SRC_ROOT}/NativeMethods.shared.cs"
concat_defines "${SRC_ROOT}/SessionOptions.shared.cs"

echo "Done."
