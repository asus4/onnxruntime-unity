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
PROJCET_DIR="$(cd "$(dirname "$0")/.." && pwd -P)"
PLUGINS_CORE_DIR="$PROJCET_DIR/com.github.asus4.onnxruntime/Plugins"
mkdir -p .tmp
TMP_DIR="$PROJCET_DIR/.tmp"

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
    # if .tgz
    elif [[ $FILE_NAME =~ \.tgz$ ]]; then
        tar -xzf $TMP_DIR/$FILE_NAME -C $TMP_DIR/$EXTRACT_DIR
    fi
}

function download_github_releases() {
    download_package $1 https://github.com/microsoft/onnxruntime/releases/download/v$TAG
}

#--------------------------------------
# ONNX Runtime
#--------------------------------------

# macOS Universal
download_github_releases onnxruntime-osx-universal2-$TAG.tgz
cp -RL $TMP_DIR/onnxruntime-osx-universal2-$TAG/onnxruntime-osx-universal2-$TAG/lib/libonnxruntime.dylib $PLUGINS_CORE_DIR/macOS/libonnxruntime.dylib

# Windows x64
download_github_releases Microsoft.ML.OnnxRuntime.DirectML.$TAG.zip
cp $TMP_DIR/Microsoft.ML.OnnxRuntime.DirectML.$TAG/runtimes/win-x64/native/onnxruntime.dll $PLUGINS_CORE_DIR/Windows/x64/
download_github_releases onnxruntime-win-x64-gpu-$TAG.zip
cp $TMP_DIR/onnxruntime-win-x64-gpu-$TAG/onnxruntime-win-x64-gpu-$TAG/lib/onnxruntime_providers_*.dll $PROJCET_DIR/com.github.asus4.onnxruntime.win-x64-gpu/Plugins/Windows/x64/

# Linux x64
download_github_releases onnxruntime-linux-x64-gpu-$TAG.tgz
cp -RL $TMP_DIR/onnxruntime-linux-x64-gpu-$TAG/onnxruntime-linux-x64-gpu-$TAG/lib/libonnxruntime.so $PLUGINS_CORE_DIR/Linux/x64/
cp $TMP_DIR/onnxruntime-linux-x64-gpu-$TAG/onnxruntime-linux-x64-gpu-$TAG/lib/libonnxruntime_providers_*.so $PROJCET_DIR/com.github.asus4.onnxruntime.linux-x64-gpu/Plugins/Linux/x64/

# iOS
download_package pod-archive-onnxruntime-c-$TAG.zip https://onnxruntimepackages.z14.web.core.windows.net
mkdir -p $PLUGINS_CORE_DIR/iOS~/onnxruntime.xcframework/
cp -R $TMP_DIR/pod-archive-onnxruntime-c-$TAG/onnxruntime.xcframework/* $PLUGINS_CORE_DIR/iOS~/onnxruntime.xcframework/
ls $PLUGINS_CORE_DIR/iOS~/onnxruntime.xcframework/

# Android
curl -L https://repo1.maven.org/maven2/com/microsoft/onnxruntime/onnxruntime-android/$TAG/onnxruntime-android-$TAG.aar -o $PLUGINS_CORE_DIR/Android/onnxruntime-android.aar

echo "Done."
exit 0
