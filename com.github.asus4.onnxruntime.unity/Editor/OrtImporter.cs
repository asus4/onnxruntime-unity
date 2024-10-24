using System.IO;
using UnityEngine;
using UnityEditor.AssetImporters;

namespace Microsoft.ML.OnnxRuntime.Unity.Editor
{
    /// <summary>
    /// Imports *.ort or *.onnx file as OrtAsset
    /// 
    /// Sentis in installed and want to import the onnx model as OrtAsset,
    /// Select importer the dropdown of the onnx asset
    /// </summary>
    [ScriptedImporter(1, new[] { "ort" }, new[] { "onnx" })]
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
