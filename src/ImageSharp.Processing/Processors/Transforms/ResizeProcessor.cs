﻿// <copyright file="ResizeProcessor.cs" company="James Jackson-South">
// Copyright (c) James Jackson-South and contributors.
// Licensed under the Apache License, Version 2.0.
// </copyright>

namespace ImageSharp.Processing.Processors
{
    using System;
    using System.Buffers;
    using System.Numerics;
    using System.Threading.Tasks;

    using ImageSharp.Experimental;

    /// <summary>
    /// Provides methods that allow the resizing of images using various algorithms.
    /// </summary>
    /// <remarks>
    /// This version and the <see cref="CompandingResizeProcessor{TColor}"/> have been separated out to improve performance.
    /// </remarks>
    /// <typeparam name="TColor">The pixel format.</typeparam>
    public class ResizeProcessor<TColor> : ResamplingWeightedProcessor<TColor>
        where TColor : struct, IPackedPixel, IEquatable<TColor>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResizeProcessor{TColor}"/> class.
        /// </summary>
        /// <param name="sampler">The sampler to perform the resize operation.</param>
        /// <param name="width">The target width.</param>
        /// <param name="height">The target height.</param>
        public ResizeProcessor(IResampler sampler, int width, int height)
            : base(sampler, width, height, new Rectangle(0, 0, width, height))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResizeProcessor{TColor}"/> class.
        /// </summary>
        /// <param name="sampler">The sampler to perform the resize operation.</param>
        /// <param name="width">The target width.</param>
        /// <param name="height">The target height.</param>
        /// <param name="resizeRectangle">
        /// The <see cref="Rectangle"/> structure that specifies the portion of the target image object to draw to.
        /// </param>
        public ResizeProcessor(IResampler sampler, int width, int height, Rectangle resizeRectangle)
            : base(sampler, width, height, resizeRectangle)
        {
        }

        private static readonly ArrayPool<Vector4> Vector4Pool = ArrayPool<Vector4>.Create(1024 * 1024 * 256, 50);

        private unsafe void OnApplyImplColor(
            ImageBase<Color> source,
            Rectangle sourceRectangle,
            int width,
            int height,
            int minX,
            int minY,
            int maxX,
            int maxY,
            int startX,
            int startY)
        {
            using (PixelAccessor<Color> targetPixels = new PixelAccessor<Color>(width, height))
            {
                Vector4[] firstPassPixels = Vector4Pool.Rent(width * source.Height);

                fixed (Vector4* fpPtr = firstPassPixels)
                {
                    using (PixelAccessor<Color> sourcePixels = source.Lock())
                    {
                        if (sourcePixels.Width % 2 != 0)
                        {
                            throw new NotImplementedException("Bzzz");
                        }

                        Parallel.For(
                            0,
                            sourceRectangle.Height,
                            this.ParallelOptions,
                            y =>
                                {
                                    Color* rowPtr = (Color*)sourcePixels.DataPointer;
                                    rowPtr += y * sourcePixels.Width;

                                    Vector4[] tempVectors = Vector4Pool.Rent(sourcePixels.Width);

                                    fixed (Vector4* vPtr = tempVectors)
                                    {
                                        ExperimentalConverters.ColorToVector4BithackBatched(
                                            rowPtr,
                                            vPtr,
                                            sourcePixels.Width);
                                    }

                                    for (int x = minX; x < maxX; x++)
                                    {
                                        // Ensure offsets are normalised for cropping and padding.

                                        Weights ws = this.HorizontalWeights.Weights[x - startX];
                                        float* horizontalValues = ws.Values;
                                        int left = ws.LeftIndex;

                                        // Destination color components
                                        Vector4 destination = Vector4.Zero;

                                        for (int i = 0; i < ws.Count; i++)
                                        {
                                            float xw = horizontalValues[i];
                                            int index = left + i;
                                            destination += tempVectors[index] * xw;
                                        }

                                        firstPassPixels[y * width + x] = destination;
                                    }

                                    Vector4Pool.Return(tempVectors);
                                });

                        // Now process the rows.
                        Parallel.For(
                            minY,
                            maxY,
                            this.ParallelOptions,
                            y =>
                                {
                                    // Ensure offsets are normalised for cropping and padding.
                                    var ws = this.VerticalWeights.Weights[y - startY];
                                    float* verticalValues = ws.Values;
                                    int left = ws.LeftIndex;

                                    for (int x = 0; x < width; x++)
                                    {
                                        // Destination color components
                                        Vector4 destination = Vector4.Zero;

                                        for (int i = 0; i < ws.Count; i++)
                                        {
                                            float yw = verticalValues[i];
                                            int index = left + i;
                                            destination += firstPassPixels[index * width + x] * yw;
                                        }

                                        Color d = default(Color);
                                        d.PackFromVector4(destination);
                                        targetPixels[x, y] = d;
                                    }
                                });

                        source.SwapPixelsBuffers(targetPixels);
                    }
                }

                Vector4Pool.Return(firstPassPixels);
            }
        }

