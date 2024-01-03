using UnityEngine;

namespace Microsoft.ML.OnnxRuntime.Unity
{
    /// <summary>
    /// Simple asset to hold *.ort file as byte array
    /// </summary>
    public class OrtAsset : ScriptableObject
    {
        [HideInInspector]
        public byte[] bytes;
    }
}
