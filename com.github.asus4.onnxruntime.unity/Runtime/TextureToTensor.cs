namespace Microsoft.ML.OnnxRuntime.Unity
{
    using System;
    using System.Runtime.InteropServices;
    using UnityEngine;

    /// <summary>
    /// Port from TextureSource: MIT License
    /// https://github.com/asus4/TextureSource
    /// </summary>
    public class TextureToTensor<T> : IDisposable
        where T : unmanaged
    {
        private static ComputeShader compute;
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

        private readonly int kernel;
        private readonly RenderTexture texture;
        private readonly GraphicsBuffer tensor;
        private const int CHANNELS = 3; // RGB for now
        public readonly int width;
        public readonly int height;

        private readonly T[] tensorData;
        public RenderTexture Texture => texture;
        public ReadOnlySpan<T> TensorData => tensorData;
        public Matrix4x4 TransformMatrix { get; private set; } = Matrix4x4.identity;

        public TextureToTensor(int width, int height)
        {
            this.width = width;
            this.height = height;

            var desc = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32)
            {
                enableRandomWrite = true,
                useMipMap = false,
                depthBufferBits = 0,
            };
            texture = new RenderTexture(desc);
            texture.Create();

            int stride = Marshal.SizeOf(default(T));
            tensor = new GraphicsBuffer(GraphicsBuffer.Target.Structured, CHANNELS * width * height, stride);
            tensorData = new T[CHANNELS * width * height];

            if (compute == null)
            {
                const string SHADER_PATH = "com.github.asus4.onnxruntime.unity/TextureToTensor";
                compute = Resources.Load<ComputeShader>(SHADER_PATH);
            }
            kernel = compute.FindKernel("TextureToTensor");
        }

        public void Dispose()
        {
            texture.Release();
            UnityEngine.Object.Destroy(texture);
            tensor.Release();
        }

        public void Transform(Texture input, Matrix4x4 t)
        {
            TransformMatrix = t;

            compute.SetTexture(kernel, _InputTex, input, 0);
            compute.SetTexture(kernel, _OutputTex, texture, 0);
            compute.SetBuffer(kernel, _OutputTensor, tensor);
            compute.SetMatrix(_TransformMatrix, t);
            compute.SetInts(_OutputSize, texture.width, texture.height);
            compute.SetFloats(_Mean, Mean.x, Mean.y, Mean.z);
            compute.SetFloats(_StdDev, Std.x, Std.y, Std.z);

            compute.Dispatch(kernel, Mathf.CeilToInt(texture.width / 8f), Mathf.CeilToInt(texture.height / 8f), 1);

            tensor.GetData(tensorData);
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
            switch (mode)
            {
                case AspectMode.None:
                    return new Vector2(1, 1);
                case AspectMode.Fit:
                    if (srcAspect > dstAspect)
                    {
                        float s = srcAspect / dstAspect;
                        return new Vector2(1, s);
                    }
                    else
                    {
                        float s = dstAspect / srcAspect;
                        return new Vector2(s, 1);
                    }
                case AspectMode.Fill:
                    if (srcAspect > dstAspect)
                    {
                        float s = dstAspect / srcAspect;
                        return new Vector2(s, 1);
                    }
                    else
                    {
                        float s = srcAspect / dstAspect;
                        return new Vector2(1, s);
                    }
                default:
                    throw new Exception("Unknown aspect mode");
            }
        }
    }
}
