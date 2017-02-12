using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImageSharp.Tests.Colors
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    using ImageSharp.Experimental;

    using Xunit;
    using Xunit.Abstractions;

    public class ExperimentalConvertersTests : MeasureFixture
    {
        public ExperimentalConvertersTests(ITestOutputHelper output)
            : base(output)
        {
        }
        
        [Theory]
        [InlineData(10, 20, 30, 255)]
        [InlineData(1, 2, 3, 4)]
        [InlineData(255, 254, 100, 42)]
        public void ColorToVector4Bithack(byte r, byte g, byte b, byte a)
        {
            Color color = new Color(r, g, b, a);

            Vector4 actual = ExperimentalConverters.ColorToVector4Bithack(color);
            Vector4 expected = color.ToVector4();
            

            Assert.Equal(expected, actual, new ApproximateFloatComparer(0.01f));
        }

        //[Theory]
        //[InlineData(0.1f, 0.2f, 0.3f, 0.4f)]
        //[InlineData(0.11f, 0.22f, 0.33f, 1.0f)]
        //[InlineData(0.111f, 0.222f, 0.333f, 1.1f)]
        //public void Vector4ToColorFast(float x, float y, float z, float w)
        //{
        //    Vector4 v = new Vector4(x, y, z, w);

        //    Color actual = ExperimentalConverters.Vector4ToColorFast(v);
        //    Color expected = new Color(v);

        //    Assert.Equal(actual, expected);
        //}
        
        private static Color[] GenerateColorInput(int count)
        {
            Color[] result = new Color[count];
            Random rnd = new Random(42);
            for (int i = 0; i < count; i++)
            {
                result[i] = new Color
                                {
                                    R = (byte)rnd.Next(255),
                                    G = (byte)rnd.Next(255),
                                    B = (byte)rnd.Next(255),
                                    A = (byte)rnd.Next(255)
                                };
            }
            return result;
        }

        [Theory]
        [InlineData(2)]
        [InlineData(10)]
        [InlineData(100)]
        public void ColorToVector4BithackBatchedArrays(int inputSize)
        {
            Color[] input = GenerateColorInput(inputSize);
            Vector4[] result = new Vector4[input.Length + 2];

            ExperimentalConverters.ColorToVector4BithackBatched(input, result);

            for (int i = 0; i < input.Length; i++)
            {
                Vector4 expected = input[i].ToVector4();
                Assert.Equal(expected, result[i], new ApproximateFloatComparer(0.01f));
            }
        }
        
        [Theory]
        [InlineData(2)]
        [InlineData(10)]
        [InlineData(100)]
        public void ColorToVector4BasicBatched(int inputSize)
        {
            Color[] input = GenerateColorInput(inputSize);
            Vector4[] result = new Vector4[input.Length + 2];

            ExperimentalConverters.ColorToVector4BasicBatched(input, result);

            for (int i = 0; i < input.Length; i++)
            {
                Vector4 expected = input[i].ToVector4();
                Assert.Equal(expected, result[i], new ApproximateFloatComparer(0.01f));
            }
        }
        
        [Fact]
        public void ColorToVector4BithackBatchedArraysBenchmark()
        {
            Color[] input = GenerateColorInput(256);
            Vector4[] result = new Vector4[input.Length + 2];

            this.Measure(
                1000000,
                () =>
                    {
                        ExperimentalConverters.ColorToVector4BithackBatched(input, result);
                    });
        }
    }
}
