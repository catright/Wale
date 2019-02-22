using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Runtime.InteropServices;
using Wale.CoreAudio;

namespace Wale
{
    public class AudioControl : AudioControlInternal
    {
        private Wale.WPF.Properties.Settings settings = Wale.WPF.Properties.Settings.Default;
        private Wale.WPF.Datalink DL;
        #region global variables
        /// <summary>
        /// List&lt;Session&gt;
        /// </summary>
        public SessionList Sessions => audio?.Sessions;
        public bool Debug { get => DP.DebugMode; set => DP.DebugMode = value; }
        public double MasterPeak
        {
            get
            {
                if (audio != null && !audio.NoDevice) { return audio.MasterPeak; }
                else if (audio != null && audio.NoDevice) { return -2; }
                else return 0;
            }
        }
        public double MasterVolume
        {
            get
            {
                if (audio != null && !audio.NoDevice) { return audio.MasterVolume; }
                else if (audio != null && audio.NoDevice) { return -2; }
                else return 0;
            }
        }
        public double UpRate { get => upRate; set { upRate = (value * settings.AutoControlInterval / 1000); } }
        public double Kurtosis { get => kurtosis; set => kurtosis = value; }
        public VFunction.Func VFunc { get => _VFunc; set => _VFunc = value; }
        #endregion

        #region class loads
        public AudioControl(WPF.Datalink dl)
        {
            DL = dl;
            DP = new JDPack.DebugPack(false);
        }
        /// <summary>
        /// Start audio controller
        /// </summary>
        /// <param name="debug">True when enable debug mode</param>
        public void Start(bool debug = false)
        {
            Debug = debug;

            UpdateVFunc();

            audio = new Audio((float)settings.TargetLevel, settings.AverageTime, settings.AutoControlInterval, true)
            {
                ExcludeList = settings.ExcList.Cast<string>().ToList()
            };

            ControlTasks.Add(ControllerCleanTask());
            //controllerCleanTask = new Task(ControllerCleanTask);
            //controllerCleanTask.Start();

            ControlTasks.Add(AudioControlTask());
            //audioControlTask = new Task(AudioControlTask);
            //audioControlTask.Start();
        }
        #endregion
        

        public List<DeviceData> GetDeviceMap() { return audio.GetDeviceList(); }
        public Tuple<string,string> GetDeviceName() { return audio.DeviceNameTpl; }

        #region Master Volume controls
        public void VolumeUp(double v) { SetMasterVolume(MasterVolume + v); }
        public void VolumeDown(double v) { SetMasterVolume(MasterVolume - v); }
        public void SetMasterVolume(double v)
        {
            if (v < 0) { JDPack.FileLog.Log($"Set Volume Error: {v}"); return; }
            v = v > 1 ? 1 : v < 0.01 ? 0.01 : v;

            DP.DML($"SetTo:{v}");
            audio.MasterVolume = (float)v;
        }
        #endregion


        #region Session Control
        private void SessionControl(Session s)
        {
            if (s.ProcessID < 0) { JDPack.FileLog.Log($"{s.Name} is changed. Dispose session"); s.Dispose(); return; }

            StringBuilder dm = new StringBuilder().Append($"AutoVolume:{s.Name}({s.ProcessID}), inc={s.AutoIncluded}");

            if (!settings.ExcList.Contains(s.Name) && s.AutoIncluded && s.State == SessionState.Active)
            {
                double peak = s.Peak;
                // math 2^0=1 but skip math calculation and set relFactor to 1 for calc speed when relative is 0
                double relFactor = (s.Relative == 0 ? 1 : Math.Pow(2, s.Relative));
                double volume = s.Volume / relFactor;
                dm.Append($" P:{peak:n3} V:{volume:n3}");

                // control volume when audio session makes sound
                if (peak > settings.MinPeak)
                {
                    double tVol, UpLimit;

                    // update average
                    if (settings.Averaging) s.SetAverage(peak);

                    // when averaging, lower volume once if current peak exceeds average or set volume along average.
                    if (settings.Averaging && peak < s.AveragePeak) tVol = settings.TargetLevel / s.AveragePeak;
                    else tVol = settings.TargetLevel / peak;

                    // calc upLimit by vfunc
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
                    dm.Append($" T={tVol:n3} UL={UpLimit:n3}");//Console.WriteLine($" T={tVol:n3} UL={UpLimit:n3}");
                    s.Volume = (float)((tVol > UpLimit ? UpLimit : tVol) * relFactor);
                }
                DP.DML(dm.ToString());// print debug message
            }
        }
        public void UpdateAverageParam() { lock (Lockers.Sessions) { audio.UpdateAvTimeAll(settings.AverageTime, settings.AutoControlInterval); } }
        public void UpdateVFunc() { if (!Enum.TryParse(settings.VFunc, out _VFunc)) JDPack.FileLog.Log("Invalid function for session control"); return; }
        #endregion

