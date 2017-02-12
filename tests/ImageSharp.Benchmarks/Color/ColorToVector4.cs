namespace ImageSharp.Benchmarks
{
    using System;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    using BenchmarkDotNet.Attributes;

    using ImageSharp.Experimental;

    public class ColorToVector4
    {
        private Color[] input;

        private Vector4[] result;

        [Params(32)]
        public int InputSize { get; set; }

        [Setup]
        public void Setup()
        {
            this.input = new Color[this.InputSize];
            this.result = new Vector4[this.InputSize];

            Random rnd = new Random(42);

            for (int i = 0; i < this.InputSize; i++)
            {
                this.input[i] = new Color(
                    rnd.Next(255),
                    rnd.Next(255),
                    rnd.Next(255)
                    );
            }
        }

        //[Benchmark(Baseline = true)]
        public void Standard()
        {
            ColorToVector4StandardArrays(this.input, this.result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ColorToVector4StandardArrays(Color[] input, Vector4[] result)
        {
            for (int i = 0; i < input.Length; i++)
            {
                // Note: unlike the implementation on current master branch, Color.ToVector4() is inlined here!
                result[i] = input[i].ToVector4();
            }
        }

        //[Benchmark]
        public void Bithack()
        {
            ColorToVector4Bithack(this.input, this.result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ColorToVector4Bithack(Color[] input, Vector4[] result)
        {
            Color color;
            for (int i = 0; i < input.Length; i++)
            {
                color = input[i];
                result[i] = ExperimentalConverters.ColorToVector4Bithack(color);
            }
        }

        [Benchmark]
        public void BithackBatched()
        {
            ExperimentalConverters.ColorToVector4BithackBatchedArrays(this.input, this.result);
        }

        //[Benchmark]
        public unsafe void BatchedPointers()
        {
            ColorToVector4ImplBatchedPointers(this.input, this.result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void ColorToVector4ImplBatchedPointers(Color[] input, Vector4[] result)
        {
            fixed (Color* cFixed = input)
            {
                fixed (Vector4* rFixed = result)
                {
                    Color c;
                    Vector4 v;
                    Vector4 maxBytes = new Vector4(255);

                    Color* cPtr = cFixed;
                    Vector4* rPtr = rFixed;
                    for (int i = 0; i < input.Length; i++)
                    {
                        v = new Vector4(cPtr->R, cPtr->G, cPtr->B, cPtr->A);
                        v /= maxBytes;
                        *rPtr = v;
                        cPtr++;
                        rPtr++;
                    }
                }
            }
        }
    }
}