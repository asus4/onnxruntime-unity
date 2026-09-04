#!/bin/bash

# Publish packages manually after checking the draft release
# Packages larger than the npm limit are skipped. See scripts/check-package-size.sh
MAX_TARBALL_BYTES=188743680 # 180 MiB

function publish() {
    TGZ=$(ls packages/$1/*.tgz)
    SIZE=$(wc -c < "$TGZ")
    if [ "$SIZE" -gt "$MAX_TARBALL_BYTES" ]; then
        echo "Skip $1: tarball ($SIZE bytes) exceeds the npm limit ($MAX_TARBALL_BYTES bytes)"
        return
    fi
    npm publish "$TGZ" --tag latest
}

publish com.github.asus4.onnxruntime
publish com.github.asus4.onnxruntime.unity
publish com.github.asus4.onnxruntime-extensions
publish com.github.asus4.onnxruntime-genai
publish com.github.asus4.onnxruntime.linux-x64-gpu
publish com.github.asus4.onnxruntime.win-x64-gpu

echo "Done."
exit 0
