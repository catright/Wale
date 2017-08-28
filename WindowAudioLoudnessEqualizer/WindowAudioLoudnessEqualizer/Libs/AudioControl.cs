using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Runtime.InteropServices;
using Wale.Subclasses;

namespace Wale
{
    //public static class Locker { public static object locker = new object(); }
    public class AudioControl : IDisposable
    {
        //internal variables
        private Wale.Properties.Settings settings = Wale.Properties.Settings.Default;
        private JLdebPack.DebugPackage DP;
        private Wale.Subclasses.MasterDevice masterDevice;
        
        private object terminatelock = new object(), autoconlock = new object(), AClocker = new object();
        private bool disposed = false, terminate = false;//, _AutoControl = true;
        private double baseVol, baseVolSquare, upRate = 0.02, skewness = 0.5;
        private VFunction.FactorsForSlicedLinear sliceFactors;
        private Task audioControlTask, controllerCleanTask;

        //global variables
        public Wale.Subclasses.SessionDataPack Sessions;
        //public bool AutoControl { get => Autocon(); set { Autocon(value); if (!Autocon()) ResetAllSessionVolume(); } }
        public bool Debug { get => DP.DebugMode; set => DP.DebugMode = value; }
        public double MasterPeak { get { if (masterDevice != null) return masterDevice.MasterPeak; else return 0; } }
        public double MasterVolume
        {
            get { if (masterDevice != null) return masterDevice.MasterVolume; else return 0; }
            set { if (masterDevice != null) masterDevice.MasterVolume = (float)value; }
        }
        public double UpRate { get => upRate; set { upRate = (value * settings.AutoControlInterval / 1000); } }
        public double Skewness { get => skewness; set => skewness = value; }

        //class loads.
        public AudioControl(double bVol, Transformation.TransMode tmode = Transformation.TransMode.Transform1)
        {
            DP = new JLdebPack.DebugPackage(false);
            Transformation.ChangeTransformMethod(tmode);
            SetBaseTo(bVol);
        }
        public void Start(bool debug = false)
        {
            Debug = debug;

            masterDevice = new MasterDevice();

            Sessions = new SessionDataPack();
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
                await audioControlTask;
                await controllerCleanTask;
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
            baseVol = bVol;

            baseVolSquare = baseVol * baseVol;
            sliceFactors = VFunction.GetFactorsForSlicedLinear(UpRate, baseVol);
        }
        private void CalcBaseDerivatives() { }
        public void VolumeUp(double v) { SetVolume(MasterVolume + v); }
        public void VolumeDown(double v) { SetVolume(MasterVolume - v); }
        public void SetVolume(double v)
        {
            if (v < 0.01) v = 0.01;
            else if (v > 1) v = 1;

            DP.DML("SetTo:" + v);
            Wale.Subclasses.Audio.SetMasterVolume((float)v);
        }


        
        //Session Control
        private void ResetAllSessionVolume()
        {
            uint[] ids = Wale.Subclasses.Audio.GetApplicationIDs();
            for (int i = 0; i < ids.Count(); i++) { SetSessionVolume(ids[i], 0.01); }
        }
        private void SetSessionVolume(uint id, double v)
        {
            v += Sessions.GetRelative(id);
            if (v < 0.01) v = 0.01;
            else if (v > 1) v = 1;

            DP.DML(string.Format("SessionTo:{0:n6}", v));
            Sessions.GetData(id).SetVol(v);
            //Wale.Subclasses.AudioManager.SetApplicationVolume(id, (float)v);
        }


