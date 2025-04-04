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
                    CopyOrtXCFramework(report,
                    PACKAGE_PATH,
                    FRAMEWORK_SRC,
                    FRAMEWORK_DST);
                    break;
            }
        }

        /// <summary>
        /// A common method that copies and set options for ONNX Runtime XCFramework in iOS
        /// </summary>
        /// <param name="report">A build report</param>
        /// <param name="packagePath">A package path start from "Packages/com.domain.package"</param>
        /// <param name="frameworkSrcPath">A source XCFramework path</param>
        /// <param name="frameworkDstPath">A destination XCFramework path</param>
        public static void CopyOrtXCFramework(
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
            CopyDirectory(frameworkSrcAbsPath, frameworkDstAbsPath);

            // Then add to Xcode project
            string frameworkGuid = pbxProject.AddFile(frameworkDstAbsPath, frameworkDstPath, PBXSourceTree.Source);
            string unityTargetGuid = pbxProject.GetUnityFrameworkTargetGuid();
            string targetBuildPhaseGuid = pbxProject.AddFrameworksBuildPhase(unityTargetGuid);
            pbxProject.AddFileToBuildSection(unityTargetGuid, targetBuildPhaseGuid, frameworkGuid);

#if ORT_GENAI_ENABLED
            // NOTE: Required only when GenAI package is installed
            // GenAI loads the dynamic library in runtime. need to embed in the main target
            string mainTargetGuid = pbxProject.GetUnityMainTargetGuid();
            pbxProject.AddFileToEmbedFrameworks(mainTargetGuid, frameworkGuid);
#endif // ORT_GENAI_ENABLED

            pbxProject.WriteToFile(pbxProjectPath);
#endif // UNITY_IOS
        }

        static void CopyDirectory(string source, string dest)
        {
            if (Directory.Exists(dest) && !FileUtil.DeleteFileOrDirectory(dest))
            {
                throw new IOException($"Failed to delete directory '{dest}'.");
            }
            FileUtil.CopyFileOrDirectoryFollowSymlinks(source, dest);
        }
    }
}
