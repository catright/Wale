using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio;
using NAudio.CoreAudioApi;

namespace NTest
{
    class Linker : JPack.NotifyPropertyChanged
    {
        public Linker()
        {
            Name = "TEST";
            //Ss = new ObservableCollection<SessionData>(); 
            SNames = new ObservableCollection<string>();
        }

        public string Name { get => Get<string>(); set => Set(value); }
        public MMDevice MMD { get => Get<MMDevice>(); set => Set(value); }
        public MasterData MD { get => Get<MasterData>(); set => Set(value); }

        //public ObservableCollection<SessionData> Ss { get => Get<ObservableCollection<SessionData>>(); set => Set(value); }

        public ObservableCollection<string> SNames { get => Get<ObservableCollection<string>>(); set => Set(value); }
    }
}