        //Automatic volume control.
        private void Refresh(bool first = false)
        {
            lock (AClocker)
            {
                lock (Lockers.Sessions)
                {
                    uint[] ids = Wale.Subclasses.Audio.GetApplicationIDs();
                    for (int i = 0; i < ids.Count(); i++)
                    {
                        SessionState state = Wale.Subclasses.Audio.GetApplicationState(ids[i]);
                        if (state != SessionState.Expired)
                        {
                            if (!Sessions.CheckData(ids[i])) Sessions.Add(new SessionData(i, ids[i], Wale.Subclasses.Audio.GetApplicationName(ids[i])));
                            SessionData sd = Sessions.GetData(ids[i]);
                            if (state == SessionState.Active) sd.Active = true;
                            else if (state == SessionState.Inactive) sd.Active = false;
                            sd.SetIndex(i);
                            sd.SetAvTime(settings.AverageTime, settings.AutoControlInterval);
                            sd.SetPeak(Wale.Subclasses.Audio.GetApplicationPeak(ids[i]));
                            sd.SetVol(Wale.Subclasses.Audio.GetApplicationVolume(ids[i]));
                            sd.Averaging = settings.Averaging;
                        }
                    }

                    SessionDataPack expired = new SessionDataPack();
                    Sessions.ForEach(s =>
                    {
                        bool found = false;
                        for (int i = 0; i < ids.Count(); i++)
                        {
                            if (s.ID == ids[i] && Wale.Subclasses.Audio.GetApplicationState(ids[i]) != SessionState.Expired)
                            {
                                found = true;
                            }
                        }
                        if (!found) { expired.Add(s); }
                    });
                    expired.ForEach(s => Sessions.Remove(s));
                    expired.Clear();
                }
            }
            //DP.DML($"{Sessions.Count} ({System.DateTime.Now.Ticks})");
        }
        private async void AudioControlTask()
        {
            JLdebPack.DebugPack.Log("Audio Control Task Start");
            List<Task> aas = new List<Task>();
            while (!Terminate())
            {
                Refresh();
                lock (Lockers.Sessions)
                {
                    //bool auto = Autocon();
                    if (settings.AutoControl)
                    {
                        Sessions.ForEach(s =>
                        {
                            aas.Add(new Task(new Action(() =>
                            {
                                DP.DM($"AutoVolume:{s.Name}({s.ID}), inc={s.AutoIncluded}");
                                if (s.Active && s.AutoIncluded)
                                {
                                    DP.DM($" P:{s.Peak:n3}");
                                    if (s.Peak > settings.MinPeak)
                                    {
                                        DP.DM($" V:{s.Volume:n3}");
                                        double tVol, UpLimit;
                                        if (s.Averaging) s.SetAverage(s.Peak);
                                        if (s.Averaging && s.Peak < s.AveragePeak) tVol = baseVolSquare / s.AveragePeak;
                                        else tVol = baseVolSquare / s.Peak;
                                        switch (settings.VFunc)
                                        {
                                            case VFunction.Func.Linear:
                                                UpLimit = VFunction.Linear(s.Volume, UpRate) + s.Volume;
                                                break;
                                            case VFunction.Func.SlicedLinear:
                                                UpLimit = VFunction.SlicedLinear(s.Volume, UpRate, baseVol, sliceFactors.A, sliceFactors.B) + s.Volume;
                                                break;
                                            case VFunction.Func.Reciprocal:
                                                UpLimit = VFunction.Reciprocal(s.Volume, UpRate, skewness) + s.Volume;
                                                break;
                                            case VFunction.Func.FixedReciprocal:
                                                UpLimit = VFunction.FixedReciprocal(s.Volume, UpRate, skewness) + s.Volume;
                                                break;
                                            default:
                                                UpLimit = 0;
                                                break;
                                        }
                                        DP.DM($" T={tVol:n3} UL={UpLimit:n3}");
                                        SetSessionVolume(s.ID, (tVol > UpLimit) ? UpLimit : tVol);
                                    }
                                    DP.DML("");
                                }
                            })));
                        });
                        aas.ForEach(t => t.Start());
                    }
                }
                //System.Threading.Thread.Sleep(AutoDelay);
                await Task.Delay(settings.AutoControlInterval);

                if (settings.AutoControl) await Task.WhenAll(aas);
                aas.Clear();

            }
            JLdebPack.DebugPack.Log("Audio Control Task End");
        }
        private async void ControllerCleanTask()
        {
            JLdebPack.DebugPack.Log("Controller Clean Task(GC) Start");
            uint logCounter = uint.MaxValue;
            while (!Terminate())
            {
                List<Task> aas = new List<Task>();

                //bool auto = Autocon();
                if (settings.AutoControl)
                {
                    Sessions.ForEach(s => {
                        aas.Add(new Task(new Action(() =>
                        {
                            if (!s.Expired && s.Volume != 0.01)
                            {
                                SetSessionVolume(s.ID, 0.01);
                                s.ResetAverage();
                            }
                        })));
                    });
                    aas.ForEach(t => t.Start());
                }/**/

                //System.Threading.Thread.Sleep(AutoDelay);
                await Task.Delay(settings.GCInterval);

                if (settings.AutoControl) await Task.WhenAll(aas);
                aas.Clear();

                //System.Threading.Thread.Sleep(settings.GCInterval);

                long mmc = GC.GetTotalMemory(true);
                Console.WriteLine($"Total Memory: {mmc:n0}");
                if (logCounter > 1800000/settings.GCInterval)
                {
                    JLdebPack.DebugPack.Log($"Total Memory: {mmc:n0}");
                    logCounter = 0;
                }
                logCounter++;
            }
            JLdebPack.DebugPack.Log("Controller Clean Task(GC) End");
        }


        
    }//End Class AudioControl
    

    
}//End Namespace WindowAudioLoudnessEqualizer