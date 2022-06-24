using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wale.Configs;
using Wale.CoreAudio;
using Wale.Extensions;

namespace Wale.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Debug Flags
        /// <summary>
        /// Dev view visibility on Main window
        /// </summary>
        private readonly bool Dev = true;
        /// <summary>
        /// Debug flag for main window
        /// </summary>
        private readonly bool debug = false;

        private readonly bool mouseWheelDebug = false;
        /// <summary>
        /// Debug flag for audio controller
        /// </summary>
        private readonly bool audioDebug = false;

        /// <summary>
        /// Debug flag for session update task
        /// </summary>
        private readonly bool updateSessionDebug = false;
        #endregion
        #region Variables
        // objects
        /// <summary>
        /// datalink
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        internal General gl => Manager.GL;
        /// <summary>
        /// Tray icon
        /// </summary>
        private System.Windows.Forms.NotifyIcon NI;
        /// <summary>
        /// Audio controller
        /// </summary>
        public Controller.AudioControl Audio { get; set; }
        /// <summary>
        /// Debug message pack
        /// </summary>
        private JPack.MPack DP;
        /// <summary>
        /// window update task list
        /// </summary>
        private readonly List<Task> UpdateTasks = new List<Task>();


        // flags
        private readonly object _FinishAppLock = new object(), _activelock = new object();
        private volatile bool FirstLoad = true, _FinishApp = false, _activated = false;
        /// <summary>
        /// attempt to finish whole app
        /// </summary>
        private bool FinishApp
        {
            get { bool val; lock (_FinishAppLock) { val = _FinishApp; } return val; }
            set { lock (_FinishAppLock) { _FinishApp = value; } }
        }
        /// <summary>
        /// window is activated
        /// </summary>
        private bool Active
        {
            get { bool val; lock (_activelock) { val = _activated; } return val; }
            set { lock (_activelock) { _activated = value; } }
        }
        private bool loaded = false;

        // visuals
        private double mainHeight;
        private volatile bool nowConfig = false;
        #endregion

        #region initializing
        public MainWindow()
        {
            InitializeComponent();
            CheckSettings();
            MakeComponents();
            MakeNI();
            StartAudio();
            MakeConfigs();
            Log("AppStarted");
        }
        /// <summary>
        /// Check settings from previous version of the app
        /// </summary>
        private void CheckSettings()
        {
            Action<string> actionUpgrade1 = new Action<string>(v =>
            {
                //MessageBox.Show("Settings Upgrade Entry");
                try
                {
                    gl.Upgrade(v); // MessageBox.Show("Settings Upgraded.");

                    try { M.F($"Settings are Upgraded from {gl.GetPreviousVersion("Version")}", verbose: gl.VerboseLog); }
                    catch { MessageBox.Show("FileLog Error on settings upgrade"); }
                }
                catch (Exception e) { MessageBox.Show($"Unknown Error on upgrade settings\r{e.Message}\rYour setting would be reset to default"); gl.Reset(); }
                finally
                {
                    //gl.Version = v;// MessageBox.Show("Settings Version Upgraded");
                    //gl.Save();// MessageBox.Show("Settings Saved");
                }
            });
            Action<Properties.Settings> actionUpgrade2 = new Action<Properties.Settings>(old =>
            {
                try
                {
                    gl.AutoControl = old.AutoControl;
                    gl.AlwaysTop = old.AlwaysTop;
                    gl.StayOn = old.StayOn;
                    gl.RunAtWindowsStartup = old.RunAtWindowsStartup;
                    gl.Averaging = old.Averaging;
                    gl.AdvancedView = old.AdvancedView;

                    gl.TargetLevel = old.TargetLevel;
                    gl.AverageTime = old.AverageTime;
                    gl.Kurtosis = old.Kurtosis;
                    gl.MasterVolumeStep = old.MasterVolumeInterval;
                    gl.MinPeak = old.MinPeak;
                    gl.UpRate = old.UpRate;
                    gl.GCInterval = old.GCInterval;
                    gl.UIUpdateInterval = old.UIUpdateInterval;
                    gl.AutoControlInterval = old.AutoControlInterval;

                    gl.VFunc = old.VFunc;
                    gl.AppTitle = old.AppTitle;
                    gl.ProcessPriority = old.ProcessPriority;
                    gl.ExcList = old.ExcList.Cast<string>().Distinct().ToList();
                    gl.CombineSession = old.CombineSession;
                    gl.AudioUnit = old.AudioUnit;
                    gl.Version = old.Version;
                    gl.PreviousVersion = old.GetPreviousVersion("Version").ToString();

                    gl.ShowSessionIcon = old.ShowSessionIcon;
                    gl.CollapseSubSessions = old.CollapseSubSessions;
                    gl.StaticMode = old.StaticMode;
                    gl.PnameForAppname = old.PnameForAppname;
                    gl.MainTitleforAppname = old.MainTitleforAppname;
                    gl.AutoExcludeOnManualSet = old.AutoExcludeOnManualSet;

                    //try { JPack.FileLog.Log($"Settings are Upgraded from {gl.PreviousVersion}"); }
                    //catch { MessageBox.Show("FileLog Error on settings upgrade"); }
                }
                catch (Exception e) { MessageBox.Show($"Unknown Error on upgrade settings\r{e.Message}\rYour setting would be reset to default"); gl.Reset(); }
                finally { gl.Save(); }
            });

            bool er = false;
            string ver = string.Empty;
            try { ver = gl.Version; } catch { er = true; }

            //upgrade windows default settings to wale customized config
            if (ver == "") actionUpgrade2(Properties.Settings.Default);
            //upgrade config to new version
            if (er || ver != AppVersion.FullVersion)
            {
                try { actionUpgrade1(AppVersion.FullVersion); }
                catch { }
            }
        }
        /// <summary>
        /// Make UI components
        /// </summary>
        private void MakeComponents()
        {
            DP = new JPack.MPack(debug);

            // clear contents for design
            LogScroll.Content = string.Empty;

            // set contents for design
            //if (string.IsNullOrWhiteSpace(AppVersion.Option)) this.Title = ($"WALE v{AppVersion.LongVersion}");
            this.Title = ($"WALE v{AppVersion.Version}");//-{AppVersion.Option}
            gl.SubVersion = AppVersion.SubVersion;
            gl.AppTitle = this.Title;

            Dispatcher?.Invoke(() =>
            {
                Left = SystemParameters.WorkArea.Width - this.Width;
                Top = SystemParameters.WorkArea.Height - this.Height;
            });

            // set data context
            gl.ACHz = 1.0 / (2.0 * gl.AutoControlInterval / 1000.0);
            gl.ACAvCnt = gl.AverageTime / gl.AutoControlInterval;
            this.DataContext = gl;

            // settings property changed event
            gl.PropertyChanged += Settings_PropertyChanged;

            // set process priority
            switch (gl.ProcessPriority)
            {
                case "High": gl.ProcessPriorityHigh = true; break;
                case "Above Normal": gl.ProcessPriorityAboveNormal = true; break;
                case "Normal": gl.ProcessPriorityNormal = true; break;
            }
            SetPriority(gl.ProcessPriority, true);

            // Check for Update
            //Tuple<bool, string> uc = AppUpdateCheck.Check();
            //if (uc.Item1)
            //{
            //DL.AppUpdateMsg = uc.Item1 ? Visibility.Visible : Visibility.Hidden;
            //DL.UpdateLink = uc.Item2;
            //}

            Dispatcher?.Invoke(() => { SessionPanel.Children.Clear(); });
            Log("OK1");
        }
        /// <summary>
        /// Make notifyicon - Task bar icon
        /// </summary>
        private void MakeNI()
        {
            // make icon
            NI = new System.Windows.Forms.NotifyIcon()
            {
                Text = $"{this.Title}.{AppVersion.SubVersion}",
                Icon = Properties.Resources.WaleLeftOn,
                Visible = true
            };

            // make context menu
            List<System.Windows.Forms.MenuItem> items = new List<System.Windows.Forms.MenuItem>();
            foreach (var item in MainContext.Items)
            {
                System.Windows.Forms.MenuItem NIitem = MenuItemConvert(item);
                if (NIitem != null)
                {

                    if (item.GetType() == typeof(MenuItem) && (item as MenuItem).Items.Count > 0)
                    {
                        foreach (var subitem in (item as MenuItem).Items)
                        {
                            System.Windows.Forms.MenuItem NIsubitem = MenuItemConvert(subitem);
                            if (NIsubitem != null)
                            {

                                if (subitem.GetType() == typeof(MenuItem) && (subitem as MenuItem).Items.Count > 0)
                                {
                                    foreach (var subsubitem in (subitem as MenuItem).Items)
                                    {
                                        System.Windows.Forms.MenuItem NIsubsubitem = MenuItemConvert(subsubitem);
                                        if (NIsubsubitem != null)
                                        {

                                            NIsubitem.MenuItems.Add(NIsubsubitem);
                                        }
                                    }
                                }

                                NIitem.MenuItems.Add(NIsubitem);
                            }
                        }
                    }

                    items.Add(NIitem);
                }
            }
            NI.ContextMenu = new System.Windows.Forms.ContextMenu(items.ToArray());

            // icon click event
            this.NI.MouseClick += new System.Windows.Forms.MouseEventHandler(NI_MouseClick);
        }
        private System.Windows.Forms.MenuItem MenuItemConvert(object item)
        {
            if (item.GetType() == typeof(MenuItem))
            {
                return new System.Windows.Forms.MenuItem(
                    (item as MenuItem).Header.ToString().Replace('_', '&'),
                    new EventHandler((s, e) => { (item as MenuItem).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); })
                    );
            }
            else if (item.GetType() == typeof(Separator))
            {
                return new System.Windows.Forms.MenuItem("-");
            }
            else
            {
                Console.WriteLine("MenuItem is null");
                return null;
            }
        }

        /// <summary>
        /// Read necessary setting values and start audio controller and all tasks
        /// </summary>
        private void StartAudio(bool restart = false)
        {
            // start audio controller
            try
            {
                Audio = new Controller.AudioControl(gl);
                //Audio.RestartRequested += Audio_RestartRequested;
                Audio.SessionAdded += Audio_SessionAdded;
                Audio.Start(audioDebug);
            }
            catch (Exception e) { if (e.HResult.IsUnknown()) M.F($"{e}", verbose: gl.VerboseLog); }//CSCore.CoreAudioAPI.CoreAudioAPIException

            // start window update tasks
            UpdateTasks.Clear();
            //if (!restart) UpdateTasks.Add(new Task(CheckFirstLoadTask));
            if (!restart) UpdateTasks.Add(Task.Run(CheckFirstLoadTask));
            //UpdateTasks.Add(new Task(UpdateStateTask));
            //UpdateTasks.Add(new Task(UpdateMasterDeviceTask));
            //UpdateTasks.Add(new Task(UpdateSessionTask));
            UpdateTasks.Add(Task.Run(UpdateStateTask));
            UpdateTasks.Add(Task.Run(UpdateMasterDeviceTask));
            UpdateTasks.Add(Task.Run(UpdateSessionTask));
            //UpdateTasks.ForEach(t => t.Start());

            //_Restarting = false;
            Log("OK2");
        }

        /// <summary>
        /// Initialization when window is poped up. Read all setting values, store all values as original, draw all graphs.
        /// </summary>
        private void MakeConfigs()
        {
            // make config tab
            ConfigSet cs = new ConfigSet(this, debug);
            cs.LogInvokedEvent += Cs_LogInvokedEvent;
            ConfTab.Content = cs;
        }
        #endregion


        #region Update Tasks
        /// <summary>
        /// Starter task for check first Load. deprecated
        /// </summary>
        private void CheckFirstLoadTask()
        {
            Log("[Start] CheckFirstLoadTask");
            while (!FinishApp && FirstLoad)
            {
                Dispatcher?.Invoke(() => { Hide(); FirstLoad = false; loaded = true; });
                Thread.Sleep((int)gl.AutoControlInterval);
            }
            Log("[End] CheckFirstLoadTask");
        }

        /// <summary>
        /// Update task for master device status.
        /// </summary>
        private void UpdateStateTask()
        {
            Log("[Start] UpdateStateTask");
            bool On = true;
            while (!FinishApp)
            {
                if (Audio.MasterPeak >= 0 && Audio.MasterVolume >= 0 && !On)
                {
                    On = true;
                    NI.Icon = Properties.Resources.WaleLeftOn;
                }
                else if ((Audio.MasterPeak < 0 || Audio.MasterVolume < 0) && On)
                {
                    On = false;
                    NI.Icon = Properties.Resources.WaleRightOff;
                }
                Thread.Sleep((int)gl.UIUpdateInterval);
            }
            Log("[End] UpdateStateTask");
        }

        /// <summary>
        /// Update task for master device which is currently used
        /// </summary>
        private void UpdateMasterDeviceTask()
        {
            Log("[Start] UpdateVolumeTask");
            while (!FinishApp)
            {
                if (Active && (bool)Audio?.CanAccess)
                {
                    var (desc, friendly) = Audio.DeviceName;
                    if (gl.CurrentDevice != desc)
                    {
                        gl.CurrentDevice = desc;
                        gl.CurrentDeviceLong = friendly;
                    }

                    Dispatcher?.Invoke(() =>
                    {
                        double hbuf = MasterPanel.Height + Visual.MainWindowBaseHeight;
                        hbuf = hbuf > Visual.MainWindowHeightDefault ? hbuf : Visual.MainWindowHeightDefault;
                        window.MinHeight = hbuf;
                    });
                }
                Thread.Sleep((int)gl.UIUpdateInterval);
            }
            Log("[End] UpdateVolumeTask");
        }

        /// <summary>
        /// Update task to make UIs of all sessions
        /// </summary>
        private void UpdateSessionTask()
        {
            Log("[Start] UpdateSessionTask");
            //JPack.DebugPack SDP = new JPack.DebugPack(updateSessionDebug);
            nameGetStd = (uint)Math.Round(2000 / gl.UIUpdateInterval, 0);
            //nameGetTimer.Restart();
            while (!FinishApp)
            {
                if (Active && (bool)Audio?.CanAccess) UpdateSession();
                Thread.Sleep(Convert.ToInt32(gl.UIUpdateInterval));
            }
            Log("[End] UpdateSessionTask");
        }
        private string activeTabName = "MAIN";
        private uint nameGetCount = 0, nameGetStd = 1;
        //private readonly Stopwatch nameGetTimer = new Stopwatch();
        private void UpdateSession()
        {
            Guid workingPID = Guid.Empty;
            try
            {
                // We don't need to update the main tab if we are currently not using it
                if (!string.IsNullOrWhiteSpace(activeTabName) && activeTabName != "MAIN") return;
                if (updateSessionDebug) Debug.Write("Getting Sessions");

                // do when there is session
                string did = Audio?.DeviceID;
                if (updateSessionDebug) Debug.Write($" from device[{did}]");
                int count = Audio?.Sessions.Count ?? 0;
                if (updateSessionDebug) Debug.WriteLine("  Count:" + count);
                if (count <= 0) return;

                // Update exist session
                bool getname = nameGetCount++ > nameGetStd;//nameGetTimer.ElapsedMilliseconds > 1000;
                bool stopProgress = false;
                Dispatcher?.Invoke(() =>
                {
                    try
                    {
                        foreach (MeterSet mSet in SessionPanel.Children)
                        {
                            workingPID = mSet.ID;
                            // update session that is not expired
                            _ = mSet.UpdateData(did, getname);
                        }
                    }
                    catch (InvalidOperationException) { stopProgress = true; }
                });
                if (stopProgress) return;
                if (getname) nameGetCount = 0;
                //if (getname) nameGetTimer.Restart();

                // realign if requested
                if (reAlignRequested) Realign();
            }
            catch (NullReferenceException)
            {
                if (workingPID != Guid.Empty) MeterSet_SessionRemoved(workingPID);
            }
            catch (Exception e)
            {
                Log($"MainWindow: {e}");
            }
        }

        private bool reAlignRequested = false;
        private void Realign()
        {
            //re-align when there is(are) added or removed session(s)
            if (gl.VerboseLog) Log("Re-aligning");
            double lastHeight = 0, sessionCount = 0, minHeight = 0, spacing = gl.AdvancedView ? Visual.SessionBlockHeightDetail : Visual.SessionBlockHeightNormal;
            Dispatcher?.Invoke(() =>
            {
                lastHeight = this.Height;
                sessionCount = SessionPanel.Children.Count;
                minHeight = this.MinHeight;
                //double lastHeight = this.Height,
                //    spacing = gl.AdvancedView
                //        ? Wale.Configs.Visual.SessionBlockHeightDetail
                //        : Wale.Configs.Visual.SessionBlockHeightNormal;
                //double newHeight = (double)(SessionPanel.Children.Count) * spacing + 60 + 2;
                //if (newHeight < this.MinHeight) { newHeight = Wale.Configs.Visual.MainWindowHeightDefault; }
                ////Console.WriteLine($"fsgH:{fsgHeight},DF:{dif}");
                //mainHeight = newHeight;
                //if (!nowConfig) DoChangeHeightSB(newHeight, "0:0:.1");
            });
            double newHeight = sessionCount * spacing + 60 + 2;
            if (newHeight < minHeight) newHeight = Visual.MainWindowHeightDefault;
            mainHeight = newHeight;
            if (!nowConfig) DoChangeHeightSB(newHeight, "0:0:.1");

            reAlignRequested = false;
            Log("Re-aligned");
        }

        private void Audio_SessionAdded(object sender, Session s)
        {
            //System.Diagnostics.Debug.WriteLine("mainwindow:sessionadded");
            // Check whether session is already added
            bool found = false;
            string name;
            lock (s.Locker) name = s.Name;
            int idx = -1;
            Dispatcher?.Invoke(() =>
            {
                idx = SessionPanel.Children.Count;
                foreach (MeterSet m in SessionPanel.Children)
                {
                    if (s.ID == m.ID) { found = true; break; }
                    if (m.CompareTo(name) < 0) { idx = SessionPanel.Children.IndexOf(m); }
                }
            });
            if (found || idx < 0) return;

            // get and apply saved session config
            lock (s.Locker) { if (GetSessionConfigFromFile()) ApplyCurrentSessionConfig(s); }

            // make new MeterSet
            MeterSet set = null;
            Dispatcher?.Invoke(() =>
            {
                set = new MeterSet(this, s, updateSessionDebug);
                set.Logged += (loggedsender, t) => MeterSet_Log(t);
                set.SessionChanged += (changedsender, e) => MeterSet_SessionChanged(changedsender, e);
                set.SessionExpired += (expiredsender, e) => MeterSet_SessionRemoved(e);
            });
            if (set != null)
            {
                // add the MeterSet
                Dispatcher?.Invoke(() =>
                {
                    if (idx < SessionPanel.Children.Count) SessionPanel.Children.Insert(idx, set);
                    else SessionPanel.Children.Add(set);
                });

                // realign SessionPanel
                reAlignRequested = true;

                // log new MeterSet added
                string pid = "..";
                try { pid = s.ProcessID.ToString(); }
                catch (Exception e) { if (e.HResult.IsUnknown()) Log(e.Message); }
                Log($"New MeterSet: {name}({pid}) {s.ID}");
            }
        }

        private void MeterSet_Log(string msg) => Log(msg, caller: null);
        private void MeterSet_SessionChanged(object sender, EventArgs e) => SaveSessionConfigToFile();

        private readonly object ssRemoveListLock = new object();
        private readonly List<Guid> ssRemoveList = new List<Guid>();
        private void MeterSet_SessionRemoved(Guid id, string name = "", int pid = -1)//, string ident = "")
        {
            bool work = false;
            lock (ssRemoveListLock)
            {
                if (!ssRemoveList.Contains(id)) { ssRemoveList.Add(id); work = true; }
            }
            if (!work) return;
            // find MeterSet equivalent of removed Session
            MeterSet item = null;
            try
            {
                Dispatcher?.Invoke(() =>
                {
                    foreach (MeterSet m in SessionPanel.Children) { if (id == m.ID) { item = m; break; } }
                    if (item != null) SessionPanel.Children.Remove(item);
                });
            }
            catch (TaskCanceledException) { }
            catch (Exception e) { M.F($"MainWindow: {e}", verbose: gl.VerboseLog); }

            // remove found MeterSet when it exists
            if (item != null)
            {
                // realign SessionPanel
                reAlignRequested = true;
                try
                {
                    name = item.SessionName;
                    pid = item.ProcessID;
                }
                catch (Exception e) { if (e.HResult.IsUnknown()) Log(e.Message); }
                lock (ssRemoveListLock) { ssRemoveList.Remove(id); }
            }

            // log MeterSet removed
            if (item != null)
            {
                Log($"Remove MeterSet:{name}({pid}) {id}");
                item = null;
            }
            else if (gl.VerboseLog) Log($"Remove MeterSet:not found {name}({pid}) {id}, maybe already removed");
        }

        #endregion



        #region Toolstrip menu events
        private async Task Shutdown()
        {
            SaveSessionConfigToFile();
            Active = false;
            FinishApp = true;
            NI.Visible = false;
            NI.Dispose();
            if (UpdateTasks != null) await Task.WhenAll(UpdateTasks.Append(Audio.Stop()));
            UpdateTasks.Clear();
            Audio?.Dispose();
        }
        private async void OnProgramShutdown(object sender, EventArgs e)
        {
            MessageBoxResult dialogResult = MessageBox.Show(
                Localization.Interpreter.Current.AreYouSureToTerminateWaleCompletely,
                Localization.Interpreter.Current.Exit,
                MessageBoxButton.OKCancel
            );
            if (dialogResult == MessageBoxResult.OK)
            {
                Log("Get ready to exit");
                await Shutdown();
                Log("Exiting");
                this.Close();
            }
        }
        private async void OnProgramRestart(object sender, EventArgs e)
        {
            MessageBoxResult dialogResult = MessageBox.Show(
                Localization.Interpreter.Current.AreYouSureToRestartWale,
                Localization.Interpreter.Current.Restart,
                MessageBoxButton.OKCancel
            );
            if (dialogResult == MessageBoxResult.OK)
            {
                Log("Get ready to restart");
                await Shutdown();
                Log("Restarting");
                Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
        }

        private bool? OpenWindow(Window form)
        {
            DP.D($"{form.GetType()}");
            form.Icon = this.Icon;
            System.Drawing.Point p = new JPack.UI.FormPack().PointFromMouse(-(int)(form.Width / 2), -(int)form.Height, JPack.UI.FormPack.PointMode.AboveTaskbar);
            form.Left = p.X;
            form.Top = p.Y;
            return form.ShowDialog();
        }
        private void ConfigToolStripMenuItem_Click(object sender, RoutedEventArgs e) => OpenWindow(new Configuration(this));
        private void DeviceMapToolStripMenuItem_Click(object sender, EventArgs e) => OpenWindow(new DeviceMap());
        private void HelpToolStripMenuItem_Click(object sender, EventArgs e) => OpenWindow(new Help());
        private void LicensesToolStripMenuItem_Click(object sender, EventArgs e) => OpenWindow(new License());

        private void OpenLogDirectoryToolStripMenuItem_Click(object sender, EventArgs e) => JPack.Log.OpenWorkDirectoryOnExplorer();

        private void WindowsSoundSetting_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo("control", "mmsys.cpl sounds"));
        private void WindowsVolumeMixer_Click(object sender, RoutedEventArgs e) => Process.Start("sndvol.exe");

        #endregion
        #region Master Volume control methods and events
        private void Up_Click(object sender, EventArgs e)
        {
            if (Audio != null)
                Audio.MasterVolume += gl.AudioUnit == 0
                    ? gl.MasterVolumeStep
                    : (Audio.MasterVolume.TodB() + 2).ToLinear() - Audio.MasterVolume;
        }
        private void Down_Click(object sender, EventArgs e)
        {
            if (Audio != null)
                Audio.MasterVolume -= gl.AudioUnit == 0
                    ? gl.MasterVolumeStep
                    : Audio.MasterVolume - (Audio.MasterVolume.TodB() - 2).ToLinear();
        }
        private void VolumeSet_Click(object sender, EventArgs e)
        {
            DP.D("Set ");
            try { if (Audio != null) Audio.MasterVolume = Convert.ToDouble(TargetVolumeBox.Text); }
            catch { Log("fail to convert master volume\n"); MessageBox.Show("Invalid Volume"); return; }
        }
        private void TargetVolumeBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter) { VolumeSet_Click(sender, e); e.Handled = true; }
        }

        private void TargetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TargetLabel.Content = gl.TargetLevel.Level(gl.AudioUnit).ToString();
            LastValues.TargetLevel = gl.TargetLevel;
            gl.Save();
        }
        private void LimitSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LimitLabel.Content = gl.LimitLevel.Level(gl.AudioUnit).ToString();
            LastValues.LimitLevel = gl.LimitLevel;
            gl.Save();
        }

        private void MasterLabel_MouseDown(object sender, MouseButtonEventArgs e) => Audio.MasterMuted = !Audio.MasterMuted;

        #endregion
        #region Session Control methods and events
        private const string SessionDataFile = "SessionData.conf", cmtind = "###";
        private List<SessionDataStructure> SavedSessionConfig = new List<SessionDataStructure>();
        struct SessionDataStructure
        {
            public string SessionIdentifier;
            public bool AutoIncluded;
            public float Relative;
        }
        private bool ReadSessionConfigFile(out string d)
        {
            string f = Path.Combine(Manager.WorkingPath, SessionDataFile);
            if (File.Exists(f))
            {
                d = File.ReadAllText(f, Encoding.UTF8);
                return true;
            }
            d = string.Empty;
            return false;
        }
        private List<SessionDataStructure> ParseSessionConfigFile(string df)
        {
            List<SessionDataStructure> sessionDatas = new List<SessionDataStructure>();

            string[] d = df.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string st in d)
            {
                if (!st.StartsWith(cmtind))//Exclude comment line
                {
                    string[] sb = st.Split(new[] { cmtind }, StringSplitOptions.None);//split out inline comments
                    // Log($"sb={sb.Length}");foreach (string ss in sb) { Log(ss); }
                    string[] s = sb[0].Split('\t');
                    // Log($"s={s.Length}"); foreach (string ss in s) { Log(ss); }
                    if (s.Length == 3)//check whether the line is formatted properly
                    {
                        sessionDatas.Add(new SessionDataStructure()
                        {
                            SessionIdentifier = s[0],
                            AutoIncluded = Convert.ToBoolean(s[1]),
                            Relative = (float)Convert.ToDouble(s[2])
                        });
                    }
                    else { Log($"Error: when parsing session data file, {st}"); }
                }
            }
            return sessionDatas;
        }
        private bool GetSessionConfigFromFile()
        {
            if (ReadSessionConfigFile(out string d))
            {
                SavedSessionConfig = ParseSessionConfigFile(d);
                //Log("Session Config Read");
                return true;
            }
            return false;
        }
        //private void ApplyCurrentSessionConfig()
        //{
        //    SavedSessionConfig?.ForEach((sd) =>
        //    {
        //        lock (Locks.Session)
        //        {
        //            Audio.Sessions.GetSessionBySID(sd.SessionIdentifier).ForEach((s) => { s.Auto = sd.AutoIncluded; s.Relative = sd.Relative; });
        //        }
        //    });
        //    Log("Session Config Applied");
        //}
        private void ApplyCurrentSessionConfig(Session s)
        {
            if (SavedSessionConfig.Exists(sds => sds.SessionIdentifier == s.SessionIdentifier))
            {
                SessionDataStructure sd = SavedSessionConfig.Find(sds => sds.SessionIdentifier == s.SessionIdentifier);
                s.Auto = sd.AutoIncluded;
                s.Relative = sd.Relative;
            }
            if (gl.VerboseLog) Log($"Session Config Applied on Session {s.Name}");
        }
        private List<SessionDataStructure> ParseSessionConfig()
        {
            List<SessionDataStructure> sessionDatas = new List<SessionDataStructure>();

            List<string> sidList = new List<string>();

            lock (Audio.Sessions?.Locker)
            {
                Audio.Sessions?.ForEach((s) =>
                {
                    string sid = s.SessionIdentifier;
                    if (!sidList.Contains(sid)) { sidList.Add(sid); }
                });
            }

            sidList.ForEach((sid) =>
            {
                lock (Audio.Sessions?.Locker)
                {
                    List<Session> ss = Audio.Sessions.GetSessionList(sid);
                    if (ss.Count == 1) { sessionDatas.Add(new SessionDataStructure() { SessionIdentifier = ss[0].SessionIdentifier, AutoIncluded = ss[0].Auto, Relative = ss[0].Relative }); }
                    else if (ss.Count > 1)
                    {
                        Dictionary<bool, int> incCount = new Dictionary<bool, int>() { { true, 0 }, { false, 0 } };
                        Dictionary<float, int> relCount = new Dictionary<float, int>();

                        ss.ForEach((s) =>
                        {
                            if (s.Auto) { incCount[true]++; } else { incCount[false]++; }
                            if (relCount.ContainsKey(s.Relative)) { relCount[s.Relative]++; }
                            else { relCount.Add(s.Relative, 1); }
                        });

                        sessionDatas.Add(new SessionDataStructure()
                        {
                            SessionIdentifier = ss[0].SessionIdentifier,
                            AutoIncluded = incCount[true] > incCount[false],
                            Relative = relCount.First((d) => { return d.Value == relCount.Values.Max(); }).Key
                        });
                    }
                }
            });

            return sessionDatas;
        }
        private void SaveSessionConfigToFile()
        {
            string f = Path.Combine(Manager.WorkingPath, SessionDataFile);

            List<SessionDataStructure> data = ParseSessionConfig();

            if (ReadSessionConfigFile(out string d))//check data is already saved into file
            {
                ParseSessionConfigFile(d).ForEach(sf =>
                {
                    if (!data.Exists(sd => sd.SessionIdentifier == sf.SessionIdentifier))//get non-updated data from file
                    {
                        data.Add(sf);
                    }
                });
            }

            StringBuilder df = new StringBuilder();
            df.AppendLine($"{cmtind} Session Configuration Data of Wale");// '###' is the comment indicator
            df.AppendLine($"{cmtind} Last Update : {DateTime.Now.ToLocalTime()}");

            foreach (SessionDataStructure s in data)
            {
                df.AppendLine($"{s.SessionIdentifier}\t{s.AutoIncluded}\t{s.Relative}");
            }

            File.WriteAllText(f, df.ToString(), Encoding.UTF8);

            SavedSessionConfig = data;
            //Log("Session Config Saved");
        }
        #endregion

        #region NI events
        private void NI_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left) { DP.D("IconLeftClick"); Active = true; Show(); Activate(); }
        }
        #endregion

        #region Window Events
        private void DevShow_CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Dev) { gl.ACDevShow = gl.ACDevShow == Visibility.Visible ? Visibility.Hidden : Visibility.Visible; }
            Log($"DevShow {gl.ACDevShow}");
        }
        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (loaded) switch (e.PropertyName)
                {
                    case nameof(gl.AutoControlInterval):
                    case nameof(gl.AverageTime):
                        gl.ACHz = 1.0 / (2.0 * gl.AutoControlInterval / 1000.0);
                        gl.ACAvCnt = gl.AverageTime / gl.AutoControlInterval;
                        if (gl.VerboseLog) Log($"Update Avr Param {gl.AverageTime:n3}ms({gl.ACAvCnt:n0}), {gl.AutoControlInterval:n3}ms");
                        break;
                    case nameof(gl.AdvancedView) when nowConfig:
                        if (gl.AdvancedView) DoChangeHeightSB(Visual.ConfigSetLongHeight + Visual.MainWindowBaseHeight);
                        else DoChangeHeightSB(Visual.ConfigSetHeight + Visual.MainWindowBaseHeight);
                        reAlignRequested = true;
                        break;
                    case nameof(gl.AudioUnit):
                        TargetLabel.Content = gl.TargetLevel.Level(gl.AudioUnit).ToString();
                        LimitLabel.Content = gl.LimitLevel.Level(gl.AudioUnit).ToString();
                        break;
                    case nameof(gl.UIUpdateInterval):
                        nameGetStd = (uint)Math.Round(2000 / gl.UIUpdateInterval, 0);
                        break;
                }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (!gl.StayOn)
            {
                Hide();
                Active = false;
            }
        }
        private void MasterTab_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (mouseWheelDebug) DP.D($"MouseWheel Captured:{e.Delta}");
            if (e.Delta > 0) Up_Click(sender, e);
            else if (e.Delta < 0) Down_Click(sender, e);
        }

        private void ChangeAlwaysTop(object sender, ExecutedRoutedEventArgs e)
        {
            gl.AlwaysTop = !gl.AlwaysTop;
            gl.Save();
        }
        private void ChangeStayOn(object sender, ExecutedRoutedEventArgs e)
        {
            gl.StayOn = !gl.StayOn;
            gl.Save();
        }
        private void ChangeDetailView(object sender, ExecutedRoutedEventArgs e)
        {
            gl.AdvancedView = !gl.AdvancedView;
            gl.Save();
        }
        private void ChangeAudioUnit(object sender, ExecutedRoutedEventArgs e)
        {
            switch (gl.AudioUnit)
            {
                case 0: gl.AudioUnit = 1; break;
                case 1: gl.AudioUnit = 0; break;
            }
            gl.Save();
        }
        private void ShiftToMasterTab(object sender, ExecutedRoutedEventArgs e) => Dispatcher?.Invoke(() => Tabs.SelectedIndex = 0);
        private void ShiftToSessionTab(object sender, ExecutedRoutedEventArgs e) => Dispatcher?.Invoke(() => Tabs.SelectedIndex = 1);
        private void ShiftToLogTab(object sender, ExecutedRoutedEventArgs e) => Dispatcher?.Invoke(() => Tabs.SelectedIndex = 2);

        private void DoChangeHeightSB(double newHeight, string transition = null)
        {
            if (gl.WindowHeight == newHeight) return;
            var transitRegex = new Regex(@"\d:\d:\d", RegexOptions.IgnoreCase);
            if (transition != null && transitRegex.IsMatch(transition)) gl.Transition = transition;
            gl.WindowHeight = newHeight;
            Dispatcher?.Invoke(() =>
            {
                if (this.Height == gl.WindowHeight) return;
                gl.WindowTop = this.Top + (this.Height - gl.WindowHeight);
                BeginStoryboard(this.FindResource("changeHeightSB") as System.Windows.Media.Animation.Storyboard);
            });
        }

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var items = (sender as TabControl)?.Items;
            if (items == null) return;

            foreach (TabItem tab in items)
            {
                if (!tab.IsSelected) continue;

                switch (tab.Header.ToString())
                {
                    case string a when a.Contains("Config") && !nowConfig:
                        if (gl.AdvancedView)
                            DoChangeHeightSB(Visual.ConfigSetLongHeight + Visual.MainWindowBaseHeight);
                        else
                            DoChangeHeightSB(Visual.ConfigSetHeight + Visual.MainWindowBaseHeight);
                        nowConfig = true;
                        activeTabName = "CONFIG";
                        break;
                    case string a when !a.Contains("Config") && nowConfig:
                        DoChangeHeightSB(mainHeight);
                        nowConfig = false;
                        activeTabName = a.Contains("Log") ? "LOG" : "MAIN";
                        break;
                    case string a when a.Contains("Log"):
                        LogScroll.ScrollToEnd();
                        activeTabName = "LOG";
                        break;
                    default:
                        activeTabName = "MAIN";
                        break;
                }
            }
        }
        private void Cs_LogInvokedEvent(object sender, ConfigSet.LogEventArgs e) => Log(e.Message);



        private void Priority_RadioButton_Click(object sender, RoutedEventArgs e)
        {
            if (!loaded) { e.Handled = true; return; }
            RadioButton s = sender as RadioButton;
            if ((bool)s.IsChecked) SetPriority(s.Content.ToString());
        }
        private void SetPriority(string priority, bool force = false)
        {
            if (gl.ProcessPriority == priority && !force) return;
            Log($"Set process priority {priority}");
            gl.ProcessPriority = priority;
            ProcessPriorityClass ppc = ProcessPriorityClass.Normal;
            switch (priority)
            {
                case "High": ppc = ProcessPriorityClass.High; break;
                case "Above Normal": ppc = ProcessPriorityClass.AboveNormal; break;
                case "Normal": ppc = ProcessPriorityClass.Normal; break;
            }
            //JPack.Debug.CML($"SPP H={gl.ProcessPriorityHigh} A={gl.ProcessPriorityAboveNormal} N={gl.ProcessPriorityNormal}");
            //gl.Save();
            using (Process p = Process.GetCurrentProcess()) { p.PriorityClass = ppc; }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (touchControl) TouchControl(sender, e);
            else if (titleControl) TitleControl(sender, e);
        }
        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            touchControl = false;
            titleControl = false;
        }
        #endregion
        #region Window Touch Events
        private volatile bool ts, t1, t2, touchControl = false;
        private Point tPos;
        private void MasterPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //Log("dT start");
            //Console.WriteLine("dTm start");
            tPos = PointToScreen(e.GetPosition(this));
            touchControl = true;
            ts = false;
            t1 = false;
            t2 = false;
        }
        private void MasterPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (touchControl) TouchControl(sender, e);
            else if (titleControl) TitleControl(sender, e);
        }
        private void TouchControl(object sender, MouseEventArgs e)
        {
            //Log("dT move");
            //Console.WriteLine("dTm move");
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                double crit = 30, ori = 10;
                Point mouse = PointToScreen(e.GetPosition(this));
                double x = mouse.X - tPos.X;
                //Log($"dT={x}");
                //Console.WriteLine($"dTm={x}");

                if (!t1 && !t2 && !ts && x > crit) { t1 = true; }// Console.WriteLine($"dTm={x} t1 {t1}"); }
                if (t1 && !t2 && !ts && x < ori) { t2 = true; }// Console.WriteLine($"dTm={x} t2 {t2}"); }
                if (t1 && t2 && !ts && x > crit) { ts = true; }// Console.WriteLine($"dTm={x} ts {ts}"); }
                if (t1 && t2 && ts)
                {
                    DevShow_CommandBinding_Executed(this, null);
                    ts = false;
                    t1 = false;
                    t2 = false;
                }
            }
        }

        #endregion
        #region title panel control, location and size check events
        private volatile bool titleControl = false;
        private Point titlePosition;
        private void TitlePanel_MouseDown(object sender, MouseButtonEventArgs e) { titlePosition = e.GetPosition(this); titleControl = true; }// Console.WriteLine($"TMD {titlePosition.X}, {titlePosition.Y}"); }
        private void TitlePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (touchControl) TouchControl(sender, e);
            else if (titleControl) TitleControl(sender, e);
        }
        private void TitleControl(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point mouse = PointToScreen(e.GetPosition(this));
                var visource = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;

                double x = mouse.X / visource.M11 - titlePosition.X;
                double y = mouse.Y / visource.M22 - titlePosition.Y;

                //Console.Write($"TMM {x},{y} ({x+this.Width},{y+this.Height})");
                //Console.Write($" [{SystemParameters.WorkArea.Left}-{SystemParameters.WorkArea.Right},");
                //Console.Write($"{SystemParameters.WorkArea.Top}-{SystemParameters.WorkArea.Bottom}]");
                //Console.Write($" L {this.Left},{this.Left+this.Width}-{this.Width}");
                //Console.Write($" T {this.Top},{this.Top+this.Height}-{this.Height}");

                //Console.Write($" DPI {visource.M11}, {visource.M22}");
                //Console.Write($" ! M/D {mouse.X / visource.M11}, {mouse.Y / visource.M22} !");
                //Console.Write($" FPS {SystemParameters.FullPrimaryScreenWidth}, {SystemParameters.FullPrimaryScreenHeight}");
                //Console.Write($" MPS {SystemParameters.MaximizedPrimaryScreenWidth}, {SystemParameters.MaximizedPrimaryScreenHeight}");
                //Console.Write($" PS {SystemParameters.PrimaryScreenWidth}, {SystemParameters.PrimaryScreenHeight}");
                //Console.Write($" VS {SystemParameters.VirtualScreenWidth}, {SystemParameters.VirtualScreenHeight}");
                //Console.WriteLine("");

                var loc = TitleBar.CheckWindowLocation(this, x, y);
                //Console.WriteLine($"{loc.Item1},{loc.Item2}");
                this.Left = loc.Item1;
                //this.Top = loc.Item2;
                gl.WindowTop = loc.Item2;
                BeginStoryboard(this.FindResource("changeTopSB") as System.Windows.Media.Animation.Storyboard);
            }
        }
        private void Window_LocationAndSizeChanged(object sender, EventArgs e)
        {
            var loc = TitleBar.CheckWindowLocation(this, this.Left, this.Top);
            if (this.Left != loc.Item1) this.Left = loc.Item1;
            if (this.Top != loc.Item2) this.Top = loc.Item2;
        }
        #endregion


        #region Logging

        /// <summary>
        /// <see cref="JPack.FileLog.Log(string, bool)"/>. Suffixed " *[<paramref name="caller"/>]". Prefixed "hh:mm> " instead suffix when write on log tab.
        /// </summary>
        /// <param name="o">message to log</param>
        /// <param name="newLine">flag for making newline after the end of the <paramref name="o"/></param>
        /// <param name="caller"><see cref="System.Runtime.CompilerServices.CallerMemberNameAttribute"/></param>
        private void Log(object o, bool newLine = true, [System.Runtime.CompilerServices.CallerMemberName] string caller = null)
        {
            M.F(o, DP.Enable, newLine, gl.VerboseLog, caller);
            if (newLine) Dispatcher?.Invoke(() => LogScroll.Content += M.Message(o) + Environment.NewLine);
            else Dispatcher?.Invoke(() => LogScroll.Content += M.Message(o));//AppendText(LogScroll, M.Message(o));
        }
        /// <summary>
        /// Event to scroll down to end of the newest log.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Logs_VisibleChanged(object sender, RoutedEventArgs e)
        {
            if (LogScroll.IsVisible)
            {
                LogScroll.ScrollToEnd();
                //Logs.SelectionStart = Logs.TextLength;
                //Logs.ScrollToCaret();
            }
        }

        #endregion
    }

}
