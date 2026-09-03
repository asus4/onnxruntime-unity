using System;
using NUnit.Framework;

namespace Microsoft.ML.OnnxRuntime.Unity.Tests
{
    public class OrtTelemetryTest
    {
        [Test]
        public void TelemetryIsDisabled()
        {
            Assert.IsNotNull(OrtEnv.Instance());
            Assert.AreEqual("1", Environment.GetEnvironmentVariable("ORT_DISABLE_TELEMETRY"));
        }
    }
}
