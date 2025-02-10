#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Profiling;

namespace Microsoft.ML.OnnxRuntime.Unity
{
    /// <summary>
    /// Base class for the model that has image input
    /// </summary>
    /// <typeparam name="T">Type of model input ex, float or short</typeparam>
    public class ImageInference<T> : IDisposable
        where T : unmanaged
    {
        public readonly ImageInferenceOptions imageOptions;

        protected readonly InferenceSession session;
        protected readonly SessionOptions sessionOptions;
        protected ReadOnlyCollection<OrtValue> inputs;
        protected ReadOnlyCollection<OrtValue> outputs;

        protected readonly bool isDynamicShape;
        protected TextureToTensor<T> textureToTensor;
        protected int height = -1;
        protected int width = -1;

        private bool disposed;

        /// <summary>
        /// Gets the input texture of the model
        /// </summary>
        public Texture InputTexture => textureToTensor.Texture;

        /// <summary>
        /// Gets the input to viewport matrix
        /// </summary>
        public Matrix4x4 InputToViewportMatrix => textureToTensor.TransformMatrix;

        // Profilers
        static readonly ProfilerMarker preprocessPerfMarker = new($"{typeof(ImageInference<T>).Name}.Preprocess");
        static readonly ProfilerMarker runPerfMarker = new($"{typeof(ImageInference<T>).Name}.Session.Run");
        static readonly ProfilerMarker postprocessPerfMarker = new($"{typeof(ImageInference<T>).Name}.Postprocess");

        /// <summary>
        /// Create an inference that has Image input
        /// </summary>
        /// <param name="model">byte array of the Ort model</param>
        /// <param name="imageOptions">Options for the image inference</param>
        /// <param name="sessionOptions">Custom session options. If null, default session options will be created.</param>
        public ImageInference(byte[] model, ImageInferenceOptions imageOptions, SessionOptions? sessionOptions = null)
        {
            this.imageOptions = imageOptions;

            try
            {
                this.sessionOptions = sessionOptions ?? new SessionOptions();
                imageOptions.executionProvider.AppendExecutionProviders(this.sessionOptions);
                session = new InferenceSession(model, this.sessionOptions);
            }
            catch (Exception e)
            {
                session?.Dispose();
                this.sessionOptions?.Dispose();
                throw e;
            }
            session.LogIOInfo();

            // Skip initialization if the model has dynamic shape
            isDynamicShape = session.ContainsDynamicInput();
            if (isDynamicShape)
            {
                inputs = new List<OrtValue>().AsReadOnly();
                outputs = new List<OrtValue>().AsReadOnly();
                textureToTensor = CreateTextureToTensor(64, 64);
                return;
            }

            // Allocate inputs/outputs
            inputs = AllocateTensors(session.InputMetadata);
            outputs = AllocateTensors(session.OutputMetadata);

            // Find image input info
            var shape = session.InputMetadata.Values
                .Where(metadata => metadata.IsTensor && IsSupportedImage(metadata.Dimensions))
                .Select(metadata => metadata.Dimensions)
                .FirstOrDefault();
            if (shape == null)
            {
                throw new ArgumentException("No supported image input found");
            }
            else
            {
                height = shape[2];
                width = shape[3];
            }
            textureToTensor = CreateTextureToTensor(width, height);
        }

        ~ImageInference()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }
            if (disposing)
            {
                textureToTensor?.Dispose();
                session?.Dispose();
                sessionOptions?.Dispose();
                foreach (var ortValue in inputs)
                {
                    ortValue.Dispose();
                }
                foreach (var ortValue in outputs)
                {
                    ortValue.Dispose();
                }
            }

