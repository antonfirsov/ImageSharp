namespace ImageSharp.Sandbox46
{
    using System;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    using Xunit;
    using Xunit.Abstractions;

    public class SandboxTests
    {
        private ITestOutputHelper Output { get; }

        public SandboxTests(ITestOutputHelper output)
        {
            this.Output = output;
        }

        [Fact]
        public void HelloSpan()
        {
            int[] data = new[] { 1, 2, 3 };
            Span<int> span = new Span<int>(data);

            Assert.Equal(span[1], 2);
        }
        
        private static void ColorFoo<TColor>(TColor[] input, Vector4[] result)
            where TColor : struct, IPackedPixel, IEquatable<TColor>
        {
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = input[i].ToVector4() + new Vector4(1, 2, 3, 4);
            }
        }

        [Theory]
        [InlineData(1024)]
        public void Foo(int size)
        {
            Vector4[] blah1 = new Vector4[size];
            Color[] colors1 = new Color[size];
            Argb[] colors2 = new Argb[size];

            for (int i = 0; i < 10000; i++)
            {
                ColorFoo(colors1, blah1);
                ColorFoo(colors2, blah1);
            }
        }

        [Fact]
        public void NonInlined()
        {
            int sum = 0;
            for (int i = 0; i < 1000000; i++)
            {
                sum += A();
            }
            this.Output.WriteLine(sum.ToString());
        }

        [Fact]
        public void Inlined()
        {
            int sum = 0;
            for (int i = 0; i < 1000000; i++)
            {
                sum += B();
            }
            this.Output.WriteLine(sum.ToString());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int A()
        {
            int res = -10000;
            for (int i = 0; i < 30; i++)
            {
                res += i*i-i;
            }
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int B()
        {
            int res = -10000;
            for (int i = 0; i < 30; i++)
            {
                res += i * i - i;
            }
            return res;
        } 

    }
}