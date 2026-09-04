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
        /// <summary>
        /// WebGPU EP (Windows)
        /// </summary>
        WebGPU = 3,
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
        /// Append the first execution provider that can be initialized in the priority order, or fall back to CPU
        /// </summary>
        /// <param name="options">A session options</param>
        public void AppendExecutionProviders(SessionOptions options)
        {
            foreach (var provider in executionProviderPriorities)
            {
                try
                {
                    AddExecutionProvider(options, provider);
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to initialize {provider} execution provider: {e.Message}");
                }
            }
            if (executionProviderPriorities.Length > 0)
            {
                Debug.Log("No execution provider was initialized. Using the default CPU provider.");
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
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsServer:
#if ORT_GPU_PROVIDER_WIN
                    // Player builds only: the providers must be next to onnxruntime.dll
                    if (TryAppendCudaProviders(options))
                    {
                        break;
                    }
#endif
                    Debug.Log("WebGPU is enabled");
                    WebGpuExecutionProvider.Append(options);
                    break;
                case RuntimePlatform.LinuxEditor:
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxServer:
#if ORT_GPU_PROVIDER_LINUX
                    if (TryAppendCudaProviders(options))
                    {
                        break;
                    }
                    throw new NotSupportedException("TensorRT / CUDA execution providers could not be initialized");
#else
                    throw new NotSupportedException("GPU execution provider is not bundled for Linux. Install com.github.asus4.onnxruntime.linux-x64-gpu");
#endif
                // TODO: Add WebGL build
                default:
                    Debug.LogWarning($"Execution provider is not supported on {platform}");
                    break;
            }
        }

#if ORT_GPU_PROVIDER_WIN || ORT_GPU_PROVIDER_LINUX
        /// <summary>
        /// Append TensorRT and CUDA providers. Requires CUDA, cuDNN and TensorRT installed on the machine
        /// </summary>
        private static bool TryAppendCudaProviders(SessionOptions options)
        {
            bool appended = false;
            try
            {
                options.AppendExecutionProvider_Tensorrt();
                Debug.Log("TensorRT is enabled");
                appended = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"TensorRT is not available: {e.Message}");
            }
            try
            {
                options.AppendExecutionProvider_CUDA();
                Debug.Log("CUDA is enabled");
                appended = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"CUDA is not available: {e.Message}");
            }
            return appended;
        }
#endif

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
                case ExecutionProviderPriority.WebGPU:
                    Debug.Log("WebGPU is enabled");
                    WebGpuExecutionProvider.Append(options);
                    break;
            }
        }
    }
}
