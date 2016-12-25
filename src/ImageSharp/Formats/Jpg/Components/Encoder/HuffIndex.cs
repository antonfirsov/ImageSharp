﻿// <copyright file="HuffIndex.cs" company="James Jackson-South">
// Copyright (c) James Jackson-South and contributors.
// Licensed under the Apache License, Version 2.0.
// </copyright>
namespace ImageSharp.Formats.Jpg.Components.Encoder
{
    /// <summary>
    /// Enumerates the Huffman tables
    /// </summary>
    internal enum HuffIndex
    {
        /// <summary>
        /// The DC luminance huffman table index
        /// </summary>
        LuminanceDC = 0,

        // ReSharper disable UnusedMember.Local

        /// <summary>
        /// The AC luminance huffman table index
        /// </summary>
        LuminanceAC = 1,

        /// <summary>
        /// The DC chrominance huffman table index
        /// </summary>
        ChrominanceDC = 2,

        /// <summary>
        /// The AC chrominance huffman table index
        /// </summary>
        ChrominanceAC = 3,

        // ReSharper restore UnusedMember.Local
    }
}