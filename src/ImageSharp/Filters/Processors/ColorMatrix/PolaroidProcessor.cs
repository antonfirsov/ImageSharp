﻿// <copyright file="PolaroidProcessor.cs" company="James Jackson-South">
// Copyright (c) James Jackson-South and contributors.
// Licensed under the Apache License, Version 2.0.
// </copyright>

namespace ImageSharp.Processors
{
    using System;
    using System.Numerics;

    /// <summary>
    /// Converts the colors of the image recreating an old Polaroid effect.
    /// </summary>
    /// <typeparam name="TColor">The pixel format.</typeparam>
    /// <typeparam name="TPacked">The packed format. <example>uint, long, float.</example></typeparam>
    public class PolaroidProcessor<TColor, TPacked> : ColorMatrixFilter<TColor, TPacked>
        where TColor : struct, IPackedPixel<TPacked>
        where TPacked : struct, IEquatable<TPacked>
    {
        /// <inheritdoc/>
        public override Matrix4x4 Matrix => new Matrix4x4()
        {
            M11 = 1.538F,
            M12 = -0.062F,
            M13 = -0.262F,
            M21 = -0.022F,
            M22 = 1.578F,
            M23 = -0.022F,
            M31 = .216F,
            M32 = -.16F,
            M33 = 1.5831F,
            M41 = 0.02F,
            M42 = -0.05F,
            M43 = -0.05F,
            M44 = 1
        };

        /// <inheritdoc/>
        protected override void AfterApply(ImageBase<TColor, TPacked> source, Rectangle sourceRectangle)
        {
            TColor packedV = default(TColor);
            packedV.PackFromVector4(new Color(102, 34, 0).ToVector4()); // Very dark orange [Brown tone]
            new VignetteProcessor<TColor, TPacked> { VignetteColor = packedV }.Apply(source, sourceRectangle);

            TColor packedG = default(TColor);
            packedG.PackFromVector4(new Color(255, 153, 102, 178).ToVector4()); // Light orange
            new GlowProcessor<TColor, TPacked> { GlowColor = packedG, Radius = source.Width / 4F }.Apply(source, sourceRectangle);
        }
    }
}