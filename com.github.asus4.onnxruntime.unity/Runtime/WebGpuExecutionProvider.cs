using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Microsoft.ML.OnnxRuntime.Unity
{
    /// <summary>
    /// WebGPU plugin Execution Provider (Direct3D 12), bundled in the core package for Windows.
    /// https://onnxruntime.ai/docs/execution-providers/WebGPU-ExecutionProvider.html
    /// </summary>
    public static class WebGpuExecutionProvider
    {
        public const string EpName = "WebGpuExecutionProvider";

        const string RegistrationName = "webgpu_ep_registration";
        const string CorePackagePath = "Packages/com.github.asus4.onnxruntime";
        const string LibraryFileName = "onnxruntime_providers_webgpu.dll";

        /// <summary>
        /// Default provider options. See the official docs for all keys.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> DefaultOptions = new Dictionary<string, string>()
        {
            { "powerPreference", "high-performance" },
        };

        /// <summary>
        /// Whether the plugin library is bundled for the current platform
        /// </summary>
        public static bool IsSupported => Application.platform switch
        {
            RuntimePlatform.WindowsEditor => true,
            RuntimePlatform.WindowsPlayer => true,
            RuntimePlatform.WindowsServer => true,
            _ => false,
        };

        // Disposing OrtEnv re-creates the singleton without the plugin, so track the instance
        static OrtEnv registeredEnv;

        /// <summary>
        /// Append the WebGPU execution provider. The plugin library is registered to the OrtEnv on first use.
        /// </summary>
        /// <param name="options">A session options</param>
        /// <param name="epOptions">Provider options. DefaultOptions if null</param>
        /// <param name="device">The device to use. The most performant GPU if null</param>
        public static void Append(SessionOptions options, IReadOnlyDictionary<string, string> epOptions = null, OrtEpDevice device = null)
        {
            var env = OrtEnv.Instance();
            // The WebGPU EP accepts only one device per session
            device ??= SelectDevice(GetDevices(env));
            Debug.Log($"WebGPU device: {Describe(device)}");
            options.AppendExecutionProvider(env, new[] { device }, epOptions ?? DefaultOptions);
        }

        /// <summary>
        /// Get the WebGPU EP devices. The plugin library is registered to the OrtEnv if needed.
        /// </summary>
        public static IReadOnlyList<OrtEpDevice> GetDevices(OrtEnv env)
        {
            if (!IsSupported)
            {
                throw new NotSupportedException($"WebGPU execution provider is not bundled for {Application.platform}");
            }

            var devices = FindDevices(env);
            if (devices.Count == 0 && !ReferenceEquals(registeredEnv, env))
            {
                Register(env);
                devices = FindDevices(env);
                Debug.Log($"WebGPU devices: {string.Join(", ", devices.Select(Describe))}");
            }
            if (devices.Count == 0)
            {
                throw new NotSupportedException("No WebGPU device is available. A Direct3D 12 capable GPU is required.");
            }
            return devices;
        }

        static void Register(OrtEnv env)
        {
            string path = ResolveLibraryPath();
            Debug.Log($"Registering WebGPU execution provider: {path}");
            try
            {
                env.RegisterExecutionProviderLibrary(RegistrationName, path);
            }
            catch (OnnxRuntimeException e) when (e.Message.Contains("already"))
            {
                // Already registered by a previous domain (Editor domain reload)
            }
            registeredEnv = env;
        }

        /// <summary>
        /// Select the most performant device: DXGI high-performance order, discrete GPU, then video memory
        /// </summary>
        public static OrtEpDevice SelectDevice(IReadOnlyList<OrtEpDevice> devices)
        {
            if (devices == null || devices.Count == 0)
            {
                throw new ArgumentException("No WebGPU device", nameof(devices));
            }
            return devices
                .Select(device => (device, meta: device.HardwareDevice.Metadata.Entries))
                .OrderBy(x => GetLong(x.meta, "DxgiHighPerformanceIndex", long.MaxValue))
                .ThenBy(x => GetLong(x.meta, "Discrete", 0) == 1 ? 0 : 1)
                .ThenByDescending(x => GetLong(x.meta, "DxgiVideoMemory", 0))
                .First()
                .device;
        }

        static IReadOnlyList<OrtEpDevice> FindDevices(OrtEnv env)
        {
            var all = env.GetEpDevices().Where(d => d.EpName == EpName).ToList();
            var gpus = all.Where(d => d.HardwareDevice.Type == OrtHardwareDeviceType.GPU).ToList();
            return gpus.Count > 0 ? gpus : all;
        }

        static long GetLong(IReadOnlyDictionary<string, string> meta, string key, long fallback)
        {
            if (!meta.TryGetValue(key, out string value))
            {
                return fallback;
            }
            // Leading number only: e.g. "12010 MB"
            int end = 0;
            while (end < value.Length && char.IsDigit(value[end]))
            {
                end++;
            }
            return end > 0 && long.TryParse(value.Substring(0, end), out long result) ? result : fallback;
        }

        static string Describe(OrtEpDevice device)
        {
            var hw = device.HardwareDevice;
            string name = hw.Metadata.Entries.TryGetValue("Description", out string description) ? description : hw.Vendor;
            return $"{name} ({hw.Type}, vendor:0x{hw.VendorId:X4} device:0x{hw.DeviceId:X4})";
        }

        /// <summary>
        /// Absolute path of the plugin library, so that dxcompiler.dll and dxil.dll are resolved from the same folder
        /// </summary>
        internal static string ResolveLibraryPath()
        {
            string arch = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                _ => throw new NotSupportedException($"Unsupported architecture: {RuntimeInformation.ProcessArchitecture}"),
            };

#if UNITY_EDITOR
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(CorePackagePath);
            if (package != null)
            {
                string path = Path.Combine(package.resolvedPath, "Plugins", "Windows", arch, LibraryFileName);
                if (File.Exists(path))
                {
                    return Path.GetFullPath(path);
                }
            }
#else
            // Native plugins are copied into <App>_Data/Plugins/<arch>/
            string pluginsDir = Path.Combine(Application.dataPath, "Plugins");
            string[] candidates =
            {
                Path.Combine(pluginsDir, arch == "x64" ? "x86_64" : "ARM64"),
                Path.Combine(pluginsDir, "x86_64"),
                Path.Combine(pluginsDir, "ARM64"),
                Path.Combine(pluginsDir, "arm64"),
                pluginsDir,
            };
            foreach (string dir in candidates)
            {
                string path = Path.Combine(dir, LibraryFileName);
                if (File.Exists(path))
                {
                    return Path.GetFullPath(path);
                }
            }
#endif
            Debug.LogWarning($"Could not locate {LibraryFileName}. Falling back to the default DLL search path.");
            return LibraryFileName;
        }
    }
}
