using UnityEngine;

namespace Microsoft.ML.OnnxRuntime.Unity
{
    /// <summary>
    /// Simple asset to hold *.ort and *.onnx file as byte array
    /// 
    /// *.onnx can be imported only when com.unity.sentis is NOT installed
    /// </summary>
    public class OrtAsset : ScriptableObject
    {
        [HideInInspector]
        public byte[] bytes;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Warn when file is LFS pointer
            const string LSF_HEADER = "version https://git-lfs.github.com/spec/";
            if (bytes.Length > LSF_HEADER.Length)
            {
                string header = System.Text.Encoding.UTF8.GetString(bytes, 0, LSF_HEADER.Length);
                if (header == LSF_HEADER)
                {
                    Debug.LogError("This is LFS file. Please use 'git lfs pull' to download the actual file.", this);
                }
            }
        }
#endif // UNITY_EDITOR
    }
}
