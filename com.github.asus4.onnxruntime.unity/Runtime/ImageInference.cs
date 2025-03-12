#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using UnityEngine;
using Unity.Profiling;

namespace Microsoft.ML.OnnxRuntime.Unity
{
    /// <summary>
    /// Base class for the model that has image input
    /// </summary>
    /// <typeparam name="T">Type of model input. ex: float or short</typeparam>
    public class ImageInference<T> : IDisposable
        where T : unmanaged
    {
        public readonly ImageInferenceOptions imageOptions;

        protected readonly InferenceSession session;
        protected readonly SessionOptions sessionOptions;
        protected readonly RunOptions runOptions;
        protected ReadOnlyCollection<OrtValue> inputs;
        protected ReadOnlyCollection<OrtValue> outputs;

        protected readonly bool isDynamicInputShape;
        protected readonly bool isDynamicOutputShape;
        protected TextureToTensor<T> textureToTensor;
        protected int Width => textureToTensor.width;
        protected int Height => textureToTensor.height;

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
                runOptions = new RunOptions();
            }
            catch (Exception e)
            {
                session?.Dispose();
                this.sessionOptions?.Dispose();
                runOptions?.Dispose();
                throw e;
            }
            session.LogIOInfo();

            // Check if the model has dynamic shape
            isDynamicInputShape = session.InputMetadata.Values.Any(meta => meta.ContainsDynamic());
            isDynamicOutputShape = session.OutputMetadata.Values.Any(meta => meta.ContainsDynamic());

            // Allocate inputs/outputs
            inputs = isDynamicInputShape
                ? new List<OrtValue>().AsReadOnly()
                : AllocateTensors(session.InputMetadata);
            outputs = isDynamicOutputShape
                ? new List<OrtValue>().AsReadOnly()
                : AllocateTensors(session.OutputMetadata);
            textureToTensor = isDynamicInputShape
                ? CreateTextureToTensor(64, 64) // Allocate with dummy size
                : CreateTextureToTensor(session.InputMetadata);
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
                runOptions?.Dispose();
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

            if (isDynamicOutputShape)
            {
                // Run inference
                runPerfMarker.Begin();
                using var disposableOutputs = session.Run(runOptions, session.InputNames, inputs, session.OutputNames);
                runPerfMarker.End();

                // Post process
                postprocessPerfMarker.Begin();
                PostProcess(disposableOutputs);
                postprocessPerfMarker.End();
            }
            else
            {
                // Run inference
                runPerfMarker.Begin();
                session.Run(runOptions, session.InputNames, inputs, session.OutputNames, outputs);
                runPerfMarker.End();

                // Post process
                postprocessPerfMarker.Begin();
                PostProcess(outputs);
                postprocessPerfMarker.End();
            }
        }

        /// <summary>
        /// Preprocess to convert texture to tensor.
        /// Override this method if you need to do custom preprocessing.
        /// </summary>
        /// <param name="texture">a texture</param>
        protected virtual void PreProcess(Texture texture)
        {
            if (isDynamicInputShape && inputs.Count == 0)
            {
                throw new NotSupportedException("Override this method to create inputs");
            }
            var tensorData = textureToTensor.Transform(texture, imageOptions.aspectMode);
            tensorData.CopyTo(inputs[0].GetTensorMutableDataAsSpan<T>());
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
        /// Run inference with the given texture
        /// </summary>
        /// <param name="texture">any type of texture</param>
        /// <returns>The task</returns>
        public virtual async Awaitable RunAsync(Texture texture, CancellationToken cancellationToken)
        {
            if (isDynamicInputShape)
            {
                // TODO: implement dynamic shape
                throw new NotImplementedException();
            }
            cancellationToken.ThrowIfCancellationRequested();

            // Pre process
            await PreProcessAsync(texture, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // Run inference
            await Awaitable.BackgroundThreadAsync();
            session.Run(runOptions, session.InputNames, inputs, session.OutputNames, outputs);
            cancellationToken.ThrowIfCancellationRequested();

            // Post process
            await PostProcessAsync(outputs, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Preprocess to convert texture to tensor.
        /// Override this method if you need to do custom preprocessing.
        /// </summary>
        /// <param name="texture">A input texture</param>
        protected virtual async Awaitable PreProcessAsync(Texture texture, CancellationToken cancellationToken)
        {
            await Awaitable.MainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();
            var tensorData = await textureToTensor.TransformAsync(texture, imageOptions.aspectMode, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            tensorData.AsReadOnlySpan().CopyTo(inputs[0].GetTensorMutableDataAsSpan<T>());
        }

        /// <summary>
        /// Async version of Postprocess
        /// Override this method if you need.
        /// </summary>
        protected async virtual Awaitable PostProcessAsync(IReadOnlyList<OrtValue> outputs, CancellationToken cancellationToken)
        {
            await Awaitable.MainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();
            PostProcess(outputs);
        }
#endif // UNITY_2023_1_OR_NEWER

        protected virtual TextureToTensor<T> CreateTextureToTensor(IReadOnlyDictionary<string, NodeMetadata> inputMetadata)
        {
            if (isDynamicInputShape)
            {
                throw new NotSupportedException("Dynamic shape is not supported. Override this method to create TextureToTensor");
            }

            // Find image input
            static bool IsSupportedShape(int[] shape)
            {
                int channels = shape.Length switch
                {
                    4 => shape[0] == 1 ? shape[1] : 0, // NCWH
                    3 => shape[0], // CHW
                    _ => 0
                };
                // Only RGB is supported for now
                return channels == 3;
            }
            var shape = inputMetadata.Values
                .Where(metadata => metadata.IsTensor)
                .Where(metadata => IsSupportedShape(metadata.Dimensions))
                .Select(metadata => metadata.Dimensions)
                .FirstOrDefault()
                ?? throw new NotSupportedException("No supported input found. Override this method to create TextureToTensor");

            int width = shape[3];
            int height = shape[2];
            return CreateTextureToTensor(width, height);
        }

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
    }
}
