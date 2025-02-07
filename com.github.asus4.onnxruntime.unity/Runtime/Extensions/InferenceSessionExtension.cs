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
        /// Log input and output information
        /// </summary>
        /// <param name="session">An InferenceSession</param>
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void LogIOInfo(this InferenceSession session)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Version: {OrtEnv.Instance().GetVersionString()}");
            sb.AppendLine("Input:");
            foreach (var kv in session.InputMetadata)
            {
                string key = kv.Key;
                NodeMetadata meta = kv.Value;
                sb.AppendLine($"[{key}] shape: {string.Join(",", meta.Dimensions)}, type: {meta.ElementType} isTensor: {meta.IsTensor}");
            }

            sb.AppendLine();
            sb.AppendLine("Output:");
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

        /// <summary>
        /// Checks if the InferenceSession contains any dynamic input dimensions.
        /// </summary>
        /// <param name="session">The InferenceSession to check.</param>
        /// <returns>True if any input is dynamic; otherwise, false.</returns>
        public static bool ContainsDynamicInput(this InferenceSession session)
        {
            return session.InputMetadata.Values.Any(meta => meta.ContainsDynamic());
        }
    }
}
