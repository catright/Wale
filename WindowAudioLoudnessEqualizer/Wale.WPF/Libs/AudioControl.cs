using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Runtime.InteropServices;
using Wale.CoreAudio;
using System.Windows;
using HighPrecisionTimer;

namespace Wale
{
    public class AudioControl : AudioControlInternal
    {
        //private Wale.WPF.Properties.Settings settings = Wale.WPF.Properties.Settings.Default;
        private Wale.Configuration.General settings;
        //private Wale.WPF.Datalink DL;
        private Wale.Configuration.General DL => settings;
        #region global variables
        /// <summary>
        /// List&lt;Session&gt;
        /// </summary>
        public SessionList Sessions => core?.Sessions;
        public bool Restarting => (bool)core?.Restarting;
        public bool Restarted { get => (bool)core?.Restarted; set { if (core != null) core.Restarted = false; } }
        public bool Debug { get => DP.DebugMode; set => DP.DebugMode = value; }
        public double MasterPeak
        {
            get
            {
                if (core != null && !core.NoDevice) { return core.MasterPeak; }
                else if (core != null && core.NoDevice) { return -2; }
                else return 0;
            }
        }
        public double MasterVolume
        {
            get
            {
                if (core != null && !core.NoDevice) { return core.MasterVolume; }
                else if (core != null && core.NoDevice) { return -2; }
                else return 0;
            }
        }
        public double UpRate { get => upRate; set { upRate = (value * settings.AutoControlInterval / 1000); } }
        public double Kurtosis { get => kurtosis; set => kurtosis = value; }
        public VFunction.Func VFunc { get => _VFunc; set => _VFunc = value; }
        #endregion

        #region class loads
        public AudioControl(ref Wale.Configuration.General gn)
        {
            //DL = dl;
            settings = gn;
            if (settings.Version == "") JPack.FileLog.Log("AutioControl: Using new config");
            DP = new JPack.DebugPack(false);
        }
        /// <summary>
        /// Start audio controller
        /// </summary>
        /// <param name="debug">True when enable debug mode</param>
        public void Start(bool debug = false)
        {
            Debug = debug;

            UpdateVFunc();

            //core = new Core(
            //    (float)settings.TargetLevel,
            //    settings.AverageTime,
            //    settings.AutoControlInterval,
            //    settings.ExcList,
            //    true
            //);
            core = new Core(ref settings, true);
            core.RestartRequested += Audio_RestartRequested;
            ControlTasks.Add(ControllerCleanTask());
            //controllerCleanTask = new Task(ControllerCleanTask);
            //controllerCleanTask.Start();

            ControlTasks.Add(AudioControlTask());
            //audioControlTask = new Task(AudioControlTask);
            //audioControlTask.Start();
        }

        private void Audio_RestartRequested(object sender, EventArgs e) { RestartRequest(); }
        #endregion


        public List<DeviceData> GetDeviceMap() => core.GetDeviceList();
        public Tuple<string, string> GetDeviceName() => core.DeviceNameTpl;

        #region Master Volume controls
        public void VolumeUp(double v) { SetMasterVolume(MasterVolume + v); }
        public void VolumeDown(double v) { SetMasterVolume(MasterVolume - v); }
        public void SetMasterVolume(double v)
        {
            if (v < 0) { JPack.FileLog.Log($"Set Volume Error: {v}"); return; }
            v = v > 1 ? 1 : v < 0.01 ? 0.01 : v;

            DP.DML($"SetTo:{v}");
            core.MasterVolume = (float)v;
        }
        #endregion


