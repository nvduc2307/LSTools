using System.Diagnostics;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Application
{
    public sealed class RebarExecutionMetrics
    {
        private readonly Dictionary<string, TimeSpan> _durations =
            new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, TimeSpan> Durations => _durations;

        public IDisposable Measure(string stage)
        {
            if (string.IsNullOrWhiteSpace(stage))
                throw new ArgumentException("A performance stage name is required.", nameof(stage));
            return new Measurement(this, stage);
        }

        public string ToSummary()
        {
            return string.Join(
                ", ",
                _durations.Select(item => $"{item.Key}={item.Value.TotalMilliseconds:0}ms"));
        }

        private void Add(string stage, TimeSpan elapsed)
        {
            if (_durations.TryGetValue(stage, out var current))
                _durations[stage] = current + elapsed;
            else
                _durations.Add(stage, elapsed);
        }

        private sealed class Measurement : IDisposable
        {
            private readonly RebarExecutionMetrics _owner;
            private readonly string _stage;
            private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
            private bool _disposed;

            public Measurement(RebarExecutionMetrics owner, string stage)
            {
                _owner = owner;
                _stage = stage;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _stopwatch.Stop();
                _owner.Add(_stage, _stopwatch.Elapsed);
            }
        }
    }
}
