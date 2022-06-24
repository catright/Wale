using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Wale.Extensions;

namespace Wale.Configs
{
    /// <summary>
    /// Data container for Wale
    /// </summary>
    public class General : JPack.NotifyPropertyChanged
    {
        public General()
        {
            DefaultValues();
        }
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
            CompRate = 1.0;
            AverageTime = 3000;
            Kurtosis = .35;
            MasterVolumeStep = .05;
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
            VerboseLog = false;
            MainTitleforAppname = false;
            AutoExcludeOnManualSet = false;

            AppUpdateMsg = System.Windows.Visibility.Hidden;
            ACDevShow = System.Windows.Visibility.Hidden;
            Transition = "0:0:.2";

            //AverageColor = (System.Windows.Media.Brushes.Lime, System.Windows.Media.Brushes.LimeGreen);
        }

        #region Properties
        public bool AutoControl { get => Get<bool>(); set => Set(value); }
        public bool AlwaysTop { get => Get<bool>(); set => Set(value); }
        public bool StayOn { get => Get<bool>(); set => Set(value); }
        public bool RunAtWindowsStartup { get => Get<bool>(); set => Set(value); }
        public bool Averaging { get => Get<bool>(); set => Set(value); }
        public bool AdvancedView { get => Get<bool>(); set => Set(value); }
        /// <summary>
        /// [Linear]
        /// </summary>
        public double TargetLevel { get => Get<double>(); set { Set(value); TargetLevelDB = value.TodB(); } }
        [JsonIgnore]
        public double TargetLevelDB { get => Get<double>(); set => Set(value); }
        /// <summary>
        /// [Linear]
        /// </summary>
        public double LimitLevel { get => Get<double>(); set => Set(value); }
        public double CompRate { get => Get<double>(); set { Set(value); CompRatioDB = value.TodB(); } }
        [JsonIgnore]
        public double CompRatioDB { get => Get<double>(); private set => Set(value); }
        /// <summary>
        /// [ms]
        /// </summary>
        public double AverageTime { get => Get<double>(); set => Set(value); }
        public double Kurtosis { get => Get<double>(); set => Set(value); }
        /// <summary>
        /// [Linear]
        /// </summary>
        public double MasterVolumeStep { get => Get<double>(); set => Set(value); }
        /// <summary>
        /// [Linear]
        /// </summary>
        public double MinPeak { get => Get<double>(); set => Set(value); }
        public double UpRate { get => Get<double>(); set => Set(value); }
        /// <summary>
        /// [ms]
        /// </summary>
        public double GCInterval { get => Get<double>(); set => Set(value); }
        /// <summary>
        /// [ms]
        /// </summary>
        public double UIUpdateInterval { get => Get<double>(); set => Set(value); }
        /// <summary>
        /// [ms]
        /// </summary>
        public double AutoControlInterval { get => Get<double>(); set => Set(value); }
        public bool ForceMMT { get => Get<bool>(); set => Set(value); }

        public string VFunc { get => Get<string>(); set { Set(value); OnPropertyChanged("DFunc"); } }
        /// <summary>
        /// Reflection of <see cref="VFunc"/>
        /// </summary>
        [JsonIgnore]
        public string DFunc { get => VFunc; set => VFunc = value; }
        public string AppTitle { get => Get<string>(); set => Set(value); }
        public string ProcessPriority { get => Get<string>(); set => Set(value); }
        public List<string> ExcList { get => Get<List<string>>(); set => Set(value); }
        public bool CombineSession { get => Get<bool>(); set => Set(value); }
        /// <summary>
        /// 0=Linear, 1=deciBel
        /// </summary>
        public int AudioUnit { get => Get<int>(); set => Set(value); }
        public string Version { get => Get<string>(); set => Set(value); }
        public string PreviousVersion { get => Get<string>(); set => Set(value); }

        public bool ShowSessionIcon { get => Get<bool>(); set => Set(value); }
        public bool CollapseSubSessions { get => Get<bool>(); set => Set(value); }
        public bool StaticMode { get => Get<bool>(); set => Set(value); }
        public bool PnameForAppname { get => Get<bool>(); set => Set(value); }
        public bool VerboseLog { get => Get<bool>(); set => Set(value); }
        public bool MainTitleforAppname { get => Get<bool>(); set => Set(value); }
        public bool AutoExcludeOnManualSet { get => Get<bool>(); set => Set(value); }

        //public (System.Windows.Media.Brush, System.Windows.Media.Brush) AverageColor { get => Get<(System.Windows.Media.Brush, System.Windows.Media.Brush)>(); set => Set(value); }
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
        [JsonIgnore]
        private string InstalledPath => AppDomain.CurrentDomain.BaseDirectory;
        [JsonIgnore]
        private string AppDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WaleAudioControl");
        [JsonIgnore]
        public string WorkingPath { get; set; } = Directory.GetCurrentDirectory();
        [JsonIgnore]
        private string StoringPath => Path.Combine(WorkingPath, ConfFileName);
        [JsonIgnore]
        private string ConfFileName => "WaleGeneral.conf";
        #endregion
        #region Methods
        public void Init()
        {
            //PathInit();
            if (!IsSavedExists) { Reset(); }
            else { Read(); }
        }
        public void PathInit()
        {
            WorkingPath = InstalledPath;
            if (IsSavedExists) return;
            else
            {
                Regex pf = new Regex(@"Program Files");
                //M.F($"regex:pf: {pf.IsMatch(InstalledPath)}");
                WorkingPath = pf.IsMatch(InstalledPath) ? AppDataPath : InstalledPath;
            }
        }
        private bool IsSavedExists => File.Exists(StoringPath);

        public void Read()
        {
            bool res = JPack.JsonSaveManager.TryRead(out General old, WorkingPath, ConfFileName);
            //System.Diagnostics.Debug.WriteLine($"read config {res}");
            if (!res) { M.F("Failed to read config"); return; }
            //old.ExcList.ForEach(x => System.Diagnostics.Debug.WriteLine(x));
            //try { old.ExcList = old.ExcList.Distinct().ToList(); } catch { }
            Update(old);
            //M.F("Succeed to read config");
        }
        public void Update(General old)
        {
            foreach (System.Reflection.PropertyInfo pi in this.GetType().GetProperties())
            {
                if (!Attribute.IsDefined(pi, typeof(JsonIgnoreAttribute)))
                {
                    System.Reflection.PropertyInfo opi = old.GetType().GetProperty(pi.Name);
                    //M.D($"{opi.GetValue(old).GetType()} {typeof(List<string>)}");
                    var buffer = opi.GetValue(old);
                    if (buffer.GetType() == typeof(List<string>)) buffer = (buffer as List<string>).Distinct().ToList();
                    if (opi != null && buffer != null) pi.SetValue(this, buffer);
                }
            }
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