        #region Session Control
        public class SesseionEventArgs : EventArgs
        {
            public SesseionEventArgs(Session session) { Session = session; }
            public Session Session { get; }
        }
        public delegate void SessionAddedDelegate(object sender, SesseionEventArgs e);
        public event SessionAddedDelegate SessionAdded;
        public event SessionAddedDelegate SesseionRemoved;
        //public event SessionAddedDelegate PeakVolumeChanged;
        private void SessionControl(Session s)
        {
            // Session removed
            if (s == null || s.State == SessionState.Expired || s.Disposed) { SesseionRemoved?.Invoke(this, new SesseionEventArgs(s)); return; }

            lock (s.Locker)
            {
                // occur when got session change which is not intended
                if (s?.ProcessID < 0) { JPack.FileLog.Log($"{s.Name} is changed. Dispose session"); s.Dispose(); RestartRequest(); return; }

                // New session added
                if (s.NewlyAdded) { s.NewlyAdded = false; SessionAdded?.Invoke(this, new SesseionEventArgs(s)); }
                // Session removed
                if (s == null || s.State == SessionState.Expired || s.Disposed) { SesseionRemoved?.Invoke(this, new SesseionEventArgs(s)); return; }

                // Control session(=s) when s is not in exclude list, auto included, and not muted
                if (!settings.ExcList.Contains(s.Name) && s.AutoIncluded && s.SoundEnabled)
                {
                    // static control when in static mode
                    if (settings.StaticMode) StaticControl(s);
                    // active control only when session is active
                    else if (s.State == SessionState.Active) ActiveControl(s);
                }
            }
        }
        private void ActiveControl(Session s)
        {
            StringBuilder dm = new StringBuilder().Append($"AutoVolume:{s.Name}({s.ProcessID}), inc={s.AutoIncluded}");
            double peak = s.Peak;
            // math 2^0=1 but skip math calculation and set relFactor to 1 for calc speed when relative is 0
            double relFactor = (s.Relative == 0 ? 1 : Math.Pow(Wale.Configuration.Audio.RelativeBase, s.Relative));
            double volume = s.Volume / relFactor;
            dm.Append($" P:{peak:n3} V:{volume:n3}");
            if (volume == 0) volume = 0.001;

            // control volume when audio session makes sound
            if (peak > settings.MinPeak)
            {
                double tVol, UpLimit;

                // update average
                if (s.State != SessionState.Active) { return; }//Check session activity
                if (settings.Averaging) s.SetAverage(peak);

                // when averaging, lower volume once if current peak exceeds average or set volume along average.
                //double cutLv = settings.TargetLevel + (settings.TargetLevel * settings.LimitLevel);
                if (s.State != SessionState.Active) { return; }//Check session activity
                tVol = Volume(peak, s.AveragePeak, volume);

                // calc upLimit by vfunc, deprecated
                switch (VFunc)
                {
                    case VFunction.Func.Linear:
                        UpLimit = VFunction.Linear(volume, UpRate) + volume;
                        break;
                    case VFunction.Func.SlicedLinear:
                        UpLimit = VFunction.SlicedLinear(volume, UpRate, settings.TargetLevel, sliceFactors.A, sliceFactors.B) + volume;
                        break;
                    case VFunction.Func.Reciprocal:
                        UpLimit = VFunction.Reciprocal(volume, UpRate, kurtosis) + volume;
                        break;
                    case VFunction.Func.FixedReciprocal:
                        UpLimit = VFunction.FixedReciprocal(volume, UpRate, kurtosis) + volume;
                        break;
                    default:
                        UpLimit = upRate + volume;
                        break;
                }

                // set volume
                if (s.State != SessionState.Active) { return; }//Check session activity
                dm.Append($" T={tVol:n3} UL={UpLimit:n3}");//Console.WriteLine($" T={tVol:n3} UL={UpLimit:n3}");
                float fVol = (float)((tVol > UpLimit ? UpLimit : tVol) * relFactor);
                //System.Diagnostics.Debug.WriteLine($"{volume} {fVol} {tVol}");
                s.Volume = fVol;
            }
            DP.DML(dm.ToString());// print debug message
        }
        private double Volume(double peak, double average, double volume)
        {
            if (settings.Averaging) return Volume1(peak, average);
            else return Volume2(peak, volume, settings.CompRate);
        }
        private double Volume0(double peak, double average)
        {
            if (peak < settings.LimitLevel) return settings.TargetLevel / average;
            else return settings.LimitLevel / peak;
        }
        private double Volume1(double peak, double average)
        {
            double tVol = settings.TargetLevel / average;
            if (tVol * peak > settings.LimitLevel) tVol = settings.LimitLevel / peak;
            return tVol;
        }
        private double Volume2(double peak, double volume, double rate = 2)
        {
            //System.Diagnostics.Debug.WriteLine("vol2 running");
            //if (rate < 1) rate = 1;
            //double tVol = settings.TargetLevel * (1 / (((settings.TargetLevel - peak) / rate) + peak));
            double tVol = settings.TargetLevel / Math.Pow(peak, rate);
            if (tVol * peak > settings.LimitLevel) tVol = settings.LimitLevel / peak;
            return tVol;
        }
        private void StaticControl(Session s)
        {
            double relFactor = (s.Relative == 0 ? 1 : Math.Pow(4, s.Relative));
            s.Volume = (float)(settings.TargetLevel * relFactor);
        }

