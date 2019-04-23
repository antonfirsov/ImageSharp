﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.ParallelUtils;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Memory;
using SixLabors.Primitives;

namespace SixLabors.ImageSharp
{
    /// <summary>
    /// Represents a single frame in a animation.
    /// </summary>
    /// <typeparam name="TPixel">The pixel format.</typeparam>
    public sealed class ImageFrame<TPixel> : IPixelSource<TPixel>, IDisposable
        where TPixel : struct, IPixel<TPixel>
    {
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageFrame{TPixel}" /> class.
        /// </summary>
        /// <param name="configuration">The configuration which allows altering default behaviour or extending the library.</param>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="height">The height of the image in pixels.</param>
        internal ImageFrame(Configuration configuration, int width, int height)
            : this(configuration, width, height, new ImageFrameMetadata())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageFrame{TPixel}" /> class.
        /// </summary>
        /// <param name="configuration">The configuration which allows altering default behaviour or extending the library.</param>
        /// <param name="size">The <see cref="Size"/> of the frame.</param>
        /// <param name="metadata">The metadata.</param>
        internal ImageFrame(Configuration configuration, Size size, ImageFrameMetadata metadata)
            : this(configuration, size.Width, size.Height, metadata)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageFrame{TPixel}" /> class.
        /// </summary>
        /// <param name="configuration">The configuration which allows altering default behaviour or extending the library.</param>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="height">The height of the image in pixels.</param>
        /// <param name="metadata">The metadata.</param>
        internal ImageFrame(Configuration configuration, int width, int height, ImageFrameMetadata metadata)
            : this(configuration, width, height, default(TPixel), metadata)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageFrame{TPixel}" /> class.
        /// </summary>
        /// <param name="configuration">The configuration which allows altering default behaviour or extending the library.</param>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="height">The height of the image in pixels.</param>
        /// <param name="backgroundColor">The color to clear the image with.</param>
        internal ImageFrame(Configuration configuration, int width, int height, TPixel backgroundColor)
            : this(configuration, width, height, backgroundColor, new ImageFrameMetadata())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageFrame{TPixel}" /> class.
        /// </summary>
        /// <param name="configuration">The configuration which allows altering default behaviour or extending the library.</param>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="height">The height of the image in pixels.</param>
        /// <param name="backgroundColor">The color to clear the image with.</param>
        /// <param name="metadata">The metadata.</param>
        internal ImageFrame(Configuration configuration, int width, int height, TPixel backgroundColor, ImageFrameMetadata metadata)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.MustBeGreaterThan(width, 0, nameof(width));
            Guard.MustBeGreaterThan(height, 0, nameof(height));

            this.Configuration = configuration;
            this.MemoryAllocator = configuration.MemoryAllocator;
            this.PixelBuffer = this.MemoryAllocator.Allocate2D<TPixel>(width, height);
            this.Metadata = metadata ?? new ImageFrameMetadata();
            this.Clear(configuration.GetParallelOptions(), backgroundColor);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageFrame{TPixel}" /> class wrapping an existing buffer.
        /// </summary>
        /// <param name="configuration">The configuration providing initialization code which allows extending the library.</param>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="height">The height of the image in pixels.</param>
        /// <param name="memorySource">The memory source.</param>
        internal ImageFrame(Configuration configuration, int width, int height, MemorySource<TPixel> memorySource)
            : this(configuration, width, height, memorySource, new ImageFrameMetadata())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageFrame{TPixel}" /> class wrapping an existing buffer.
        /// </summary>
        /// <param name="configuration">The configuration providing initialization code which allows extending the library.</param>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="height">The height of the image in pixels.</param>
        /// <param name="memorySource">The memory source.</param>
        /// <param name="metadata">The metadata.</param>
        internal ImageFrame(Configuration configuration, int width, int height, MemorySource<TPixel> memorySource, ImageFrameMetadata metadata)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.MustBeGreaterThan(width, 0, nameof(width));
            Guard.MustBeGreaterThan(height, 0, nameof(height));
            Guard.NotNull(metadata, nameof(metadata));

            this.Configuration = configuration;
            this.MemoryAllocator = configuration.MemoryAllocator;
            this.PixelBuffer = new Buffer2D<TPixel>(memorySource, width, height);
            this.Metadata = metadata;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageFrame{TPixel}" /> class.
        /// </summary>
        /// <param name="configuration">The configuration which allows altering default behaviour or extending the library.</param>
        /// <param name="source">The source.</param>
        internal ImageFrame(Configuration configuration, ImageFrame<TPixel> source)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(source, nameof(source));

            this.Configuration = configuration;
            this.MemoryAllocator = configuration.MemoryAllocator;
            this.PixelBuffer = this.MemoryAllocator.Allocate2D<TPixel>(source.PixelBuffer.Width, source.PixelBuffer.Height);
            source.PixelBuffer.GetSpan().CopyTo(this.PixelBuffer.GetSpan());
            this.Metadata = source.Metadata.DeepClone();
        }

