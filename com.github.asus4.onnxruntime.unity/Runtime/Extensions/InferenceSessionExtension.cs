using System;
using System.Diagnostics;
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
    }
}
