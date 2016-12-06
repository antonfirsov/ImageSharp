using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using ImageSharp.Benchmarks.Image;
using ImageSharp.Tests;
using JetBrains.dotMemoryUnit;
using Xunit;
using Xunit.Abstractions;

namespace ImageSharp.Benchmarks46
{
    public class BenchmarkJpegTests : MeasureFixture
    {

        public BenchmarkJpegTests(ITestOutputHelper output) : base(output)
        {
            EnablePrinting = false;
            DotMemoryUnitTestOutput.SetOutputMethod(output.WriteLine);
        }

        internal BenchmarkJpegTests() : base(null) { }

        [DotMemoryUnit(CollectAllocations = true)]
        [Theory]
        [InlineData(DecodeJpegFull.JpegTestingMode.CalliphoraOnly, 1)]
        [InlineData(DecodeJpegFull.JpegTestingMode.CalliphoraOnly, 30)]
        [InlineData(DecodeJpegFull.JpegTestingMode.All, 10)]
        public void DecodeJpegFull_JpegImageSharp(DecodeJpegFull.JpegTestingMode mode, int times)
        {
            var decodePlz = new DecodeJpegFull {Mode = mode};
            decodePlz.ReadImages();

            Measure(times, () => decodePlz.JpegImageSharp());
        }


        [DotMemoryUnit(CollectAllocations = true)]
        [Theory]
        [InlineData(DecodeJpegFull.JpegTestingMode.CalliphoraOnly, 1)]
        [InlineData(DecodeJpegFull.JpegTestingMode.CalliphoraOnly, 30)]
        [InlineData(DecodeJpegFull.JpegTestingMode.All, 10)]
        public void DecodeJpegFull_JpegSystemDrawing(DecodeJpegFull.JpegTestingMode mode, int times)
        {
            var decodePlz = new DecodeJpegFull { Mode = mode };
            decodePlz.ReadImages();

            Measure(times, () => decodePlz.JpegSystemDrawing());
        }
    }
}