        /// <inheritdoc/>
        protected override unsafe void OnApply(ImageBase<TColor> source, Rectangle sourceRectangle)
        {
            // Jump out, we'll deal with that later.
            if (source.Width == this.Width && source.Height == this.Height && sourceRectangle == this.ResizeRectangle)
            {
                return;
            }

            int width = this.Width;
            int height = this.Height;
            int startY = this.ResizeRectangle.Y;
            int endY = this.ResizeRectangle.Bottom;
            int startX = this.ResizeRectangle.X;
            int endX = this.ResizeRectangle.Right;

            int minX = Math.Max(0, startX);
            int maxX = Math.Min(width, endX);
            int minY = Math.Max(0, startY);
            int maxY = Math.Min(height, endY);

            if (this.Sampler is NearestNeighborResampler)
            {
                // Scaling factors
                float widthFactor = sourceRectangle.Width / (float)this.ResizeRectangle.Width;
                float heightFactor = sourceRectangle.Height / (float)this.ResizeRectangle.Height;

                using (PixelAccessor<TColor> targetPixels = new PixelAccessor<TColor>(width, height))
                {
                    using (PixelAccessor<TColor> sourcePixels = source.Lock())
                    {
                        Parallel.For(
                            minY,
                            maxY,
                            this.ParallelOptions,
                            y =>
                                {
                                    // Y coordinates of source points
                                    int originY = (int)((y - startY) * heightFactor);

                                    for (int x = minX; x < maxX; x++)
                                    {
                                        // X coordinates of source points
                                        targetPixels[x, y] = sourcePixels[(int)((x - startX) * widthFactor), originY];
                                    }
                                });
                    }

                    // Break out now.
                    source.SwapPixelsBuffers(targetPixels);
                    return;
                }
            }

            if (typeof(TColor) == typeof(Color))
            {
                this.OnApplyImplColor(
                    source as ImageBase<Color>,
                    sourceRectangle,
                    width,
                    height,
                    minX,
                    minY,
                    maxX,
                    maxY,
                    startX,
                    startY);
                return;
            }

            // Interpolate the image using the calculated weights.
            // A 2-pass 1D algorithm appears to be faster than splitting a 1-pass 2D algorithm
            // First process the columns. Since we are not using multiple threads startY and endY
            // are the upper and lower bounds of the source rectangle.
            using (PixelAccessor<TColor> targetPixels = new PixelAccessor<TColor>(width, height))
            {
                using (PixelAccessor<TColor> sourcePixels = source.Lock())
                using (PixelAccessor<TColor> firstPassPixels = new PixelAccessor<TColor>(width, source.Height))
                {
                    Parallel.For(
                        0,
                        sourceRectangle.Height,
                        this.ParallelOptions,
                        y =>
                            {
                                for (int x = minX; x < maxX; x++)
                                {
                                    // Ensure offsets are normalised for cropping and padding.
                                    Weights ws = this.HorizontalWeights.Weights[x - startX];
                                    float* horizontalValues = ws.Values;
                                    int index = ws.LeftIndex;

                                    // Destination color components
                                    Vector4 destination = Vector4.Zero;

                                    for (int i = 0; i < ws.Count; i++)
                                    {
                                        float xw = horizontalValues[i];
                                        destination += sourcePixels[index, y].ToVector4() * xw;
                                    }

                                    TColor d = default(TColor);
                                    d.PackFromVector4(destination);
                                    firstPassPixels[x, y] = d;
                                }
                            });

                    // Now process the rows.
                    Parallel.For(
                        minY,
                        maxY,
                        this.ParallelOptions,
                        y =>
                            {
                                // Ensure offsets are normalised for cropping and padding.
                                Weights ws = this.VerticalWeights.Weights[y - startY];
                                float* verticalValues = ws.Values;
                                int left = ws.LeftIndex;

                                for (int x = 0; x < width; x++)
                                {
                                    // Destination color components
                                    Vector4 destination = Vector4.Zero;

                                    for (int i = 0; i < ws.Count; i++)
                                    {
                                        float yw = verticalValues[i];
                                        int index = left + i;
                                        destination += firstPassPixels[x, index].ToVector4() * yw;
                                    }

                                    TColor d = default(TColor);
                                    d.PackFromVector4(destination);
                                    targetPixels[x, y] = d;
                                }
                            });
                }

                source.SwapPixelsBuffers(targetPixels);
            }
        }
    }
}