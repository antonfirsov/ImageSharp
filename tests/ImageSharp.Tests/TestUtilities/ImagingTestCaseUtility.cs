﻿// <copyright file="ImagingTestCaseUtility.cs" company="James Jackson-South">
// Copyright (c) James Jackson-South and contributors.
// Licensed under the Apache License, Version 2.0.
// </copyright>
namespace ImageSharp.Tests.TestUtilities
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Utility class to provide information about the test image & the test case for the test code,
    /// and help managing IO.
    /// </summary>
    public class ImagingTestCaseUtility
    {
        /// <summary>
        /// Name of the TColor in the owner <see cref="TestImageFactory{TColor,TPacked}"/>
        /// </summary>
        public string PixelTypeName { get; set; } = string.Empty;

        /// <summary>
        /// The name of the file which is provided by <see cref="TestImageFactory{TColor,TPacked}"/>
        /// Or a short string describing the image in the case of a non-file based image provider.
        /// </summary>
        public string SourceFileOrDescription { get; set; } = string.Empty;

        /// <summary>
        /// The name of the test class (by default)
        /// </summary>
        public string TestGroupName { get; set; } = string.Empty;

        /// <summary>
        /// The name of the test case (by default)
        /// </summary>
        public string TestName { get; set; } = string.Empty;

        /// <summary>
        /// Root directory for output images
        /// </summary>
        public string TestOutputRoot { get; set; } = FileTestBase.TestOutputRoot;

        public string GetTestOutputDir()
        {
            string testGroupName = Path.GetFileNameWithoutExtension(this.TestGroupName);

            string dir = $@"{this.TestOutputRoot}{testGroupName}";
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Gets the recommended file name for the output of the test
        /// </summary>
        /// <param name="extension"></param>
        /// <returns>The required extension</returns>
        public string GetTestOutputFileName(string extension = null)
        {
            string fn = string.Empty;

            fn = Path.GetFileNameWithoutExtension(this.SourceFileOrDescription);
            extension = extension ?? Path.GetExtension(this.SourceFileOrDescription);
            extension = extension ?? ".bmp";

            if (extension[0] != '.')
            {
                extension = '.' + extension;
            }

            if (fn != string.Empty) fn = '_' + fn;

            string pixName = this.PixelTypeName;
            if (pixName != string.Empty)
            {
                pixName = '_' + pixName + ' ';
            }

            return $"{this.GetTestOutputDir()}/{this.TestName}{pixName}{fn}{extension}";
        }

        /// <summary>
        /// Encodes image by the format matching the required extension, than saves it to the recommended output file.
        /// </summary>
        /// <typeparam name="TColor">The pixel format of the image</typeparam>
        /// <typeparam name="TPacked">The packed format of the image</typeparam>
        /// <param name="image">The image instance</param>
        /// <param name="extension">The requested extension</param>
        public void SaveTestOutputFile<TColor, TPacked>(Image<TColor, TPacked> image, string extension = null)
            where TColor : struct, IPackedPixel<TPacked> where TPacked : struct, IEquatable<TPacked>
        {
            string path = this.GetTestOutputFileName(extension);

            var format = Bootstrapper.Instance.ImageFormats.First(f => f.Encoder.IsSupportedFileExtension(extension));

            using (var stream = File.OpenWrite(path))
            {
                image.Save(stream, format);
            }
        }

        internal void Init(MethodInfo method)
        {
            this.TestGroupName = method.DeclaringType.Name;
            this.TestName = method.Name;
        }
    }
}