            disposed = true;
        }

        /// <summary>
        /// Run inference with the given texture
        /// </summary>
        /// <param name="texture">any type of texture</param>
        public virtual void Run(Texture texture)
        {
            // Pre process
            preprocessPerfMarker.Begin();
            PreProcess(texture);
            preprocessPerfMarker.End();

            // Run inference
            runPerfMarker.Begin();
            session.Run(null, session.InputNames, inputs, session.OutputNames, outputs);
            runPerfMarker.End();

            // Post process
            postprocessPerfMarker.Begin();
            PostProcess(outputs);
            postprocessPerfMarker.End();
        }

        /// <summary>
        /// Run inference with the given texture
        /// </summary>
        /// <param name="texture">any type of texture</param>
        /// <returns>The task</returns>
        public virtual async Task RunAsync(Texture texture, CancellationToken cancellationToken)
        {
            if (isDynamicShape)
            {
                // TODO: implement dynamic shape
                throw new NotImplementedException();
            }

            // Pre process
#if UNITY_2023_1_OR_NEWER
            await PreProcessAsync(texture, cancellationToken);
#else
            PreProcess(texture);
#endif // UNITY_2023_1_OR_NEWER
            cancellationToken.ThrowIfCancellationRequested();

            // Run inference
            _ = await session.RunAsync(null, session.InputNames, inputs, session.OutputNames, outputs);
            cancellationToken.ThrowIfCancellationRequested();

            // Post process
#if UNITY_2023_1_OR_NEWER
            await PostProcessAsync(outputs);
#else
            PostProcess(outputs);
#endif // UNITY_2023_1_OR_NEWER
            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Preprocess to convert texture to tensor.
        /// Override this method if you need to do custom preprocessing.
        /// </summary>
        /// <param name="texture">a texture</param>
        protected virtual void PreProcess(Texture texture)
        {
            var inputSpan = inputs[0].GetTensorMutableDataAsSpan<T>();
            var tensorData = textureToTensor.Transform(texture, imageOptions.aspectMode);
            tensorData.CopyTo(inputSpan);
        }

        /// <summary>
        /// Postprocess to convert tensor to output.
        /// Need to override this method to get the output.
        /// </summary>
        protected virtual void PostProcess(IReadOnlyList<OrtValue> outputs)
        {
            // Override this in subclass
        }

        // Awaitable support
#if UNITY_2023_1_OR_NEWER

        /// <summary>
        /// Preprocess to convert texture to tensor.
        /// Override this method if you need to do custom preprocessing.
        /// </summary>
        /// <param name="texture">A input texture</param>
        protected virtual async Awaitable PreProcessAsync(Texture texture, CancellationToken cancellationToken)
        {
            var tensorData = await textureToTensor.TransformAsync(texture, imageOptions.aspectMode, cancellationToken);
            tensorData.AsReadOnlySpan().CopyTo(inputs[0].GetTensorMutableDataAsSpan<T>());
        }

        /// <summary>
        /// Async version of Postprocess
        /// Override this method if you need.
        /// </summary>
        protected async virtual Awaitable PostProcessAsync(IReadOnlyList<OrtValue> outputs)
        {
            await Awaitable.MainThreadAsync();
            PostProcess(outputs);
        }
#endif // UNITY_2023_1_OR_NEWER

        /// <summary>
        /// Create TextureToTensor instance.
        /// Override this method if you need to use custom TextureToTensor.
        /// </summary>
        /// <returns>A TextureToTensor<typeparamref name="T"/> instance</returns>
        protected virtual TextureToTensor<T> CreateTextureToTensor(int width, int height)
        {
            return new TextureToTensor<T>(width, height)
            {
                Mean = imageOptions.mean,
                Std = imageOptions.std
            };
        }

        private static ReadOnlyCollection<OrtValue> AllocateTensors(IReadOnlyDictionary<string, NodeMetadata> metadata)
        {
            var values = new List<OrtValue>();

            foreach (NodeMetadata meta in metadata.Values)
            {
                if (!meta.IsTensor)
                {
                    throw new ArgumentException("Only tensor input is supported");
                }
                values.Add(meta.CreateTensorOrtValue());
            }
            return values.AsReadOnly();
        }

        private static bool IsSupportedImage(int[] shape)
        {
            int channels = shape.Length switch
            {
                4 => shape[0] == 1 ? shape[1] : 0,
                3 => shape[0],
                _ => 0
            };
            // Only RGB is supported for now
            return channels == 3;
        }

    }
}
