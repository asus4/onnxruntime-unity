using System;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Microsoft.ML.OnnxRuntime.Unity
{
    /// <summary>
    /// Extension methods for GraphicsBuffer
    /// </summary>
    public static class GraphicsBufferExtension
    {
        /// <summary>
        /// Set Span to GraphicsBuffer without copying
        /// </summary>
        /// <param name="buffer">A GraphicsBuffer</param>
        /// <param name="span">The span data to be set</param>
        /// <typeparam name="T">The type of data</typeparam>
        public unsafe static void SetData<T>(this GraphicsBuffer buffer, ReadOnlySpan<T> span)
            where T : unmanaged
        {
            fixed (void* ptr = span)
            {
                var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
                    ptr, span.Length, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                var handle = AtomicSafetyHandle.Create();
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, handle);
#endif // ENABLE_UNITY_COLLECTIONS_CHECKS

                buffer.SetData(arr);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.Release(handle);
#endif // ENABLE_UNITY_COLLECTIONS_CHECKS
            }
        }
    }
}
