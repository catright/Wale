using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

        // add 22px to conf heights when another lineitem is added
        /// <summary>
        /// Height of config set
        /// </summary>
        public static double ConfigSetHeight => 311;
        /// <summary>
        /// Long height of config set when advanced view is selected
        /// </summary>
        public static double ConfigSetLongHeight => 439;

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
        //public General() { Init(); }
        public General() { DefaultValues(); }
        //public General(string installPath) { InstalledPath = installPath; Init(); }
        #endregion
        internal void DefaultValues()
        {
            AutoControl = true;
            AlwaysTop = true;
            StayOn = false;
            RunAtWindowsStartup = false;
            Averaging = true;
            AdvancedView = false;

            TargetLevel = .15;
            LimitLevel = .25;
            CompRate = 0.96;
            AverageTime = 3000;
            Kurtosis = .35;
            MasterVolumeInterval = .05;
            MinPeak = .002;
            UpRate = 1;
            GCInterval = 30000;
            UIUpdateInterval = 100;
            AutoControlInterval = 15;
            ForceMMT = false;

            VFunc = "None";
            AppTitle = "WALE";
            ProcessPriority = "High";
            ExcList = new List<string>() { "audacity", "obs64", "Studio One", "shotcut", "Resolve", "Cakewalk", "amddvr", "ShellExperienceHost", "Windows Shell Experience Host" }.Distinct().ToList();
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

            AppUpdateMsg = System.Windows.Visibility.Hidden;
            ACDevShow = System.Windows.Visibility.Hidden;
            Transition = "0:0:.2";
        }

        #region Properties
        public bool AutoControl { get => Get<bool>(); set => Set(value); }
        public bool AlwaysTop { get => Get<bool>(); set => Set(value); }
        public bool StayOn { get => Get<bool>(); set => Set(value); }
        public bool RunAtWindowsStartup { get => Get<bool>(); set => Set(value); }
        public bool Averaging { get => Get<bool>(); set => Set(value); }
        public bool AdvancedView { get => Get<bool>(); set => Set(value); }

        public double TargetLevel { get => Get<double>(); set => Set(value); }
        public double LimitLevel { get => Get<double>(); set => Set(value); }
        public double CompRate { get => Get<double>(); set => Set(value); }
        public double AverageTime { get => Get<double>(); set => Set(value); }
        public double Kurtosis { get => Get<double>(); set => Set(value); }
        public double MasterVolumeInterval { get => Get<double>(); set => Set(value); }
        public double MinPeak { get => Get<double>(); set => Set(value); }
        public double UpRate { get => Get<double>(); set => Set(value); }
        public double GCInterval { get => Get<double>(); set => Set(value); }
        public double UIUpdateInterval { get => Get<double>(); set => Set(value); }
        public double AutoControlInterval { get => Get<double>(); set => Set(value); }
        public bool ForceMMT { get => Get<bool>(); set => Set(value); }

        public string VFunc { get => Get<string>(); set => Set(value); }
        public string AppTitle { get => Get<string>(); set => Set(value); }
        public string ProcessPriority { get => Get<string>(); set => Set(value); }
        //public StringCollection ExcList { get => Get<StringCollection>(); set => Set(value); }
        public List<string> ExcList { get => Get<List<string>>(); set => Set(value); }
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
        #region Datalink
        [Newtonsoft.Json.JsonIgnore]
        public string SubVersion { get => Get<string>(); set => Set(value); }
        [Newtonsoft.Json.JsonIgnore]
        public System.Windows.Visibility AppUpdateMsg { get => Get<System.Windows.Visibility>(); set => Set(value); }
        [Newtonsoft.Json.JsonIgnore]
        public string UpdateLink { get => Get<string>(); set => Set(value); }

        [Newtonsoft.Json.JsonIgnore]
        public bool ProcessPriorityHigh { get => Get<bool>(); set => Set(value); }
        [Newtonsoft.Json.JsonIgnore]
        public bool ProcessPriorityAboveNormal { get => Get<bool>(); set => Set(value); }
        [Newtonsoft.Json.JsonIgnore]
        public bool ProcessPriorityNormal { get => Get<bool>(); set => Set(value); }

        [Newtonsoft.Json.JsonIgnore]
        public string CurrentDevice { get => Get<string>(); set => Set(value); }
        [Newtonsoft.Json.JsonIgnore]
        public string CurrentDeviceLong { get => Get<string>(); set => Set(value); }

        [Newtonsoft.Json.JsonIgnore]
        public double MasterVolume { get => Get<double>(); set => Set(Math.Round(value, 3)); }
        [Newtonsoft.Json.JsonIgnore]
        public double MasterPeak { get => Get<double>(); set => Set(Math.Round(value, 3)); }


        [Newtonsoft.Json.JsonIgnore]
        public System.Windows.Visibility ACDevShow { get => Get<System.Windows.Visibility>(); set => Set(value); }
        [Newtonsoft.Json.JsonIgnore]
        public double ACElapsed { get => Get<double>(); set => Set(Math.Round(value, 3)); }
        [Newtonsoft.Json.JsonIgnore]
        public double ACWaited { get => Get<double>(); set => Set(Math.Round(value, 3)); }
        [Newtonsoft.Json.JsonIgnore]
        public double ACEWdif { get => Get<double>(); set => Set(Math.Round(value, 3)); }

        [Newtonsoft.Json.JsonIgnore]
        public double ACAvCnt { get => Get<double>(); set => Set(Math.Round(value, 0)); }
        [Newtonsoft.Json.JsonIgnore]
        public double ACHz { get => Get<double>(); set => Set(Math.Round(value, 2)); }

        // window height change storyboard parameters
        [Newtonsoft.Json.JsonIgnore]
        public double WindowHeight { get => Get<double>(); set => Set(value); }
        [Newtonsoft.Json.JsonIgnore]
        public double WindowTop { get => Get<double>(); set => Set(value); }
        [Newtonsoft.Json.JsonIgnore]
        public string Transition { get => Get<string>(); set => Set(value); }

        // slider storyboard on configset
        //public double AudioUnit { get => Get<double>(); set => Set(value); }

        #endregion


        #region Properties storing system Variables
        [Newtonsoft.Json.JsonIgnore]
        private string InstalledPath => System.AppDomain.CurrentDomain.BaseDirectory;
        [Newtonsoft.Json.JsonIgnore]
        private string AppDataPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WaleAudioControl");
        [Newtonsoft.Json.JsonIgnore]
        public string WorkingPath { get; set; } = Directory.GetCurrentDirectory();
        [Newtonsoft.Json.JsonIgnore]
        private string StoringPath => System.IO.Path.Combine(WorkingPath, ConfFileName);
        [Newtonsoft.Json.JsonIgnore]
        private string ConfFileName => "WaleGeneral.conf";
        #endregion
        #region Methods
        public void Init()
        {
            PathInit();
            if (!IsSavedExists) { Reset(); }
            else { Read(); }
        }
        private void PathInit()
        {
            WorkingPath = InstalledPath;
            if (File.Exists(StoringPath)) return;
            else
            {
                JPack.FileLog.Log($"regex:pf: {new Regex(@"Program Files").IsMatch(InstalledPath)}");
                WorkingPath = new Regex(@"Program Files").IsMatch(InstalledPath) ? AppDataPath : InstalledPath;
            }
        }
        private bool IsSavedExists => File.Exists(StoringPath);

        public void Read()
        {
            bool res = JPack.JsonSaveManager.TryRead(out General old, WorkingPath, ConfFileName);
            //System.Diagnostics.Debug.WriteLine($"read config {res}");
            if (!res) { JPack.FileLog.Log("Failure to read config"); return; }
            //old.ExcList.ForEach(x => System.Diagnostics.Debug.WriteLine(x));
            //try { old.ExcList = old.ExcList.Distinct().ToList(); } catch { }
            Update(old);
            JPack.FileLog.Log("Succeed to read config");
        }
        public void Update(General old)
        {
            foreach (System.Reflection.PropertyInfo pi in this.GetType().GetProperties())
            {
                if (!Attribute.IsDefined(pi, typeof(Newtonsoft.Json.JsonIgnoreAttribute)))
                {
                    System.Reflection.PropertyInfo opi = old.GetType().GetProperty(pi.Name);
                    //System.Diagnostics.Debug.WriteLine($"{opi.GetValue(old).GetType()} {typeof(List<string>)}");
                    var buffer = opi.GetValue(old);
                    if (buffer.GetType() == typeof(List<string>)) buffer = (buffer as List<string>).Distinct().ToList();
                    if (opi != null && buffer != null) pi.SetValue(this, buffer);
                }
            }
            //AutoControl = old.AutoControl;
            //AlwaysTop = old.AlwaysTop;
            //StayOn = old.StayOn;
            //RunAtWindowsStartup = old.RunAtWindowsStartup;
            //Averaging = old.Averaging;
            //AdvancedView = old.AdvancedView;

            //TargetLevel = old.TargetLevel;
            //LimitLevel = old.LimitLevel;
            //CompRate = old.CompRate;
            //AverageTime = old.AverageTime;
            //Kurtosis = old.Kurtosis;
            //MasterVolumeInterval = old.MasterVolumeInterval;
            //MinPeak = old.MinPeak;
            //UpRate = old.UpRate;
            //GCInterval = old.GCInterval;
            //UIUpdateInterval = old.UIUpdateInterval;
            //AutoControlInterval = old.AutoControlInterval;
            //ForceMMT = old.ForceMMT;

            //VFunc = old.VFunc;
            //AppTitle = old.AppTitle;
            //ProcessPriority = old.ProcessPriority;
            //ExcList = old.ExcList.Distinct().ToList();
            //CombineSession = old.CombineSession;
            //AudioUnit = old.AudioUnit;
            //Version = old.Version;
            //PreviousVersion = old.PreviousVersion;

            //ShowSessionIcon = old.ShowSessionIcon;
            //CollapseSubSessions = old.CollapseSubSessions;
            //StaticMode = old.StaticMode;
            //PnameForAppname = old.PnameForAppname;
            //MainTitleforAppname = old.MainTitleforAppname;
            //AutoExcludeOnManualSet = old.AutoExcludeOnManualSet;
        }

        /// <summary>
        /// Stores current values of the application settings to a file.
        /// </summary>
        public void Save() => JPack.JsonSaveManager.Save(this, WorkingPath, ConfFileName);
        /// <summary>
        /// Upgrades application settings to reflect a more recent installation of the application.
        /// </summary>
        public void Upgrade(string version)
        {
            if (Version == version) return;

            PreviousVersion = Version;
            Version = version;

            Save();
        }

        /// <summary>
        /// Returns the value of the PreviousVersion property.
        /// This method is newly introduced for Wale and not same as original method of Properties.settings on WPF system.
        /// </summary>
        /// <returns></returns>
        public object GetPreviousVersion() => PreviousVersion;
        /// <summary>
        /// Returns the value of the named property for the previous version of the same application.
        /// This method is only for code continuity and always returns the result of GetPreviousVersion method w/o any params.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the property whose the value is to be returned.</param>
        /// <returns></returns>
        public object GetPreviousVersion(string propertyName) => GetPreviousVersion();
        /// <summary>
        /// Restores the persisted application settings values to their corresponding default properties.
        /// </summary>
        public void Reset() => DefaultValues();
        #endregion

    }

}