        #region Automatic volume control
        private async Task AudioControlTask()
        {
            JDPack.FileLog.Log("Audio Control Task Start");
            List<Task> aas = new List<Task>();
            uint logCounter = 0, logCritical = 10000, swCritical = (uint)(settings.UIUpdateInterval / settings.AutoControlInterval);
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            List<double> elapsed = new List<double>(), ewdif = new List<double>(), waited = new List<double>();
            //System.Timers.Timer timer = new System.Timers.Timer();
            while (!Terminate)
            {
                sw.Restart();
                if (settings.AutoControl)
                {
                    try
                    {
                        lock (Lockers.Sessions)
                        {
                            //if(audio.MasterDeviceIsDisposed != true) { JDPack.FileLog.Log("Master Device is changed. Restart."); audio.Restart(); }
                            Sessions.ForEach(s => aas.Add(new Task(() => SessionControl(s))));
                        }
                        if (logCounter > logCritical)
                        {
                            aas.Add(new Task(() => JDPack.FileLog.Log($"Auto control task passed for {logCritical} times.")));
                            logCounter = 0;
                        }
                        aas.ForEach(t => t.Start());
                    }
                    catch (InvalidOperationException e) { JDPack.FileLog.Log($"Error(AudioControlTask): Session collection was modified.\r\n\t{e.ToString()}"); }
                }

                await Task.WhenAll(aas);
                aas.Clear();
                logCounter++;

                sw.Stop();
                TimeSpan el = sw.Elapsed;
                sw.Start();
                TimeSpan d = new TimeSpan((long)(settings.AutoControlInterval * 10000)).Subtract(el);
                //Console.WriteLine($"ACTaskElapsed={sw.ElapsedMilliseconds}(-{d.Ticks / 10000:n3})[ms]");
                elapsed.Add((double)el.Ticks / 10000);
                ewdif.Add((double)d.Ticks / 10000);
                //if (d.Ticks > 0) { System.Threading.Thread.Sleep(d); }
                if (d.Ticks > 0) { await Task.Delay(d); }
                sw.Stop();
                //Console.WriteLine($"ACTaskWaited ={(double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency * 1000:n3}[ms]");// *10000000 T[100ns]
                waited.Add((double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency * 1000);

                if (waited.Count > swCritical)
                {
                    DL.ACElapsed = elapsed.Average(); DL.ACEWdif = elapsed.Average(); DL.ACWaited = waited.Average();
                    elapsed.Clear(); ewdif.Clear(); waited.Clear();
                    swCritical = 0;
                }
            }// end while loop

            JDPack.FileLog.Log("Audio Control Task End");
        }// end AudioControlTask
        private async Task ControllerCleanTask()
        {
            JDPack.FileLog.Log("Controller Clean Task(GC) Start");
            List<Task> aas = new List<Task>();
            uint logCounter = uint.MaxValue, logCritical = (uint)(1800000 / settings.GCInterval);

            while (!Terminate)
            {
                if (settings.AutoControl)
                {
                    try
                    {
                        Sessions.ForEach(s =>
                        {
                            aas.Add(new Task(new Action(() =>
                            {
                                if (s.AutoIncluded && !audio.ExcludeList.Contains(s.Name))
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
                    catch (InvalidOperationException e) { JDPack.FileLog.Log($"Error(ControllerCleanTask): Session collection was modified.\r\n\t{e.ToString()}"); }
                }
                
                await Task.Delay(new TimeSpan((long)(settings.GCInterval * 10000)));

                if (settings.AutoControl)
                {
                    await Task.WhenAll(aas);
                    aas.Clear();
                }

                long mmc = GC.GetTotalMemory(true);
                if (Debug) { Console.WriteLine($"Total Memory: {mmc:n0}"); }
                if (logCounter > logCritical)
                {
                    JDPack.FileLog.Log($"Memory Cleaned, Total Memory: {mmc:n0}");
                    logCounter = 0;
                }

                logCounter++;

            }
            JDPack.FileLog.Log("Controller Clean Task(GC) End");
        }
        #endregion


    }//End Class AudioControl
    public class AudioControlInternal : IDisposable
    {
        #region private variables
        protected JDPack.DebugPack DP;
        protected CoreAudio.Audio audio;

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
        private bool disposed = false;
        ~AudioControlInternal() { Dispose(false); }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual async void Dispose(bool disposing)
        {
            if (!disposed)
            {
                Terminate = true;
                await Task.WhenAll(ControlTasks);
                //if (audioControlTask != null) await audioControlTask;
                //if (controllerCleanTask != null) await controllerCleanTask;
                if (disposing)
                {
                    ControlTasks.ForEach(i => i.Dispose());
                    //audioControlTask.Dispose();
                    //controllerCleanTask.Dispose();
                }
                await Task.Delay(250);
                disposed = true;
            }
        }
        #endregion
    }
}//End Namespace WindowAudioLoudnessEqualizer