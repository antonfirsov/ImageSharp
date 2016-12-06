using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace ImageSharp.Tests
{
    public class MeasureFixture
    {
        protected bool EnablePrinting = true;

        protected void Measure(int times, Action action, [CallerMemberName] string operationName = null)
        {
            if(EnablePrinting) Output?.WriteLine($"{operationName} X {times} ...");
            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < times; i++)
            {
                action();
            }

            sw.Stop();
            if (EnablePrinting) Output?.WriteLine($"{operationName} finished in {sw.ElapsedMilliseconds} ms");
        }

        public MeasureFixture(ITestOutputHelper output)
        {
            Output = output;
        }

        protected ITestOutputHelper Output { get; }
    }
}