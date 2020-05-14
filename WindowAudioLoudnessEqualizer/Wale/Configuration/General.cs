using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Wale.Configuration
{
    /// <summary>
    /// Data container for Wale
    /// </summary>
    public static class Visual
    {
        /// <summary>
        /// Default width of main visual window
        /// </summary>
        public static double MainWindowWidth => 480;
        /// <summary>
        /// Default height of main visual window
        /// </summary>
        public static double MainWindowHeightDefault => 285;

        /// <summary>
        /// Base height of main window includes title bar and tab selector
        /// </summary>
        public static double MainWindowBaseHeight => 60;
        /// <summary>
        /// Title bar height of Wale windows
        /// </summary>
        public static double TitleBarHeight => 35;

        /// <summary>
        /// Height of config set
        /// </summary>
        public static double ConfigSetHeight => 265;
        /// <summary>
        /// Long height of config set when advanced view is selected
        /// </summary>
        public static double ConfigSetLongHeight => 415;

        /// <summary>
        /// Default height of windows except main window
        /// </summary>
        public static double SubWindowHeightDefault => 360;

        /// <summary>
        /// Visual block of each session when normal view
        /// </summary>
        public static double SessionBlockHeightNormal => 27;
        /// <summary>
        /// Visual block of each session when detailed view
        /// </summary>
        public static double SessionBlockHeightDetail => 42;
    }

    public class General : JPack.NotifyPropertyChanged
    {
        #region Initialize
        public General() { Init(); }
        //public General(string installPath) { InstalledPath = installPath; Init(); }
        #endregion
        private void DefaultValues()
        {
            AutoControl = true;
            AlwaysTop = true;
            StayOn = false;
            RunAtWindowsStartup = false;
            Averaging = true;
            AdvancedView = false;

            TargetLevel = .15;
            AverageTime = 3000;
            Kurtosis = .35;
            MasterVolumeInterval = .05;
            MinPeak = .002;
            UpRate = 1;
            GCInterval = 30000;
            UIUpdateInterval = 100;
            AutoControlInterval = 15;

            VFunc = "None";
            AppTitle = "WALE";
            ProcessPriority = "High";
            ExcList = new StringCollection() { };
            CombineSession = false;
            AudioUnit = 0;
            Version = "";
            PreviousVersion = "";

            ShowSessionIcon = true;
            CollapseSubSessions = true;
            StaticMode = false;
            PnameForAppname = false;
            MainTitleforAppname = false;
            AutoExcludeOnManualSet = false;
        }

        #region Properties
        public bool AutoControl { get => Get<bool>(); set => Set(value); }
        public bool AlwaysTop { get => Get<bool>(); set => Set(value); }
        public bool StayOn { get => Get<bool>(); set => Set(value); }
        public bool RunAtWindowsStartup { get => Get<bool>(); set => Set(value); }
        public bool Averaging { get => Get<bool>(); set => Set(value); }
        public bool AdvancedView { get => Get<bool>(); set => Set(value); }

        public double TargetLevel { get => Get<double>(); set => Set(value); }
        public double AverageTime { get => Get<double>(); set => Set(value); }
        public double Kurtosis { get => Get<double>(); set => Set(value); }
        public double MasterVolumeInterval { get => Get<double>(); set => Set(value); }
        public double MinPeak { get => Get<double>(); set => Set(value); }
        public double UpRate { get => Get<double>(); set => Set(value); }
        public double GCInterval { get => Get<double>(); set => Set(value); }
        public double UIUpdateInterval { get => Get<double>(); set => Set(value); }
        public double AutoControlInterval { get => Get<double>(); set => Set(value); }

        public string VFunc { get => Get<string>(); set => Set(value); }
        public string AppTitle { get => Get<string>(); set => Set(value); }
        public string ProcessPriority { get => Get<string>(); set => Set(value); }
        public StringCollection ExcList { get => Get<StringCollection>(); set => Set(value); }
        public bool CombineSession { get => Get<bool>(); set => Set(value); }
        public int AudioUnit { get => Get<int>(); set => Set(value); }
        public string Version { get => Get<string>(); set => Set(value); }
        public string PreviousVersion { get => Get<string>(); set => Set(value); }
        
        public bool ShowSessionIcon { get => Get<bool>(); set => Set(value); }
        public bool CollapseSubSessions { get => Get<bool>(); set => Set(value); }
        public bool StaticMode { get => Get<bool>(); set => Set(value); }
        public bool PnameForAppname { get => Get<bool>(); set => Set(value); }
        public bool MainTitleforAppname { get => Get<bool>(); set => Set(value); }
        public bool AutoExcludeOnManualSet { get => Get<bool>(); set => Set(value); }
        #endregion


        #region Properties storing system Variables
        public string InstalledPath;
        public string AppDataPath;
        public string WorkingPath;
        private string StoringPath;
        #endregion
        #region Methods
        private void Init() {
            PathInit();
            if (!IsSavedExists) { Reset(); }
            else { Read(); }
        }
        private void PathInit()
        {
            InstalledPath = System.AppDomain.CurrentDomain.BaseDirectory;
            AppDataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WaleAudioControl");
            Regex pf = new Regex(@"Program Files");
            WorkingPath = pf.IsMatch(InstalledPath) ? AppDataPath : InstalledPath;
            StoringPath = System.IO.Path.Combine(WorkingPath, "WaleConfigGeneral.txt");
        }
        private bool IsSavedExists => File.Exists(StoringPath);

        private void Read()
        {

        }
        /// <summary>
        /// Stores current values of the application settings to a file.
        /// </summary>
        public void Save()
        {
            throw new NotImplementedException("Save");
        }
        /// <summary>
        /// Upgrades application settings to reflect a more recent installation of the application.
        /// </summary>
        public void Upgrade()
        {
            throw new NotImplementedException("Upgrade");
        }
        /// <summary>
        /// Returns the value of the PreviousVersion property.
        /// This method is newly introduced for Wale and not same as original method of Properties.settings on WPF system.
        /// </summary>
        /// <returns></returns>
        public object GetPreviousVersion()
        {
            return PreviousVersion;
        }
        /// <summary>
        /// Returns the value of the named property for the previous version of the same application.
        /// This method is only for code continuity and always returns the result of GetPreviousVersion method w/o any params.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the property whose the value is to be returned.</param>
        /// <returns></returns>
        public object GetPreviousVersion(string propertyName) { return GetPreviousVersion(); }
        /// <summary>
        /// Restores the persisted application settings values to their corresponding default properties.
        /// </summary>
        public void Reset() { DefaultValues(); }
        #endregion

    }
}
