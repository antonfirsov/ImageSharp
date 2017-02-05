namespace ImageSharp.Benchmarks
{
    using System;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    using BenchmarkDotNet.Attributes;

    public class ColorToVector4
    {
        private Color[] input;

        private Vector4[] result;

        [Params(16, 64, 256)]
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
        public void StandardArrays()
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
        public void BatchedArrays()
        {
            ColorToVector4BatchedArrays(this.input, this.result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ColorToVector4BatchedArrays(Color[] input, Vector4[] result)
        {
            for (int i = 0; i < input.Length; i++)
            {
                ref Color c = ref input[i];
                Vector4 v = new Vector4(c.R, c.G, c.B, c.A);
                v /= new Vector4(255);
                result[i] = v;
            }
        }

        [Benchmark]
        public void StandardSpans()
        {
            ColorToVector4StandardSpans(this.input, this.result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ColorToVector4StandardSpans(Span<Color> input, Span<Vector4> result)
        {
            for (int i = 0; i < input.Length; i++)
            {
                result.GetItem(i) = input[i].ToVector4();
            }
        }

        [Benchmark]
        public void StandardSpansMainCallIsInlined()
        {
            Span<Color> input = new Span<Color>(this.input);
            Span<Vector4> result = new Span<Vector4>(this.result);

            for (int i = 0; i < input.Length; i++)
            {
                result.GetItem(i) = input[i].ToVector4();
            }
        }
        
        [Benchmark]
        public void BatchedSpans()
        {
            ColorToVector4BatchedSpans(this.input, this.result);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ColorToVector4BatchedSpans(Span<Color> input, Span<Vector4> result)
        {
            Vector4 v;
            Vector4 maxBytes = new Vector4(255);
            for (int i = 0; i < input.Length; i++)
            {
                ref Color c = ref input.GetItem(i);
                v = new Vector4(c.R, c.G, c.B, c.A);
                v /= maxBytes;
                result.GetItem(i) = v;
            }
        }

        [Benchmark]
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