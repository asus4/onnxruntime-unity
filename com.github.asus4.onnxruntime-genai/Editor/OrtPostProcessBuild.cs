using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Microsoft.ML.OnnxRuntime.GenAI.Editor
{
    using CorePostProcessBuild = Microsoft.ML.OnnxRuntime.Editor.OrtPostProcessBuild;

    /// <summary>
    /// Custom post-process build for ONNX Runtime GenAI
    /// </summary>
    public class OrtPostProcessBuild : IPostprocessBuildWithReport
    {
        private const string PACKAGE_PATH = "Packages/com.github.asus4.onnxruntime-genai";
        private const string FRAMEWORK_SRC = "Plugins/iOS~/onnxruntime-genai.xcframework";
        private const string FRAMEWORK_DST = "Libraries/onnxruntime-genai.xcframework";

        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            switch (report.summary.platform)
            {
                case BuildTarget.iOS:
                    CorePostProcessBuild.PostprocessBuildIOS(report,
                        PACKAGE_PATH,
                        FRAMEWORK_SRC,
                        FRAMEWORK_DST);
                    break;
            }
        }
    }
}
