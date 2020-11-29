using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace NTest
{
    class Core
    {
        private void DW(object o) => System.Diagnostics.Debug.WriteLine(o);
        public Core(Linker l)
        {
            L = l;

            GetMMDE();
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
                    L.MD.Update(AMI.MasterPeakValue);
                }, token);
                await Task.WhenAll(work, Task.Delay(100, token));
            }
        }


        private MMDeviceEnumerator MMDE;
        private void GetMMDE()
        {
            if (MMDE == null) MMDE = new MMDeviceEnumerator();
            var adc = new AudioDevicedChanged();
            adc.DefaultDeviceChanged += Adc_DefaultDeviceChanged;
            adc.DeviceStateChanged += Adc_DeviceStateChanged;
            MMDE.RegisterEndpointNotificationCallback(adc);
        }

        private void Adc_DefaultDeviceChanged(object sender, DefaultDeviceChangedEventArgs e) => GetMMD();
        private void Adc_DeviceStateChanged(object sender, DeviceStateChangedEventArgs e) => GetMMD();


        private AudioEndpointVolume AEV;
        public void SetVol(double v) { if (AEV != null) AEV.MasterVolumeLevelScalar = (float)v; }
        private void GetMMD()
        {
            DW(1000);
            L.MMD = MMDE.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            if (L.MMD == null) return;

            AEV = L.MMD.AudioEndpointVolume;
            AEV.OnVolumeNotification += AEV_OnVolumeNotification;

            L.MD = new MasterData(AEV.Mute, AEV.MasterVolumeLevelScalar);

            AMI = L.MMD.AudioMeterInformation;

            TryGetSs();
        }
        private void AEV_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            L.MD.Update(data.Muted, data.MasterVolume);
        }




        private AudioSessionManager ASM;
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
        //private List<Session> SS = new List<Session>();
        private void GetSs()
        {
            DW(2000);
            if (ASM == null && L.MMD != null) ASM = L.MMD.AudioSessionManager;
            ASM.OnSessionCreated += ASM_OnSessionCreated; ;
            
            for (int i = 0; i < ASM.Sessions.Count; i++) AddNewSession((IAudioSessionControl)ASM.Sessions[i]);
        }


        private void AddNewSession(IAudioSessionControl iasc)
        {
            var asc = iasc as AudioSessionControl;
            L.SNames.Add(asc.DisplayName);
            //L.Ss.Add(new SessionData(asc.BasePtr));
            //var s = new Session(asc.BasePtr, L);
            //s.Offline += S_Offline;
            //s.Online += S_Online;
            //SS.Add(s);
            //DW($"Ss#:{L.Ss.Count} {SS.Count}");
        }
        private void RemoveSession(AudioSessionControl asc)
        {
            //SS.Remove(asc as Session);
            //L.Ss.Remove(L.Ss.FirstOrDefault(s => s.BasePtr == asc.BasePtr));
        }


        private void ASM_OnSessionCreated(object sender, IAudioSessionControl newSession) => AddNewSession(newSession);
        //private void S_Online(object sender, EventArgs e) => AddNewSession(sender as Session);
        //private void S_Offline(object sender, EventArgs e) => RemoveSession(sender as Session);



    }


    public class AudioDevicedChanged : IMMNotificationClient
    {
        public delegate void DefaultDeviceChangedDelegate(object sender, DefaultDeviceChangedEventArgs e);
        public event DefaultDeviceChangedDelegate DefaultDeviceChanged;
        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) => DefaultDeviceChanged?.Invoke(this, new DefaultDeviceChangedEventArgs(flow, role, defaultDeviceId));
        
        public void OnDeviceAdded(string pwstrDeviceId)
        {
            
        }

        public void OnDeviceRemoved(string deviceId)
        {
            
        }

        public delegate void DeviceStateChangedDelegate(object sender, DeviceStateChangedEventArgs e);
        public event DeviceStateChangedDelegate DeviceStateChanged;
        public void OnDeviceStateChanged(string deviceId, DeviceState newState) => DeviceStateChanged?.Invoke(this, new DeviceStateChangedEventArgs(deviceId, newState));
        
        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
            
        }
    }
    public class DefaultDeviceChangedEventArgs : EventArgs
    {
        public string DeviceId { get; set; }
        public DataFlow DataFlow { get; set; }
        public Role Role { get; set; }
        public DefaultDeviceChangedEventArgs(DataFlow dataFlow, Role role, string deviceId) { DataFlow = dataFlow; Role = role; DeviceId = deviceId; }
    }
    public class DeviceStateChangedEventArgs : EventArgs
    {
        public string DeviceId { get; set; }
        public DeviceState NewState { get; set; }
        public DeviceStateChangedEventArgs(string deviceId, DeviceState newState) { DeviceId = deviceId; NewState = newState; }
    }
}
