using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Microsoft.ML.OnnxRuntime.Unity
{
    /// <summary>
    /// Provides Unity specific environment variables for ONNX Runtime
    /// </summary>
    public static class OrtUnityEnv
    {
        // OnnxRuntime-GenAI requires `ORT_LIB_PATH` environment variable to load the dynamic library
        // https://github.com/microsoft/onnxruntime-genai/blob/a763c441713ef0623a35240ec90f4311f1da35e8/src/models/onnxruntime_api.h#L230-L235
        const string OrtLibPathEnv = "ORT_LIB_PATH";

        public static void InitializeOrtLibPath()
        {
            string libPath = GetOrtLibPath();
            if (string.IsNullOrEmpty(libPath))
            {
                Debug.Log($"{OrtLibPathEnv}: Not set.");
                return;
            }

            Environment.SetEnvironmentVariable(OrtLibPathEnv, libPath);
            Debug.Log($"{OrtLibPathEnv}: {libPath}");
        }

        internal static string GetOrtLibPath()
        {
            string archPrefix = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                Architecture.Arm => "arm",
                Architecture.Arm64 => "arm64",
                _ => throw new NotSupportedException($"Unknown architecture: {RuntimeInformation.ProcessArchitecture}")
            };

            string PluginPath = Path.GetFullPath("Packages/com.github.asus4.onnxruntime/Plugins");
            return Application.platform switch
            {
                RuntimePlatform.OSXEditor => Path.Combine(PluginPath, "macOS", archPrefix, "libonnxruntime.dylib"),
                RuntimePlatform.OSXPlayer => throw new NotImplementedException(),
                RuntimePlatform.LinuxEditor => Path.Combine(PluginPath, "Linux", archPrefix, "libonnxruntime.so"),
                RuntimePlatform.LinuxPlayer => throw new NotImplementedException(),
                // No need to set ORT_LIB_PATH for other platforms
                _ => string.Empty
            };
        }
    }
}
