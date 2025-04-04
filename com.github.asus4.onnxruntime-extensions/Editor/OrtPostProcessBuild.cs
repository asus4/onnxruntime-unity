using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif // UNITY_IOS

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
#if UNITY_IOS
                    CorePostProcessBuild.CopyOrtXCFramework(report,
                        PACKAGE_PATH,
                        FRAMEWORK_SRC,
                        FRAMEWORK_DST);
                    AddIOSDependentFrameworks(report);
#endif // UNITY_IOS
                    break;
            }
        }

#if UNITY_IOS

        static void AddFrameworkIfNotExist(PBXProject proj, string targetGuid, string frameworkName, bool weak)
        {
            if (proj.ContainsFramework(targetGuid, frameworkName))
            {
                return;
            }
            proj.AddFrameworkToProject(targetGuid, frameworkName, weak);
        }

        static void AddIOSDependentFrameworks(BuildReport report)
        {
            string pbxProjectPath = PBXProject.GetPBXProjectPath(report.summary.outputPath);
            PBXProject pbxProject = new();
            pbxProject.ReadFromFile(pbxProjectPath);

            string frameworkGuid = pbxProject.GetUnityFrameworkTargetGuid();

            AddFrameworkIfNotExist(pbxProject, frameworkGuid, "ImageIO.framework", false);
            AddFrameworkIfNotExist(pbxProject, frameworkGuid, "MobileCoreServices.framework", false);

            pbxProject.WriteToFile(pbxProjectPath);
        }
#endif // UNITY_IOS

    }
}
