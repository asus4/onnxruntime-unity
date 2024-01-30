using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Microsoft.ML.OnnxRuntime.Unity
{
    /// <summary>
    /// Extension methods for ComputeBuffer
    /// </summary>
    public static class ComputeBufferExtension
    {
        private delegate void SetNativeDataDelegate(IntPtr data, int nativeBufferStartIndex, int computeBufferStartIndex, int count, int elemSize);
        private static readonly Lazy<MethodInfo> setNativeDataMethod = new(() =>
        {
            var flags = BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance;
            return typeof(ComputeBuffer).GetMethod("InternalSetNativeData", flags);
        });

        private static readonly Dictionary<ComputeBuffer, SetNativeDataDelegate> setNativeDataCache = new();

        private static SetNativeDataDelegate FindSetNativeDataDelegate(this ComputeBuffer buffer)
        {
            if (!setNativeDataCache.TryGetValue(buffer, out SetNativeDataDelegate setNativeDataDelegate))
            {
                setNativeDataDelegate = (SetNativeDataDelegate)setNativeDataMethod.Value.CreateDelegate(typeof(SetNativeDataDelegate), buffer);
                setNativeDataCache.Add(buffer, setNativeDataDelegate);
            }
            return setNativeDataDelegate;
        }

        public unsafe static void SetData<T>(this ComputeBuffer buffer, ReadOnlySpan<T> data)
            where T : unmanaged
        {
            fixed (T* dataPtr = &data.GetPinnableReference())
            {
                buffer.SetNativeData((IntPtr)dataPtr, 0, 0, data.Length, sizeof(T));
            }
        }

        /// <summary>
        /// Unsafe: Calling InternalSetNativeData via reflection
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetNativeData(this ComputeBuffer buffer, IntPtr data, int nativeBufferStartIndex, int computeBufferStartIndex, int count, int elemSize)
        {
            FindSetNativeDataDelegate(buffer)(data, nativeBufferStartIndex, computeBufferStartIndex, count, elemSize);
        }

        public static void ReleaseWithDelegateCache(this ComputeBuffer buffer)
        {
            setNativeDataCache.Remove(buffer);
            buffer.Release();
        }
    }
}
