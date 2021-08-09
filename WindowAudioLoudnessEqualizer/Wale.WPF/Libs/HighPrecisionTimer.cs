using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HighPrecisionTimer
{
    #region HPTimer
    public class HPTimer
    {
        public enum Select { MMT, TQT, TPT }
        public static Task Delay(int millisecond, Select sel = Select.MMT)
        {
            if (millisecond >= 15) return Task.Delay(millisecond);
            else if (millisecond >= 1)
            {
                switch (sel)
                {
                    case Select.MMT: return MultimediaTimer.Delay(millisecond);
                    case Select.TQT: return TQTDelay(millisecond);
                    case Select.TPT: return TPreciseTimer(millisecond);
                    default: return MultimediaTimer.Delay(millisecond);
                }
            }
            else return null;
        }

        private Task PreciseTimerWork(int delay)
        {
            if (delay <= 0) return Task.CompletedTask;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            int ticks = (int)((double)delay * (double)System.Diagnostics.Stopwatch.Frequency / 1000);
            sw.Start();

            while (sw.IsRunning)
            {
                if (sw.ElapsedTicks > ticks) { sw.Stop(); break; }
                //if (sw.ElapsedMilliseconds > delay) { sw.Stop(); break; }
                System.Threading.Thread.Sleep(1);
            }
            return Task.CompletedTask;
        }
        private async Task APreciseTimer(int delay) => await PreciseTimerWork(delay);
        private static async Task TPreciseTimer(int delay)
        {
            if (delay <= 0) return;
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            int ticks = (int)((double)delay * (double)System.Diagnostics.Stopwatch.Frequency / 1000);
            sw.Start();

            while (sw.IsRunning)
            {
                if (sw.ElapsedTicks > ticks) { sw.Stop(); break; }
                await Task.Run(() => { System.Threading.Thread.Sleep(new TimeSpan(ticks)); });
            }
        }


        public static Task TQTDelay(int millisecondsDelay, CancellationToken token = default)
        {
            if (millisecondsDelay < 0)
            {
                throw new ArgumentOutOfRangeException("millisecondsDelay", millisecondsDelay, "The value cannot be less than 0.");
            }

            if (millisecondsDelay == 0)
            {
                return Task.CompletedTask;
            }

            token.ThrowIfCancellationRequested();

            var completionSource = new TaskCompletionSource<object>();

            //WaitOrTimerCallback callback = (object s, bool timedout) => { completionSource.TrySetResult(null); };
            //var timerId = NativeMethods.CreateTimerQueueTimer(out IntPtr handle, IntPtr.Zero, callback, IntPtr.Zero, (uint)millisecondsDelay, (uint)0, 0x00000008);
            //if (!timerId)
            //{
            //    int error = Marshal.GetLastWin32Error();
            //    throw new Win32Exception(error);
            //}

            TimerQueueTimer timer = new TimerQueueTimer();
            TimerQueueTimer.WaitOrTimerDelegate callback = new TimerQueueTimer.WaitOrTimerDelegate((IntPtr s, bool timedout) => { completionSource.TrySetResult(null); });
            timer.Delay((uint)millisecondsDelay, callback);

            return completionSource.Task;
        }
    }
    #endregion

    #region MMTimer
    /*
    * Author: mzboray
    * URL: https://github.com/mzboray/HighPrecisionTimer
    * MIT License
    */
    /// <summary>
    /// A timer based on the multimedia timer API with 1ms precision.
    /// </summary>
    public class MultimediaTimer : IDisposable
    {
        private const int EventTypeSingle = 0;
        private const int EventTypePeriodic = 1;

        private static readonly Task TaskDone = Task.FromResult<object>(null);

        private bool disposed = false;
        private int interval, resolution;
        private volatile uint timerId;

        // Hold the timer callback to prevent garbage collection.
        private readonly MultimediaTimerCallback Callback;

        public MultimediaTimer()
        {
            Callback = new MultimediaTimerCallback(TimerCallbackMethod);
            Resolution = 5;
            Interval = 10;
        }

        ~MultimediaTimer()
        {
            Dispose(false);
        }

        /// <summary>
        /// The period of the timer in milliseconds.
        /// </summary>
        public int Interval
        {
            get
            {
                return interval;
            }
            set
            {
                CheckDisposed();

                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");

                interval = value;
                if (Resolution > Interval)
                    Resolution = value;
            }
        }

        /// <summary>
        /// The resolution of the timer in milliseconds. The minimum resolution is 0, meaning highest possible resolution.
        /// </summary>
        public int Resolution
        {
            get
            {
                return resolution;
            }
            set
            {
                CheckDisposed();

                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");

                resolution = value;
            }
        }

        /// <summary>
        /// Gets whether the timer has been started yet.
        /// </summary>
        public bool IsRunning
        {
            get { return timerId != 0; }
        }

        public static Task Delay(int millisecondsDelay, CancellationToken token = default(CancellationToken))
        {
            if (millisecondsDelay < 0)
            {
                throw new ArgumentOutOfRangeException("millisecondsDelay", millisecondsDelay, "The value cannot be less than 0.");
            }

            if (millisecondsDelay == 0)
            {
                return TaskDone;
            }

            token.ThrowIfCancellationRequested();

            // allocate an object to hold the callback in the async state.
            object[] state = new object[1];
            var completionSource = new TaskCompletionSource<object>(state);
            //var completionSource = new TaskCompletionSource<object>();
            MultimediaTimerCallback callback = (uint id, uint msg, ref uint uCtx, uint rsv1, uint rsv2) =>
            {
                // Note we don't need to kill the timer for one-off events.
                completionSource.TrySetResult(null);
            };
            //WaitOrTimerCallback callback = (object s, bool timedout) => { completionSource.TrySetResult(null); };

            //TimerQueueTimer timer = new TimerQueueTimer();
            //TimerQueueTimer.WaitOrTimerDelegate callback = new TimerQueueTimer.WaitOrTimerDelegate((IntPtr s, bool timedout) => { completionSource.TrySetResult(null); });
            state[0] = callback;
            UInt32 userCtx = 0;
            var timerId = NativeMethods.TimeSetEvent((uint)millisecondsDelay, (uint)0, callback, ref userCtx, EventTypeSingle);
            //var timerId = NativeMethods.CreateTimerQueueTimer(out IntPtr handle, IntPtr.Zero, callback, IntPtr.Zero, (uint)millisecondsDelay, (uint)0, 0x00000008);
            //timer.Delay((uint)millisecondsDelay, callback);
            //if (!timerId)
            if (timerId == 0)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }

            return completionSource.Task;
        }

        public void Start()
        {
            CheckDisposed();

            if (IsRunning)
                throw new InvalidOperationException("Timer is already running");

            // Event type = 0, one off event
            // Event type = 1, periodic event
            UInt32 userCtx = 0;
            timerId = NativeMethods.TimeSetEvent((uint)Interval, (uint)Resolution, Callback, ref userCtx, EventTypePeriodic);
            if (timerId == 0)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }
        }

        public void Stop()
        {
            CheckDisposed();

            if (!IsRunning)
                throw new InvalidOperationException("Timer has not been started");

            StopInternal();
        }

        private void StopInternal()
        {
            NativeMethods.TimeKillEvent(timerId);
            timerId = 0;
        }

        public event EventHandler Elapsed;

        public void Dispose()
        {
            Dispose(true);
        }

        private void TimerCallbackMethod(uint id, uint msg, ref uint userCtx, uint rsv1, uint rsv2)
        {
            var handler = Elapsed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void CheckDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException("MultimediaTimer");
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;

            disposed = true;
            if (IsRunning)
            {
                StopInternal();
            }

            if (disposing)
            {
                Elapsed = null;
                GC.SuppressFinalize(this);
            }
        }
    }

    internal delegate void MultimediaTimerCallback(UInt32 id, UInt32 msg, ref UInt32 userCtx, UInt32 rsv1, UInt32 rsv2);

    internal static class NativeMethods
    {
        [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeSetEvent")]
        internal static extern UInt32 TimeSetEvent(UInt32 msDelay, UInt32 msResolution, MultimediaTimerCallback callback, ref UInt32 userCtx, UInt32 eventType);

        [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeKillEvent")]
        internal static extern void TimeKillEvent(UInt32 uTimerId);

        //[DllImport("kernel32.dll", SetLastError = true, EntryPoint = "CreateTimerQueueTimer")]
        //internal static extern bool CreateTimerQueueTimer(out IntPtr phNewTimer, IntPtr TimerQueue, WaitOrTimerCallback Callback, IntPtr Parameter, UInt32 Duetime, UInt32 Period, UInt64 Flags);
    }
    #endregion

    #region TimerQueueTimer
    public class TimerQueueTimer : IDisposable
    {

        IntPtr phNewTimer; // Handle to the timer.

        #region Win32 TimerQueueTimer Functions

        [DllImport("kernel32.dll")]
        static extern bool CreateTimerQueueTimer(
            out IntPtr phNewTimer,          // phNewTimer - Pointer to a handle; this is an out value
            IntPtr TimerQueue,              // TimerQueue - Timer queue handle. For the default timer queue, NULL
            WaitOrTimerDelegate Callback,   // Callback - Pointer to the callback function
            IntPtr Parameter,               // Parameter - Value passed to the callback function
            uint DueTime,                   // DueTime - Time (milliseconds), before the timer is set to the signaled state for the first time 
            uint Period,                    // Period - Timer period (milliseconds). If zero, timer is signaled only once
            uint Flags                      // Flags - One or more of the next values (table taken from MSDN):
                                            // WT_EXECUTEINTIMERTHREAD 	The callback function is invoked by the timer thread itself. This flag should be used only for short tasks or it could affect other timer operations.
                                            // WT_EXECUTEINIOTHREAD 	The callback function is queued to an I/O worker thread. This flag should be used if the function should be executed in a thread that waits in an alertable state.

            // The callback function is queued as an APC. Be sure to address reentrancy issues if the function performs an alertable wait operation.
            // WT_EXECUTEINPERSISTENTTHREAD 	The callback function is queued to a thread that never terminates. This flag should be used only for short tasks or it could affect other timer operations.

            // Note that currently no worker thread is persistent, although no worker thread will terminate if there are any pending I/O requests.
            // WT_EXECUTELONGFUNCTION 	Specifies that the callback function can perform a long wait. This flag helps the system to decide if it should create a new thread.
            // WT_EXECUTEONLYONCE 	The timer will be set to the signaled state only once.
            );

        [DllImport("kernel32.dll")]
        static extern bool DeleteTimerQueueTimer(
            IntPtr timerQueue,              // TimerQueue - A handle to the (default) timer queue
            IntPtr timer,                   // Timer - A handle to the timer
            IntPtr completionEvent          // CompletionEvent - A handle to an optional event to be signaled when the function is successful and all callback functions have completed. Can be NULL.
            );


        [DllImport("kernel32.dll")]
        static extern bool DeleteTimerQueue(IntPtr TimerQueue);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        #endregion

        public delegate void WaitOrTimerDelegate(IntPtr lpParameter, bool timerOrWaitFired);

        public TimerQueueTimer()
        {

        }

        public void Delay(uint dueTime, WaitOrTimerDelegate callbackDelegate)
        {
            IntPtr pParameter = IntPtr.Zero;

            bool success = CreateTimerQueueTimer(
                // Timer handle
                out phNewTimer,
                // Default timer queue. IntPtr.Zero is just a constant value that represents a null pointer.
                IntPtr.Zero,
                // Timer callback function
                callbackDelegate,
                // Callback function parameter
                pParameter,
                // Time (milliseconds), before the timer is set to the signaled state for the first time.
                dueTime,
                // Period - Timer period (milliseconds). If zero, timer is signaled only once.
                0,
                (uint)(Flag.WT_EXECUTEINIOTHREAD | Flag.WT_EXECUTEONLYONCE));

            if (!success)
                throw new QueueTimerException("Error creating QueueTimer");
        }

        public void Create(uint dueTime, uint period, WaitOrTimerDelegate callbackDelegate)
        {
            IntPtr pParameter = IntPtr.Zero;

            bool success = CreateTimerQueueTimer(
                // Timer handle
                out phNewTimer,
                // Default timer queue. IntPtr.Zero is just a constant value that represents a null pointer.
                IntPtr.Zero,
                // Timer callback function
                callbackDelegate,
                // Callback function parameter
                pParameter,
                // Time (milliseconds), before the timer is set to the signaled state for the first time.
                dueTime,
                // Period - Timer period (milliseconds). If zero, timer is signaled only once.
                period,
                (uint)Flag.WT_EXECUTEINIOTHREAD);

            if (!success)
                throw new QueueTimerException("Error creating QueueTimer");
        }

        public void Delete()
        {
            //bool success = DeleteTimerQueue(IntPtr.Zero);
            bool success = DeleteTimerQueueTimer(
                IntPtr.Zero, // TimerQueue - A handle to the (default) timer queue
                phNewTimer,  // Timer - A handle to the timer
                IntPtr.Zero  // CompletionEvent - A handle to an optional event to be signaled when the function is successful and all callback functions have completed. Can be NULL.
                );
            int error = Marshal.GetLastWin32Error();
            //CloseHandle(phNewTimer);
        }

        private enum Flag
        {
            WT_EXECUTEDEFAULT = 0x00000000,
            WT_EXECUTEINIOTHREAD = 0x00000001,
            //WT_EXECUTEINWAITTHREAD       = 0x00000004,
            WT_EXECUTEONLYONCE = 0x00000008,
            WT_EXECUTELONGFUNCTION = 0x00000010,
            WT_EXECUTEINTIMERTHREAD = 0x00000020,
            WT_EXECUTEINPERSISTENTTHREAD = 0x00000080,
            //WT_TRANSFER_IMPERSONATION    = 0x00000100
        }

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            Delete();
        }

        #endregion
    }

    public class QueueTimerException : Exception
    {
        public QueueTimerException(string message) : base(message)
        {
        }

        public QueueTimerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
    #endregion
}
