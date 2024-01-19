#nullable enable

using System;
using System.Collections.Generic;
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
        protected readonly string[] inputNames;
        protected readonly OrtValue[] inputs;
        protected readonly string[] outputNames;
        protected readonly OrtValue[] outputs;

        protected readonly TextureToTensor<T> textureToTensor;

        protected readonly string inputImageKey;
        protected readonly int channels;
        protected readonly int height;
        protected readonly int width;

        public Texture InputTexture => textureToTensor.Texture;
        public Matrix4x4 InputToViewportMatrix => textureToTensor.TransformMatrix;

        // Profilers
        static readonly ProfilerMarker preprocessPerfMarker = new($"{typeof(ImageInference<T>).Name}.Preprocess");
        static readonly ProfilerMarker runPerfMarker = new($"{typeof(ImageInference<T>).Name}.Session.Run");
        static readonly ProfilerMarker postprocessPerfMarker = new($"{typeof(ImageInference<T>).Name}.Postprocess");

        /// <summary>
        /// Create an inference that has Image input
        /// </summary>
        /// <param name="model">byte array of the Ort model</param>
        public ImageInference(byte[] model, ImageInferenceOptions options)
        {
            imageOptions = options;

            try
            {
                sessionOptions = new SessionOptions();
                options.executionProvider.AppendExecutionProviders(sessionOptions);
                session = new InferenceSession(model, sessionOptions);
            }
            catch (Exception e)
            {
                session?.Dispose();
                sessionOptions?.Dispose();
                throw e;
            }
            session.LogIOInfo();

            // Allocate inputs/outputs
            (inputNames, inputs) = AllocateTensors(session.InputMetadata);
            (outputNames, outputs) = AllocateTensors(session.OutputMetadata);

            // Find image input info
            foreach (var kv in session.InputMetadata)
            {
                NodeMetadata meta = kv.Value;
                if (meta.IsTensor)
                {
                    int[] shape = meta.Dimensions;
                    if (IsSupportedImage(meta.Dimensions))
                    {
                        inputImageKey = kv.Key;
                        channels = shape[1];
                        height = shape[2];
                        width = shape[3];
                        break;
                    }
                }
            }
            if (inputImageKey == null)
            {
                throw new ArgumentException("Image input not found");
            }

            textureToTensor = new TextureToTensor<T>(width, height)
            {
                Mean = options.mean,
                Std = options.std
            };
        }

        public virtual void Dispose()
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

        public virtual void Run(Texture texture)
        {
            // Pre process
            preprocessPerfMarker.Begin();
            PreProcess(texture);
            preprocessPerfMarker.End();

            // Run inference
            runPerfMarker.Begin();
            session.Run(null, inputNames, inputs, outputNames, outputs);
            runPerfMarker.End();

            // Post process
            postprocessPerfMarker.Begin();
            PostProcess();
            postprocessPerfMarker.End();
        }

        protected virtual void PreProcess(Texture texture)
        {
            textureToTensor.Transform(texture, imageOptions.aspectMode);
            var inputSpan = inputs[0].GetTensorMutableDataAsSpan<T>();
            textureToTensor.TensorData.CopyTo(inputSpan);
        }

        protected virtual void PostProcess()
        {
            // Override this in subclass
        }

        private static (string[], OrtValue[]) AllocateTensors(IReadOnlyDictionary<string, NodeMetadata> metadata)
        {
            var names = new List<string>();
            var values = new List<OrtValue>();

            foreach (var kv in metadata)
            {
                NodeMetadata meta = kv.Value;
                if (meta.IsTensor)
                {
                    names.Add(kv.Key);
                    values.Add(meta.CreateTensorOrtValue());
                }
                else
                {
                    throw new ArgumentException("Only tensor input is supported");
                }
            }
            return (names.ToArray(), values.ToArray());
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
            // return channels == 1 || channels == 3 || channels == 4;
        }

    }
}
