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
    //public static class Locker { public static object locker = new object(); }
    public class AudioControl : IDisposable
    {
        //internal variables
        private Wale.WinForm.Properties.Settings settings = Wale.WinForm.Properties.Settings.Default;
        private JDPack.DebugPack DP;
        private CoreAudio.Audio audio;
        //private Wale.CoreAudio.MasterDevice masterDevice;
        
        private object terminatelock = new object(), autoconlock = new object(), AClocker = new object();
        private bool disposed = false, terminate = false;//, _AutoControl = true;
        private double baseLv, baseLvSquare, upRate = 0.02, kurtosis = 0.5;
        private VFunction.FactorsForSlicedLinear sliceFactors;
        private Task audioControlTask, controllerCleanTask;

        //global variables
        public Wale.CoreAudio.Sessions2 Sessions;
        //public bool AutoControl { get => Autocon(); set { Autocon(value); if (!Autocon()) ResetAllSessionVolume(); } }
        public bool Debug { get => DP.DebugMode; set => DP.DebugMode = value; }
        //public double MasterPeak { get { if (masterDevice != null) return masterDevice.MasterPeak; else return 0; } }
        /*public double MasterVolume
        {
            get { if (masterDevice != null) return masterDevice.MasterVolume; else return 0; }
            set { if (masterDevice != null) masterDevice.MasterVolume = (float)value;
}
        }/**/
        public double MasterPeak { get { if (audio != null) return audio.MasterPeak; else return 0; } }
        public double MasterVolume { get { if (audio != null) return audio.MasterVolume; else return 0; } }
        public double UpRate { get => upRate; set { upRate = (value * settings.AutoControlInterval / 1000); } }
        public double Kurtosis { get => kurtosis; set => kurtosis = value; }

        //class loads.
        public AudioControl(double bVol, Transformation.TransMode tmode = Transformation.TransMode.Transform1)
        {
            DP = new JDPack.DebugPack(false);
            Transformation.ChangeTransformMethod(tmode);
            SetBaseTo(bVol);
        }
        public void Start(bool debug = false)
        {
            Debug = debug;

            audio = new CoreAudio.Audio(true);
            //masterDevice = new MasterDevice();

            Sessions = new Sessions2();
            Refresh();

            controllerCleanTask = new Task(ControllerCleanTask);
            controllerCleanTask.Start();

            audioControlTask = new Task(AudioControlTask);
            audioControlTask.Start();
        }

        //Dispose
        ~AudioControl() { Dispose(false); }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual async void Dispose(bool disposing)
        {
            if (!disposed)
            {
                Terminate(true);
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

        //flag controls
        private bool Terminate() { bool val; lock (terminatelock) { val = terminate; } return val; }
        private void Terminate(bool val) { lock (terminatelock) { terminate = val; } }
        //private bool Autocon() { bool val; lock (_autoconlock) { val = _AutoControl; } return val; }
        //private void Autocon(bool val) { lock (_autoconlock) { _AutoControl = val; } }


        
        //Volume controls
        public void SetBaseTo(double bVol)
        {
            if (bVol > 1) bVol = 1;
            else if (bVol < 0.01) bVol = 0.01;
            baseLv = bVol;

            baseLvSquare = baseLv * baseLv;
            sliceFactors = VFunction.GetFactorsForSlicedLinear(UpRate, baseLv);
        }
        private void CalcBaseDerivatives() { }
        public void VolumeUp(double v) { SetVolume(MasterVolume + v); }
        public void VolumeDown(double v) { SetVolume(MasterVolume - v); }
        public void SetVolume(double v)
        {
            if (v < 0.01) v = 0.01;
            else if (v > 1) v = 1;

            DP.DML("SetTo:" + v);
            audio.MasterVolume = (float)v;
            //Wale.Subclasses.Audio.SetMasterVolume((float)v);
        }


        
        //Session Control
        private void ResetAllSessionVolume()
        {
            audio.Sessions.ForEach(s => s.Volume = 0.01f);
            //uint[] ids = Wale.Subclasses.Audio.GetApplicationIDs();
            //for (int i = 0; i < ids.Count(); i++) { SetSessionVolume(ids[i], 0.01); }
        }
        private void SetSessionVolume(int id, double v)
        {
            v += Sessions.GetRelative(id);
            if (v < 0.01) v = 0.01;
            else if (v > 1) v = 1;

            DP.DML(string.Format("SessionTo:{0:n6}", v));
            Sessions.GetSession(id).Volume = (float)v;
            //Sessions.GetSession(id).SetVolume((float)v);
        }


        //Automatic volume control.
        private void Refresh(bool first = false)
        {
            audio.UpdateSession();
            Sessions = audio.Sessions;
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
        private void SessionControl(Session2 s)
        {
            string dm = $"AutoVolume:{s.Name}({s.PID}), inc={s.AutoIncluded}";
            if (s.State == SessionState.Active && s.AutoIncluded)
            {
                double peak = s.Peak, volume = s.Volume;
                dm += $" P:{peak:n3} V:{volume:n3}";
                if (peak > settings.MinPeak)
                {
                    double tVol, UpLimit;
                    if (s.Averaging) s.SetAverage(peak);
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
                    dm += $" T={tVol:n3} UL={UpLimit:n3}";//Console.WriteLine($" T={tVol:n3} UL={UpLimit:n3}");
                    SetSessionVolume(s.PID, (tVol > UpLimit) ? UpLimit : tVol);
                }
                DP.DML(dm);
                //Console.WriteLine(dm);
            }
        }
        private async void AudioControlTask()
        {
            JDPack.FileLog.Log("Audio Control Task Start");
            List<Task> aas = new List<Task>();
            uint logCounter = 0, logCritical = 10000;
            //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            while (!Terminate())
            {
                //sw.Restart();
                //Task wait = Task.Delay(settings.AutoControlInterval);
                Refresh();
                
                if (settings.AutoControl)
                {
                    lock (Lockers.Sessions)
                    {
                        Sessions.ForEach(s => aas.Add(new Task(() => SessionControl(s))));
                    }
                    aas.ForEach(t => t.Start());
                }/**/

                await Task.Delay(settings.AutoControlInterval);
                //await wait;
                //System.Threading.Thread.Sleep(settings.AutoControlInterval);
                
                if (settings.AutoControl)
                {
                    await Task.WhenAll(aas);
                    aas.Clear();
                }/**/

                if (logCounter > logCritical)
                {
                    JDPack.FileLog.Log($"Auto control task passed for {logCritical} times.");
                    logCounter = 0;
                }
                logCounter++;
                
                //sw.Stop();
                //Console.WriteLine($"ACTaskElapsed={sw.ElapsedMilliseconds}");
            }// end while loop

            JDPack.FileLog.Log("Audio Control Task End");
        }// end AudioControlTask
        private async void ControllerCleanTask()
        {
            JDPack.FileLog.Log("Controller Clean Task(GC) Start");
            List<Task> aas = new List<Task>();
            uint logCounter = uint.MaxValue;

            while (!Terminate())
            {
                //Task wait = Task.Delay(settings.GCInterval);

                //bool auto = Autocon();
                if (settings.AutoControl)
                {
                    Sessions.ForEach(s => {
                        aas.Add(new Task(new Action(() =>
                        {
                            SessionState state = s.State;
                            if (state != SessionState.Active && s.Volume != 0.01)
                            {
                                SetSessionVolume(s.PID, 0.01);
                                s.ResetAverage();
                            }
                        })));
                    });
                    aas.ForEach(t => t.Start());
                }/**/
                
                await Task.Delay(settings.GCInterval);

                if (settings.AutoControl)
                {
                    await Task.WhenAll(aas);
                    aas.Clear();
                }/**/

                long mmc = GC.GetTotalMemory(true);
                Console.WriteLine($"Total Memory: {mmc:n0}");
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


        
    }//End Class AudioControl
    

    
}//End Namespace WindowAudioLoudnessEqualizer