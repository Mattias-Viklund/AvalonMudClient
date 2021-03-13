using System;
using System.Diagnostics;

namespace MoonSharp.Interpreter.Diagnostics.PerformanceCounters
{
    /// <summary>
    /// This class is not *really* IDisposable.. it's just use to have a RAII like pattern.
    /// You are free to reuse this instance after calling Dispose.
    /// </summary>
    internal class PerformanceStopwatch : IDisposable, IPerformanceStopwatch
    {
        private int _count;
        private PerformanceCounter _counter;
        private int _reentrant;
        private Stopwatch _stopwatch = new Stopwatch();

        public PerformanceStopwatch(PerformanceCounter perfcounter)
        {
            _counter = perfcounter;
        }

        public void Dispose()
        {
            _reentrant -= 1;

            if (_reentrant == 0)
            {
                _stopwatch.Stop();
            }
        }


        public IDisposable Start()
        {
            if (_reentrant == 0)
            {
                _count += 1;
                _stopwatch.Start();
            }

            _reentrant += 1;

            return this;
        }

        public PerformanceResult GetResult()
        {
            return new PerformanceResult
            {
                Type = PerformanceCounterType.TimeMilliseconds,
                Global = false,
                Name = _counter.ToString(),
                Instances = _count,
                Counter = _stopwatch.ElapsedMilliseconds
            };
        }
    }
}