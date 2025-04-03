#!/bin/bash

set -e -x -u

# Ensure the tag format is like 1.2.3
if [[ ! $1 =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Tag $1 is not in the correct format. It should be like `$0 1.2.3`"
    exit 1
fi

# Define Variables
TAG=$1
PROJCET_DIR="$(cd "$(dirname "$0")/.." && pwd -P)"
PLUGINS_DIR="$PROJCET_DIR/com.github.asus4.onnxruntime-genai/Plugins"
mkdir -p .tmp
TMP_DIR="$PROJCET_DIR/.tmp"

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

# Download binaries from NuGet and place in the Unity package
# https://www.nuget.org/api/v2/package/Microsoft.ML.OnnxRuntimeGenAI/{VERSION}

download_nuget Microsoft.ML.OnnxRuntimeGenAI $TAG
EXTRACT_DIR=$(echo $TMP_DIR/Microsoft.ML.OnnxRuntimeGenAI-$TAG/runtimes)
# exit 0

# macOS
cp $EXTRACT_DIR/osx-arm64/native/libonnxruntime-genai.dylib $PLUGINS_DIR/macOS/arm64/
cp $EXTRACT_DIR/osx-x64/native/libonnxruntime-genai.dylib $PLUGINS_DIR/macOS/x64/

# Windows
cp $EXTRACT_DIR/win-arm64/native/onnxruntime-genai.dll $PLUGINS_DIR/Windows/arm64/
cp $EXTRACT_DIR/win-x64/native/onnxruntime-genai.dll $PLUGINS_DIR/Windows/x64/

# Linux
# cp $EXTRACT_DIR/linux-arm64/native/libonnxruntime-genai.so $PLUGINS_DIR/Linux/arm64/
cp $EXTRACT_DIR/linux-x64/native/libonnxruntime-genai.so $PLUGINS_DIR/Linux/x64/

# Android
cp $EXTRACT_DIR/android/native/onnxruntime-genai.aar $PLUGINS_DIR/Android/

# iOS xcframework
rm -rf $PLUGINS_DIR/iOS/onnxruntime-genai.xcframework
mkdir -p $PLUGINS_DIR/iOS~/onnxruntime-genai.xcframework
unzip -o $EXTRACT_DIR/ios/native/onnxruntime-genai.xcframework.zip -d $PLUGINS_DIR/iOS/onnxruntime-genai.xcframework

echo "Done."
exit 0
