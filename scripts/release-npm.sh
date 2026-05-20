#!/bin/bash

# Publish packages manually after checking the draft release.
#
# Note: GPU packages (com.github.asus4.onnxruntime.win-x64-gpu /
# .linux-x64-gpu) were removed in v0.5. Users install
# Microsoft.ML.OnnxRuntime.Gpu.Windows / .Gpu.Linux through NuGetForUnity
# instead. Run `npm deprecate` on the legacy GPU NPM packages once with a
# message pointing at the new flow.
npm publish packages/com.github.asus4.onnxruntime/*.tgz --tag latest
npm publish packages/com.github.asus4.onnxruntime.unity/*.tgz --tag latest
npm publish packages/com.github.asus4.onnxruntime-extensions/*.tgz --tag latest
npm publish packages/com.github.asus4.onnxruntime-genai/*.tgz --tag latest

echo "Done."
exit 0
