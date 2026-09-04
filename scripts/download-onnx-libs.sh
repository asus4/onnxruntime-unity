#!/bin/bash

set -e -x -u

# Ensure the tag format is like 1.2.3
if [[ ! $1 =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Tag $1 is not in the correct format. It should be like `$0 1.2.3`"
    exit 1
fi

# Define Variables
TAG=$1
# WebGPU plugin EP is versioned separately: https://www.nuget.org/packages/Microsoft.ML.OnnxRuntime.EP.WebGpu
WEBGPU_EP_TAG=${2:-0.3.0}
if [[ ! $WEBGPU_EP_TAG =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "WebGPU EP tag $WEBGPU_EP_TAG is not in the correct format. It should be like `$0 1.2.3 0.3.0`"
    exit 1
fi
PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd -P)"
PLUGINS_DIR="$PROJECT_DIR/com.github.asus4.onnxruntime/Plugins"
TMP_DIR="$PROJECT_DIR/.tmp"
mkdir -p $TMP_DIR

#--------------------------------------
# Functions
#--------------------------------------

function download_nuget() {
    PACKAGE_NAME=$1
    VERSION=$2
    EXTRACT_DIR=$(echo $PACKAGE_NAME-$VERSION)

    # Skip if the directory already exists
    if [ -d $TMP_DIR/$EXTRACT_DIR ]; then
        echo "$EXTRACT_DIR already exists. Skipping download."
        return
    fi

    curl -L https://www.nuget.org/api/v2/package/$PACKAGE_NAME/$VERSION -o $TMP_DIR/$PACKAGE_NAME-$VERSION.nupkg
    mkdir -p $TMP_DIR/$EXTRACT_DIR
    unzip -o $TMP_DIR/$PACKAGE_NAME-$VERSION.nupkg -d $TMP_DIR/$EXTRACT_DIR
}


# Remove the 1DS telemetry ContentProvider and network permissions from an AAR manifest.
# Telemetry is disabled by this package (ORT_DISABLE_TELEMETRY), so the provider is never used.
# NOTE: As a result, opting in to telemetry on Android is not supported.
function strip_telemetry_manifest() {
    AAR_PATH=$1
    WORK_DIR=$TMP_DIR/aar-manifest
    rm -rf $WORK_DIR && mkdir -p $WORK_DIR
    unzip -q $AAR_PATH AndroidManifest.xml -d $WORK_DIR
    python3 - "$WORK_DIR/AndroidManifest.xml" <<'PY'
import re, sys
p = sys.argv[1]
s = open(p).read()
s = re.sub(r'\s*<uses-permission android:name="android.permission.(INTERNET|ACCESS_NETWORK_STATE)" />', '', s)
s = re.sub(r'\s*<provider\b[^>]*?/>', '', s, flags=re.S)
open(p, 'w').write(s)
PY
    (cd $WORK_DIR && zip -q $AAR_PATH AndroidManifest.xml)
}

#--------------------------------------
# ONNX Runtime
#--------------------------------------

# Download NuGet packages and place in the Unity package
# https://www.nuget.org/api/v2/package/Microsoft.ML.OnnxRuntime/{VERSION}

download_nuget Microsoft.ML.OnnxRuntime $TAG
download_nuget Microsoft.ML.OnnxRuntime.Gpu.Linux $TAG
download_nuget Microsoft.ML.OnnxRuntime.Gpu.Windows $TAG
download_nuget Microsoft.ML.OnnxRuntime.EP.WebGpu $WEBGPU_EP_TAG

EXTRACT_DIR=$(echo $TMP_DIR/Microsoft.ML.OnnxRuntime-$TAG/runtimes)

# Android
cp $EXTRACT_DIR/android/native/onnxruntime.aar $PLUGINS_DIR/Android/
strip_telemetry_manifest $PLUGINS_DIR/Android/onnxruntime.aar

# iOS XCFramework
rm -rf $PLUGINS_DIR/iOS~/onnxruntime.xcframework
mkdir -p $PLUGINS_DIR/iOS~/onnxruntime.xcframework/
unzip -o $EXTRACT_DIR/ios/native/onnxruntime.xcframework.zip -d $PLUGINS_DIR/iOS~/
ls $PLUGINS_DIR/iOS~/onnxruntime.xcframework/

# macOS
# x86_64 binary is no longer provided as of ONNX Runtime 1.24.1
# cp $EXTRACT_DIR/osx-x64/native/libonnxruntime.dylib $PLUGINS_DIR/macOS/x64/
cp $EXTRACT_DIR/osx-arm64/native/libonnxruntime.dylib $PLUGINS_DIR/macOS/arm64/

# Windows
# x64 uses the GPU package build: it includes the CPU provider and exports the CUDA / TensorRT entry points
cp $EXTRACT_DIR/win-arm64/native/*.dll $PLUGINS_DIR/Windows/arm64/
GPU_EXTRACT_DIR=$(echo $TMP_DIR/Microsoft.ML.OnnxRuntime.Gpu.Windows-$TAG/runtimes)
cp $GPU_EXTRACT_DIR/win-x64/native/onnxruntime.dll $PLUGINS_DIR/Windows/x64/
cp $GPU_EXTRACT_DIR/win-x64/native/onnxruntime_providers_shared.dll $PLUGINS_DIR/Windows/x64/

# Windows: WebGPU plugin EP (DirectML is no longer published by Microsoft)
WEBGPU_DIR=$(echo $TMP_DIR/Microsoft.ML.OnnxRuntime.EP.WebGpu-$WEBGPU_EP_TAG)
cp $WEBGPU_DIR/runtimes/win-arm64/native/*.dll $PLUGINS_DIR/Windows/arm64/
cp $WEBGPU_DIR/runtimes/win-x64/native/*.dll $PLUGINS_DIR/Windows/x64/
cp $WEBGPU_DIR/ThirdPartyNotices.txt $PROJECT_DIR/com.github.asus4.onnxruntime/ThirdPartyNotices-WebGpuEP.txt

# Linux
# arm64 is not supported by Unity
# cp $EXTRACT_DIR/linux-arm64/native/libonnxruntime.so $PLUGINS_DIR/Linux/arm64/
# x64 uses the GPU package build (same reason as Windows)
GPU_EXTRACT_DIR=$(echo $TMP_DIR/Microsoft.ML.OnnxRuntime.Gpu.Linux-$TAG/runtimes)
cp $GPU_EXTRACT_DIR/linux-x64/native/libonnxruntime.so $PLUGINS_DIR/Linux/x64/
cp $GPU_EXTRACT_DIR/linux-x64/native/libonnxruntime_providers_shared.so $PLUGINS_DIR/Linux/x64/

# Third-party notices
cp $TMP_DIR/Microsoft.ML.OnnxRuntime-$TAG/ThirdPartyNotices.txt $PROJECT_DIR/com.github.asus4.onnxruntime/ThirdPartyNotices.txt

# Microsoft.ML.OnnxRuntime.Gpu.Windows: CUDA / TensorRT providers only (providers_shared is in the core package)
EXTRACT_DIR=$(echo $TMP_DIR/Microsoft.ML.OnnxRuntime.Gpu.Windows-$TAG/runtimes)
GPU_WIN_DIR=$PROJECT_DIR/com.github.asus4.onnxruntime.win-x64-gpu/Plugins/Windows/x64
rm -f $GPU_WIN_DIR/onnxruntime_providers_shared.dll
cp $EXTRACT_DIR/win-x64/native/onnxruntime_providers_cuda.dll $GPU_WIN_DIR/
cp $EXTRACT_DIR/win-x64/native/onnxruntime_providers_tensorrt.dll $GPU_WIN_DIR/

# Microsoft.ML.OnnxRuntime.Gpu.Linux
EXTRACT_DIR=$(echo $TMP_DIR/Microsoft.ML.OnnxRuntime.Gpu.Linux-$TAG/runtimes)
GPU_LINUX_DIR=$PROJECT_DIR/com.github.asus4.onnxruntime.linux-x64-gpu/Plugins/Linux/x64
rm -f $GPU_LINUX_DIR/libonnxruntime_providers_shared.so
cp $EXTRACT_DIR/linux-x64/native/libonnxruntime_providers_cuda.so $GPU_LINUX_DIR/
cp $EXTRACT_DIR/linux-x64/native/libonnxruntime_providers_tensorrt.so $GPU_LINUX_DIR/

echo "Done."
exit 0
