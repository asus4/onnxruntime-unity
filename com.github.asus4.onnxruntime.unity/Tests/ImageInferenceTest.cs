using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Mathematics;

namespace Microsoft.ML.OnnxRuntime.Unity.Tests
{
    public class ImageInferenceTest
    {
        [TestCase(1000, 560, 1920, 1080, 1000, 8)] // lands
        [TestCase(560, 1000, 1080, 1920, 1000, 8)] // portrait
        public void TestResizeToMaxSize(int expectedX, int expectedY, int inputX, int inputY, int maxSize, int alignmentSize)
        {
            var expected = new int2(expectedX, expectedY);
            var input = new int2(inputX, inputY);
            var actual = ImageInference<float>.ResizeToMaxSize(input, maxSize, alignmentSize);
            Assert.AreEqual(expected.x, actual.x);
            Assert.AreEqual(expected.y, actual.y);
        }
    }
}