        /// <summary>
        /// Gets the <see cref="MemoryAllocator" /> to use for buffer allocations.
        /// </summary>
        public MemoryAllocator MemoryAllocator { get; }

        /// <summary>
        /// Gets the <see cref="Configuration"/> instance associated with this <see cref="ImageFrame{TPixel}"/>.
        /// </summary>
        internal Configuration Configuration { get; }

        /// <summary>
        /// Gets the image pixels. Not private as Buffer2D requires an array in its constructor.
        /// </summary>
        internal Buffer2D<TPixel> PixelBuffer { get; private set; }

        /// <inheritdoc/>
        Buffer2D<TPixel> IPixelSource<TPixel>.PixelBuffer => this.PixelBuffer;

        /// <summary>
        /// Gets the width.
        /// </summary>
        public int Width => this.PixelBuffer.Width;

        /// <summary>
        /// Gets the height.
        /// </summary>
        public int Height => this.PixelBuffer.Height;

        /// <summary>
        /// Gets the metadata of the frame.
        /// </summary>
        public ImageFrameMetadata Metadata { get; }

        /// <summary>
        /// Gets or sets the pixel at the specified position.
        /// </summary>
        /// <param name="x">The x-coordinate of the pixel. Must be greater than or equal to zero and less than the width of the image.</param>
        /// <param name="y">The y-coordinate of the pixel. Must be greater than or equal to zero and less than the height of the image.</param>
        /// <returns>The <see typeparam="TPixel"/> at the specified position.</returns>
        public TPixel this[int x, int y]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.PixelBuffer[x, y];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.PixelBuffer[x, y] = value;
        }

        /// <summary>
        /// Gets the size of the frame.
        /// </summary>
        /// <returns>The <see cref="Size"/></returns>
        public Size Size() => new Size(this.Width, this.Height);

        /// <summary>
        /// Gets the bounds of the frame.
        /// </summary>
        /// <returns>The <see cref="Rectangle"/></returns>
        public Rectangle Bounds() => new Rectangle(0, 0, this.Width, this.Height);

        /// <summary>
        /// Gets a reference to the pixel at the specified position.
        /// </summary>
        /// <param name="x">The x-coordinate of the pixel. Must be greater than or equal to zero and less than the width of the image.</param>
        /// <param name="y">The y-coordinate of the pixel. Must be greater than or equal to zero and less than the height of the image.</param>
        /// <returns>The <see typeparam="TPixel"/> at the specified position.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref TPixel GetPixelReference(int x, int y) => ref this.PixelBuffer[x, y];

        /// <summary>
        /// Copies the pixels to a <see cref="Buffer2D{TPixel}"/> of the same size.
        /// </summary>
        /// <param name="target">The target pixel buffer accessor.</param>
        internal void CopyTo(Buffer2D<TPixel> target)
        {
            if (this.Size() != target.Size())
            {
                throw new ArgumentException("ImageFrame<TPixel>.CopyTo(): target must be of the same size!", nameof(target));
            }

            this.GetPixelSpan().CopyTo(target.GetSpan());
        }

        /// <summary>
        /// Switches the buffers used by the image and the pixelSource meaning that the Image will "own" the buffer from the pixelSource and the pixelSource will now own the Images buffer.
        /// </summary>
        /// <param name="pixelSource">The pixel source.</param>
        internal void SwapOrCopyPixelsBufferFrom(ImageFrame<TPixel> pixelSource)
        {
            Guard.NotNull(pixelSource, nameof(pixelSource));

            Buffer2D<TPixel>.SwapOrCopyContent(this.PixelBuffer, pixelSource.PixelBuffer);
        }

        /// <summary>
        /// Disposes the object and frees resources for the Garbage Collector.
        /// </summary>
        internal void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.PixelBuffer?.Dispose();
            this.PixelBuffer = null;

            // Note disposing is done.
            this.isDisposed = true;
        }

        /// <inheritdoc/>
        public override string ToString() => $"ImageFrame<{typeof(TPixel).Name}>({this.Width}x{this.Height})";

        /// <summary>
        /// Clones the current instance.
        /// </summary>
        /// <returns>The <see cref="ImageFrame{TPixel}"/></returns>
        internal ImageFrame<TPixel> Clone() => this.Clone(this.Configuration);

        /// <summary>
        /// Clones the current instance.
        /// </summary>
        /// <param name="configuration">The configuration providing initialization code which allows extending the library.</param>
        /// <returns>The <see cref="ImageFrame{TPixel}"/></returns>
        internal ImageFrame<TPixel> Clone(Configuration configuration) => new ImageFrame<TPixel>(configuration, this);

        /// <summary>
        /// Returns a copy of the image frame in the given pixel format.
        /// </summary>
        /// <typeparam name="TPixel2">The pixel format.</typeparam>
        /// <returns>The <see cref="ImageFrame{TPixel2}"/></returns>
        internal ImageFrame<TPixel2> CloneAs<TPixel2>()
            where TPixel2 : struct, IPixel<TPixel2> => this.CloneAs<TPixel2>(this.Configuration);

        /// <summary>
        /// Returns a copy of the image frame in the given pixel format.
        /// </summary>
        /// <typeparam name="TPixel2">The pixel format.</typeparam>
        /// <param name="configuration">The configuration providing initialization code which allows extending the library.</param>
        /// <returns>The <see cref="ImageFrame{TPixel2}"/></returns>
        internal ImageFrame<TPixel2> CloneAs<TPixel2>(Configuration configuration)
            where TPixel2 : struct, IPixel<TPixel2>
        {
            if (typeof(TPixel2) == typeof(TPixel))
            {
                return this.Clone(configuration) as ImageFrame<TPixel2>;
            }

            var target = new ImageFrame<TPixel2>(configuration, this.Width, this.Height, this.Metadata.DeepClone());

            ParallelHelper.IterateRows(
                this.Bounds(),
                configuration,
                (rows) =>
                    {
                        for (int y = rows.Min; y < rows.Max; y++)
                        {
                            Span<TPixel> sourceRow = this.GetPixelRowSpan(y);
                            Span<TPixel2> targetRow = target.GetPixelRowSpan(y);
                            PixelOperations<TPixel>.Instance.To(configuration, sourceRow, targetRow);
                        }
                    });

            return target;
        }

        /// <summary>
        /// Clears the bitmap.
        /// </summary>
        /// <param name="parallelOptions">The parallel options.</param>
        /// <param name="value">The value to initialize the bitmap with.</param>
        internal void Clear(ParallelOptions parallelOptions, TPixel value)
        {
            Span<TPixel> span = this.GetPixelSpan();

            if (value.Equals(default))
            {
                span.Clear();
            }
            else
            {
                span.Fill(value);
            }
        }

        /// <inheritdoc/>
        void IDisposable.Dispose() => this.Dispose();
    }
}