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
        private void DW(object o) => System.Diagnostics.Debug.WriteLine(o);
        public Core(Linker l)
        {
            L = l;

            MMnotify();
            GetMMD();
            LoadMP();
        }
        public void Unload()
        {
            UnloadMP();
            //foreach(var s in SS) { s.Unload(); }
            //L.Ss.Clear();
        }
        Linker L;

        private CancellationTokenSource cts;
        private void LoadMP() { cts = new CancellationTokenSource(); _ = MasterPeak(cts.Token); }
        private void UnloadMP() { cts.Cancel(); }
        private AudioMeterInformation AMI;
        private async Task MasterPeak(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Task work = Task.Run(() =>
                {
                    if (AMI == null) return;
                    L.MD.Update(AMI.PeakValue);
                }, token);
                await Task.WhenAll(work, Task.Delay(100, token));
            }
        }



        private MMNotificationClient MMN = new MMNotificationClient();
        private void MMnotify()
        {
            if (MMN == null) MMN = new CSCore.CoreAudioAPI.MMNotificationClient();
            MMN.DefaultDeviceChanged += CoreTestMainWindow_DefaultDeviceChanged;
            MMN.DeviceStateChanged += Mmn_DeviceStateChanged;
        }
        private void CoreTestMainWindow_DefaultDeviceChanged(object sender, DefaultDeviceChangedEventArgs e) => GetMMD();
        private void Mmn_DeviceStateChanged(object sender, DeviceStateChangedEventArgs e) => GetMMD();


        private AudioEndpointVolume AEV;
        public void SetVol(double v) { if (AEV != null) AEV.MasterVolumeLevelScalar = (float)v; }
        private void GetMMD()
        {
            DW(1000);
            L.MMD = CSCore.CoreAudioAPI.MMDeviceEnumerator.TryGetDefaultAudioEndpoint(CSCore.CoreAudioAPI.DataFlow.Render, CSCore.CoreAudioAPI.Role.Multimedia);
            if (L.MMD == null) return;

            AEV = AudioEndpointVolume.FromDevice(L.MMD);
            var aevc = new AudioEndpointVolumeCallback();
            aevc.NotifyRecived += Mepv_NotifyRecived;
            AEV.RegisterControlChangeNotify(aevc);
            L.MD = new MasterData(AEV.IsMuted, AEV.MasterVolumeLevelScalar);

            AMI = AudioMeterInformation.FromDevice(L.MMD);

            TryGetSs();
        }
        private void Mepv_NotifyRecived(object sender, AudioEndpointVolumeCallbackEventArgs e)
        {
            L.MD.Update(e.IsMuted, e.MasterVolume);
        }




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
            DW(2000);
            if (ASM == null && L.MMD != null) ASM = AudioSessionManager2.FromMMDevice(L.MMD);
            ASM.SessionCreated += ASM_SessionCreated;

            foreach (var asc in ASM.GetSessionEnumerator()) AddNewSession(asc);
        }
        private void AddNewSession(AudioSessionControl asc)
        {
            //L.Ss.Add(new SessionData(asc.BasePtr));
            var s = new Session(asc.BasePtr, L);
            //s.Offline += S_Offline;
            //s.Online += S_Online;
            SS.Add(s);
            DW($"Ss#:{L.Ss.Count} {SS.Count}");
        }
        private void RemoveSession(AudioSessionControl asc)
        {
            //SS.Remove(asc as Session);
            //L.Ss.Remove(L.Ss.FirstOrDefault(s => s.BasePtr == asc.BasePtr));
        }


        private void ASM_SessionCreated(object sender, SessionCreatedEventArgs e) => AddNewSession(e.NewSession);
        private void S_Online(object sender, EventArgs e) => AddNewSession(sender as Session);
        private void S_Offline(object sender, EventArgs e) => RemoveSession(sender as Session);



        
    }

    public static class CoreExtention
    {
        public static double ToDecibel(this double val) => 20 * Math.Log10(val);
        public static float ToDecibel(this float val) => (float)(20 * Math.Log10(val));
    }
}
