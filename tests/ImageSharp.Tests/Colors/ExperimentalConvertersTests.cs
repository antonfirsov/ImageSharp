﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImageSharp.Tests.Colors
{
    using System.Numerics;
    using System.Runtime.InteropServices;

    using ImageSharp.Experimental;

    using Xunit;
    using Xunit.Abstractions;

    public class ExperimentalConvertersTests
    {

        private ITestOutputHelper Output { get; }

        public ExperimentalConvertersTests(ITestOutputHelper output)
        {
            this.Output = output;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct UIntFloatUnion
        {
            [FieldOffset(0)]
            public float f;

            [FieldOffset(0)]
            public uint i;
        }

        [Fact]
        public void PrintMagic()
        {
            UIntFloatUnion t = default(UIntFloatUnion);
            t.f = 32768.0f;

            this.Output.WriteLine(t.i.ToString());
            this.Output.WriteLine(Vector<uint>.Count.ToString());
            this.Output.WriteLine(Vector<float>.Count.ToString());
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

        public const uint MagicUInt = 1191182336; // reinterpreted value of 32768.0f

        [Fact]
        public void UnpackUints()
        {
            Color[] input = { new Color(1, 2, 3, 4), new Color(10, 20, 30, 42) };

            uint[] result = new uint[input.Length * 4+8];

            ExperimentalConverters.UnpackUints(input, result);

            uint[] expected =
                {
                    MagicUInt | 1, MagicUInt | 2, MagicUInt | 3, MagicUInt | 4,
                    MagicUInt | 10, MagicUInt | 20, MagicUInt | 30, MagicUInt | 42
                };

            Assert.Equal(expected, result);
        }

        [Fact]
        public void ColorToVector4BithackBatchedArrays()
        {
            Color[] input = { new Color(1, 2, 3, 4), new Color(10, 20, 30, 42) };
            Vector4[] result = new Vector4[input.Length];

            ExperimentalConverters.ColorToVector4BithackBatchedArrays(input, result);

            for (int i = 0; i < input.Length; i++)
            {
                Vector4 expected = input[i].ToVector4();
                Assert.Equal(expected, result[i], new ApproximateFloatComparer(0.01f));
            }
        }
    }
}