        public void UpdateAverageParam() { lock (Locks.Session) { core.UpdateAvTimeAll(settings.AverageTime, settings.AutoControlInterval); } }
        public void UpdateVFunc() { if (!Enum.TryParse(settings.VFunc, out _VFunc)) JPack.FileLog.Log("Invalid function for session control"); return; }
        #endregion

        #region Automatic volume control
        //private Task HPTimer(int d) => HighPrecisionTimer.HPT.Delay(d, HPT.Select.MMT);
        //private readonly TaskSynchronize.AsyncLock TaskSync = new TaskSynchronize.AsyncLock();
        //private volatile bool ACrunning = false, CCrunning = false;
        private async Task AudioControlTask()
        {
            JPack.FileLog.Log("Audio Control Task Start");
            List<Task> aas = new List<Task>();
            uint logCounter = 0, logCritical = 100000, swCritical = (uint)(1);//settings.UIUpdateInterval / settings.AutoControlInterval
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            List<double> elapsed = new List<double>(), waited = new List<double>(), widif = new List<double>();
            bool acDev = false;
            //System.Timers.Timer timer = new System.Timers.Timer();

            while (!Terminate)
            {
                //ACrunning = true;
                //while (CCrunning) { await HPTimer(1); }
                TimeSpan d = new TimeSpan(0, 0, 0, 0, (int)(settings.StaticMode ? settings.UIUpdateInterval : settings.AutoControlInterval));
                Task waitTask = HPT.Delay((int)d.TotalMilliseconds, HPT.Select.MMT, DL.ForceMMT);//HPTimer((int)d.TotalMilliseconds);
                sw.Restart();

                if (settings.AutoControl)
                {
                    try
                    {
                        lock (Locks.Session)
                        {
                            //if (audio.MasterDeviceIsDisposed != true) { JPack.FileLog.Log("Master Device is changed. Restart."); audio.Restart(); }
                            //if (audio.MasterDeviceIsDisposed != true) { JPack.FileLog.Log("Master Device is changed. Restart."); RestartRequest(); }
                            //if (settings.StaticMode) { Sessions.ForEach(s => aas.Add(new Task(() => SessionControlinStaticMode(s)))); }
                            Sessions.DisposedCheck();
                            Sessions.ForEach(s =>
                            {
                                aas.Add(new Task(() => SessionControl(s)));
                            });

                            if (logCounter > logCritical)
                            {
                                aas.Add(new Task(() => JPack.FileLog.Log($"Auto control task passed for {logCritical} times.")));
                                logCounter = 0;
                            }
                            aas.ForEach(t => t.Start());
                        }
                    }
                    catch (InvalidOperationException e) { JPack.FileLog.Log($"Error(AudioControlTask): Session collection was modified.\r\n\t{e.ToString()}"); }
                    catch (Exception e) { JPack.FileLog.Log($"Error(AudioControlTask): Unknown.\r\n\t{e.ToString()}"); }
                }

                acDev = DL.ACDevShow == Visibility.Visible;

                logCounter++;
                await Task.WhenAll(aas);
                aas.Clear();
                sw.Stop();
                TimeSpan el = sw.Elapsed;

                sw.Start();
                //Console.WriteLine($"ACTaskElapsed={sw.ElapsedMilliseconds}(-{d.Ticks / 10000:n3})[ms]");
                if (acDev) elapsed.Add(el.TotalMilliseconds);//(double)el.Ticks / 10000
                                                             //if (d.Ticks > 0) { System.Threading.Thread.Sleep(d); }
                try
                {
                    await (waitTask == null ? Task.CompletedTask : waitTask);
                    //if (waitTask != null) await waitTask;
                    //else JPack.FileLog.Log("waitTask is null");
                }
                catch (Exception e) { JPack.FileLog.Log($"AudioControlTask: Exception on waitTask. {e}"); }
                //finally { if (waitTask != null) waitTask.Dispose(); }
                sw.Stop();
                //Console.WriteLine($"ACTaskWaited ={(double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency * 1000:n3}[ms]");// *10000000 T[100ns]
                TimeSpan wt = sw.Elapsed;
                if (acDev) waited.Add(wt.TotalMilliseconds);//(double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency * 1000
                if (acDev) widif.Add(wt.Subtract(d).TotalMilliseconds);//(double)d.Ticks / 10000

                if (acDev && waited.Count >= swCritical)
                {
                    DL.ACElapsed = elapsed.Average(); DL.ACWaited = waited.Average(); DL.ACEWdif = widif.Average();
                    elapsed.Clear(); waited.Clear(); widif.Clear();
                    //swCritical = 0;
                }
                //ACrunning = false;
            }// end while loop

            JPack.FileLog.Log("Audio Control Task End");
        }// end AudioControlTask
        private async Task ControllerCleanTask()
        {
            JPack.FileLog.Log("Controller Clean Task(GC) Start");
            List<Task> aas = new List<Task>();
            uint logCounter = uint.MaxValue, logCritical = (uint)(1800000 / settings.GCInterval);
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            //TimeSpan elapsed = new TimeSpan();

            while (!Terminate)
            {
                //CCrunning = true;
                //while (ACrunning) { await HPTimer(1); }
                sw.Restart();
                if (settings.AutoControl)
                {
                    try
                    {
                        Sessions.ForEach(s =>
                        {
                            aas.Add(new Task(new Action(() =>
                            {
                                if (s.AutoIncluded && !core.ExcludeList.Contains(s.Name))
                                {
                                    SessionState state = s.State;
                                    if (state != SessionState.Active && s.Volume != 0.01)
                                    {
                                            //SetSessionVolume(s.ProcessID, 0.01);
                                            s.Volume = 0.01f;
                                        s.ResetAverage();
                                    }
                                }
                            })));
                        });

                        aas.ForEach(t => t.Start());
                    }
                    catch (InvalidOperationException e) { JPack.FileLog.Log($"Error(ControllerCleanTask): Session collection was modified.\r\n\t{e.ToString()}"); }
                }

                await Task.Delay(new TimeSpan((long)(settings.GCInterval * 10000)));

                if (settings.AutoControl)
                {
                    await Task.WhenAll(aas);
                    aas.Clear();
                }

                long mmc = GC.GetTotalMemory(false);
                TimeSpan el = sw.Elapsed;
                if (Debug) { Console.WriteLine($"Total Memory: {mmc:n0}/{el.TotalMilliseconds}ms"); }
                if (logCounter > logCritical)
                {
                    JPack.FileLog.Log($"Memory Cleaned, Total Memory: {mmc:n0}/{el.TotalMilliseconds}ms");
                    logCounter = 0;
                }

                logCounter++;
                //CCrunning = false;
            }
            JPack.FileLog.Log("Controller Clean Task(GC) End");
        }

