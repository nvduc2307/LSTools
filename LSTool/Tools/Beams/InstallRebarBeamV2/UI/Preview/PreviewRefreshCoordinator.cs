using System;
using System.Windows.Threading;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.UI.Preview
{
    [Flags]
    internal enum PreviewRegion
    {
        None = 0,
        MainBars = 1,
        SideBars = 2,
        AllBars = MainBars | SideBars
    }

    internal sealed class PreviewRefreshCoordinator : IDisposable
    {
        private readonly DispatcherTimer _timer;
        private readonly Action<PreviewRegion> _refresh;
        private PreviewRegion _pendingRegions;

        public PreviewRefreshCoordinator(Action<PreviewRegion> refresh, TimeSpan delay)
        {
            _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));
            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = delay
            };
            _timer.Tick += OnTick;
        }

        public void Request(PreviewRegion regions)
        {
            _pendingRegions |= regions;
            _timer.Stop();
            _timer.Start();
        }

        public void CancelPending()
        {
            _timer.Stop();
            _pendingRegions = PreviewRegion.None;
        }

        public void Dispose()
        {
            CancelPending();
            _timer.Tick -= OnTick;
        }

        private void OnTick(object? sender, EventArgs eventArgs)
        {
            _timer.Stop();
            RefreshPendingRegions();
        }

        private void RefreshPendingRegions()
        {
            if (_pendingRegions == PreviewRegion.None)
                return;

            var regions = _pendingRegions;
            _pendingRegions = PreviewRegion.None;
            _refresh(regions);
        }
    }
}
