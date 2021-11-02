using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.MediaFoundation;

namespace CoreTest
{
    public class Core
    {
        Linker L;
        public Core(Linker l, bool autoload = true)
        {
            L = l;
            if (autoload) Load();
        }
        public void Load()
        {
            MMnotify();
            GetMMD();
            LoadMSP();
            TryGetSs();
        }
        public void Unload()
        {
            UnloadMMnotify();
            //foreach(var s in SS) { s.Unload(); }
            //L.Ss.Clear();
            UnloadSs();
            UnloadMSP();
            UnloadMMD();
        }

        private CancellationTokenSource cts;
        /// <summary>
        /// Start MasterPeak polling Task
        /// </summary>
        private void LoadMSP() { cts = new CancellationTokenSource(); _ = MasterPeak(cts.Token); }
        /// <summary>
        /// Stop MasterPeak polling Task
        /// </summary>
        private void UnloadMSP() { cts.Cancel(); }
        private async Task MasterPeak(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Task work = Task.Run(() =>
                {
                    if (AMI == null) return;
                    L.MSD.Update(AMI.PeakValue);
                }, token);
                await Task.WhenAll(work, Task.Delay(100, token));
            }
        }



        private MMNotificationClient MMN = new MMNotificationClient();
        /// <summary>
        /// Set MMnotification events
        /// </summary>
        private void MMnotify()
        {
            M.D(100, "MMnotify");
            if (MMN == null) MMN = new CSCore.CoreAudioAPI.MMNotificationClient();
            MMN.DefaultDeviceChanged += MMN_DefaultDeviceChanged;
            MMN.DeviceStateChanged += MMN_DeviceStateChanged;
            MMN.DeviceRemoved += MMN_DeviceRemoved;
            M.D(109);
        }
        private void UnloadMMnotify()
        {
            M.D(190, "UnloadMMnotify");
            if (MMN != null)
            {
                MMN.DefaultDeviceChanged -= MMN_DefaultDeviceChanged;
                MMN.DeviceStateChanged -= MMN_DeviceStateChanged;
                MMN.DeviceRemoved -= MMN_DeviceRemoved;
            }
            MMN.Dispose();
            M.D(199);
        }

        /// <summary>
        /// Get new default device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MMN_DeviceRemoved(object sender, DeviceNotificationEventArgs e) => GetMMD();
        /// <summary>
        /// Get new default device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MMN_DefaultDeviceChanged(object sender, DefaultDeviceChangedEventArgs e) => GetMMD();
        /// <summary>
        /// Get new default device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MMN_DeviceStateChanged(object sender, DeviceStateChangedEventArgs e) => GetMMD();


        private AudioEndpointVolume AEV;
        private AudioMeterInformation AMI;
        private AudioEndpointVolumeCallback AEVC;
        private void GetMMD()
        {
            M.D(1000, "GetMMD");
            L.MMD = CSCore.CoreAudioAPI.MMDeviceEnumerator.TryGetDefaultAudioEndpoint(CSCore.CoreAudioAPI.DataFlow.Render, CSCore.CoreAudioAPI.Role.Multimedia);
            if (L.MMD == null) { M.D(1001, M.Kind.ER, "MMD=null"); return; }

            AEV = AudioEndpointVolume.FromDevice(L.MMD);
            AEVC = new AudioEndpointVolumeCallback();
            AEVC.NotifyRecived += AEV_NotifyRecived;
            AEV.RegisterControlChangeNotify(AEVC);
            L.MSD = new MasterData(AEV.IsMuted, AEV.MasterVolumeLevelScalar);

            AMI = AudioMeterInformation.FromDevice(L.MMD);
            //M.D(1008);
            //TryGetSs();
            M.D(1009);
        }
        private void UnloadMMD()
        {
            M.D(1900, "UnloadMMD");
            AMI.Dispose();
            AEVC.NotifyRecived -= AEV_NotifyRecived;
            AEV.UnregisterControlChangeNotify(AEVC);
            AEV.Dispose();
            L.MMD.Dispose();
            L.MMD = null;
            L.MSD = null;
            M.D(1909);
        }
        private void AEV_NotifyRecived(object sender, AudioEndpointVolumeCallbackEventArgs e)
        {
            L.MSD.Update(e.IsMuted, e.MasterVolume);
        }

