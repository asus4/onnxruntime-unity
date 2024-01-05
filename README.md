# ONNX Runtime Plugin for Unity

[![npm](https://img.shields.io/npm/v/com.github.asus4.onnxruntime?label=npm)](https://www.npmjs.com/package/com.github.asus4.onnxruntime)

Pre-built ONNX Runtime libraries for Unity.

## [See Examples](https://github.com/asus4/onnxruntime-unity-examples)

[https://github.com/asus4/onnxruntime-unity-examples](https://github.com/asus4/onnxruntime-unity-examples)

<https://github.com/asus4/onnxruntime-unity-examples/assets/357497/96ed9913-41b7-401d-a634-f0e2de4fc3c7>

## Tested environment

- Unity: 2022.3.16f1 (LTS)
- ONNX Runtime: 1.16.3

### Execution Providers

ONNX Runtime supports running on hardware acceleration libraries. See [official docs](https://onnxruntime.ai/docs/execution-providers/).

| Platform | CPU | CoreML | NNAPI | CUDA | TensorRT | DirectML | XNNPACK |
| --- | --- | --- | --- | --- | --- | --- | --- |
| macOS | :white_check_mark: | :white_check_mark: | | | | | |
| iOS | :white_check_mark: | :white_check_mark: | | | | | :construction: |
| Android | :white_check_mark: | | :white_check_mark: | | | | :construction: |
| Windows | :white_check_mark: | | | :construction: | :construction: | :white_check_mark: | |
| Linux | :white_check_mark: | | | :construction: | :construction: | | |

:white_check_mark: : Supported in Unity Core library  
:construction: : Experimental Preview

## How to Install

Pre-built libraries are available on [NPM](https://www.npmjs.com/package/com.github.asus4.onnxruntime). Add the following `scopedRegistries` and `dependencies` in `Packages/manifest.json`.

```json
  "scopedRegistries": [
    {
      "name": "NPM",
      "url": "https://registry.npmjs.com",
      "scopes": [
        "com.github.asus4"
      ]
    }
  ]
  "dependencies": {
    // Core library
    "com.github.asus4.onnxruntime": "0.1.4",
    // (Optional) Utilities for Unity
    "com.github.asus4.onnxruntime.unity": "0.1.4",
    // (Optional) GPU provider extensions for Windows/Linux (each 300mb+)
    // CPU for Windows/Linux is included in core library
    "com.github.asus4.onnxruntime.win-x64-gpu": "0.1.4",
    "com.github.asus4.onnxruntime.linux-x64-gpu": "0.1.4",
    ... other dependencies
  }
```

## Links for libraries

- [macOS](https://github.com/microsoft/onnxruntime/releases/)
- [Android](https://central.sonatype.com/artifact/com.microsoft.onnxruntime/onnxruntime-android/versions)
- [iOS](https://github.com/CocoaPods/Specs/tree/master/Specs/3/a/a/onnxruntime-c)
