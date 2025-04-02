using System;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Microsoft.ML.OnnxRuntime.Unity;

namespace Microsoft.ML.OnnxRuntime.UnityEx
{
    /// <summary>
    /// Interface for Detection Task
    /// </summary>
    /// <typeparam name="T">Detection struct</typeparam>
    public interface IDetection<T> : IComparable<T>
        where T : unmanaged
    {
        int Label { get; }
        Rect Rect { get; }
    }

    public static class DetectionUtil
    {
        /// <summary>
        /// Non-Maximum Suppression (Multi-Class)
        /// </summary>
        /// <param name="proposals">A list of proposals which should be sorted in descending order</param>
        /// <param name="result">A result of NMS</param>
        /// <param name="iouThreshold">A threshold of IoU (Intersection over Union)</param>
        public unsafe static void NMS<T>(
            NativeSlice<T> proposals,
            NativeList<T> result,
            float iouThreshold)
            where T : unmanaged, IDetection<T>
        {
            int proposalsLength = proposals.Length;
            T* proposalsPtr = (T*)proposals.GetUnsafeReadOnlyPtr();
            NMS(proposalsPtr, proposalsLength, result, iouThreshold);
        }

        /// <summary>
        /// Non-Maximum Suppression (Multi-Class)
        /// </summary>
        /// <param name="proposals">A list of proposals which should be sorted in descending order</param>
        /// <param name="result">A result of NMS</param>
        /// <param name="iouThreshold">A threshold of IoU (Intersection over Union)</param>
        public unsafe static void NMS<T>(
           NativeList<T> proposals,
           NativeList<T> result,
           float iouThreshold)
           where T : unmanaged, IDetection<T>
        {
            int proposalsLength = proposals.Length;
            T* proposalsPtr = proposals.GetUnsafeReadOnlyPtr();
            NMS(proposalsPtr, proposalsLength, result, iouThreshold);
        }

        unsafe static void NMS<T>(
            T* proposalsPtr,
            int proposalsLength,
            NativeList<T> result,
            float iouThreshold)
            where T : unmanaged, IDetection<T>
        {
            result.Clear();

            for (int i = 0; i < proposalsLength; i++)
            {
                T* a = proposalsPtr + i;
                bool keep = true;

                for (int j = 0; j < result.Length; j++)
                {
                    T* b = result.GetUnsafeReadOnlyPtr() + j;

                    // Ignore different classes
                    if (b->Label != a->Label)
                    {
                        continue;
                    }

                    float iou = a->Rect.IntersectionOverUnion(b->Rect);
                    if (iou > iouThreshold)
                    {
                        keep = false;
                        break;
                    }
                }

                if (keep)
                {
                    result.Add(*a);
                }
            }
        }
    }
}