        protected virtual void RestartRequest() => RestartRequested?.Invoke(this, new EventArgs());
        public event EventHandler RestartRequested;
        #endregion


    }//End Class AudioControl


    public class AudioControlInternal : IDisposable
    {
        #region private variables
        protected JPack.DebugPack DP;
        protected CoreAudio.Core core;

        protected object terminatelock = new object(), autoconlock = new object(), AClocker = new object(), AccuTimerlock = new object();
        private bool _Terminate = false;
        protected bool Terminate { get { lock (terminatelock) { return _Terminate; } } set { lock (terminatelock) { _Terminate = value; } } }
        //private bool _AccuTimer = false;
        //protected bool AccuTimer { get { lock (AccuTimerlock) { return _AccuTimer; } } set { lock (AccuTimerlock) { _AccuTimer = value; } } }
        protected System.Threading.CancellationTokenSource AccuTimerCTS;
        protected double upRate = 0.02, kurtosis = 0.5;
        protected VFunction.Func _VFunc;
        protected VFunction.FactorsForSlicedLinear sliceFactors;
        protected List<Task> ControlTasks = new List<Task>();//audioControlTask, controllerCleanTask;
        #endregion

        protected System.Timers.Timer Timer;
        protected void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) { if (!AccuTimerCTS.IsCancellationRequested) AccuTimerCTS.Cancel(); }

        #region Dispose
        public bool Disposed => disposed;
        private bool disposed = false, disposing = false;
        //~AudioControlInternal() { Dispose(false); }
        public void Dispose()
        {
            Dispose(true);
            //GC.SuppressFinalize(this);
        }
        protected virtual async void Dispose(bool disposeSafe)
        {
            if (!disposed && !disposing)
            {
                disposing = true;
                if (disposeSafe)
                {
                    Terminate = true;
                    await Task.WhenAll(ControlTasks);
                    //if (audioControlTask != null) await audioControlTask;
                    //if (controllerCleanTask != null) await controllerCleanTask;

                    ControlTasks.ForEach(i => i?.Dispose());
                    //audioControlTask.Dispose();
                    //controllerCleanTask.Dispose();

                    core?.Dispose();

                    await Task.Delay(250);
                }
                disposed = true;
            }
        }
        #endregion
    }
}//End Namespace WindowAudioLoudnessEqualizer