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
            Assert.IsTrue(File.Exists(libPath), $"{libPath} does not exist");
        }
    }
}
