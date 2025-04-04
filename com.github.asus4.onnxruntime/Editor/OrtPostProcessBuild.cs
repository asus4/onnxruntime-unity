using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
#endif // UNITY_IOS

namespace Microsoft.ML.OnnxRuntime.Editor
{
    /// <summary>
    /// Custom post-process build for ONNX Runtime
    /// </summary>
    public class OrtPostProcessBuild : IPostprocessBuildWithReport
    {
        const string PACKAGE_PATH = "Packages/com.github.asus4.onnxruntime";
        const string FRAMEWORK_SRC = "Plugins/iOS~/onnxruntime.xcframework";
        const string FRAMEWORK_DST = "Libraries/onnxruntime.xcframework";

        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            switch (report.summary.platform)
            {
                case BuildTarget.iOS:
                    PostprocessBuildIOS(report,
                    PACKAGE_PATH,
                    FRAMEWORK_SRC,
                    FRAMEWORK_DST);
                    break;
            }
        }

        public static void PostprocessBuildIOS(
            BuildReport report,
            string packagePath,
            string frameworkSrcPath,
            string frameworkDstPath)
        {
#if UNITY_IOS
            string pbxProjectPath = PBXProject.GetPBXProjectPath(report.summary.outputPath);
            PBXProject pbxProject = new();
            pbxProject.ReadFromFile(pbxProjectPath);

            // Copy XCFramework to the Xcode project folder
            string frameworkSrcAbsPath = Path.Combine(packagePath, frameworkSrcPath);
            string frameworkDstAbsPath = Path.Combine(report.summary.outputPath, frameworkDstPath);
            FileUtil.ReplaceDirectory(frameworkSrcAbsPath, frameworkDstAbsPath);

            // Then add to Xcode project
            string frameworkGuid = pbxProject.AddFile(frameworkDstAbsPath, frameworkDstPath, PBXSourceTree.Source);
            string targetGuid = pbxProject.GetUnityFrameworkTargetGuid();
            // pbxProject.AddFileToEmbedFrameworks(targetGuid, frameworkGuid);
            string targetBuildPhaseGuid = pbxProject.AddFrameworksBuildPhase(targetGuid);
            pbxProject.AddFileToBuildSection(targetGuid, targetBuildPhaseGuid, frameworkGuid);

            pbxProject.WriteToFile(pbxProjectPath);
#endif // UNITY_IOS
        }
    }
}
