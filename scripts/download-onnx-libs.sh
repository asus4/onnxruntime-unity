#!/bin/bash

set -e -x -u

# Ensure the tag format is like 1.2.3
if [[ ! $1 =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Tag $1 is not in the correct format. It should be like `$0 1.2.3`"
    exit 1
fi

# Define Variables
TAG=$1
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

#--------------------------------------
# ONNX Runtime
#--------------------------------------

# Download NuGet packages and place in the Unity package
# https://www.nuget.org/api/v2/package/Microsoft.ML.OnnxRuntime/{VERSION}

download_nuget Microsoft.ML.OnnxRuntime $TAG
download_nuget Microsoft.ML.OnnxRuntime.Gpu.Linux $TAG
download_nuget Microsoft.ML.OnnxRuntime.Gpu.Windows $TAG

EXTRACT_DIR=$(echo $TMP_DIR/Microsoft.ML.OnnxRuntime-$TAG/runtimes)

# Android
cp $EXTRACT_DIR/android/native/onnxruntime.aar $PLUGINS_DIR/Android/

# iOS XCFramework
rm -rf $PLUGINS_DIR/iOS~/onnxruntime.xcframework
mkdir -p $PLUGINS_DIR/iOS~/onnxruntime.xcframework/
unzip -o $EXTRACT_DIR/ios/native/onnxruntime.xcframework.zip -d $PLUGINS_DIR/iOS~/
ls $PLUGINS_DIR/iOS~/onnxruntime.xcframework/

# macOS
cp $EXTRACT_DIR/osx-x64/native/libonnxruntime.dylib $PLUGINS_DIR/macOS/x64/
cp $EXTRACT_DIR/osx-arm64/native/libonnxruntime.dylib $PLUGINS_DIR/macOS/arm64/

# Windows
cp $EXTRACT_DIR/win-arm64/native/*.dll $PLUGINS_DIR/Windows/arm64/
cp $EXTRACT_DIR/win-x64/native/*.dll $PLUGINS_DIR/Windows/x64/

# Linux
# arm64 is not supported by Unity
# cp $EXTRACT_DIR/linux-arm64/native/libonnxruntime.so $PLUGINS_DIR/Linux/arm64/
cp $EXTRACT_DIR/linux-x64/native/*.so $PLUGINS_DIR/Linux/x64/

# Microsoft.ML.OnnxRuntime.Gpu.Windows 
EXTRACT_DIR=$(echo $TMP_DIR/Microsoft.ML.OnnxRuntime.Gpu.Windows-$TAG/runtimes)
cp $EXTRACT_DIR/win-x64/native/onnxruntime_*.dll $PROJECT_DIR/com.github.asus4.onnxruntime.win-x64-gpu/Plugins/Windows/x64/

# Microsoft.ML.OnnxRuntime.Gpu.Linux
EXTRACT_DIR=$(echo $TMP_DIR/Microsoft.ML.OnnxRuntime.Gpu.Linux-$TAG/runtimes)
cp $EXTRACT_DIR/linux-x64/native/libonnxruntime_*.so $PROJECT_DIR/com.github.asus4.onnxruntime.linux-x64-gpu/Plugins/Linux/x64/

echo "Done."
exit 0
