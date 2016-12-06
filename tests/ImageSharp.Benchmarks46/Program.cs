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
            //var cfg = ManualConfig.CreateEmpty();
            //cfg.Add(new MemoryDiagnoser());

            //BenchmarkRunner.Run<DecodeJpegFull>(cfg);
        }
    }
}
