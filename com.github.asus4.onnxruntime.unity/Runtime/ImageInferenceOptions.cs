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

        public ExecutionProviderOptions executionProvider;

        /// <summary>
        /// Custom session options. If null, default session options will be created.
        /// </summary>
        /// <value>Returns a SessionOptions if provided, otherwise null</value>
        public SessionOptions CustomSessionOptions { get; set; } = null;
    }
}
