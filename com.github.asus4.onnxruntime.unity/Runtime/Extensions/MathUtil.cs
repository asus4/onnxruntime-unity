using Unity.Mathematics;

namespace Microsoft.ML.OnnxRuntime.Unity
{
    public static class MathUtil
    {
        public static int2 ResizeToMaxSize(int2 size, int maxSize, int alignmentSize)
        {
            if (math.all(size <= maxSize))
            {
                return size;
            }

            float aspectRatio = (float)size.x / size.y;
            float2 sizeF = aspectRatio > 1
                ? new float2(maxSize, maxSize / aspectRatio)
                : new float2(maxSize * aspectRatio, maxSize);

            // Round to a multiple of 'alignmentSize'
            return (int2)math.round(sizeF / alignmentSize) * alignmentSize;
        }
    }
}
