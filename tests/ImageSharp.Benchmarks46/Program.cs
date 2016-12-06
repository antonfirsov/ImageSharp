using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using ImageSharp.Benchmarks.Image;


namespace ImageSharp.Benchmarks46
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkJpegTests test = new BenchmarkJpegTests();
            Console.WriteLine("DecodeJpegFull_JpegImageSharp ..");
            test.DecodeJpegFull_JpegImageSharp(DecodeJpegFull.JpegTestingMode.All, 10);
            Console.WriteLine("Done.");
        }
    }
}
