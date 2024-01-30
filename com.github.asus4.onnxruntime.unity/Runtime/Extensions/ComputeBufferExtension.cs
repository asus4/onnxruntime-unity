using System;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Microsoft.ML.OnnxRuntime.Unity
{
    /// <summary>
    /// Extension methods for ComputeBuffer
    /// </summary>
    public static class ComputeBufferExtension
    {
        public unsafe static void SetData<T>(this ComputeBuffer buffer, ReadOnlySpan<T> data)
            where T : unmanaged
        {
            fixed (T* dataPtr = &data.GetPinnableReference())
            {
                var nativeArr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
                    dataPtr, data.Length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeArr, AtomicSafetyHandle.Create());
#endif
                buffer.SetData(nativeArr);
            }
        }
    }
}
