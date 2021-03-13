using System;
using System.Diagnostics;

namespace MoonSharp.Interpreter.Diagnostics.PerformanceCounters
{
    /// <summary>
    /// This class is not *really* IDisposable.. it's just use to have a RAII like pattern.
    /// You are free to reuse this instance after calling Dispose.
    /// </summary>
    internal class GlobalPerformanceStopwatch : IPerformanceStopwatch
    {
        private int _count;
        private PerformanceCounter _counter;
        private long _elapsed;

        public GlobalPerformanceStopwatch(PerformanceCounter perfcounter)
        {
            _counter = perfcounter;
        }

        public IDisposable Start()
        {
            return new GlobalPerformanceStopwatch_StopwatchObject(this);
        }

        public PerformanceResult GetResult()
        {
            return new PerformanceResult
            {
                Type = PerformanceCounterType.TimeMilliseconds,
                Global = false,
                Name = _counter.ToString(),
                Instances = _count,
                Counter = _elapsed
            };
        }

        private void SignalStopwatchTerminated(Stopwatch sw)
        {
            _elapsed += sw.ElapsedMilliseconds;
            _count += 1;
        }

        private class GlobalPerformanceStopwatch_StopwatchObject : IDisposable
        {
            private GlobalPerformanceStopwatch _parent;
            private Stopwatch _stopwatch;

            public GlobalPerformanceStopwatch_StopwatchObject(GlobalPerformanceStopwatch parent)
            {
                _parent = parent;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                _parent.SignalStopwatchTerminated(_stopwatch);
            }
        }
    }
}