using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CSCore.CoreAudioAPI;

namespace CoreTest
{
    public class Session : AudioSessionControl//, INotifyPropertyChanged
    {
        public Session(IntPtr ptr, ref Linker l) : base(ptr)
        {
            L = l;
            SessionDisconnected += Session_SessionDisconnected;
            StateChanged += Session_StateChanged;
            SimpleVolumeChanged += Session_SimpleVolumeChanged;

            var sav = QueryInterface<SimpleAudioVolume>();
            //SSD.Update(sav.IsMuted, sav.MasterVolume, QueryInterface<AudioMeterInformation>().PeakValue); ;
        }
        public void Unload()
        {
            SessionDisconnected -= Session_SessionDisconnected;
            StateChanged -= Session_StateChanged;
            SimpleVolumeChanged -= Session_SimpleVolumeChanged;
        }


        private Linker L;
        private SessionData SSD => L.SSD?.FirstOrDefault(s => s.BasePtr == BasePtr);

        //public new string DisplayName => base.DisplayName;
        //private bool _Muted;
        //public bool Muted { get => _Muted; set { SimpleAudioVolume.FromAudioClient(new AudioClient(BasePtr)).IsMuted = value; } }
        //private double _Volume;
        //public double Volume { get => _Volume; set { SimpleAudioVolume.FromAudioClient(new AudioClient(BasePtr)).MasterVolume = (float)value; } }
        //private double _Peak;
        //public double Peak { get => _Peak; }



        private void Session_SimpleVolumeChanged(object sender, AudioSessionSimpleVolumeChangedEventArgs e)
        {
            M.D(201000);
            //_Muted = e.IsMuted;
            //_Volume = e.NewVolume;
            //SSD.Update(e.IsMuted, e.NewVolume);
            //Set("Muted");
            //Set("Vol");
        }

        private void Session_SessionDisconnected(object sender, AudioSessionDisconnectedEventArgs e) => Offline?.Invoke(this, new EventArgs());
        private void Session_StateChanged(object sender, AudioSessionStateChangedEventArgs e)
        {
            if (e.NewState == AudioSessionState.AudioSessionStateActive) Online?.Invoke(this, new EventArgs());
            else Offline?.Invoke(this, new EventArgs());
        }

        public event EventHandler Online;
        public event EventHandler Offline;
        //public event PropertyChangedEventHandler PropertyChanged;
        //private void Set(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
