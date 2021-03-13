using System;

namespace MoonSharp.Interpreter.Diagnostics.PerformanceCounters
{
    internal class DummyPerformanceStopwatch : IPerformanceStopwatch, IDisposable
    {
        public static DummyPerformanceStopwatch Instance = new DummyPerformanceStopwatch();
        private PerformanceResult _result;

        private DummyPerformanceStopwatch()
        {
            _result = new PerformanceResult
            {
                Counter = 0,
                Global = true,
                Instances = 0,
                Name = "::dummy::",
                Type = PerformanceCounterType.TimeMilliseconds
            };
        }

        public void Dispose()
        {
        }


        public IDisposable Start()
        {
            return this;
        }

        public PerformanceResult GetResult()
        {
            return _result;
        }
    }
}