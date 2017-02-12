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

        [Params(16, 32, 256)]
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

        [Benchmark(Baseline = true)]
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

        [Benchmark]
        public void BithackBatched2()
        {
            ExperimentalConverters.ColorToVector4BithackBatchedArrays2(this.input, this.result);
        }

        //[Benchmark]
        public void BatchedPointers()
        {
            ExperimentalConverters.ColorToVector4BasicBatched(this.input, this.result);
        }
    }
}