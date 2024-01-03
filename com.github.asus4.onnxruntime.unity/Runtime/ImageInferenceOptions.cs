
using System;
using UnityEngine;

namespace Microsoft.ML.OnnxRuntime.Unity
{
    public enum AspectMode
    {
        /// <summary>
        /// Resizes the image without keeping the aspect ratio.
        /// </summary>
        None = 0,
        /// <summary>
        /// Resizes the image to contain full area and padded black pixels.
        /// </summary>
        Fit = 1,
        /// <summary>
        /// Trims the image to keep aspect ratio.
        /// </summary>
        Fill = 2,
    }

    /// <summary>
    /// An option to create a Image inference
    /// </summary>
    [Serializable]
    public class ImageInferenceOptions
    {
        [Header("Image Preprocessing")]
        [Tooltip("How to resize the image")]
        public AspectMode aspectMode = AspectMode.Fit;
        public Vector3 mean = new(0.485f, 0.456f, 0.406f);
        public Vector3 std = new(0.229f, 0.224f, 0.225f);

        [Header("Inference options")]
        [Tooltip("Whether to use GPU")]
        public bool useGPU = false;


        public SessionOptions CreateSessionOptions()
        {
            SessionOptions options = new();
            if (useGPU)
            {
                try
                {
                    AppendExecutionProvider(Application.platform, options);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to setup GPU: {e.Message}");
                }
            }

            return options;
        }

        /// <summary>
        /// Automatically append execution provider based on the platform
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="options"></param>
        public void AppendExecutionProvider(RuntimePlatform platform, SessionOptions options)
        {
            switch (platform)
            {
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXServer:
                case RuntimePlatform.IPhonePlayer:
                    options.AppendExecutionProvider_CoreML(
                        CoreMLFlags.COREML_FLAG_ENABLE_ON_SUBGRAPH);
                    Debug.Log("CoreML is enabled");
                    break;
                case RuntimePlatform.Android:
                    options.AppendExecutionProvider_Nnapi(
                        // NNApi can fallback to CPU if GPU is not available.
                        // But in general, it will be slower than OnnxRuntime CPU inference.
                        // Thus, we disable CPU fallback.
                        // It throws an exception if GPU is not available.
                        NnapiFlags.NNAPI_FLAG_USE_FP16 | NnapiFlags.NNAPI_FLAG_CPU_DISABLED);
                    Debug.Log("NNAPI is enabled");
                    break;
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsServer:
                case RuntimePlatform.LinuxEditor:
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxServer:
                    options.AppendExecutionProvider_Tensorrt();
                    // options.AppendExecutionProvider_CUDA();
                    Debug.Log("TensorRT is enabled");
                    break;
                // TODO: Add WebGL build
                default:
                    Debug.LogWarning($"Execution provider is not supported on {platform}");
                    break;
            }
        }
    }
}
