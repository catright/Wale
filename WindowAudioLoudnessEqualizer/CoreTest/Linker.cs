using CSCore.CoreAudioAPI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreTest
{
    public class Linker : JPack.NotifyPropertyChanged
    {
        public Linker() { Name = "TEST"; SSD = new ObservableCollection<SessionData>(); SSN = new ObservableCollection<string>(); }

        public string Name { get => Get<string>(); set => Set(value); }
        public MMDevice MMD { get => Get<MMDevice>(); set => Set(value); }

        /// <summary>
        /// Master Data
        /// </summary>
        public MasterData MSD { get => Get<MasterData>(); set => Set(value); }

        /// <summary>
        /// Sesseion Data
        /// </summary>
        public ObservableCollection<SessionData> SSD { get => Get<ObservableCollection<SessionData>>(); set => Set(value); }

        /// <summary>
        /// Session Names
        /// </summary>
        public ObservableCollection<string> SSN { get => Get<ObservableCollection<string>>(); set => Set(value); }

    }
}
