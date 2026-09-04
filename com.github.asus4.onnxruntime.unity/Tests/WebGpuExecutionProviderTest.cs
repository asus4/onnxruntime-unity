using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Microsoft.ML.OnnxRuntime.Unity.Tests
{
    public class WebGpuExecutionProviderTest
    {
        [Test]
        public void LibraryPathExists()
        {
            if (!WebGpuExecutionProvider.IsSupported)
            {
                Assert.Ignore($"WebGPU EP is not bundled for {Application.platform}");
            }
            string path = WebGpuExecutionProvider.ResolveLibraryPath();
            Assert.IsTrue(Path.IsPathRooted(path), $"Expected an absolute path: {path}");
            Assert.IsTrue(File.Exists(path), $"{path} does not exist");
        }

        [Test]
        public void GetDevicesReturnsWebGpuDevice()
        {
            if (!WebGpuExecutionProvider.IsSupported)
            {
                Assert.Ignore($"WebGPU EP is not bundled for {Application.platform}");
            }
            var devices = WebGpuExecutionProvider.GetDevices(OrtEnv.Instance());
            Assert.Greater(devices.Count, 0, "No WebGPU device found");
            foreach (var device in devices)
            {
                Assert.AreEqual(WebGpuExecutionProvider.EpName, device.EpName);
            }
        }

        [Test]
        public void GetDevicesAfterOrtEnvRecreated()
        {
            if (!WebGpuExecutionProvider.IsSupported)
            {
                Assert.Ignore($"WebGPU EP is not bundled for {Application.platform}");
            }
            Assert.Greater(WebGpuExecutionProvider.GetDevices(OrtEnv.Instance()).Count, 0);
            // Disposing OrtEnv re-creates the singleton; the plugin must be registered again
            OrtEnv.Instance().Dispose();
            var devices = WebGpuExecutionProvider.GetDevices(OrtEnv.Instance());
            Assert.Greater(devices.Count, 0, "No WebGPU device after OrtEnv was re-created");
        }
    }
}
