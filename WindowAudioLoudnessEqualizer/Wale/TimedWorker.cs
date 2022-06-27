using PrecisionTiming;
using System;
using System.Threading.Tasks;

namespace Wale
{
    public class TimedWorker : IDisposable
    {
        public TimedWorker(int resolution = 5) => timer.SetResolution(resolution);

        protected readonly PrecisionTimer timer = new PrecisionTimer();
        protected readonly object actlock = new object();
        protected volatile bool act = false;

        public int Interval
        {
            get => timer.GetInterval ?? 0;
            set
            {
                Stop();
                if (task != null) Start(task, value);
                else if (work != null) Start(work, value);
            }
        }
        public int Resolution
        {
            get => timer.GetResolution ?? 0;
            set => timer.SetResolution(value);
        }
        public bool OneShot
        {
            get => !timer.GetPeriodic ?? false;
            set => timer.SetPeriodic(!value);
        }

        private Action work;
        private Func<Task> task;

        public void Start(Action action, int interval)
        {
            work = action;
            timer.SetInterval(() =>
            {

                bool work = false;
                lock (actlock) { if (!act) { act = true; work = true; } }
                if (work)
                {
                    action.Invoke();
                }
                if (work) { lock (actlock) { act = false; } }
            }, interval);
        }
        public void Start(Func<Task> action, int interval)
        {
            task = action;
            timer.SetInterval(async () =>
            {

                bool work = false;
                lock (actlock) { if (!act) { act = true; work = true; } }
                if (work)
                {
                    await action.Invoke();
                }
                if (work) { lock (actlock) { act = false; } }
            }, interval);
        }
        public void Stop(Action afterStop = null)
        {
            try
            {
                if ((bool)timer.IsRunning()) timer.Stop();
            }
            catch (Exception e) { M.F($"TimedWorker: Unknown exception on stopping timer. {e}"); }
            afterStop?.Invoke();
        }

        #region Disposing
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                try
                {
                    if ((bool)timer?.IsRunning()) timer?.Stop();
                    timer?.Dispose();
                }
                catch (Exception e) { M.F($"TimedWorker: Unknown exception on disposing timer. {e}"); }
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~TimedWorker()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
