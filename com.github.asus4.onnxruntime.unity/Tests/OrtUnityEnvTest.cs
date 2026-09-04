using System.IO;
using NUnit.Framework;

namespace Microsoft.ML.OnnxRuntime.Unity.Tests
{
    public class OrtUnityEnvTest
    {
        [Test]
        public void GetOrtLibPathExists()
        {
            string libPath = OrtUnityEnv.GetOrtLibPath();
            if (string.IsNullOrEmpty(libPath))
            {
                Assert.Ignore("ORT_LIB_PATH is not required on this platform");
            }
            Assert.IsTrue(File.Exists(libPath), $"{libPath} does not exist");
        }
    }
}
