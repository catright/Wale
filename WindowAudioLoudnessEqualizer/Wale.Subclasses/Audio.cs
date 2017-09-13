using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using CSCore.CoreAudioAPI;

namespace Wale.CoreAudio
{
    public enum SessionState { Active, Inactive, Expired }
    public class Audio
    {
        #region Public Items
        public float MasterVolume { get => masterVolume; set => masterVolume = value; }
        public float MasterPeak { get => masterPeak; }
        public Sessions2 Sessions { get => sessionList; }

        public Audio() { }
        /// <summary>
        /// Instantiate new instance of Audio class.
        /// Automatically do UpdateDevice when <paramref name="autoStart"/> is true.
        /// </summary>
        /// <param name="autoStart"></param>
        public Audio(bool autoStart)
        {
            if (autoStart)
            {
                UpdateDevice();
                UpdateSession();
            }
        }
        
        /// <summary>
        /// Get new default MMDevice, VolumeSource, and PeakMeter.
        /// </summary>
        public void UpdateDevice()
        {
            GetDefaultDevice();
            GetMasterVolume();
            GetMasterPeak();
        }

        public void UpdateSession()
        {
            GetSession();
        }
        #endregion


        #region Private Master Items
        //Master Volume Items
        private float masterVolume { get => vs.MasterVolumeLevelScalar; set => vs.MasterVolumeLevelScalar = value; }
        private AudioEndpointVolume vs;
        private void GetMasterVolume()
        {
            Guid IID_IAudioEndpointVolume = typeof(AudioEndpointVolume).GUID;
            IntPtr i = defaultDevice.Activate(IID_IAudioEndpointVolume, 0, IntPtr.Zero);
            vs = new AudioEndpointVolume(i);

        }

        //Master Peak Items
        private float masterPeak { get => pm.PeakValue; }
        private AudioMeterInformation pm;
        private void GetMasterPeak()
        {
            Guid IID_IAudioMeterInformation = typeof(AudioMeterInformation).GUID;
            IntPtr o = defaultDevice.Activate(IID_IAudioMeterInformation, CSCore.Win32.CLSCTX.CLSCTX_LOCAL_SERVER, IntPtr.Zero);
            pm = new AudioMeterInformation(o);
        }
        
        //Master Device Items
        private MMDevice defaultDevice;
        private void GetDefaultDevice()
        {
            using (CSCore.CoreAudioAPI.MMDeviceEnumerator enumerator = new CSCore.CoreAudioAPI.MMDeviceEnumerator())
            {
                defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
        }
        private IEnumerable<MMDevice> deviceList;
        private void EnumerateWasapiDevices()
        {
            using (CSCore.CoreAudioAPI.MMDeviceEnumerator enumerator = new CSCore.CoreAudioAPI.MMDeviceEnumerator())
            {
                deviceList = enumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active);
            }
        }
        #endregion


        #region Private Session Items
        //Session Control Items
        private Sessions2 sessionList;
        private void GetSession()
        {
            Guid IID = typeof(AudioSessionManager2).GUID;
            IntPtr o = defaultDevice.Activate(IID, 0, IntPtr.Zero);

            sessionList = new Sessions2();
            using (AudioSessionManager2 obj = new AudioSessionManager2(o))
            {
                if (obj == null) return;
                using (AudioSessionEnumerator ase = obj.GetSessionEnumerator())
                {
                    /*for (int i = 0; i < ase.Count; i++)
                    {
                        AudioSessionControl asc = ase.GetSession(i);
                        sessionList.Add(new Session2(new AudioSessionControl2(asc.BasePtr)));
                    }/**/
                    foreach (AudioSessionControl asc in ase)
                    {
                        sessionList.Add(new Session2(new AudioSessionControl2(asc.BasePtr)));
                    }
                }
            }
            //sessionList.ForEach(s => Console.WriteLine($"{s.PID}"));
            
        }
        #endregion


    }//End class Audio
}