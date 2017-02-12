// <copyright file="ResamplingWeightedProcessor.cs" company="James Jackson-South">
// Copyright (c) James Jackson-South and contributors.
// Licensed under the Apache License, Version 2.0.
// </copyright>

namespace ImageSharp.Processing.Processors
{
    using System;
    using System.Buffers;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Provides methods that allow the resizing of images using various algorithms.
    /// Adapted from <see href="http://www.realtimerendering.com/resources/GraphicsGems/gemsiii/filter_rcg.c"/>
    /// </summary>
    /// <typeparam name="TColor">The pixel format.</typeparam>
    public abstract class ResamplingWeightedProcessor<TColor> : ImageProcessor<TColor>
        where TColor : struct, IPackedPixel, IEquatable<TColor>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResamplingWeightedProcessor{TColor}"/> class.
        /// </summary>
        /// <param name="sampler">The sampler to perform the resize operation.</param>
        /// <param name="width">The target width.</param>
        /// <param name="height">The target height.</param>
        /// <param name="resizeRectangle">
        /// The <see cref="Rectangle"/> structure that specifies the portion of the target image object to draw to.
        /// </param>
        protected ResamplingWeightedProcessor(IResampler sampler, int width, int height, Rectangle resizeRectangle)
        {
            Guard.NotNull(sampler, nameof(sampler));
            Guard.MustBeGreaterThan(width, 0, nameof(width));
            Guard.MustBeGreaterThan(height, 0, nameof(height));

            this.Sampler = sampler;
            this.Width = width;
            this.Height = height;
            this.ResizeRectangle = resizeRectangle;
        }

        /// <summary>
        /// Gets the sampler to perform the resize operation.
        /// </summary>
        public IResampler Sampler { get; }

        /// <summary>
        /// Gets the width.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the height.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets the resize rectangle.
        /// </summary>
        public Rectangle ResizeRectangle { get; }

        /// <summary>
        /// Gets or sets the horizontal weights.
        /// </summary>
        protected Weights.Block HorizontalWeights { get; set; }

        /// <summary>
        /// Gets or sets the vertical weights.
        /// </summary>
        protected Weights.Block VerticalWeights { get; set; }

        /// <inheritdoc/>
        protected override void BeforeApply(ImageBase<TColor> source, Rectangle sourceRectangle)
        {
            if (!(this.Sampler is NearestNeighborResampler))
            {
                this.HorizontalWeights = this.PrecomputeWeights(
                    this.ResizeRectangle.Width,
                    sourceRectangle.Width);

                this.VerticalWeights = this.PrecomputeWeights(
                    this.ResizeRectangle.Height,
                    sourceRectangle.Height);
            }
        }

        protected override void AfterApply(ImageBase<TColor> source, Rectangle sourceRectangle)
        {
            base.AfterApply(source, sourceRectangle);
            this.HorizontalWeights?.Dispose();
            this.HorizontalWeights = null;
            this.VerticalWeights?.Dispose();
            this.VerticalWeights = null;
        }

        /// <summary>
        /// Computes the weights to apply at each pixel when resizing.
        /// </summary>
        protected unsafe Weights.Block PrecomputeWeights(int destinationSize, int sourceSize)
        {
            float ratio = (float)sourceSize / destinationSize;
            float scale = ratio;

            if (scale < 1F)
            {
                scale = 1F;
            }

            IResampler sampler = this.Sampler;
            float radius = (float)Math.Ceiling(scale * sampler.Radius);
            Weights.Block result = new Weights.Block(sourceSize, destinationSize);
            //Weights[] result = new Weights[destinationSize];

            for (int i = 0; i < destinationSize; i++)
            {
                float center = ((i + .5F) * ratio) - .5F;

                // Keep inside bounds.
                int left = (int)Math.Ceiling(center - radius);
                if (left < 0)
                {
                    left = 0;
                }

                int right = (int)Math.Floor(center + radius);
                if (right > sourceSize - 1)
                {
                    right = sourceSize - 1;
                }

                float sum = 0;

                float* ptr = result.DataPtr + i * sourceSize;

                result.Weights[i] = new Weights(left, right, ptr);
                Weights ws = result.Weights[i];

                float* weights = ws.Values;
                //Weight[] weights = new Weight[right - left + 1];

                for (int j = left; j <= right; j++)
                {
                    float weight = sampler.GetValue((j - center) / scale);
                    sum += weight;
                    weights[j - left] = weight;
                }

                // Normalise, best to do it here rather than in the pixel loop later on.
                if (sum > 0)
                {
                    for (int w = 0; w < ws.Count; w++)
                    {
                        weights[w] = weights[w] / sum;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Represents a collection of weights and their sum.
        /// </summary>
        protected unsafe struct Weights
        {
            /// <summary>
            /// sdads
            /// </summary>
            public int LeftIndex;

            /// <summary>
            /// asda
            /// </summary>
            public int RightIndex;

            public int Count;
            
            //public float[] Values { get; }
            public float* Values;

            private static readonly ArrayPool<float> ArrayPool = ArrayPool<float>.Create(1024 * 16, 50);

            /// <summary>
            /// adsda
            /// </summary>
            public Weights(int leftIndex, int rightIndex, float* values)
            {
                this.LeftIndex = leftIndex;
                this.RightIndex = rightIndex;
                this.Count = rightIndex - leftIndex;
                this.Values = values;
            }

            public unsafe class Block : IDisposable
            {
                public float[] Data { get; }
                public Weights[] Weights { get; }
                public float* DataPtr { get; }

                private GCHandle handle;

                public Block(int sourceSize, int destinationSize)
                {
                    this.Data = ArrayPool<float>.Shared.Rent(sourceSize * destinationSize);
                    this.Weights = new Weights[destinationSize];
                    this.handle = GCHandle.Alloc(this.Data, GCHandleType.Pinned);
                    this.DataPtr = (float*)this.handle.AddrOfPinnedObject();
                }

                public void Dispose()
                {
                    this.handle.Free();
                    ArrayPool<float>.Shared.Return(this.Data);
                }
            }
        }
    }
}