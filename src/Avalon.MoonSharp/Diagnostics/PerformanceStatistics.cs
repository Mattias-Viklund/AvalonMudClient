using System;
using System.Text;
using Cysharp.Text;
using MoonSharp.Interpreter.Diagnostics.PerformanceCounters;

namespace MoonSharp.Interpreter.Diagnostics
{
    /// <summary>
    /// A single object of this type exists for every script and gives access to performance statistics.
    /// </summary>
    public class PerformanceStatistics
    {
        private static IPerformanceStopwatch[] _globalStopwatches = new IPerformanceStopwatch[(int) PerformanceCounter.LastValue];
        private bool _enabled;
        private IPerformanceStopwatch[] _stopwatches = new IPerformanceStopwatch[(int) PerformanceCounter.LastValue];


        /// <summary>
        /// Gets or sets a value indicating whether this collection of performance stats is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (value && !_enabled)
                {
                    if (_globalStopwatches[(int) PerformanceCounter.AdaptersCompilation] == null)
                    {
                        _globalStopwatches[(int) PerformanceCounter.AdaptersCompilation] =
                            new GlobalPerformanceStopwatch(PerformanceCounter.AdaptersCompilation);
                    }

                    for (int i = 0; i < (int) PerformanceCounter.LastValue; i++)
                    {
                        _stopwatches[i] = _globalStopwatches[i] ?? new PerformanceStopwatch((PerformanceCounter) i);
                    }
                }
                else if (!value && _enabled)
                {
                    _stopwatches = new IPerformanceStopwatch[(int) PerformanceCounter.LastValue];
                    _globalStopwatches = new IPerformanceStopwatch[(int) PerformanceCounter.LastValue];
                }

                _enabled = value;
            }
        }


        /// <summary>
        /// Gets the result of the specified performance counter .
        /// </summary>
        /// <param name="pc">The PerformanceCounter.</param>
        public PerformanceResult GetPerformanceCounterResult(PerformanceCounter pc)
        {
            var pco = _stopwatches[(int) pc];
            return pco?.GetResult();
        }

        /// <summary>
        /// Starts a stopwatch.
        /// </summary>
        internal IDisposable StartStopwatch(PerformanceCounter pc)
        {
            var pco = _stopwatches[(int) pc];
            return pco?.Start();
        }

        /// <summary>
        /// Starts a stopwatch.
        /// </summary>
        internal static IDisposable StartGlobalStopwatch(PerformanceCounter pc)
        {
            var pco = _globalStopwatches[(int) pc];
            return pco?.Start();
        }

        /// <summary>
        /// Gets a string with a complete performance log.
        /// </summary>
        public string GetPerformanceLog()
        {
            using (var sb = ZString.CreateStringBuilder())
            {
                for (int i = 0; i < (int)PerformanceCounter.LastValue; i++)
                {
                    var res = this.GetPerformanceCounterResult((PerformanceCounter)i);

                    if (res != null)
                    {
                        sb.AppendLine(res.ToString());
                    }
                }

                return sb.ToString();
            }
        }
    }
}