        public void SetVol(double v) => SetVol((float)v);
        public void SetVol(float v) { if (AEV != null) AEV.MasterVolumeLevelScalar = v; }



        private AudioSessionManager2 ASM;
        private void TryGetSs()
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA) GetSs();
            else
            {
                using (ManualResetEvent waitHandle = new ManualResetEvent(false))
                {
                    ThreadPool.QueueUserWorkItem(
                        (o) =>
                        {
                            try
                            {
                                GetSs();
                            }
                            finally
                            {
                                waitHandle.Set();
                            }
                        });
                    waitHandle.WaitOne();
                }
            }
        }
        private List<Session> SS = new List<Session>();
        private void GetSs()
        {
            M.D(20000, "GetSs");
            if (ASM == null && L.MMD != null) ASM = AudioSessionManager2.FromMMDevice(L.MMD);
            ASM.SessionCreated += ASM_SessionCreated;

            foreach (var asc in ASM.GetSessionEnumerator()) AddNewSession(asc);
        }
        private void UnloadSs()
        {
            M.D(29000, "UnloadSs");
            if (ASM != null)
            {
                ASM.SessionCreated -= ASM_SessionCreated;
                ASM.Dispose();
                ASM = null;
            }
            M.D(29001);
            SS.ForEach(s => s?.Dispose());
            SS.Clear();
            M.D(29002);
            L.SSD.Clear();
            M.D(29003);
            L.SSN.Clear();
            M.D(29009);
        }
        private void AddNewSession(AudioSessionControl asc)
        {
            M.D(21000, "AddNewSession");
            var s = new Session(asc.BasePtr, ref L);
            s.Offline += S_Offline;
            s.Online += S_Online;
            SS.Add(s);
            L.SSD.Add(new SessionData(asc.BasePtr, asc.DisplayName));
            L.SSN.Add(s.DisplayName);
            SessionAdded?.Invoke(this, new SessionEventArgs(asc.BasePtr));
            M.D(21001, $"# SS:{SS.Count}, SSD:{L.SSD.Count}, SSN:{L.SSN.Count}");
        }
        private void RemoveSession(AudioSessionControl asc)
        {
            M.D(22000, "RemoveSession");
            //SS.Remove(asc as Session);
            L.SSD.Remove(L.SSD.FirstOrDefault(s => s.BasePtr == asc.BasePtr));
            L.SSN.Remove(asc.DisplayName);
            SessionRemoved?.Invoke(this, new SessionEventArgs(asc.BasePtr));
            M.D(22001, $"# SS:{SS.Count}, SSD:{L.SSD.Count}, SSN:{L.SSN.Count}");
        }
        public delegate void SessionEventHandler(object sender, SessionEventArgs e);
        public event SessionEventHandler SessionAdded;
        public event SessionEventHandler SessionRemoved;


        private void ASM_SessionCreated(object sender, SessionCreatedEventArgs e) => AddNewSession(e.NewSession);
        private void S_Online(object sender, EventArgs e) => AddNewSession(sender as Session);
        private void S_Offline(object sender, EventArgs e) => RemoveSession(sender as Session);



    }

    public class SessionEventArgs : EventArgs
    {
        public SessionData SSD;
        public IntPtr BasePtr;
        public SessionEventArgs(SessionData d) { SSD = d; }
        public SessionEventArgs(IntPtr p) { BasePtr = p; }
    }
    public static class CoreExtention
    {
        public static double ToDecibel(this double val) => 20 * Math.Log10(val);
        public static float ToDecibel(this float val) => (float)(20 * Math.Log10(val));
    }
}
