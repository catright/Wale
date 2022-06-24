using HighPrecisionTimer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Wale.Extensions;

namespace Wale.Controller
{
    public partial class AudioControl
    {
        #region class loads
        /// <summary>
        /// Start audio controller
        /// </summary>
        /// <param name="debug">True when to enable debug mode</param>
        public void Start(bool debug = false)
        {
            Debug = debug;

            core = new CoreAudio.Manager(gl);
            //core.RestartRequested += (sender, e) => RestartRequested?.Invoke(sender, e);
            core.PropertyChanged += (sender, e) => Core_PropertyChanged(sender, e);
            //Core_PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("ALL"));

            Control = new SessionControl(gl, debug);
            Control.SessionAdded += (sender, s) => SessionAdded?.Invoke(sender, s);
            //Control.SessionRemoved += (sender, id) => SessionRemoved?.Invoke(sender, id);

            ControlTasks.Add(ControllerCleanTask());
            ControlTasks.Add(AudioControlTask());
        }
        /// <summary>
        /// Stop audio controller safely
        /// </summary>
        public async Task Stop()
        {
            Terminate = true;
            await Task.WhenAll(ControlTasks);
        }

        private void Core_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "MMDPeakValue":
                    OnPropertyChanged("MasterPeak");
                    break;
                case "MMDVolumeValue":
                    OnPropertyChanged("MasterVolume");
                    break;
                case "MMDMuted":
                    OnPropertyChanged("MasterMuted");
                    break;
                case "ALL":
                    OnPropertyChanged("MasterPeak");
                    OnPropertyChanged("MasterVolume");
                    OnPropertyChanged("MasterMuted");
                    break;
            }
        }
        #endregion

        #region Controller
        /// <inheritdoc cref="CoreAudio.Manager.MMDName"/>
        public Tuple<string, string> DeviceName => core?.MMDName;
        public string DeviceID => core?.DeviceID ?? string.Empty;
        public double MasterPeak => core?.MMDPeakValue ?? -1;
        public double MasterVolume
        {
            get => core?.MMDVolumeValue ?? -1;
            set { if (core != null) core.MMDVolumeValue = value.Clip(); }
        }
        public bool MasterMuted
        {
            get => (bool)core?.MMDMuted;
            set { if (core != null) core.MMDMuted = value; }
        }

        private SessionControl Control;
        public event EventHandler<CoreAudio.Session> SessionAdded;
        //public event EventHandler<Guid> SessionRemoved;
        #endregion

        #region Automatic volume control
        /// <inheritdoc cref="CoreAudio.SessionList"/>
        public CoreAudio.SessionList Sessions => core?.Sessions;

        //private readonly TaskSynchronize.AsyncLock TaskSync = new TaskSynchronize.AsyncLock();
        private async Task AudioControlTask()
        {
            M.F("[Start] Audio Control Task", verbose: gl.VerboseLog);
            List<Task> aas = new List<Task>();
            uint logCounter = 0, logCritical = 100000, swCritical = (uint)(0);//gl.UIUpdateInterval / gl.AutoControlInterval
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            List<double> elAudio = new List<double>(), elWait = new List<double>(), widif = new List<double>();
            bool acDev = false;

            while (!Terminate)
            {
                while (!CanAccess) await HPT.Delay((int)(gl.StaticMode ? gl.UIUpdateInterval : gl.AutoControlInterval), HPT.Select.MMT, gl.ForceMMT);
                sw.Restart();
                TimeSpan d = new TimeSpan(0, 0, 0, 0, (int)(gl.StaticMode ? gl.UIUpdateInterval : gl.AutoControlInterval));
                Task waitTimer = HPT.Delay((int)d.TotalMilliseconds, HPT.Select.MMT, gl.ForceMMT);
                if (gl.AutoControl)
                {
                    aas.Clear();
                    try
                    {
                        lock (Sessions.Locker)
                        {
                            Sessions.DisposedCheck();
                            //Sessions.ForEach(s => aas.Add(new Task(() => Control.Run(s, core?.DeviceID))));
                            Sessions.ForEach(s => aas.Add(Task.Run(() => Control.Run(s, core?.DeviceID))));
                        }
                        //aas.ForEach(t => t.Start());

                        if (logCounter > logCritical)
                        {
                            M.F($"Auto control task passed for {logCritical} times.", verbose: gl.VerboseLog);
                            logCounter = 0;
                        }
                    }
                    catch (InvalidOperationException e) { M.F($"Error(AudioControlTask): Session collection was modified.\r\n\t{e}", verbose: gl.VerboseLog); }
                    catch (Exception e) { if (e.HResult.IsUnknown()) M.F($"Error(AudioControlTask): Unknown.\r\n\t{e}", verbose: gl.VerboseLog); }

                    logCounter++;
                }

                acDev = gl.ACDevShow == Visibility.Visible;
                if (acDev && elWait.Count > swCritical)
                {
                    gl.ACElapsed = elAudio.Average(); gl.ACWaited = elWait.Average(); gl.ACEWdif = widif.Average();
                    elAudio.Clear(); elWait.Clear(); widif.Clear();
                }
                if (gl.AutoControl) await Task.WhenAll(aas);
                sw.Stop();
                TimeSpan elapsedControl = sw.Elapsed;

                sw.Start();
                if (acDev) elAudio.Add(elapsedControl.TotalMilliseconds);
                try { await (waitTimer ?? Task.CompletedTask); }
                catch (Exception e) { M.F($"AudioControlTask: Exception on awating waitTimer. {e}", verbose: gl.VerboseLog); }
                sw.Stop();
                TimeSpan elapsedWait = sw.Elapsed;

                //Console.WriteLine($"ACTaskWaited ={(double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency * 1000:n3}[ms]");// *10000000 T[100ns]
                if (acDev)
                {
                    elWait.Add(elapsedWait.TotalMilliseconds);
                    widif.Add(elapsedWait.Subtract(d).TotalMilliseconds);
                }
            }// end while loop

            M.F("[End] Audio Control Task", verbose: gl.VerboseLog);
        }// end AudioControlTask
        private async Task ControllerCleanTask()
        {
            M.F("[Start] Controller Clean Task(GC)", verbose: gl.VerboseLog);
            List<Task> aas = new List<Task>();
            int waitone = (int)new TimeSpan(0, 0, 1).TotalMilliseconds;
            //uint logCounter = uint.MaxValue, logCritical = (uint)(1800000 / gl.GCInterval);
            //System.Diagnostics.Stopwatch loop = new System.Diagnostics.Stopwatch(), elapse = new System.Diagnostics.Stopwatch();
            //elapse.Restart();

            while (!Terminate)
            {
                //if (Debug) loop.Restart();
                int remain = (int)new TimeSpan((long)(gl.GCInterval * 10000)).TotalMilliseconds;
                while (remain > 0)
                {
                    int wait = Math.Min(remain - waitone, waitone);
                    if (wait == 0) break;
                    remain -= wait;
                    if (Terminate) { M.F("[End] Controller Clean Task(GC)", verbose: gl.VerboseLog); return; }
                    await Task.Delay(wait);
                }

                while (!CanAccess) await Task.Delay(1000);
                if (gl.AutoControl)
                {
                    aas.Clear();
                    try
                    {
                        lock (Sessions.Locker)
                        {
                            Sessions.ForEach(s =>
                            {
                                //aas.Add(new Task(() => Control.Reset(s)));
                                aas.Add(Task.Run(() => Control.Reset(s)));
                            });
                        }
                        //aas.ForEach(t => t.Start());
                    }
                    catch (InvalidOperationException e) { M.F($"Error(ControllerCleanTask): Session collection was modified.\r\n\t{e}", verbose: gl.VerboseLog); }
                    await Task.WhenAll(aas);
                }

                //if (Debug) { Console.WriteLine($"Controller Cleaned, Elapsed: {loop.ElapsedMilliseconds:n1}ms"); }
                //long mmc = GC.GetTotalMemory(true);
                //if (Debug) { Console.WriteLine($"Memory Cleaned, Managing: {mmc:n0}B"); }
                //if (logCounter > logCritical)
                //{
                //    logCounter = logCounter == uint.MaxValue ? 1 : logCounter;
                //    JPack.FileLog.Log($"Memory Cleaned {logCounter} times in {elapse.ElapsedMilliseconds.AsTime(1)}, Managing: {mmc.AsBinary()}");
                //    logCounter = 0;
                //    elapse.Restart();
                //}

                //logCounter++;
            }
            M.F("[End] Controller Clean Task(GC)", verbose: gl.VerboseLog);
        }

        //protected virtual void RestartRequest() => RestartRequested?.Invoke(this, new EventArgs());
        //public event EventHandler RestartRequested;
        #endregion

    }//End Class AudioControl


    public partial class AudioControl : JPack.NotifyPropertyChanged, IDisposable
    {
        public AudioControl(Configs.General gl)
        {
            this.gl = gl;
            if (gl.Version == "") M.F("AudioControl: Configs.General linker version is old (<0.6)", verbose: gl.VerboseLog);
        }

        private readonly Configs.General gl;
        internal CoreAudio.Manager core;
        public bool CanAccess => core?.CanAccess ?? false;

        internal bool Debug;
        private readonly object terminatelock = new object();
        private volatile bool _Terminate = false;

        internal bool Terminate
        {
            get => _Terminate;
            set { lock (terminatelock) { _Terminate = value; } }
        }
        private readonly List<Task> ControlTasks = new List<Task>();

        #region Dispose
        private bool disposedValue;
        protected virtual async void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    await Stop();
                    try { core?.Dispose(); }
                    catch (Exception e) { M.D(e.Message); }
                    finally { core = null; }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AudioControl()
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
        #endregion
    }
}