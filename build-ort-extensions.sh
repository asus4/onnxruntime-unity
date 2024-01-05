#!/bin/bash

set -e -x -u

# Modified based on build.sh (MIT license)
# https://github.com/microsoft/onnxruntime-extensions/blob/main/build.sh

# Currently only supports macOS

ROOT_DIR="$(cd "$(dirname "$0")" && pwd -P)"
ORT_EXTENSIONS_DIR=$(cd "$ROOT_DIR/../onnxruntime-extensions" && pwd -P)
cd $ORT_EXTENSIONS_DIR

OSNAME=$(uname -s)
if [ -z ${CPU_NUMBER+x} ]; then
  if [[ "$OSNAME" == "Darwin" ]]; then
    CPU_NUMBER=$(sysctl -n hw.logicalcpu)
  else
    CPU_NUMBER=$(nproc)
  fi
fi

BUILD_FLAVOR=RelWithDebInfo

function build_with_arch() {
    ARCH=$1
    target_dir=out/$OSNAME/$ARCH/$BUILD_FLAVOR
    mkdir -p "$target_dir" && cd "$target_dir"
    cmake -D CMAKE_OSX_ARCHITECTURES=$ARCH "$@" ../../../.. && cmake --build . --config $BUILD_FLAVOR  --parallel "${CPU_NUMBER}"
    lipo -info $ORT_EXTENSIONS_DIR/$target_dir/lib/libortextensions.dylib
    cd $ORT_EXTENSIONS_DIR
}

build_with_arch x86_64
build_with_arch arm64

lipo -create -output $ROOT_DIR/libortextensions.dylib $ORT_EXTENSIONS_DIR/out/$OSNAME/x86_64/$BUILD_FLAVOR/lib/libortextensions.dylib $ORT_EXTENSIONS_DIR/out/$OSNAME/arm64/$BUILD_FLAVOR/lib/libortextensions.dylib