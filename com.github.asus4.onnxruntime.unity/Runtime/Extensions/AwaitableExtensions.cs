// Requires Awaitable support
#if UNITY_2023_1_OR_NEWER

using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.ML.OnnxRuntime.Unity
{
    public static class AwaitableExtensions
    {

        public static async Task AsTask(this Awaitable a)
        {
            await a;
        }

        public static async Task<T> AsTask<T>(this Awaitable<T> a)
        {
            return await a;
        }
    }
}

#endif // UNITY_2023_1_OR_NEWER
