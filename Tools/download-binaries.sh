#!/bin/bash -xe

# Download binaries for each platform

# Ensure the tag format is like v1.2.3
if [[ ! $1 =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "VTag $1 is not in the correct format. It should be like `download-binaries.sh v1.2.3`"
    exit 1
fi

# Define Variables
PROJCET_DIR="$(cd "$(dirname "$0")/.." && pwd -P)"
PLUGINS_DIR="$PROJCET_DIR/Packages/com.github.asus4.onnxruntime/Plugins"

VTAG=$1
TAG=$(echo $VTAG | sed 's/v//g') # Remove the leading v

# Functions
function download_github_releases() {
    FILE_NAME=$1
    # Skip if the file already exists
    if [ -f $FILE_NAME ]; then
        echo "$FILE_NAME already exists. Skipping download."
        return
    fi
    # FILES
    BASE_URL=https://github.com/microsoft/onnxruntime/releases/download/$VTAG
    echo "Downloading from $BASE_URL/$FILE_NAME"
    curl -L $BASE_URL/$FILE_NAME -o $FILE_NAME
}

function extract_releases_zip() {
    SRC_DIR=$1
    TARGET_FILES=$2
    DST_DIR=$3
    unzip -o $SRC_DIR.zip
    cp $SRC_DIR/$TARGET_FILES $DST_DIR
    
    # Keep them for debugging
    # rm -rf $FILE_NAME
    # rm -rf $FILE_NAME.zip
}

function extract_releases_tgz() {
    SRC_DIR=$1
    TARGET_FILES=$2
    DST_DIR=$3
    tar -xzf $SRC_DIR.tgz
    # Copy & Resolve symbolic links
    cp -RL $SRC_DIR/$TARGET_FILES $DST_DIR
    
    # Keep them for debugging
    # rm -rf $SRC_DIR
    # rm -rf $SRC_DIR.tgz
}

# Windows x64
# download_github_releases onnxruntime-win-x64-gpu-$TAG.zip
# extract_releases_zip onnxruntime-win-x64-gpu-$TAG "lib/*.dll" $PLUGINS_DIR/Windows/x64

# macOS Universal
download_github_releases onnxruntime-osx-universal2-$TAG.tgz
extract_releases_tgz onnxruntime-osx-universal2-$TAG "lib/libonnxruntime.$TAG.dylib" $PLUGINS_DIR/macOS/libonnxruntime.dylib

# Linux x64
# download_github_releases onnxruntime-linux-x64-gpu-$TAG.tgz
# extract_releases_tgz onnxruntime-linux-x64-gpu-$TAG "lib/*.so" $PLUGINS_DIR/Linux/x64/

echo "Done."
exit 0
