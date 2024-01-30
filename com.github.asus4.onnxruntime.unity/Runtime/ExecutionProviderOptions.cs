using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.ML.OnnxRuntime.Unity
{
    public enum ExecutionProviderPriority
    {
        /// <summary>
        /// Default CPU
        /// </summary>
        None = 0,
        /// <summary>
        /// Choose GPU EP for each platform
        /// </summary>
        PlatformGPU = 1,
        /// <summary>
        /// XNNPACK EP
        /// </summary>
        XNNPACK = 2,
    }

    [Serializable]
    public class ExecutionProviderOptions
    {
        [Tooltip("Priorities of Execution Provider")]
        public ExecutionProviderPriority[] executionProviderPriorities =
        {
            ExecutionProviderPriority.PlatformGPU,
            ExecutionProviderPriority.XNNPACK
        };

        /// <summary>
        /// Create a session options with Execution Provider
        /// </summary>
        /// <param name="options">A session options</param>
        /// <returns>Created session options, which must be disposed</returns>
        public void AppendExecutionProviders(SessionOptions options)
        {

            foreach (var provider in executionProviderPriorities)
            {
                try
                {
                    AddExecutionProvider(options, provider);
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to initialize GPU Execution Provider: {e.Message}");
                }
            }
        }


        /// <summary>
        /// Append XNNPACK provider. Available on Android/iOS for now.
        /// </summary>
        /// <param name="options"></param>
        public void AppendXNNPackProvider(SessionOptions options)
        {
            // See recommended configuration for XNNPACK
            // https://onnxruntime.ai/docs/execution-providers/Xnnpack-ExecutionProvider.html#recommended-configuration

            options.AddSessionConfigEntry("session.intra_op.allow_spinning", "0");

            // Threads for XNNPACK
            int threads = Math.Clamp(SystemInfo.processorCount, 1, 4);
            options.AppendExecutionProvider("XNNPACK", new Dictionary<string, string>()
            {
                { "intra_op_num_threads", threads.ToString()},
            });

            options.IntraOpNumThreads = 1;
        }

        /// <summary>
        /// Automatically find recommended GPU execution provider for the platform
        /// </summary>
        /// <param name="platform">A runtime platform</param>
        /// <param name="options">A session options</param>
        public void AppendPlatformExecutionProvider(RuntimePlatform platform, SessionOptions options)
        {
            // Debug.Log($"Graphics device type: {SystemInfo.graphicsDeviceType}");

            switch (platform)
            {
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXServer:
                case RuntimePlatform.IPhonePlayer:
                    Debug.Log("CoreML is enabled");
                    options.AppendExecutionProvider_CoreML(
                        CoreMLFlags.COREML_FLAG_ENABLE_ON_SUBGRAPH);
                    break;
                case RuntimePlatform.Android:
                    Debug.Log("NNAPI is enabled");
                    options.AppendExecutionProvider_Nnapi(
                        // NNApi can fallback to CPU if GPU is not available.
                        // But in general, it will be slower than OnnxRuntime CPU inference.
                        // Thus, we disable CPU fallback.
                        // It throws an exception if GPU is not available.
                        NnapiFlags.NNAPI_FLAG_USE_FP16 | NnapiFlags.NNAPI_FLAG_CPU_DISABLED);
                    break;
#if ORT_GPU_PROVIDER_WIN
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsServer:
                    Debug.Log("TensorRT is enabled");
                    options.AppendExecutionProvider_Tensorrt();
                    options.AppendExecutionProvider_CUDA();
                    break;
#else
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsServer:
                    Debug.Log("DirectML is enabled");
                    options.AppendExecutionProvider_DML(0);
                    break;
#endif
                case RuntimePlatform.LinuxEditor:
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxServer:
                    Debug.Log("TensorRT is enabled");
                    options.AppendExecutionProvider_Tensorrt();
                    options.AppendExecutionProvider_CUDA();
                    break;
                // TODO: Add WebGL build
                default:
                    Debug.LogWarning($"Execution provider is not supported on {platform}");
                    break;
            }
        }

        private void AddExecutionProvider(SessionOptions options, ExecutionProviderPriority priority)
        {

            switch (priority)
            {
                case ExecutionProviderPriority.None:
                    break;
                case ExecutionProviderPriority.PlatformGPU:
                    AppendPlatformExecutionProvider(Application.platform, options);
                    break;
                case ExecutionProviderPriority.XNNPACK:
                    AppendXNNPackProvider(options);
                    break;
            }
        }
    }
}
