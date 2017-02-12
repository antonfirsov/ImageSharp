namespace ImageSharp.Benchmarks
{
    using System;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    using BenchmarkDotNet.Attributes;

    using ImageSharp.Experimental;

    public class ColorToVector4
    {
        private Color[] input;

        private Vector4[] result;

        private Color[] input2;

        private Vector4[] result2;

        private GCHandle hInput2;
        private GCHandle hResult2;

        //[Params(16, 64, 256)]
        [Params(256)]
        public int InputSize { get; set; }

        [Setup]
        public void Setup()
        {
            this.input = new Color[this.InputSize];
            this.input2 = new Color[this.InputSize];
            this.result = new Vector4[this.InputSize];
            this.result2 = new Vector4[this.InputSize];

            Random rnd = new Random(42);

            for (int i = 0; i < this.InputSize; i++)
            {
                Color c = new Color(
                    rnd.Next(255),
                    rnd.Next(255),
                    rnd.Next(255)
                );
                this.input[i] = c;
                this.input2[i] = c;
            }

            this.hInput2 = GCHandle.Alloc(this.input2, GCHandleType.Pinned);
            this.hResult2 = GCHandle.Alloc(this.result2, GCHandleType.Pinned);
        }

        [Cleanup]
        public void Cleanup()
        {
            this.hInput2.Free();
            this.hResult2.Free();
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

        [Benchmark]
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
            ExperimentalConverters.ColorToVector4BithackBatched(this.input, this.result);
        }

        [Benchmark]
        public unsafe void BithackBatchedPrePinned()
        {
            Color* pColor = (Color*)this.hInput2.AddrOfPinnedObject();
            Vector4* pResult = (Vector4*)this.hResult2.AddrOfPinnedObject();
            ExperimentalConverters.ColorToVector4BithackBatched(pColor, pResult, this.input2.Length);
        }

        [Benchmark]
        public void BatchedPointers()
        {
            ExperimentalConverters.ColorToVector4BasicBatched(this.input, this.result);
        }
    }
}