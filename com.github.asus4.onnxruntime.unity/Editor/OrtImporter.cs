using System.IO;
using UnityEngine;
using UnityEditor.AssetImporters;

namespace Microsoft.ML.OnnxRuntime.Unity.Editor
{
    /// <summary>
    /// Imports *.ort file as OrtAsset
    /// </summary>
    [ScriptedImporter(1, "ort")]
    public class OrtImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // Load *.ort file as  OrtAsset
            var asset = ScriptableObject.CreateInstance<OrtAsset>();
            asset.bytes = File.ReadAllBytes(ctx.assetPath);

            ctx.AddObjectToAsset("ort asset", asset);
            ctx.SetMainObject(asset);
        }
    }
}
