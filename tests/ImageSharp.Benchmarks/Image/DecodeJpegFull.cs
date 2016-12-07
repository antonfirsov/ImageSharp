﻿namespace ImageSharp.Benchmarks.Image
{
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;

    using BenchmarkDotNet.Attributes;

    using Image = ImageSharp.Image;
    using ImageSharpSize = ImageSharp.Size;

    public class DecodeJpegFull
    {
        private Dictionary<string, byte[]> fileNamesToBytes;

        public enum JpegTestingMode
        {
            CalliphoraOnly,

            LargeImagesOnly,

            All
        }

        [Params(JpegTestingMode.All, JpegTestingMode.LargeImagesOnly, JpegTestingMode.CalliphoraOnly)]
        public JpegTestingMode Mode { get; set; }

        private IEnumerable<byte[]> RequestedImageBytes
        {
            get
            {
                if (this.Mode == JpegTestingMode.CalliphoraOnly)
                {
                    return new[] { this.fileNamesToBytes[JpegTestImages.Calliphora] };
                }
                else if (this.Mode == JpegTestingMode.LargeImagesOnly)
                {
                    return JpegTestImages.LargeOnly.Select(fn => this.fileNamesToBytes[fn]);
                }
                else
                {
                    return this.fileNamesToBytes.Values;
                }
            }
        }

        [Benchmark(Description = "DecodeJpegFull - ImageSharp")]
        public ImageSharpSize JpegImageSharp()
        {
            ImageSharpSize lastSize = new ImageSharpSize();
            foreach (byte[] data in this.RequestedImageBytes)
            {
                using (MemoryStream memoryStream = new MemoryStream(data))
                {
                    using (Image image = new ImageSharp.Image(memoryStream))
                    {
                        lastSize = new ImageSharpSize(image.Width, image.Height);
                    }
                }
            }

            return lastSize;
        }

        [Benchmark(Baseline = true, Description = "DecodeJpegFull - System.Drawing")]
        public Size JpegSystemDrawing()
        {
            Size lastSize = new System.Drawing.Size();
            foreach (byte[] data in this.RequestedImageBytes)
            {
                using (MemoryStream memoryStream = new MemoryStream(data))
                {
                    using (System.Drawing.Image image = System.Drawing.Image.FromStream(memoryStream))
                    {
                        lastSize = image.Size;
                    }
                }
            }

            return lastSize;
        }

        [Setup]
        public void ReadImages()
        {
            this.fileNamesToBytes = this.fileNamesToBytes
                                    ?? JpegTestImages.All.Distinct().ToDictionary(fn => fn, File.ReadAllBytes);
        }

        // TODO: Use file reference or common testing library?
        private static class JpegTestImages
        {
#if BENCHMARK46
            private static readonly string folder = "../../../ImageSharp.Tests/TestImages/Formats/Jpg/";
#else
            private static readonly string folder = "../ImageSharp.Tests/TestImages/Formats/Jpg/";
#endif

            public static string Cmyk => folder + "cmyk.jpg";

            public static string Exif => folder + "exif.jpg";

            public static string Floorplan => folder + "Floorplan.jpeg";

            public static string Calliphora => folder + "Calliphora.jpg";

            public static string Turtle => folder + "turtle.jpg";

            public static string Fb => folder + "fb.jpg";

            public static string Progress => folder + "progress.jpg";

            public static string GammaDalaiLamaGray => folder + "gamma_dalai_lama_gray.jpg";

            public static class ImageTestSuite
            {
                private static readonly string folder = JpegTestImages.folder + "imagetestsuite/";

                public static string Festzug => folder + "Festzug.jpg";

                public static string Gray1 => folder + "Festzug.jpg";

                public static string Gray2 => folder + "Festzug.jpg";

                public static string Hiyamugi => folder + "Hiyamugi.jpg";

                // TODO: Add to corrupted collection:
                public static string LongVertical1 => folder + "LongVertical1.jpg";

                public static string LongVertical2 => folder + "LongVertical2.jpg";
            }

            public static class Qualities
            {
                private static readonly string folder = JpegTestImages.folder + "qualities/";

                public static string Q60 => folder + "Q60.jpg";

                public static string Q70 => folder + "Q70.jpg";

                public static string Q80 => folder + "Q80.jpg";

                public static string Q90 => folder + "Q90.jpg";
            }

            public static readonly string[] All =
                {
                    Cmyk, Exif, Floorplan, Calliphora, Turtle, Fb, Progress,
                    GammaDalaiLamaGray, ImageTestSuite.Festzug, ImageTestSuite.Gray1,
                    ImageTestSuite.Gray2, ImageTestSuite.Hiyamugi,

                    // ImageTestSuite.LongVertical1, ImageTestSuite.LongVertical2, // TODO: Add to corrupted collection
                    Qualities.Q60, Qualities.Q70, Qualities.Q80, Qualities.Q90,
                };

            public static readonly string[] LargeOnly = { Calliphora, Cmyk, Floorplan, ImageTestSuite.Hiyamugi };
        }
    }
}