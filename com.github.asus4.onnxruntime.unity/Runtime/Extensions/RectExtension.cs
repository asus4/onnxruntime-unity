using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;

namespace Microsoft.ML.OnnxRuntime.Unity
{
    /// <summary>
    /// Extension methods for Rect
    /// </summary>
    public static class RectExtension
    {
        /// <summary>
        /// Returns the area of the rectangle
        /// </summary>
        /// <param name="rect">A Rect</param>
        /// <returns>A area</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Area(this in Rect rect)
        {
            return Math.Abs(rect.width * rect.height);
        }

        /// <summary>
        /// Create intersection rectangle of two rectangles
        /// </summary>
        /// <param name="rect0">A rect</param>
        /// <param name="rect1">Another rect</param>
        /// <param name="intersection">An intersection rect</param>
        /// <returns>Return true if the rectangles intersect</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IntersectionRect(this in Rect rect0, in Rect rect1, out Rect intersection)
        {
            float intersect_xMin = Math.Max(rect0.xMin, rect1.xMin);
            float intersect_yMin = Math.Max(rect0.yMin, rect1.yMin);
            float intersect_xMax = Math.Min(rect0.xMax, rect1.xMax);
            float intersect_yMax = Math.Min(rect0.yMax, rect1.yMax);

            if (intersect_xMin > intersect_xMax || intersect_yMin > intersect_yMax)
            {
                intersection = Rect.zero;
                return false;
            }
            intersection = new Rect(intersect_xMin, intersect_yMin, intersect_xMax - intersect_xMin, intersect_yMax - intersect_yMin);
            return true;
        }

        /// <summary>
        /// Returns the IoU (intersection over union) of two rectangles
        /// See also:
        /// https://en.wikipedia.org/wiki/Jaccard_index
        /// </summary>
        /// <param name="rect0">A rect</param>
        /// <param name="rect1">A rect</param>
        /// <returns>IoU</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float IntersectionOverUnion(this in Rect rect0, in Rect rect1)
        {
            // Don't allow inverted rectangles
            Assert.IsTrue(rect0.width >= 0 && rect0.height >= 0);
            Assert.IsTrue(rect1.width >= 0 && rect1.height >= 0);

            float area0 = rect0.Area();
            float area1 = rect1.Area();
            if (area0 <= 0 || area1 <= 0)
            {
                return 0.0f;
            }
            if (!IntersectionRect(rect0, rect1, out Rect intersection))
            {
                return 0.0f;
            }

            float intersectArea = intersection.Area();
            return intersectArea / (area0 + area1 - intersectArea);
        }

        /// <summary>
        /// Flip Y axis, useful for converting between CV and Unity space
        /// </summary>
        /// <param name="rect">A rect</param>
        /// <param name="height">Height of the space</param>
        /// <returns>A flipped rect</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect FlipY(this in Rect rect, float height = 1f)
        {
            return new Rect(rect.x, height - rect.yMax, rect.width, rect.height);
        }
    }
}
