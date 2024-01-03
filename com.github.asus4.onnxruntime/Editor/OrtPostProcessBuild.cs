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

            // Copy XCFramework to in the "PROJECT/Libraries/onnxruntime.xcframework"
            string frameworkSrcPath = Path.Combine(OrtBuildHelper.PACKAGE_PATH, "Plugins/iOS~/onnxruntime.xcframework");
            string frameworkDstRelPath = "Libraries/onnxruntime.xcframework";
            string frameworkDstAbsPath = Path.Combine(report.summary.outputPath, frameworkDstRelPath);
            CopyDir(frameworkSrcPath, frameworkDstAbsPath);

            // Then add to Xcode project
            string frameworkGuid = pbxProject.AddFile(frameworkDstAbsPath, frameworkDstRelPath, PBXSourceTree.Source);
            string targetGuid = pbxProject.GetUnityFrameworkTargetGuid();
            pbxProject.AddFileToEmbedFrameworks(targetGuid, frameworkGuid);

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
