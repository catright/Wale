using CSCore.CoreAudioAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using Wale.Extensions;

namespace Wale.CoreAudio
{
    public class Map
    {
        private MMDeviceEnumerator _MMDE;
        protected MMDeviceEnumerator MMDE
        {
            get
            {
                if (_MMDE == null)
                {
                    try
                    {
                        _MMDE = new MMDeviceEnumerator();
                    }
                    catch (Exception e) { M.F(e.Message); }
                }
                return _MMDE;
            }
        }

        /// <summary>
        /// nullable
        /// </summary>
        public MapDataDevice[] Devices => EnumerateWasapiDevices().ToArray();
        private MapDataDevice[] EnumerateWasapiDevices()
        {
            try
            {
                var devices = MMDE.EnumAudioEndpoints(DataFlow.Render, CSCore.CoreAudioAPI.DeviceState.Active | CSCore.CoreAudioAPI.DeviceState.Disabled | CSCore.CoreAudioAPI.DeviceState.UnPlugged);
                if (devices == null) { M.F("EnumerateWasapiDevices: Fail to get device collection."); return null; }
                if (devices.Count == 0) { M.F("EnumerateWasapiDevices: Device collection length is 0"); return null; }

                MapDataDevice[] dataArray = new MapDataDevice[devices.Count];
                for (int i = 0; i < devices.Count; i++)
                {
                    try
                    {
                        //Console.WriteLine($"{d.FriendlyName}, {d.DeviceState}, {d.DeviceID}");
                        List<MapDataSession> slist = new List<MapDataSession>();
                        try
                        {
                            using (var asm = (devices[i] != null ? AudioSessionManager2.FromMMDevice(devices[i]) : null))
                            using (var ase = (asm?.GetSessionEnumerator()))
                            {
                                if (ase != null)
                                {
                                    foreach (AudioSessionControl asc in ase)
                                    {
                                        using (var asc2 = asc.QueryInterface<AudioSessionControl2>())
                                        {
                                            if (asc.SessionState != AudioSessionState.AudioSessionStateExpired)
                                            {
                                                try
                                                {
                                                    var d = new MapDataSession(asc2);
                                                    if (d != null) slist.Add(d);
                                                }
                                                catch { }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch { }

                        dataArray[i] = new MapDataDevice()
                        {
                            DeviceId = devices[i].DeviceID,
                            Name = devices[i].FriendlyName,
                            State = devices[i].DeviceState.GetDeviceState(),
                            Sessions = slist.ToArray()
                        };
                    }
                    catch (NullReferenceException) { }
                }
                return dataArray;
            }
            catch (Exception e) { M.F(e.Message); return null; }
        }

        private MMDevice GetDevice(string deviceId)
        {
            try
            {
                MMDevice device = MMDE.GetDevice(deviceId);
                if (device == null) M.F($"GetDevice: Fail to get mmdevice({deviceId}).");
                return device;
            }
            catch (Exception e) { M.F(e.Message); return null; }
        }
        /// <summary>
        /// Enable or Disable specific MMDevice.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="enable"></param>
        public void SetDevice(string deviceId, bool enable)
        {
            try
            {
                using (var device = GetDevice(deviceId))
                {
                    if (device == null) { M.F("SetDevice: mmdevice is null."); return; }
                    using (var ac = new AudioClient(device.Activate(typeof(AudioClient).GUID, 0, IntPtr.Zero)))
                    {
                        if (enable) ac.Start();
                        else ac.Stop();
                    }
                }
            }
            catch (Exception e) { M.F($"Error(SetDevice): {e.Message}"); return; }
        }
    }
}
