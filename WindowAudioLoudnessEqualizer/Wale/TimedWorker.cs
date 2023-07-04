using PrecisionTiming;
using System;
using System.Threading.Tasks;

namespace Wale
{
    public interface ITimedWorker : IDisposable
    {
        int Interval { get; set; }
        int Resolution { get; set; }
        bool OneShot { get; set; }

        void Start(Action action, int interval);
        void Start(Func<Task> action, int interval);
        void Stop(Action afterStop = null);
    }

    public class TimedWorker : ITimedWorker
    {
        public TimedWorker(bool mmt = false, int resolution = 5)
        {
            MMT = mmt;
            if (MMT)
            {
                mmtworker = new MMTWorker(resolution);
            }
            else
            {
                taskworker = new TaskWorker(resolution);
            }

        }

        private bool MMT = false;

        private MMTWorker mmtworker = null;
        private TaskWorker taskworker = null;

        public int Interval
        {
            get => MMT ? mmtworker.Interval : taskworker.Interval;
            set { if (MMT) mmtworker.Interval = value; else taskworker.Interval = value; }
        }
        public int Resolution
        {
            get => MMT ? mmtworker.Resolution : taskworker.Resolution;
            set { if (MMT) mmtworker.Resolution = value; else taskworker.Resolution = value; }
        }
        public bool OneShot
        {
            get => MMT ? mmtworker.OneShot : taskworker.OneShot;
            set { if (MMT) mmtworker.OneShot = value; else taskworker.OneShot = value; }
        }

        public void Start(Action action, int interval)
        {
            if (MMT) mmtworker.Start(action, interval);
            else taskworker.Start(action, interval);
        }
        public void Start(Func<Task> action, int interval)
        {
            if (MMT) mmtworker.Start(action, interval);
            else taskworker.Start(action, interval);
        }
        public void Stop(Action afterStop = null)
        {
            if (MMT) mmtworker.Stop(afterStop);
            else taskworker.Stop();
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
                    taskworker?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                mmtworker?.Dispose();
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
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

    public class MMTWorker : ITimedWorker
    {
        public MMTWorker(int resolution = 5) => timer.SetResolution(resolution);

        protected readonly PrecisionTimer timer = new PrecisionTimer();
        //protected readonly TaskTimer timer = new TaskTimer();
        protected readonly object actlock = new object();
        protected volatile bool act = false;

        public int Interval
        {
            get => timer.GetPeriod();
            set
            {
                Stop();
                if (task != null) Start(task, value);
                else if (work != null) Start(work, value);
            }
        }
        public int Resolution
        {
            get => timer.GetResolution();
            set => timer.SetResolution(value);
        }
        public bool OneShot
        {
            get => !timer.GetAutoResetMode();
            set => timer.SetAutoResetMode(!value);
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
        ~MMTWorker()
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
    public class TaskWorker : ITimedWorker
    {
        public TaskWorker(int resolution = 5) => timer.SetResolution(resolution);

        //protected readonly PrecisionTimer timer = new PrecisionTimer();
        protected readonly TaskTimer timer = new TaskTimer();
        protected readonly object actlock = new object();
        protected volatile bool act = false;

        public int Interval
        {
            get => timer.GetPeriod();
            set
            {
                Stop();
                if (task != null) Start(task, value);
                else if (work != null) Start(work, value);
            }
        }
        public int Resolution
        {
            get => timer.GetResolution();
            set => timer.SetResolution(value);
        }
        public bool OneShot
        {
            get => !timer.GetAutoResetMode();
            set => timer.SetAutoResetMode(!value);
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
        ~TaskWorker()
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

    public class TaskTimer : IDisposable
    {

        private int _Interval = 15;
        public int GetPeriod() => _Interval;

        /// <summary>
        /// Placeholder for structural consistancy, not actually used in this timer.
        /// </summary>
        public int GetResolution() => 0;
        /// <summary>
        /// Placeholder for structural consistancy, not actually used in this timer.
        /// </summary>
        public void SetResolution(int resolution) { }

        private bool _OneShot = false;
        public bool GetAutoResetMode() => _OneShot;
        public void SetAutoResetMode(bool mode) => _OneShot = mode;

        private volatile bool _Running = false;
        public bool IsRunning() => _Running;
        private volatile bool _Loop = false;

        private Action _Act = null;
        public void SetInterval(Action act = null, int interval = 15, bool start = true, int resolution = 0, TimerMode mode = TimerMode.Periodic, bool noTick = false)
        {
            if (act == null) return;
            else _Act = act;
            _Interval = interval;

            if (start)
            {
                if (mode == TimerMode.OneShot) Single();
                else Loop();
            }
        }
        private async void Single()
        {
            _Running = true;
            await Task.Delay(_Interval);
            _Act.Invoke();
            _Running = false;
        }
        private async void Loop()
        {
            _Running = true;
            _Loop = true;
            while (_Loop)
            {
                await Task.Delay(_Interval);
                _Act.Invoke();
            }
            _Running = false;
        }
        public void Stop()
        {
            _Loop = false;
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    Stop();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~TaskTimer()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
