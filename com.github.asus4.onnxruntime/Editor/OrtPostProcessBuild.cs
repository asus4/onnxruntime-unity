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
        private const string PACKAGE_PATH = "Packages/com.github.asus4.onnxruntime";
        private const string FRAMEWORK_SRC = "Plugins/iOS~/onnxruntime.xcframework";
        private const string FRAMEWORK_DST = "Libraries/onnxruntime.xcframework";

        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            switch (report.summary.platform)
            {
                case BuildTarget.iOS:
                    PostprocessBuildIOS(report);
                    break;
                case BuildTarget.Android:
                    // Nothing to do
                    break;
                // TODO: Add support for other platforms
                default:
                    Debug.Log("OnnxPostProcessBuild.OnPostprocessBuild for target " + report.summary.platform + " is not supported");
                    break;
            }
        }

        private static void PostprocessBuildIOS(BuildReport report)
        {
#if UNITY_IOS
            string pbxProjectPath = PBXProject.GetPBXProjectPath(report.summary.outputPath);
            PBXProject pbxProject = new();
            pbxProject.ReadFromFile(pbxProjectPath);

            // Copy XCFramework to the Xcode project folder
            string frameworkSrcPath = Path.Combine(PACKAGE_PATH, FRAMEWORK_SRC);
            string frameworkDstAbsPath = Path.Combine(report.summary.outputPath, FRAMEWORK_DST);
            CopyDir(frameworkSrcPath, frameworkDstAbsPath);

            // Then add to Xcode project
            string frameworkGuid = pbxProject.AddFile(frameworkDstAbsPath, FRAMEWORK_DST, PBXSourceTree.Source);
            string targetGuid = pbxProject.GetUnityFrameworkTargetGuid();
            // pbxProject.AddFileToEmbedFrameworks(targetGuid, frameworkGuid);
            string targetBuildPhaseGuid = pbxProject.AddFrameworksBuildPhase(targetGuid);
            pbxProject.AddFileToBuildSection(targetGuid, targetBuildPhaseGuid, frameworkGuid);

            pbxProject.WriteToFile(pbxProjectPath);
#endif // UNITY_IOS
        }

        private static void CopyDir(string srcPath, string dstPath)
        {
            srcPath = FileUtil.GetPhysicalPath(srcPath);
            Assert.IsTrue(Directory.Exists(srcPath), $"Framework not found at {srcPath}");

            if (Directory.Exists(dstPath))
            {
                FileUtil.DeleteFileOrDirectory(dstPath);
            }
            FileUtil.CopyFileOrDirectory(srcPath, dstPath);
        }
    }
}
