#!/bin/bash

set -e -x -u

# Download binaries for each platform
# Windows, macOS and Linux: GitHub releases
# https://github.com/microsoft/onnxruntime/releases
# iOS:
# https://cocoapods.org/
# Android:
# https://mvnrepository.com/artifact/com.microsoft.onnxruntime
# https://repo1.maven.org/maven2/com/microsoft/onnxruntime/

# Ensure the tag format is like 1.2.3
if [[ ! $1 =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Tag $1 is not in the correct format. It should be like `$0 1.2.3`"
    exit 1
fi

# Define Variables
TAG=$1
PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd -P)"
PLUGINS_DIR="$PROJECT_DIR/com.github.asus4.onnxruntime/Plugins"
mkdir -p .tmp
TMP_DIR="$PROJECT_DIR/.tmp"

#--------------------------------------
# Functions
#--------------------------------------

# Download and extract
function download_package() {
    FILE_NAME=$1
    EXTRACT_DIR=$(echo $FILE_NAME | sed 's/\.[^.]*$//') # Remove the extension
    BASE_URL=$2

    # Skip if the directory already exists
    if [ -d $TMP_DIR/$EXTRACT_DIR ]; then
        echo "$EXTRACT_DIR already exists. Skipping download."
        return
    fi

    # FILES
    echo "Downloading from $BASE_URL/$FILE_NAME"
    mkdir -p $TMP_DIR/$EXTRACT_DIR
    curl -L $BASE_URL/$FILE_NAME -o $TMP_DIR/$FILE_NAME
    # If .zip
    if [[ $FILE_NAME =~ \.zip$ ]]; then
        # unzip into tmp folder if 
        unzip -o $TMP_DIR/$FILE_NAME -d $TMP_DIR/$EXTRACT_DIR
    # if .nupkg
    elif [[ $FILE_NAME =~ \.nupkg$ ]]; then
        unzip -o $TMP_DIR/$FILE_NAME -d $TMP_DIR/$EXTRACT_DIR
    # if .tgz
    elif [[ $FILE_NAME =~ \.tgz$ ]]; then
        tar -xzf $TMP_DIR/$FILE_NAME -C $TMP_DIR/$EXTRACT_DIR
    fi
}

function download_github_releases() {
    download_package $1 https://github.com/microsoft/onnxruntime/releases/download/v$TAG
}

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
download_nuget Microsoft.ML.OnnxRuntime.DirectML $TAG
download_nuget Microsoft.ML.OnnxRuntime.Gpu.Linux $TAG
download_nuget Microsoft.ML.OnnxRuntime.Gpu.Windows $TAG

EXTRACT_DIR=$(echo $TMP_DIR/Microsoft.ML.OnnxRuntime-$TAG/runtimes)

# Android
cp $EXTRACT_DIR/android/native/onnxruntime.aar $PLUGINS_DIR/Android/

# iOS
rm -rf $PLUGINS_DIR/iOS/onnxruntime.xcframework
mkdir -p $PLUGINS_DIR/iOS/onnxruntime.xcframework/
unzip -o $EXTRACT_DIR/ios/native/onnxruntime.xcframework.zip -d $PLUGINS_DIR/iOS/
ls $PLUGINS_DIR/iOS/onnxruntime.xcframework/

# macOS
cp $EXTRACT_DIR/osx-x64/native/libonnxruntime.dylib $PLUGINS_DIR/macOS/x64/
cp $EXTRACT_DIR/osx-arm64/native/libonnxruntime.dylib $PLUGINS_DIR/macOS/arm64/

# Windows
# Microsoft.ML.OnnxRuntime.DirectML for default OnnxRuntime
EXTRACT_DIR=$(echo $TMP_DIR/Microsoft.ML.OnnxRuntime.DirectML-$TAG/runtimes)
cp $EXTRACT_DIR/win-arm64/native/onnxruntime.dll $PLUGINS_DIR/Windows/arm64/
cp $EXTRACT_DIR/win-x64/native/onnxruntime.dll $PLUGINS_DIR/Windows/x64/
cp $EXTRACT_DIR/win-x86/native/onnxruntime.dll $PLUGINS_DIR/Windows/x86/
exit 0

download_github_releases onnxruntime-win-x64-gpu-$TAG.zip
cp $TMP_DIR/onnxruntime-win-x64-gpu-$TAG/onnxruntime-win-x64-gpu-$TAG/lib/onnxruntime_providers_*.dll $PROJECT_DIR/com.github.asus4.onnxruntime.win-x64-gpu/Plugins/Windows/x64/

# Linux x64
download_github_releases onnxruntime-linux-x64-gpu-$TAG.tgz
cp -RL $TMP_DIR/onnxruntime-linux-x64-gpu-$TAG/onnxruntime-linux-x64-gpu-$TAG/lib/libonnxruntime.so $PLUGINS_DIR/Linux/x64/
cp $TMP_DIR/onnxruntime-linux-x64-gpu-$TAG/onnxruntime-linux-x64-gpu-$TAG/lib/libonnxruntime_providers_*.so $PROJECT_DIR/com.github.asus4.onnxruntime.linux-x64-gpu/Plugins/Linux/x64/

echo "Done."
exit 0
