using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Wale.Extensions;

namespace Wale.Controller
{
    public class AudioControl : JPack.NotifyPropertyChanged, IDisposable
    {
        #region class loads
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        public Configs.General gl { get; }
        public AudioControl(Configs.General gl)
        {
            this.gl = gl;
            if (gl.Version == "") M.F("AudioControl: Configs.General linker version is old (<0.6.5)", verbose: gl.VerboseLog);

            core = new CoreAudio.Manager(gl);
            core.PropertyChanged += (sender, e) => Core_PropertyChanged(sender, e);
            //Core_PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("ALL"));

            Control = new SessionControl(gl, false);
            Control.SessionAdded += (sender, s) => SessionAdded?.Invoke(sender, s);

            aWorker = new AudioWorker(gl, core, Control, false);
            cWorker = new CleanWorker(gl, core, Control, false);
        }

        #region core manager
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        internal CoreAudio.Manager core { get; private set; }
        public bool CanAccess => core?.CanAccess ?? false;

        private void Core_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(core.MMDPeakValue):
                    OnPropertyChanged(nameof(MasterPeak));
                    break;
                case nameof(core.MMDVolumeValue):
                    OnPropertyChanged(nameof(MasterVolume));
                    break;
                case nameof(core.MMDMuted):
                    OnPropertyChanged(nameof(MasterMuted));
                    break;
                case "ALL":
                    OnPropertyChanged(nameof(MasterPeak));
                    OnPropertyChanged(nameof(MasterVolume));
                    OnPropertyChanged(nameof(MasterMuted));
                    break;
            }
        }

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

        /// <inheritdoc cref="CoreAudio.SessionList"/>
        public CoreAudio.SessionList Sessions => core?.Sessions;
        #endregion

        //internal bool Debug { get; set; }
        private readonly object terminatelock = new object();
        private volatile bool _Terminate = false;
        internal bool Terminate
        {
            get => _Terminate;
            set { lock (terminatelock) { _Terminate = value; } }
        }

        private readonly SessionControl Control;
        private readonly AudioWorker aWorker;
        private readonly CleanWorker cWorker;

        public event EventHandler<CoreAudio.Session> SessionAdded;
        #endregion

        #region start stop
        private bool started = false, starting = false;
        /// <summary>
        /// Start audio controller
        /// </summary>
        /// <param name="debug">True when to enable debug mode</param>
        public bool Start(bool debug = false)
        {
            if (started) return true;
            if (starting) return true;
            else starting = true;
            //Debug = debug;

            Control.Debug = debug;

            aWorker?.Start();
            cWorker?.Start();

            starting = false;
            started = true;
            return false;
        }
        /// <summary>
        /// Stop audio controller safely
        /// </summary>
        public void Stop()
        {
            Terminate = true;
            aWorker?.Stop();
            cWorker?.Stop();
            started = false;
        }
        #endregion

        #region Dispose
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    Stop();
                    try { core?.Dispose(); }
                    catch (Exception e) { M.D(e.Message); }
                    finally { core = null; }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                aWorker?.Dispose();
                cWorker?.Dispose();
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~AudioControl()
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

    }//End Class AudioControl


    internal class AudioWorker : Worker
    {
        public AudioWorker(Configs.General gl, CoreAudio.Manager core, SessionControl control, bool start = true) : base(gl, core, control)
        {
            d = new TimeSpan(0, 0, 0, 0, (int)(gl.StaticMode ? gl.UIUpdateInterval : gl.AutoControlInterval));
            acDev = gl.ACDevShow == Visibility.Visible;

            if (start) Start();
        }

        protected override void Gl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(gl.StaticMode):
                case nameof(gl.UIUpdateInterval):
                case nameof(gl.AutoControlInterval):
                    d = new TimeSpan(0, 0, 0, 0, (int)(gl.StaticMode ? gl.UIUpdateInterval : gl.AutoControlInterval));
                    Interval = (int)(gl.StaticMode ? gl.UIUpdateInterval : gl.AutoControlInterval);
                    break;
                case nameof(gl.ACDevShow):
                    acDev = gl.ACDevShow == Visibility.Visible;
                    break;
            }
        }


        private readonly List<Task> aas = new List<Task>();
        private uint logCounter = 0;
        private const uint logCritical = 1000000, swCritical = 0;
        private readonly string logCountMsg = logCritical == 1000000 ? "1 million" : logCritical.ToString();
        private readonly System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        private readonly List<double> elAudio = new List<double>(), elWait = new List<double>(), widif = new List<double>();
        private TimeSpan d;
        private bool acDev = false;

        private async Task Work()
        {
            //Console.WriteLine($"ACTaskWaited ={(double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency * 1000:n3}[ms]");// *10000000 T[100ns]
            if (acDev)
            {
                sw.Stop();
                TimeSpan elapsedWait = sw.Elapsed;
                elWait.Add(elapsedWait.TotalMilliseconds);
                widif.Add(elapsedWait.Subtract(d).TotalMilliseconds);
                sw.Restart();
            }

            if (gl.AutoControl && CanAccess)
            {
                aas.Clear();
                try
                {
                    lock (Sessions.Locker)
                    {
                        Sessions.DisposedCheck();
                        Sessions.ForEach(s => aas.Add(Task.Run(() => Control.Run(s, core?.DeviceID))));
                    }

                    if (logCounter > logCritical)
                    {
                        M.F($"Auto control task passed for {logCountMsg} times.", verbose: gl.VerboseLog);
                        logCounter = 0;
                    }
                }
                catch (InvalidOperationException e) { M.F($"Error(AudioControlTask): Session collection was modified.\r\n\t{e}", verbose: gl.VerboseLog); }
                catch (Exception e) { if (e.HResult.IsUnknown()) M.F($"Error(AudioControlTask): Unknown.\r\n\t{e}", verbose: gl.VerboseLog); }

                logCounter++;
            }

            if (acDev && elAudio.Count > swCritical)
            {
                gl.ACElapsed = elAudio.Average(); gl.ACWaited = elWait.Average(); gl.ACEWdif = widif.Average();
                elAudio.Clear(); elWait.Clear(); widif.Clear();
            }

            if (gl.AutoControl) await Task.WhenAll(aas);

            if (acDev)
            {
                sw.Stop();
                TimeSpan elapsedControl = sw.Elapsed;
                elAudio.Add(elapsedControl.TotalMilliseconds);
                sw.Start();
            }
        }
        public void Start()
        {
            M.F("[Start] Audio Control Task", verbose: gl.VerboseLog);
            base.Start(Work, (int)(gl.StaticMode ? gl.UIUpdateInterval : gl.AutoControlInterval));
        }
        public void Stop() => base.Stop(() => M.F("[End] Audio Control Task", verbose: gl.VerboseLog));
    }
    internal class CleanWorker : Worker
    {
        public CleanWorker(Configs.General gl, CoreAudio.Manager core, SessionControl control, bool start = true) : base(gl, core, control)
        {
            if (start) Start();
        }

        protected override void Gl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(gl.GCInterval):
                    Interval = (int)gl.GCInterval;
                    break;
            }
        }


        private readonly List<Task> aas = new List<Task>();
        //private readonly int waitone = (int)new TimeSpan(0, 0, 1).TotalMilliseconds;
        //uint logCounter = uint.MaxValue, logCritical = (uint)(1800000 / gl.GCInterval);
        //System.Diagnostics.Stopwatch loop = new System.Diagnostics.Stopwatch(), elapse = new System.Diagnostics.Stopwatch();

        private async Task Work()
        {
            //elapse.Restart();
            //if (Debug) loop.Restart();

            while (!CanAccess) await Task.Delay(1000);
            if (gl.AutoControl)
            {
                aas.Clear();
                try
                {
                    lock (Sessions.Locker)
                    {
                        Sessions.ForEach(s => aas.Add(Task.Run(() => Control.Reset(s))));
                    }
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
        public void Start()
        {
            M.F("[Start] Controller Clean Task(GC)", verbose: gl.VerboseLog);
            base.Start(Work, (int)gl.GCInterval);
        }
        public void Stop() => base.Stop(() => M.F("[End] Controller Clean Task(GC)", verbose: gl.VerboseLog));
    }
    internal abstract class Worker : TimedWorker
    {
        public readonly Configs.General gl;
        public readonly CoreAudio.Manager core;
        public readonly SessionControl Control;
        public Worker(Configs.General gl, CoreAudio.Manager core, SessionControl control) : base(gl.ForceMMT)
        {
            this.gl = gl;
            this.gl.PropertyChanged += Gl_PropertyChanged;
            this.core = core;
            Control = control;
        }
        protected abstract void Gl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e);

        protected virtual CoreAudio.SessionList Sessions => core?.Sessions;
        protected virtual bool CanAccess => core?.CanAccess ?? false;
    }

}