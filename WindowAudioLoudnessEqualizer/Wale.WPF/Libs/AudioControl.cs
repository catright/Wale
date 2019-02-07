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
        //public Wale.CoreAudio.SessionDataList Sessions
        public SessionList Sessions
        {
            get
            {
                if (audio != null) return audio.Sessions;
                else return null;
            }
        }
        //public bool AutoControl { get => Autocon(); set { Autocon(value); if (!Autocon()) ResetAllSessionVolume(); } }
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
        #endregion

        #region class loads
        public AudioControl(double bVol, WPF.Datalink dl,  Transformation.TransMode tmode = Transformation.TransMode.Transform1)
        {
            DL = dl;
            DP = new JDPack.DebugPack(false);
            Transformation.ChangeTransformMethod(tmode);
            SetBaseTo(bVol);
        }
        /// <summary>
        /// Start audio controller
        /// </summary>
        /// <param name="debug">True when enable debug mode</param>
        public void Start(bool debug = false)
        {
            Debug = debug;

            audio = new CoreAudio.Audio((float)baseLv, true);
            audio.AverageTime = settings.AverageTime;
            audio.AverageInterval = settings.AutoControlInterval;
            audio.GetSessionManager();
            //masterDevice = new MasterDevice();

            //Sessions = new Sessions3();
            //Refresh();

            controllerCleanTask = new Task(ControllerCleanTask);
            controllerCleanTask.Start();

            audioControlTask = new Task(AudioControlTask);
            audioControlTask.Start();
        }
        #endregion
        

        public List<DeviceData> GetDeviceMap() { return audio.GetDeviceList(); }

        #region Master Volume controls
        public void SetBaseTo(double bVol)
        {
            baseLv = bVol > 1 ? 1 : bVol < 0.01 ? 0.01 : bVol;
            if (audio != null) { audio.TargetOutputLevel = (float)baseLv; }
            baseLvSquare = baseLv * baseLv;
            sliceFactors = VFunction.GetFactorsForSlicedLinear(UpRate, baseLv);
        }
        public void VolumeUp(double v) { SetVolume(MasterVolume + v); }
        public void VolumeDown(double v) { SetVolume(MasterVolume - v); }
        public void SetVolume(double v)
        {
            v = v > 1 ? 1 : v < 0.01 ? 0.01 : v;

            DP.DML($"SetTo:{v}");
            audio.MasterVolume = (float)v;
        }
        #endregion


        #region Session Control
        private void ResetAllSessionVolume()
        {
            lock (Lockers.Sessions) { audio.SetAllSessions(0.01f); }
            //uint[] ids = Wale.Subclasses.Audio.GetApplicationIDs();
            //for (int i = 0; i < ids.Count(); i++) { SetSessionVolume(ids[i], 0.01); }
        }
        private void SetSessionVolume(int id, double v)
        {
            //v += Sessions.GetRelative(id);
            //v = v > 1 ? 1 : v < 0.01 ? 0.01 : v;

            DP.DML($"SessionTo:{v:n6}");
            lock (Lockers.Sessions) { audio.SetSessionVolume(id, (float)v); }
        }
        private void SessionControl(Session s)
        {
            
            StringBuilder dm = new StringBuilder().Append($"AutoVolume:{s.Name}({s.PID}), inc={s.AutoIncluded}");
            if (!audio.ExcludeList.Contains(s.Name) && s.AutoIncluded && s.State == SessionState.Active)
            {
                double peak = s.Peak, volume = s.Volume;
                dm.Append($" P:{peak:n3} V:{volume:n3}");
                if (peak > settings.MinPeak)
                {
                    double tVol, UpLimit;
                    if (s.Averaging) { lock (Lockers.Sessions) { audio.SetSessionAverage(s.ProcessID, peak); } }
                    if (s.Averaging && peak <= s.AveragePeak) tVol = baseLvSquare / s.AveragePeak;
                    else tVol = baseLvSquare / peak;
                    if (!Enum.TryParse(settings.VFunc, out VFunction.Func func)) { JDPack.FileLog.Log("Invalid function for session control"); return; }
                    switch (func)
                    {
                        case VFunction.Func.Linear:
                            UpLimit = VFunction.Linear(volume, UpRate) + volume;
                            break;
                        case VFunction.Func.SlicedLinear:
                            UpLimit = VFunction.SlicedLinear(volume, UpRate, baseLv, sliceFactors.A, sliceFactors.B) + volume;
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
                    dm.Append($" T={tVol:n3} UL={UpLimit:n3}");//Console.WriteLine($" T={tVol:n3} UL={UpLimit:n3}");
                    SetSessionVolume(s.ProcessID, (tVol > UpLimit) ? UpLimit : tVol);
                }
                DP.DML(dm.ToString());
                //Console.WriteLine(dm);
            }
        }
        private void Refresh(bool first = false)
        {
            lock (Lockers.Sessions) { audio.UpdateSession(); }
            //lock (Lockers.Sessions) { Sessions = audio.Sessions; }
            /*lock (AClocker)
            {
                lock (Lockers.Sessions)
                {
                    Sessions.RefreshSessions();
                    Sessions.ForEach(s =>
                    {
                        s.SetAvTime(settings.AverageTime, settings.AutoControlInterval);
                        s.Refresh();
                    });
                }
            }/**/
            //DP.DML($"{Sessions.Count} ({System.DateTime.Now.Ticks})");
        }
        public void UpdateAverageParam() { lock (Lockers.Sessions) { audio.UpdateAvTimeAll(settings.AverageTime, settings.AutoControlInterval); } }
        #endregion

        #region Automatic volume control
        private async void AudioControlTask()
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
                //Task wait = Task.Delay(settings.AutoControlInterval);
                //Refresh();
                //aas.Add(Task.Delay(new TimeSpan((long)(settings.AutoControlInterval * 10000))));
                if (settings.AutoControl)
                {
                    try
                    {
                        lock (Lockers.Sessions)
                        {
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
                }/**/

                //await Task.Delay(new TimeSpan((long)(settings.AutoControlInterval * 10000)));
                //await wait;
                //System.Threading.Thread.Sleep(settings.AutoControlInterval);
                
                //if (settings.AutoControl)
                //{
                    await Task.WhenAll(aas);
                    aas.Clear();
                //}/**/

                /*if (logCounter > logCritical)
                {
                    await JDPack.FileLog.LogAsync($"Auto control task passed for {logCritical} times.");
                    logCounter = 0;
                }/**/
                logCounter++;
                sw.Stop();
                TimeSpan el = sw.Elapsed;
                sw.Start();
                TimeSpan d = new TimeSpan((long)(settings.AutoControlInterval * 10000)).Subtract(el);
                //Console.WriteLine($"ACTaskElapsed={sw.ElapsedMilliseconds}(-{d.Ticks / 10000:n3})[ms]");
                elapsed.Add((double)el.Ticks / 10000);
                ewdif.Add((double)d.Ticks / 10000);
                if (d.Ticks > 0)
                {
                    //Timer = new System.Timers.Timer(d.TotalMilliseconds) { AutoReset = false };
                    //Timer.Elapsed += Timer_Elapsed;
                    //Timer.Start();
                    //AccuTimerCTS = new System.Threading.CancellationTokenSource();
                    //try { await Task.Delay(d, AccuTimerCTS.Token); } catch (OperationCanceledException) { }
                    await Task.Delay(d);
                }
                //if (d.Ticks > 0) { await Task.Delay(d); }
                //await Task.Delay(new TimeSpan((long)(settings.AutoControlInterval * 10000)));
                sw.Stop();
                //Console.WriteLine($"ACTaskWaited ={(double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency * 1000:n3}[ms]");// *10000000 T[100ns]
                waited.Add((double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency * 1000);
                if (waited.Count > swCritical)
                {
                    DL.ACElapsed = elapsed.Average(); DL.ACEWdif = elapsed.Average(); DL.ACWaited = waited.Average();
                    elapsed.Clear(); ewdif.Clear(); waited.Clear();
                    swCritical = 0;
                }
                //if (elapsed.Count >= elapsed.Capacity) elapsed.RemoveAt(0);
                //elapsed.Add(sw.ElapsedMilliseconds);
            }// end while loop

            JDPack.FileLog.Log("Audio Control Task End");
        }// end AudioControlTask
        private async void ControllerCleanTask()
        {
            JDPack.FileLog.Log("Controller Clean Task(GC) Start");
            List<Task> aas = new List<Task>();
            uint logCounter = uint.MaxValue;

            while (!Terminate)
            {
                //Task wait = Task.Delay(settings.GCInterval);

                //bool auto = Autocon();
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
                                        SetSessionVolume(s.ProcessID, 0.01);
                                        s.ResetAverage();
                                    }
                                }
                            })));
                        });
                        aas.ForEach(t => t.Start());
                    }
                    catch (InvalidOperationException e) { JDPack.FileLog.Log($"Error(ControllerCleanTask): Session collection was modified.\r\n\t{e.ToString()}"); }
                }/**/
                
                await Task.Delay(new TimeSpan((long)(settings.GCInterval * 10000)));

                if (settings.AutoControl)
                {
                    await Task.WhenAll(aas);
                    aas.Clear();
                }/**/

                long mmc = GC.GetTotalMemory(true);
                if (Debug) { Console.WriteLine($"Total Memory: {mmc:n0}"); }
                if (logCounter > 1800000/settings.GCInterval)
                {
                    JDPack.FileLog.Log($"Memory Cleaned, Total Memory: {mmc:n0}");
                    logCounter = 0;
                }
                logCounter++;

                //System.Threading.Thread.Sleep(settings.GCInterval);
                //await wait;
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
        protected double baseLv, baseLvSquare, upRate = 0.02, kurtosis = 0.5;
        protected VFunction.FactorsForSlicedLinear sliceFactors;
        protected Task audioControlTask, controllerCleanTask;
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
                if (audioControlTask != null) await audioControlTask;
                if (controllerCleanTask != null) await controllerCleanTask;
                if (disposing)
                {
                    audioControlTask.Dispose();
                    controllerCleanTask.Dispose();
                }
                await Task.Delay(250);
                disposed = true;
            }
        }
        #endregion
    }
}//End Namespace WindowAudioLoudnessEqualizer