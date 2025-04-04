using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Microsoft.ML.OnnxRuntime.Extensions.Editor
{
    using CorePostProcessBuild = Microsoft.ML.OnnxRuntime.Editor.OrtPostProcessBuild;

    /// <summary>
    /// Custom post-process build for ONNX Runtime Extensions
    /// </summary>
    public class OrtPostProcessBuild : IPostprocessBuildWithReport
    {
        private const string PACKAGE_PATH = "Packages/com.github.asus4.onnxruntime-extensions";
        private const string FRAMEWORK_SRC = "Plugins/iOS~/onnxruntime_extensions.xcframework";
        private const string FRAMEWORK_DST = "Libraries/onnxruntime_extensions.xcframework";

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
