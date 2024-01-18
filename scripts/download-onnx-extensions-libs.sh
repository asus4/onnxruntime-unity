#!/bin/bash

set -e -x -u

# Download binaries for each platform
# Windows, macOS and Linux: We built them in GitHub Actions
# https://github.com/microsoft/onnxruntime
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
PLUGINS_DIR="$PROJCET_DIR/com.github.asus4.onnxruntime-extensions/Plugins"
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
    download_package $1 https://github.com/asus4/onnxruntime-unity/releases/download/v0.1.8/
}

#--------------------------------------
# ONNX Runtime
#--------------------------------------

# macOS Universal
download_github_releases onnxruntime-extensions-macos-universal-v$TAG.zip
cp $TMP_DIR/onnxruntime-extensions-macos-universal-v$TAG/libortextensions.dylib $PLUGINS_DIR/macOS/

# Windows x64
download_github_releases onnxruntime-extensions-win-x64-v$TAG.zip
cp $TMP_DIR/onnxruntime-extensions-win-x64-v$TAG/ortextensions.dll $PLUGINS_DIR/Windows/x64/

# Linux x64
download_github_releases onnxruntime-extensions-linux-x64-v$TAG.zip
cp $TMP_DIR/onnxruntime-extensions-linux-x64-v$TAG/libortextensions.so $PLUGINS_DIR/Linux/x64/

# iOS
download_package pod-archive-onnxruntime-extensions-c-$TAG.zip https://onnxruntimepackages.z14.web.core.windows.net
mkdir -p $PLUGINS_DIR/iOS~/onnxruntime_extensions.xcframework/
cp -R $TMP_DIR/pod-archive-onnxruntime-extensions-c-$TAG/onnxruntime_extensions.xcframework/* $PLUGINS_DIR/iOS~/onnxruntime_extensions.xcframework/
ls $PLUGINS_DIR/iOS~/onnxruntime_extensions.xcframework/

# Android
curl -L https://repo1.maven.org/maven2/com/microsoft/onnxruntime/onnxruntime-extensions-android/$TAG/onnxruntime-extensions-android-$TAG.aar -o $PLUGINS_DIR/Android/onnxruntime-extensions-android.aar

echo "Done."
exit 0
