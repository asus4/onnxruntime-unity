using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.ML.OnnxRuntime.Unity
{
    /// <summary>
    /// Extensions for InferenceSession
    /// </summary>
    public static class InferenceSessionExtension
    {
        /// <summary>
        /// Log input and output information of the model
        /// </summary>
        /// <param name="session">An InferenceSession</param>
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void LogIOInfo(this InferenceSession session)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Version: {OrtEnv.Instance().GetVersionString()}");

            // Input
            bool isDynamicInput = session.InputMetadata.Values.Any(meta => meta.ContainsDynamic());
            sb.AppendLine(isDynamicInput ? "Dynamic Input:" : "Input:");
            foreach (var kv in session.InputMetadata)
            {
                string key = kv.Key;
                NodeMetadata meta = kv.Value;
                sb.AppendLine($"[{key}] shape: {string.Join(",", meta.Dimensions)}, type: {meta.ElementType} isTensor: {meta.IsTensor}");
            }
            sb.AppendLine();

            // Output
            bool isDynamicOutput = session.OutputMetadata.Values.Any(meta => meta.ContainsDynamic());
            sb.AppendLine(isDynamicOutput ? "Dynamic Output:" : "Output:");
            foreach (var meta in session.OutputMetadata)
            {
                string key = meta.Key;
                NodeMetadata metaValue = meta.Value;
                sb.AppendLine($"[{key}] shape: {string.Join(",", metaValue.Dimensions)}, type: {metaValue.ElementType} isTensor: {metaValue.IsTensor}");
            }

            UnityEngine.Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Create OrtValue from NodeMetadata
        /// </summary>
        /// <param name="metadata">A metadata</param>
        /// <returns>Allocated OrtValue, should be disposed.</returns>
        public static OrtValue CreateTensorOrtValue(this NodeMetadata metadata)
        {
            if (!metadata.IsTensor)
            {
                throw new ArgumentException("metadata must be tensor");
            }
            if (metadata.ContainsDynamic())
            {
                throw new ArgumentException("Allocate manually when contains dynamic dimensions");
            }
            long[] shape = Array.ConvertAll(metadata.Dimensions, Convert.ToInt64);
            var ortValue = OrtValue.CreateAllocatedTensorValue(
                OrtAllocator.DefaultInstance, metadata.ElementDataType, shape);
            return ortValue;
        }

        /// <summary>
        /// Checks if the NodeMetadata contains dynamic dimensions.
        /// </summary>
        /// <param name="metadata">The NodeMetadata to check.</param>
        /// <returns>True if any dimension is dynamic; otherwise, false.</returns>
        public static bool ContainsDynamic(this NodeMetadata metadata)
        {
            return metadata.Dimensions.Any(d => d < 0);
        }
    }
}
