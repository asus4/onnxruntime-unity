using System;
using System.IO.Compression;
using NugetForUnity.PluginAPI;
using NugetForUnity.PluginAPI.ExtensionPoints;
using NugetForUnity.PluginAPI.Models;

namespace Microsoft.ML.OnnxRuntime.Unity.Editor
{
    public sealed class OnnxRuntimeNugetForUnityPlugin : INugetPlugin
    {
        public void Register(INugetPluginRegistry registry)
        {
            registry.RegisterPackageInstallFileHandler(new ManagedPackageInstallFileHandler(registry.PluginService));
        }
    }

    internal sealed class ManagedPackageInstallFileHandler : IPackageInstallFileHandler
    {
        private const string ManagedPackageId = "Microsoft.ML.OnnxRuntime.Managed";
        private const string ManagedAssemblyName = "Microsoft.ML.OnnxRuntime.dll";

        private readonly INugetPluginService pluginService;

        public ManagedPackageInstallFileHandler(INugetPluginService pluginService)
        {
            this.pluginService = pluginService;
        }

        public string GetPackageFolderName(INugetPackageIdentifier package, string startName)
        {
            return startName;
        }

        public bool HandleFileExtraction(INugetPackage package, ZipArchiveEntry entry, string extractDirectory)
        {
            if (!string.Equals(package.Id, ManagedPackageId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var entryName = entry.FullName.Replace('\\', '/');
            if (!IsManagedAssembly(entryName))
            {
                return false;
            }

            pluginService.LogVerbose("Skipping {0} from {1}; com.github.asus4.onnxruntime provides the Unity managed bindings.", entryName, ManagedPackageId);
            return true;
        }

        private static bool IsManagedAssembly(string entryName)
        {
            return entryName.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) &&
                   entryName.EndsWith("/" + ManagedAssemblyName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
