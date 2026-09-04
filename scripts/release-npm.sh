#!/bin/bash

# Publish packages manually after checking the draft release
npm publish packages/com.github.asus4.onnxruntime/*.tgz --tag latest
npm publish packages/com.github.asus4.onnxruntime.unity/*.tgz --tag latest
npm publish packages/com.github.asus4.onnxruntime-extensions/*.tgz --tag latest
npm publish packages/com.github.asus4.onnxruntime-genai/*.tgz --tag latest

# Large packages: 403 error might occur due to the package size. Comment them out if failed to publish.
npm publish packages/com.github.asus4.onnxruntime.linux-x64-gpu/*.tgz --tag latest
npm publish packages/com.github.asus4.onnxruntime.win-x64-gpu/*.tgz --tag latest

echo "Done."
exit 0
