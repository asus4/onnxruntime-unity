using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Microsoft.ML.OnnxRuntime.Unity
{
    /// <summary>
    /// Convert Texture to Onnx Tensor (NCHW layout)
    /// </summary>
    public class TextureToTensor<T> : IDisposable
        where T : unmanaged
    {
        private static readonly Lazy<ComputeShader> DefaultCompute = new(() =>
        {
            const string path = "com.github.asus4.onnxruntime.unity/TextureToTensor";
            return Resources.Load<ComputeShader>(path);
        });

        private static readonly int _InputTex = Shader.PropertyToID("_InputTex");
        private static readonly int _OutputTex = Shader.PropertyToID("_OutputTex");
        private static readonly int _OutputTensor = Shader.PropertyToID("_OutputTensor");
        private static readonly int _OutputSize = Shader.PropertyToID("_OutputSize");
        private static readonly int _TransformMatrix = Shader.PropertyToID("_TransformMatrix");
        private static readonly int _Mean = Shader.PropertyToID("_Mean");
        private static readonly int _StdDev = Shader.PropertyToID("_StdDev");

        private static readonly Matrix4x4 PopMatrix = Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0));
        private static readonly Matrix4x4 PushMatrix = Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0));

        public Vector3 Mean { get; set; } = new Vector3(0.485f, 0.456f, 0.406f);
        public Vector3 Std { get; set; } = new Vector3(0.229f, 0.224f, 0.225f);

        private readonly ComputeShader compute;
        private readonly int kernel;
        private readonly RenderTexture texture;
        private readonly GraphicsBuffer tensor;
        private readonly CommandBuffer commands;
        public readonly int channels;
        public readonly int width;
        public readonly int height;

        private NativeArray<T> tensorData;
        public RenderTexture Texture => texture;
        public Matrix4x4 TransformMatrix { get; private set; } = Matrix4x4.identity;

        /// <summary>
        /// Get the latest tensor data as ReadOnlySpan
        /// </summary>
        public ReadOnlySpan<T> TensorData => tensorData;

        public TextureToTensor(int width, int height, int channels = 3, ComputeShader customCompute = null)
        {
            this.width = width;
            this.height = height;
            this.channels = channels;

            if (channels != 3 && customCompute == null)
            {
                throw new ArgumentException("Default compute shader only supports 3 channels. Provide custom compute shader.");
            }

            var desc = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32)
            {
                enableRandomWrite = true,
                useMipMap = false,
                depthBufferBits = 0,
            };
            texture = new RenderTexture(desc);
            texture.Create();

            int stride = Marshal.SizeOf(default(T));
            tensor = new GraphicsBuffer(GraphicsBuffer.Target.Structured, channels * width * height, stride);
            tensorData = new NativeArray<T>(channels * width * height, Allocator.Persistent);

            compute = customCompute != null ? customCompute : DefaultCompute.Value;
            kernel = compute.FindKernel("TextureToTensor");

            // Set constant values in ComputeShader
            compute.SetInts(_OutputSize, width, height);
            compute.SetBuffer(kernel, _OutputTensor, tensor);
            compute.SetTexture(kernel, _OutputTex, texture, 0);

            commands = new CommandBuffer();
        }

        public void Dispose()
        {
            texture.Release();
            UnityEngine.Object.Destroy(texture);
            tensor.Release();
            tensorData.Dispose();
            commands.Dispose();
        }

        private void BuildCommandBuffer(Texture input, in Matrix4x4 t)
        {
            TransformMatrix = t;
            commands.Clear();
            commands.SetComputeTextureParam(compute, kernel, _InputTex, input, 0);
            commands.SetComputeMatrixParam(compute, _TransformMatrix, t);
            commands.SetComputeFloatParams(compute, _Mean, Mean.x, Mean.y, Mean.z);
            commands.SetComputeFloatParams(compute, _StdDev, Std.x, Std.y, Std.z);
            commands.DispatchCompute(compute, kernel, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);
        }

        public void Transform(Texture input, in Matrix4x4 t)
        {
            BuildCommandBuffer(input, t);
            Graphics.ExecuteCommandBuffer(commands);
            var request = AsyncGPUReadback.RequestIntoNativeArray(ref tensorData, tensor, GpuReadBackCallback);
            request.WaitForCompletion();
        }

        public void Transform(Texture input, Vector2 translate, float eulerRotation, Vector2 scale)
        {
            Matrix4x4 trs = Matrix4x4.TRS(
                translate,
                Quaternion.Euler(0, 0, -eulerRotation),
                new Vector3(scale.x, scale.y, 1));
            Transform(input, PopMatrix * trs * PushMatrix);
        }

        public void Transform(Texture input, AspectMode aspectMode)
        {
            Transform(input, GetAspectScaledMatrix(input, aspectMode));
        }

        public Matrix4x4 GetAspectScaledMatrix(Texture input, AspectMode aspectMode)
        {
            float srcAspect = (float)input.width / input.height;
            float dstAspect = (float)width / height;
            Vector2 scale = GetAspectScale(srcAspect, dstAspect, aspectMode);
            Matrix4x4 scaleMatrix = Matrix4x4.Scale(new Vector3(scale.x, scale.y, 1));
            return PopMatrix * scaleMatrix * PushMatrix;
        }

        public static Vector2 GetAspectScale(float srcAspect, float dstAspect, AspectMode mode)
        {
            bool isSrcWider = srcAspect > dstAspect;
            return (mode, isSrcWider) switch
            {
                (AspectMode.None, _) => new Vector2(1, 1),
                (AspectMode.Fit, true) => new Vector2(1, srcAspect / dstAspect),
                (AspectMode.Fit, false) => new Vector2(dstAspect / srcAspect, 1),
                (AspectMode.Fill, true) => new Vector2(dstAspect / srcAspect, 1),
                (AspectMode.Fill, false) => new Vector2(1, srcAspect / dstAspect),
                _ => throw new Exception("Unknown aspect mode"),
            };
        }

        private static void GpuReadBackCallback(AsyncGPUReadbackRequest request)
        {
            if (request.hasError)
            {
                Debug.LogError("GPU readback error");
            }
        }
    }
}
