using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using OxyPlot;
using OxyPlot.Series;
using System.Globalization;
using System.ComponentModel;
using System.IO;
using Wale.CoreAudio;

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
        /// Debug flag for master update task
        /// </summary>
        private bool updateVolumeDebug = false;
        /// <summary>
        /// Debug flag for session update task
        /// </summary>
        private readonly bool updateSessionDebug = false;
        #endregion
        #region Variables
        // objects
        /// <summary>
        /// stored setting that users can modify
        /// </summary>
        readonly Wale.WPF.Properties.Settings settings = Wale.WPF.Properties.Settings.Default;
        //readonly Wale.Configuration.General settings = Conf.settings;
        //readonly Wale.Configuration.General settings = new Wale.Configuration.General();
        /// <summary>
        /// datalink between MVVM
        /// </summary>
        readonly Datalink DL = new Datalink();
        /// <summary>
        /// Tray icon
        /// </summary>
        System.Windows.Forms.NotifyIcon NI;
        /// <summary>
        /// Audio controller
        /// </summary>
        AudioControl Audio;
        /// <summary>
        /// Debug message pack
        /// </summary>
        JPack.DebugPack DP;
        /// <summary>
        /// window update task list
        /// </summary>
        List<Task> UpdateTasks;


        // flags
        object _FinishAppLock = new object(), _activelock = new object();
        volatile bool FirstLoad = true, _FinishApp = false, _activated = false;
        /// <summary>
        /// attempt to finish whole app
        /// </summary>
        bool FinishApp { get { bool val; lock (_FinishAppLock) { val = _FinishApp; } return val; } set { lock (_FinishAppLock) { _FinishApp = value; } } }
        /// <summary>
        /// window is activated
        /// </summary>
        bool Active { get { bool val; lock (_activelock) { val = _activated; } return val; } set { lock (_activelock) { _activated = value; } } }
        bool loaded = false;

        // visuals
        double mainHeight;
        volatile bool nowConfig = false;
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
            //try { MessageBox.Show($"V. {AppVersion.FullVersion}, {settings.Version}, pre {settings.GetPreviousVersion("Version")}"); }
            //catch (System.Configuration.SettingsPropertyNotFoundException) { MessageBox.Show("Property not found"); }

            Action action = new Action(() => {
                //MessageBox.Show("Settings Upgrade Entry");
                try { settings.Upgrade(); } catch (Exception e) { MessageBox.Show($"Unknown Error on upgrade settings\r{e.Message}\rYour setting would be reset to default"); settings.Reset(); }
                //settings.Upgrade();// MessageBox.Show("Settings Upgraded.");
                settings.Version = AppVersion.FullVersion;// MessageBox.Show("Settings Version Upgraded");
                settings.Save();// MessageBox.Show("Settings Saved");
                try { JPack.FileLog.Log($"Settings are Upgraded from {settings.GetPreviousVersion("Version")}"); } catch { MessageBox.Show("FileLog Error on settings upgrade"); }
            });

            string ver = String.Empty;
            try { ver = settings.Version; } catch { action(); }
            if (ver != AppVersion.FullVersion) { action(); }
        }
        /// <summary>
        /// Make UI components
        /// </summary>
        private void MakeComponents()
        {
            DP = new JPack.DebugPack(debug);

            // clear contents for design
            LogScroll.Content = string.Empty;

            // set contents for design
            //if (string.IsNullOrWhiteSpace(AppVersion.Option)) this.Title = ($"WALE v{AppVersion.LongVersion}");
            this.Title = ($"WALE v{AppVersion.Version}");//-{AppVersion.Option}
            DL.SubVersion = AppVersion.SubVersion;
            settings.AppTitle = this.Title;

            Left = System.Windows.SystemParameters.WorkArea.Width - this.Width;
            Top = System.Windows.SystemParameters.WorkArea.Height - this.Height;

            // set data context
            DL.ACHz = 1.0 / (2.0 * settings.AutoControlInterval / 1000.0);
            DL.ACAvCnt = settings.AverageTime / settings.AutoControlInterval;
            this.DataContext = DL;

            // settings property changed event
            settings.PropertyChanged += Settings_PropertyChanged;

            // set process priority
            switch (settings.ProcessPriority)
            {
                case "High": DL.ProcessPriorityHigh = true; break;
                case "Above Normal": DL.ProcessPriorityAboveNormal = true; break;
                case "Normal": DL.ProcessPriorityNormal = true; break;
            }
            SetPriority(settings.ProcessPriority);

            // Check for Update
            //Tuple<bool, string> uc = AppUpdateCheck.Check();
            //if (uc.Item1)
            //{
                //DL.AppUpdateMsg = uc.Item1 ? Visibility.Visible : Visibility.Hidden;
                //DL.UpdateLink = uc.Item2;
            //}

            Log("OK1");
        }
        /// <summary>
        /// Make notifyicon - Task bar icon
        /// </summary>
        private void MakeNI()
        {
            // make icon
            NI = new System.Windows.Forms.NotifyIcon() {
                Text = this.Title,
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
            Audio = new AudioControl(DL) { UpRate = settings.UpRate };
            while (Audio.MasterVolume == -1)
            {
                Audio.Dispose();
                Audio = new AudioControl(DL) { UpRate = settings.UpRate };
            }
            Audio.RestartRequested += Audio_RestartRequested;
            Audio.Start(audioDebug);

            // start window update tasks
            UpdateTasks = new List<Task>();
            if (!restart) UpdateTasks.Add(new Task(CheckFirstLoadTask));
            UpdateTasks.Add(new Task(UpdateStateTask));
            UpdateTasks.Add(new Task(UpdateMasterDeviceTask));
            UpdateTasks.Add(new Task(UpdateSessionTask));
            UpdateTasks.ForEach(t => t.Start());

            RestartQueued = false;
            Log("OK2");
        }
        private async void RestartAudio()
        {
            DP.DMML("Restart Audio");
            FinishApp = true;
            Audio.RestartRequested -= Audio_RestartRequested;
            if (UpdateTasks != null && UpdateTasks.Count > 0) { await Task.WhenAll(UpdateTasks); UpdateTasks.Clear(); }
            UpdateTasks = null;
            
            //while (Audio != null) { Audio?.Dispose(); await Task.Delay(100); }
            Audio?.Dispose();
            while (Audio != null && !Audio.Disposed) { await Task.Delay(100); }
            Audio = null;

            await Task.Delay(200);
            GC.GetTotalMemory(true);
            await Task.Delay(200);

            FinishApp = false;
            StartAudio(true);
        }
        private volatile bool RestartQueued = false;
        private void Audio_RestartRequested(object sender, EventArgs e) { if (!RestartQueued) { RestartQueue(); } }
        private void RestartQueue() { Log("Restart Requested, restart may need to few seconds."); RestartQueued = true; /*await Task.Delay(100);*/ RestartAudio(); }
        /// <summary>
        /// Initialization when window is poped up. Read all setting values, store all values as original, draw all graphs.
        /// </summary>
        private void MakeConfigs()
        {
            // make config tab
            ConfigSet cs = new ConfigSet(Audio, DL, this, debug);
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
            Log("Start CheckFirstLoadTask");
            while (!FinishApp && FirstLoad)
            {
                Dispatcher?.Invoke(() => { Hide(); FirstLoad = false; loaded = true; });
                System.Threading.Thread.Sleep(new TimeSpan((long)(settings.AutoControlInterval * 10000)));
            }
            Log("End CheckFirstLoadTask");
        }

        /// <summary>
        /// Update task for master device status.
        /// </summary>
        private void UpdateStateTask()
        {
            Log("Start UpdateStateTask");
            bool On = true;
            while (!FinishApp)
            {
                if (Audio.MasterPeak >= 0 && !On)
                {
                    On = true;
                    NI.Icon = Properties.Resources.WaleLeftOn;
                }
                else if (Audio.MasterVolume < 0 && On)
                {
                    On = false;
                    NI.Icon = Properties.Resources.WaleRightOff;
                }
                System.Threading.Thread.Sleep(new TimeSpan((long)(settings.UIUpdateInterval * 10000)));
            }
            Log("End UpdateStateTask");
        }

        /// <summary>
        /// Update task for master device which is currently used
        /// </summary>
        private void UpdateMasterDeviceTask()
        {
            Log("Start UpdateVolumeTask");
            while (!FinishApp)
            {
                if (Active)
                {
                    JPack.DebugPack VDP = new JPack.DebugPack(updateVolumeDebug);
                    VDP.DMML($"base={settings.TargetLevel} vol={Audio.MasterVolume}({Audio.MasterPeak})");

                    Tuple<string, string> nbuf = Audio.GetDeviceName();
                    if (DL.CurrentDevice != nbuf.Item1) { DL.CurrentDevice = nbuf.Item1; DL.CurrentDeviceLong = nbuf.Item2; }

                    double vbuf = Audio.MasterVolume;// Console.WriteLine($"{vbuf}");
                    DL.MasterVolume = vbuf;
                    SetText(MasterLabel, VFunction.Level(vbuf, settings.AudioUnit).ToString());

                    double lbuf = Audio.MasterPeak * vbuf;// Console.WriteLine($"{lbuf}");
                    DL.MasterPeak = lbuf;
                    SetText(MasterPeakLabel, VFunction.Level(lbuf, settings.AudioUnit).ToString());

                    Dispatcher?.Invoke(() =>
                    {
                        double hbuf = MasterPanel.Height + Wale.Configuration.Visual.MainWindowBaseHeight;
                        hbuf = hbuf > Wale.Configuration.Visual.MainWindowHeightDefault ? hbuf : Wale.Configuration.Visual.MainWindowHeightDefault;
                        window.MinHeight = hbuf;
                    });
                }
                System.Threading.Thread.Sleep(new TimeSpan((long)(settings.UIUpdateInterval * 10000)));
            }
            Log("End UpdateVolumeTask");
        }

        /// <summary>
        /// Update task to make UIs of all sessions
        /// </summary>
        private void UpdateSessionTask()
        {
            Log("Start UpdateSessionTask");
            Dispatcher?.Invoke(() => { SessionPanel.Children.Clear(); });
            UpdateSession2(SessionPanel);
            while (!FinishApp)
            {
                if (Active) { UpdateSession2(SessionPanel); }
                System.Threading.Thread.Sleep(new TimeSpan((long)(settings.UIUpdateInterval * 10000)));
            }
            Log("End UpdateSessionTask");
        }
        /// <summary>
        /// real session update process, making UIs for all session
        /// </summary>
        private void UpdateSession(StackPanel grid)
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(new StackPanelConsumer(UpdateSession), new object[] { grid });  // invoking itself
                }
                else
                {
                    // the "functional part", executing only on the main thread
                    JPack.DebugPack SDP = new JPack.DebugPack(updateSessionDebug);
                    SDP.DMM("Getting Sessions");
                    int count = 0;
                    lock (Locks.Session) { count = Audio.Sessions.Count; }// count of all sessions
                    SDP.DMML("  Count:" + count);

                    // do when there is session
                    if (count > 0)
                    {
                        bool reAlign = false; // re-alignment flag
                        List<MeterSet> expired = new List<MeterSet>(); //expired tabSession.controls buffer
                        lock (Locks.Session)
                        {
                            foreach (var sc in Audio.Sessions)
                            {//check and insert new session data as meterset to tabSession.controls
                                if (sc.State != Wale.CoreAudio.SessionState.Expired)
                                {
                                    bool found = false;
                                    foreach (MeterSet item in SessionPanel.Children) { if (sc.ProcessID == item.ProcessID) found = true; }
                                    if (!found)
                                    {
                                        //Console.WriteLine($"{sc.Name}({sc.ProcessID}) {sc.DisplayName} / {sc.ProcessName} / {sc.Icon} / {sc.MainWindowTitle} / {sc.SessionIdentifier}");
                                        //string stooltip = string.IsNullOrEmpty(sc.MainWindowTitle) ? $"{sc.Name}({sc.ProcessID})" : $"{sc.Name}({sc.ProcessID}) - {sc.MainWindowTitle}";
                                        string stooltip = $"{sc.Name}({sc.ProcessID})";
                                        MeterSet set = new MeterSet(this, sc.ProcessID, sc.Name, sc.Icon, settings.AdvancedView, sc.AutoIncluded, updateSessionDebug, stooltip) { SoundEnabled = sc.SoundEnabled };
                                        int idx = SessionPanel.Children.Count;
                                        foreach (MeterSet item in SessionPanel.Children) { if (set.CompareTo(item) < 0) { idx = SessionPanel.Children.IndexOf(item); break; } }
                                        if (idx < SessionPanel.Children.Count) SessionPanel.Children.Insert(idx, set);
                                        else SessionPanel.Children.Add(set);
                                        reAlign = true;
                                        Log($"New MeterSet:{sc.Name}({sc.ProcessID})");
                                    }
                                }
                            }

                            foreach (MeterSet mSet in SessionPanel.Children)
                            {//check expired session and update not expired session
                                var session = Audio.Sessions.GetSession(mSet.ProcessID);
                                if (session == null || session.State == Wale.CoreAudio.SessionState.Expired) { expired.Add(mSet); reAlign = true; }
                                else
                                {
                                    if (settings.AdvancedView) mSet.DetailOn();
                                    else mSet.DetailOff();
                                    if (mSet.detailChanged) { reAlign = true; mSet.detailChanged = false; }
                                    //string stooltip = string.IsNullOrEmpty(session.MainWindowTitle) ? $"{session.Name}({session.ProcessID})" : $"{session.Name}({session.ProcessID}) - {session.MainWindowTitle}";
                                    string stooltip = $"{session.Name}({session.ProcessID})";
                                    if (mSet.AudioUnit != settings.AudioUnit) mSet.AudioUnit = settings.AudioUnit;
                                    mSet.UpdateData(session.Volume, session.Volume*session.Peak, session.Volume*session.AveragePeak, session.Name, stooltip);
                                    session.Relative = (float)mSet.Relative;
                                    // Sound mute check
                                    if (mSet.SoundEnableChanged) { session.SoundEnabled = mSet.SoundEnabled; mSet.SoundEnableChanged = false; }
                                    if (session.SoundEnabled != mSet.SoundEnabled) mSet.SoundEnabled = session.SoundEnabled;
                                    // Auto include check
                                    if (mSet.AutoIncludedChanged) { session.AutoIncluded = mSet.AutoIncluded; mSet.AutoIncludedChanged = false; }
                                    if (session.AutoIncluded != mSet.AutoIncluded) mSet.AutoIncluded = session.AutoIncluded;
                                }
                            }
                        }
                        foreach (MeterSet item in expired) { SetTabControl(SessionPanel, item, true); Log($"Remove MeterSet:{item.SessionName}({item.ProcessID})"); } //remove expired session as meterset from tabSession.controls
                        expired.Clear(); //clear expire buffer
                                         //realign when there are one or more new set or removed set.
                        if (reAlign)
                        {//re-align when there is(are) added or removed session(s)
                            Log("Re-aligning");
                            double lastHeight = this.Height, spacing = settings.AdvancedView ? Wale.Configuration.Visual.SessionBlockHeightDetail : Wale.Configuration.Visual.SessionBlockHeightNormal;
                            double newHeight = (double)(SessionPanel.Children.Count) * spacing + 60 + 2;
                            if (newHeight < this.MinHeight) { newHeight = Wale.Configuration.Visual.MainWindowHeightDefault; }
                            //Console.WriteLine($"fsgH:{fsgHeight},DF:{dif}");
                            mainHeight = newHeight;
                            if (!nowConfig) DoChangeHeightSB(newHeight, "0:0:.1");
                            //Console.WriteLine($"WH:{this.Height},({SystemParameters.WorkArea.Width},{SystemParameters.WorkArea.Height})");
                            Log("Re-aligned");
                        }
                    }//count check enclosure

                }
            }
            catch { DP.DMML($"fail to invoke UpdateSession"); }
        }
        private void UpdateSession2(StackPanel SessionPanel)
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(new StackPanelConsumer(UpdateSession2), new object[] { SessionPanel });  // invoking itself
                }
                else
                {
                    // the "functional part", executing only on the main thread
                    JPack.DebugPack SDP = new JPack.DebugPack(updateSessionDebug);
                    SDP.CMM("Getting Sessions");
                    int count = 0;
                    lock (Locks.Session) { count = Audio.Sessions.Count; }// count of all sessions
                    SDP.CMML("  Count:" + count);

                    // do when there is session
                    if (count > 0)
                    {
                        bool reAlign = false; // re-alignment flag
                        List<MeterSet> expired = new List<MeterSet>(); //expired tabSession.controls buffer
                        lock (Locks.Session)
                        {
                            // Add new session
                            foreach (var sc in Audio.Sessions)
                            {//check and insert new session data as meterset to tabSession.controls
                                if (sc.State != Wale.CoreAudio.SessionState.Expired)
                                {
                                    bool found = false;
                                    foreach (MeterSet item in SessionPanel.Children) { if (sc.ProcessID == item.ProcessID) { found = true; break; } }
                                    if (!found)
                                    {
                                        //if (sc.Name != sc.DisplayName && !string.IsNullOrWhiteSpace(sc.DisplayName)) { Log($"Remake name of {sc.Name}({sc.ProcessID})"); sc.Name = sc.DisplayName; }
                                        Log($"Make proper name of {sc.Name}({sc.ProcessID})");
                                        sc.Name = sc.DisplayName;//Make proper NameSet
                                        string mt = (sc.Name != sc.MainWindowTitle ? sc.MainWindowTitle : ""), name = (settings.MainTitleforAppname ? $"{sc.Name} {mt}" : sc.Name), pname = sc.NameSet.ProcessName;
                                        if (settings.PnameForAppname) { name = (settings.MainTitleforAppname ? $"{sc.NameSet.ProcessName} {mt}" : sc.NameSet.ProcessName); pname = sc.Name; }
                                        if (updateSessionDebug) { Console.WriteLine($"{name}({sc.ProcessID}) {sc.DisplayName} / {sc.ProcessName} / {sc.Icon} / {mt} / {sc.SessionIdentifier}"); }
                                        //string stooltip = string.IsNullOrEmpty(sc.MainWindowTitle) ? $"{sc.Name}({sc.ProcessID})" : $"{sc.Name}({sc.ProcessID}) - {sc.MainWindowTitle}";
                                        string stooltip = $"{pname}({sc.ProcessID}) {mt}"; Console.WriteLine(stooltip);
                                        if (GetSessionConfigFromFile()) ApplyCurrentSessionConfig(sc);//Get saved session config
                                        MeterSet set = new MeterSet(this, sc.ProcessID, name, sc.Icon, settings.AdvancedView, sc.AutoIncluded, updateSessionDebug, stooltip) { SoundEnabled = sc.SoundEnabled, Relative = sc.Relative };
                                        int idx = SessionPanel.Children.Count; //Console.WriteLine($"new meterset idx={idx}");
                                        foreach (MeterSet item in SessionPanel.Children) { if (set.CompareTo(item) < 0) { idx = SessionPanel.Children.IndexOf(item); break; } }
                                        if (idx < SessionPanel.Children.Count) { SessionPanel.Children.Insert(idx, set); }//Console.WriteLine("new meterset inserted"); }
                                        else { SessionPanel.Children.Add(set); }//Console.WriteLine("new meterset added"); }
                                        reAlign = true;
                                        Log($"New MeterSet:{sc.Name}({sc.ProcessID}) {sc.SessionIdentifier}");
                                    }
                                }
                            }
                            // Update exist session
                            foreach (MeterSet mSet in SessionPanel.Children)
                            {//check expired session and update not expired session
                                var session = Audio.Sessions.GetSession(mSet.ProcessID);
                                if (session == null || session.State == Wale.CoreAudio.SessionState.Expired) { expired.Add(mSet); reAlign = true; }
                                else
                                {
                                    if (settings.AdvancedView) mSet.DetailOn();
                                    else mSet.DetailOff();
                                    if (mSet.detailChanged) { reAlign = true; mSet.detailChanged = false; }
                                    string mt = (session.Name != session.MainWindowTitle ? session.MainWindowTitle : ""), name = (settings.MainTitleforAppname ? $"{session.Name} {mt}" : session.Name), pname = session.NameSet.ProcessName;
                                    if (settings.PnameForAppname) { name = (settings.MainTitleforAppname ? $"{session.NameSet.ProcessName} {mt}" : session.NameSet.ProcessName); pname = session.Name; }
                                    //if (session.Name != session.DisplayName && !string.IsNullOrWhiteSpace(session.DisplayName)) { Log($"Remake name of {session.Name}({session.ProcessID})"); session.Name = session.DisplayName; }
                                    //string stooltip = string.IsNullOrEmpty(session.MainWindowTitle) ? $"{session.Name}({session.ProcessID})" : $"{session.Name}({session.ProcessID}) - {session.MainWindowTitle}";
                                    string stooltip = $"{pname}({session.ProcessID}) {mt}";
                                    if (mSet.AudioUnit != settings.AudioUnit) mSet.AudioUnit = settings.AudioUnit;
                                    mSet.UpdateData(session.Volume, session.Volume * session.Peak, session.Volume * session.AveragePeak, name, stooltip);
                                    if (updateSessionDebug) { Console.WriteLine($"{session.Volume}, {session.Volume * session.Peak}, {session.Volume * session.AveragePeak}, {session.Name}, {stooltip}"); }
                                    if (session.Relative != (float)mSet.Relative) { session.Relative = (float)mSet.Relative; SaveSessionConfigToFile(); }
                                    // Sound mute check
                                    if (mSet.SoundEnableChanged) { session.SoundEnabled = mSet.SoundEnabled; mSet.SoundEnableChanged = false; }
                                    if (session.SoundEnabled != mSet.SoundEnabled) mSet.SoundEnabled = session.SoundEnabled;
                                    // Auto include check
                                    if (mSet.AutoIncludedChanged) { session.AutoIncluded = mSet.AutoIncluded; mSet.AutoIncludedChanged = false; SaveSessionConfigToFile(); }
                                    if (session.AutoIncluded != mSet.AutoIncluded) { mSet.AutoIncluded = session.AutoIncluded; }
                                }
                            }
                        }
                        // Find expired session
                        foreach (MeterSet item in expired) { SetTabControl(SessionPanel, item, true); Log($"Remove MeterSet:{item.SessionName}({item.ProcessID})"); } //remove expired session as meterset from tabSession.controls
                        expired.Clear(); //clear expire buffer
                                         //realign when there are one or more new set or removed set.
                        if (reAlign)
                        {//re-align when there is(are) added or removed session(s)
                            Log("Re-aligning");
                            //SessionPanel.Children.Cast<List>().ToList().Sort();
                            double lastHeight = this.Height, spacing = settings.AdvancedView ? Wale.Configuration.Visual.SessionBlockHeightDetail : Wale.Configuration.Visual.SessionBlockHeightNormal;
                            double newHeight = (double)(SessionPanel.Children.Count) * spacing + 60 + 2;
                            if (newHeight < this.MinHeight) { newHeight = Wale.Configuration.Visual.MainWindowHeightDefault; }
                            //Console.WriteLine($"fsgH:{fsgHeight},DF:{dif}");
                            mainHeight = newHeight;
                            if (!nowConfig) DoChangeHeightSB(newHeight, "0:0:.1");
                            //Console.WriteLine($"WH:{this.Height},({SystemParameters.WorkArea.Width},{SystemParameters.WorkArea.Height})");
                            Log("Re-aligned");
                        }
                    }//count check enclosure

                }
            }
            catch { DP.DMML($"fail to invoke UpdateSession"); }
        }
        #endregion



        #region Toolstrip menu events
        private async void OnProgramShutdown(object sender, EventArgs e)
        {
            MessageBoxResult dialogResult = MessageBox.Show(
                Localization.Interpreter.Current.AreYouSureToTerminateWaleCompletely,
                Localization.Interpreter.Current.Exit,
                MessageBoxButton.OKCancel
            );
            if (dialogResult == MessageBoxResult.OK)
            {
                DP.DMML("Exit");
                SaveSessionConfigToFile();
                Active = false;
                FinishApp = true;
                NI.Visible = false;
                NI.Dispose();
                await Task.WhenAll(UpdateTasks);
                this.Close();
                Audio.Dispose();
                Log("Closed"); DP.DMML("Closed");
            }
        }
        private void OnProgramRestart(object sender, EventArgs e) {
            MessageBoxResult dialogResult = MessageBox.Show(
                Localization.Interpreter.Current.AreYouSureToRestartWale,
                Localization.Interpreter.Current.Restart,
                MessageBoxButton.OKCancel
            );
            if (dialogResult == MessageBoxResult.OK)
            {
                RestartQueued = true;
                SaveSessionConfigToFile();
                RestartApp();
            }
        }
        private async void RestartApp()
        {
            DP.DMML("Restart App");
            Active = false;
            FinishApp = true;
            NI.Visible = false;
            NI.Dispose();
            if (UpdateTasks != null) { await Task.WhenAll(UpdateTasks); }
            Audio.Dispose();
            Log("Closed"); DP.DMML("Closed");
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void ConfigToolStripMenuItem_Click(object sender, EventArgs e) { ConfigToolStripMenuItem_Click(sender, new RoutedEventArgs()); }
        private void ConfigToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DP.DMM("Settings");
            JPack.FormPack FWP = new JPack.FormPack();
            Configuration form = new Configuration(Audio, DL) { Icon = this.Icon };
            System.Drawing.Point p = FWP.PointFromMouse(-(int)(form.Width / 2), -(int)form.Height, JPack.FormPack.PointMode.AboveTaskbar);
            form.Left = p.X;
            form.Top = p.Y;
            form.Closed += Config_FormClosed;
            form.ShowDialog();
        }
        private void Config_FormClosed(object sender, EventArgs e)
        {
            Configuration form = sender as Configuration;
            if ((bool)form.DialogResult?.Equals(true))
            {
                //Transformation.SetBaseLevel(settings.TargetLevel);
                //Audio.SetBaseTo(settings.TargetLevel);
                Audio.UpRate = settings.UpRate;
            }
            form.Closed -= Config_FormClosed;
        }

        private void deviceMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DP.DMM("DeviceMap");
            JPack.FormPack FWP = new JPack.FormPack();
            DeviceMap form = new DeviceMap() { Icon = this.Icon };
            System.Drawing.Point p = FWP.PointFromMouse(-(int)(form.Width / 2), -(int)form.Height, JPack.FormPack.PointMode.AboveTaskbar);
            form.Left = p.X;
            form.Top = p.Y;
            form.ShowDialog();
        }
        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DP.DMM("Help");
            JPack.FormPack FWP = new JPack.FormPack();
            Help form = new Help() { Icon = this.Icon };
            System.Drawing.Point p = FWP.PointFromMouse(-(int)(form.Width / 2), -(int)form.Height, JPack.FormPack.PointMode.AboveTaskbar);
            form.Left = p.X;
            form.Top = p.Y;
            form.ShowDialog();
        }
        private void licensesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DP.DMM("Licenses");
            JPack.FormPack FWP = new JPack.FormPack();
            License form = new License { Icon = this.Icon };
            System.Drawing.Point p = FWP.PointFromMouse(-(int)(form.Width / 2), -(int)form.Height, JPack.FormPack.PointMode.AboveTaskbar);
            form.Left = p.X;
            form.Top = p.Y;
            form.ShowDialog();
        }

        private void openLogDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //System.Diagnostics.Process.Start(JPack.FileLog.WorkDirectory.FullName);
            Console.WriteLine($"{JPack.FileLog.WorkDirectory.FullName}");
            JPack.FileLog.Log(JPack.FileLog.File.FullName);
            JPack.FileLog.OpenWorkDirectoryOnExplorer();
        }

        private void WindowsSoundSetting_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo soundsetting = new ProcessStartInfo();
            soundsetting.FileName = "control";
            soundsetting.Arguments = "mmsys.cpl sounds";
            Process.Start(soundsetting);
        }
        private void WindowsVolumeMixer_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("sndvol.exe");
        }

        #endregion

        #region Master Volume control methods and events
        private void Up_Click(object sender, EventArgs e)
        {
            DP.DMM("Up ");
            GetInterval();
            updateVolumeDebug = true;
            Audio.VolumeUp(CalcInterval());
            updateVolumeDebug = false;
        }
        private void Down_Click(object sender, EventArgs e)
        {
            DP.DMM("Down ");
            GetInterval();
            updateVolumeDebug = true;
            Audio.VolumeDown(CalcInterval());
            updateVolumeDebug = false;
        }
        private void VolumeSet_Click(object sender, EventArgs e)
        {
            DP.DMM("Set ");
            double? volume = null;
            try { volume = Convert.ToDouble(TargetVolumeBox.Text); } catch { Log("fail to convert master volume\n"); MessageBox.Show("Invalid Volume"); return; }
            if (volume != null)
            {
                //double? buf = Transformation.Transform((double)volume, Transformation.TransFlow.UserToMachine);
                //if (buf != null) Audio.SetVolume((double)buf);
                Audio.SetMasterVolume((double)volume);
            }
        }
        private void TargetVolumeBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter) { VolumeSet_Click(sender, e); e.Handled = true; }
        }
        //Calculate interval value for machine from user input.
        private double CalcInterval()
        {
            DP.DMM(" CalcInterval");
            double it = settings.MasterVolumeInterval;
            //double? buf = Transformation.Transform(it, Transformation.TransFlow.IntervalUserToMachine);
            //if (buf != null) it = (double)buf;
            DP.DMM($"={it} ");
            return it;
        }
        private void GetInterval()
        {
            DP.DMM(" GetInterval");
            double it = 0;
            try
            {
                it = Convert.ToDouble(VolumeIntervalBox.Text);
                if (it > 0 && it <= 1)
                {
                    settings.MasterVolumeInterval = it;
                    settings.Save();
                }
            }
            catch { Log("fail to get master volume interval\n"); MessageBox.Show("Invalid Interval"); }
        }

        private void TargetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Console.WriteLine($"{settings.TargetLevel}");
            TargetLabel.Content = VFunction.Level(settings.TargetLevel, settings.AudioUnit).ToString();
            //if (settings.BaseLevel.ToString().Length > 4) { settings.BaseLevel = Math.Round(settings.BaseLevel, 2); }
            LastValues.TargetLevel = settings.TargetLevel;
            settings.Save();
        }

        private void MasterVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Audio.SetMasterVolume(e.NewValue);
        }

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
            string f = System.IO.Path.Combine(Conf.WorkingPath, SessionDataFile);
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
        private void ApplyCurrentSessionConfig()
        {
            SavedSessionConfig?.ForEach((sd) =>
            {
                lock (Locks.Session)
                {
                    Audio.Sessions.GetSessionBySID(sd.SessionIdentifier).ForEach((s) => { s.AutoIncluded = sd.AutoIncluded; s.Relative = sd.Relative; });
                }
            });
            Log("Session Config Applied");
        }
        private void ApplyCurrentSessionConfig(Session s)
        {
            if (SavedSessionConfig.Exists(sds => sds.SessionIdentifier == s.SessionIdentifier))
            {
                SessionDataStructure sd = SavedSessionConfig.Find(sds => sds.SessionIdentifier == s.SessionIdentifier);
                s.AutoIncluded = sd.AutoIncluded;
                s.Relative = sd.Relative;
            }

            Log("Session Config Applied on Session");
        }
        private List<SessionDataStructure> ParseSessionConfig()
        {
            List<SessionDataStructure> sessionDatas = new List<SessionDataStructure>();

            List<string> sidList = new List<string>();

            lock (Locks.Session)
            {
                Audio.Sessions.ForEach((s) =>
                {
                    string sid = s.SessionIdentifier;
                    if (!sidList.Contains(sid)) { sidList.Add(sid); }
                });
            }

            sidList.ForEach((sid) =>
            {
                lock (Locks.Session)
                {
                    List<Session> ss = Audio.Sessions.GetSessionBySID(sid);
                    if (ss.Count == 1) { sessionDatas.Add(new SessionDataStructure() { SessionIdentifier = ss[0].SessionIdentifier, AutoIncluded = ss[0].AutoIncluded, Relative = ss[0].Relative }); }
                    else if (ss.Count > 1)
                    {
                        Dictionary<bool, int> incCount = new Dictionary<bool, int>() { { true, 0 }, { false, 0 } };
                        Dictionary<float, int> relCount = new Dictionary<float, int>();

                        ss.ForEach((s) =>
                        {
                            if (s.AutoIncluded) { incCount[true]++; } else { incCount[false]++; }
                            if (relCount.ContainsKey(s.Relative)) { relCount[s.Relative]++; }
                            else { relCount.Add(s.Relative, 1); }
                        });

                        sessionDatas.Add(new SessionDataStructure()
                        {
                            SessionIdentifier = ss[0].SessionIdentifier,
                            AutoIncluded = incCount[true] > incCount[false] ? true : false,
                            Relative = relCount.First((d) => { return d.Value == relCount.Values.Max(); }).Key
                        });
                    }
                }
            });
            
            return sessionDatas;
        }
        private void SaveSessionConfigToFile()
        {
            string f = System.IO.Path.Combine(Conf.WorkingPath, SessionDataFile);

            List<SessionDataStructure> data = ParseSessionConfig();

            if (ReadSessionConfigFile(out string d))//check data is already saved into file
            {
                ParseSessionConfigFile(d).ForEach(sf => {
                    if(!data.Exists(sd => sd.SessionIdentifier== sf.SessionIdentifier))//get non-updated data from file
                    {
                        data.Add(sf);
                    }
                });
            }

            StringBuilder df = new StringBuilder();
            df.AppendLine($"{cmtind} Session Configuration Data of Wale");// '###' is the comment indicator
            df.AppendLine($"{cmtind} Last Update : {DateTime.Now.ToLocalTime().ToString()}");

            foreach(SessionDataStructure s in data)
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
            if (e.Button == System.Windows.Forms.MouseButtons.Left) { DP.DMML("IconLeftClick"); Active = true; Show(); Activate(); }
        }
        #endregion

        #region Window Events

        private void DevShow_CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Dev) { DL.ACDevShow = DL.ACDevShow == Visibility.Visible ? Visibility.Hidden : Visibility.Visible; }
            Log($"DevShow {DL.ACDevShow}");
        }
        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!loaded) return;
            //DrawBase();
            //DrawNew();
            if (settings.AutoControlInterval != LastValues.AutoControlInterval || settings.AverageTime != LastValues.AverageTime)
            {
                Audio?.UpdateAverageParam();
                double avcnt = settings.AverageTime / settings.AutoControlInterval;
                Log($"Update Avr Param {settings.AverageTime:n3}ms({avcnt:n0}), {settings.AutoControlInterval:n3}ms");
                DL.ACHz = 1.0 / (2.0 * settings.AutoControlInterval / 1000.0);
                DL.ACAvCnt = avcnt;
            }
            if (settings.VFunc != LastValues.VFunc) { Audio?.UpdateVFunc(); }
            if (e.PropertyName == "AdvancedView" && nowConfig)
            {
                if (settings.AdvancedView) DoChangeHeightSB(Wale.Configuration.Visual.ConfigSetLongHeight + Wale.Configuration.Visual.MainWindowBaseHeight);
                else DoChangeHeightSB(Wale.Configuration.Visual.ConfigSetHeight + Wale.Configuration.Visual.MainWindowBaseHeight);
            }
            if(e.PropertyName == "AudioUnit") { TargetLabel.Content = VFunction.Level(settings.TargetLevel, settings.AudioUnit).ToString(); }
        }

        private void window_Deactivated(object sender, EventArgs e)
        {
            if (!settings.StayOn)
            {
                Hide();
                Active = false;
            }
        }
        private void MasterTab_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (mouseWheelDebug) DP.DMML($"MouseWheel Captured:{e.Delta}");
            if (e.Delta > 0) Up_Click(sender, e);
            else if (e.Delta < 0) Down_Click(sender, e);
        }

        private void ChangeAlwaysTop(object sender, ExecutedRoutedEventArgs e)
        {
            settings.AlwaysTop = !settings.AlwaysTop;
            settings.Save();
        }
        private void ChangeStayOn(object sender, ExecutedRoutedEventArgs e)
        {
            settings.StayOn = !settings.StayOn;
            settings.Save();
        }
        private void ChangeDetailView(object sender, ExecutedRoutedEventArgs e)
        {
            settings.AdvancedView = !settings.AdvancedView;
            settings.Save();
        }
        private void ShiftToMasterTab(object sender, ExecutedRoutedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() => Tabs.SelectedIndex = 0));
        }
        private void ShiftToSessionTab(object sender, ExecutedRoutedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() => Tabs.SelectedIndex = 1));
        }
        private void ShiftToLogTab(object sender, ExecutedRoutedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() => Tabs.SelectedIndex = 2));
        }

        private void DoChangeHeightSB(double newHeight, string transition = null)
        {
            var transitRegex = new System.Text.RegularExpressions.Regex(@"\d:\d:\d", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (transition != null && transitRegex.IsMatch(transition)) DL.Transition = transition;
            DL.WindowHeight = newHeight;
            DL.WindowTop = this.Top + (this.Height - DL.WindowHeight);
            BeginStoryboard(this.FindResource("changeHeightSB") as System.Windows.Media.Animation.Storyboard);
        }
        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (TabItem tab in (sender as TabControl).Items)
            {
                if (tab.IsSelected)
                {
                    if (tab.Header.ToString().Contains("Config") && !nowConfig)
                    {
                        if (settings.AdvancedView) DoChangeHeightSB(Wale.Configuration.Visual.ConfigSetLongHeight + Wale.Configuration.Visual.MainWindowBaseHeight);
                        else DoChangeHeightSB(Wale.Configuration.Visual.ConfigSetHeight + Wale.Configuration.Visual.MainWindowBaseHeight);
                        nowConfig = true;
                    }
                    else if (!tab.Header.ToString().Contains("Config") && nowConfig)
                    {
                        DoChangeHeightSB(mainHeight);
                        nowConfig = false;
                    }
                    if (tab.Header.ToString().Contains("Log")) { LogScroll.ScrollToEnd(); }
                }
            }
        }
        private void Cs_LogInvokedEvent(object sender, ConfigSet.LogEventArgs e)
        {
            Log(e.msg);
        }


        private void ProcessPriorityAboveNormal_Unchecked(object sender, RoutedEventArgs e)
        {
            using (Process p = Process.GetCurrentProcess())
            {
                p.PriorityClass = ProcessPriorityClass.Normal;
            }
        }
        private void ProcessPriorityAboveNormal_Checked(object sender, RoutedEventArgs e)
        {
            using (Process p = Process.GetCurrentProcess())
            {
                p.PriorityClass = ProcessPriorityClass.AboveNormal;
            }
        }

        private void Priority_RadioButton_Click(object sender, RoutedEventArgs e)
        {
            if (!loaded) { e.Handled = true; return; }
            RadioButton s = sender as RadioButton;
            if ((bool)s.IsChecked) SetPriority(s.Content.ToString());
        }
        private void SetPriority(string priority)
        {
            Log($"Set process priority {priority}");
            settings.ProcessPriority = priority;
            ProcessPriorityClass ppc = ProcessPriorityClass.Normal;
            switch (priority)
            {
                case "High": ppc = ProcessPriorityClass.High; break;
                case "Above Normal": ppc = ProcessPriorityClass.AboveNormal; break;
                case "Normal": ppc = ProcessPriorityClass.Normal; break;
            }
            JPack.Debug.CML($"SPP H={DL.ProcessPriorityHigh} A={DL.ProcessPriorityAboveNormal} N={DL.ProcessPriorityNormal}");
            //settings.Save();
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
        private void TitlePanel_MouseDown(object sender, MouseButtonEventArgs e) { titlePosition = (e.GetPosition(this)); titleControl = true; }// Console.WriteLine($"TMD {titlePosition.X}, {titlePosition.Y}"); }
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
                Console.WriteLine($"{loc.Item1},{loc.Item2}");
                this.Left = loc.Item1;
                //this.Top = loc.Item2;
                DL.WindowTop = loc.Item2;
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

        #region Function delegates for unsafe UI update
        delegate void ScrollViewerStringConsumer(ScrollViewer control, string text);
        private void AppendText(ScrollViewer control, string text)
        {
            try
            {
                if (control != null)
                {
                    if (!Dispatcher.CheckAccess())
                    {
                        DP.DMML("Invoke Required - AppendText");
                        Dispatcher.Invoke(new ScrollViewerStringConsumer(AppendText), new object[] { control, text });  // invoking itself
                    }
                    else
                    {
                        (control as ScrollViewer).Content += text;      // the "functional part", executing only on the main thread
                    }
                }
            }
            catch { Log($"fail to invoke {control.Name}\n"); }
        }/**/

        delegate void TextBlockStringConsumer(TextBlock control, string text);
        private void AppendText(TextBlock control, string text)
        {
            try
            {
                if (control != null)
                {
                    if (!Dispatcher.CheckAccess())
                    {
                        DP.DMML("Invoke Required - AppendText");
                        Dispatcher.Invoke(new TextBlockStringConsumer(AppendText), new object[] { control, text });  // invoking itself
                    }
                    else
                    {
                        (control as TextBlock).Text += text;      // the "functional part", executing only on the main thread
                    }
                }
            }
            catch { Log($"fail to invoke {control.Name}\n"); }
        }/**/

        delegate void LabelStringConsumer(Label control, string text);
        /// <summary>
        /// Invoke delegate to change the Content of Label control object.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="text"></param>
        private void SetText(Label control, string text)
        {
            try
            {
                if (control != null)
                {
                    if (!Dispatcher.CheckAccess())
                    {
                        DP.DMML("Invoke Required - MeterSet - SetText");
                        Dispatcher.Invoke(new LabelStringConsumer(SetText), new object[] { control, text });  // invoking itself
                    }
                    else
                    {
                        control.Content = text;      // the "functional part", executing only on the main thread
                    }
                }
            }
            catch { Log($"fail to invoke {control.Name}\n"); }
        }/**/

        delegate void ProgressBardoubleConsumer(ProgressBar control, double value);
        /// <summary>
        /// Invoke delegate to change the Value of ProgressBar control object.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="value"></param>
        private void SetBar(ProgressBar control, double value)
        {
            try
            {
                if (control != null)
                {
                    if (!Dispatcher.CheckAccess())
                    {
                        DP.DMML("Invoke Required - MeterSet - SetBar");
                        Dispatcher.Invoke(new ProgressBardoubleConsumer(SetBar), new object[] { control, value });  // invoking itself
                    }
                    else
                    {
                        if (value > control.Maximum) value = control.Maximum;
                        else if (value < control.Minimum) value = control.Minimum;
                        control.Value = value;      // the "functional part", executing only on the main thread
                    }
                }
            }
            catch { Log($"fail to invoke {control.Name}\n"); }
        }/**/

        delegate void GridMeterSetConsumer(StackPanel control, MeterSet set, bool add);
        private void SetTabControl(StackPanel control, MeterSet set, bool remove = false)
        {
            try
            {
                if (control != null)
                {
                    if (!Dispatcher.CheckAccess())
                    {
                        DP.DMML("Invoke Required - MeterSet - SetTabControl");
                        Dispatcher.Invoke(new GridMeterSetConsumer(SetTabControl), new object[] { control, set, remove });  // invoking itself
                    }
                    else
                    {
                        if (remove) control.Children.Remove(set);      // the "functional part", executing only on the main thread
                        else control.Children.Add(set);
                    }
                }
            }
            catch { Log($"fail to invoke {control.Name}\n"); }
        }/**/

        delegate void doubleConsumer(double value);

        private void SetWindowSize(double difference)
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(new doubleConsumer(SetWindowSize), new object[] { difference });  // invoking itself
                }
                else
                {
                    this.Top -= difference;
                    this.Height += difference;      // the "functional part", executing only on the main thread
                }
            }
            catch { DP.DMML($"fail to invoke WindowSize"); }
        }/**/

        delegate void GridConsumer(Grid grid);//MeterSet Update Delegate

        delegate void StackPanelConsumer(StackPanel grid);//MeterSet Update Delegate
        delegate void WindowConsumer();//CheckFirstLoad Delegate

        #endregion

        #region Logging

        /// <summary>
        /// Write log to log tab on main window. Always prefixed "hh:mm> ".
        /// </summary>
        /// <param name="msg">message to log</param>
        /// <param name="newLine">flag for making newline after the end of the <paramref name="msg"/>.</param>
        private void Log(string msg, bool newLine = true)
        {
            JPack.FileLog.Log(msg, newLine);
            DateTime t = DateTime.Now.ToLocalTime();
            string content = $"{t.Hour:d2}:{t.Minute:d2}>{msg}";
            if (newLine) content += "\r\n";
            AppendText(LogScroll, content);
            DP.DMML(content);
            DP.CMML(content);
            //bAllowPaintLog = true;
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
    //wpf value converter
    /*
    public class PriorityNormalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.ToString() == "Normal") return true;
            else return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //if ((bool)value == true) { return "Normal"; }
            //else return "Normal";
            return null;
        }
    }
    public class PriorityAboveNormalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.ToString() == "Above Normal") return true;
            else return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //if ((bool)value == true) { return "Above Normal"; }
            //else return "Normal";
            return null;
        }
    }
    public class PriorityHighConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.ToString() == "High") return true;
            else return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //if ((bool)value == true) { return "High"; }
            //else return "Normal";
            return null;
        }
    }*/
    public class ACdataconverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value > WPF.Properties.Settings.Default.AutoControlInterval * 1.1 || (double)value < 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    //Datalink for VM
    public class Datalink : JPack.NotifyPropertyChanged
    {
        public Datalink()
        {
            AppUpdateMsg = Visibility.Hidden;
            ACDevShow = Visibility.Hidden;
            Transition = "0:0:.2";
        }

        public string SubVersion { get => Get<string>(); set => Set(value); }
        public Visibility AppUpdateMsg { get => Get<Visibility>(); set => Set(value); }
        public string UpdateLink { get => Get<string>(); set => Set(value); }

        public bool ProcessPriorityHigh { get => Get<bool>(); set => Set(value); }
        public bool ProcessPriorityAboveNormal { get => Get<bool>(); set => Set(value); }
        public bool ProcessPriorityNormal { get => Get<bool>(); set => Set(value); }

        public string CurrentDevice { get => Get<string>(); set => Set(value); }
        public string CurrentDeviceLong { get => Get<string>(); set => Set(value); }

        public double MasterVolume { get => Get<double>(); set => Set(Math.Round(value, 3)); }
        public double MasterPeak { get => Get<double>(); set => Set(Math.Round(value, 3)); }


        public Visibility ACDevShow { get => Get<Visibility>(); set => Set(value); }
        public double ACElapsed { get => Get<double>(); set => Set(Math.Round(value, 3)); }
        public double ACWaited { get => Get<double>(); set => Set(Math.Round(value, 3)); }
        public double ACEWdif { get => Get<double>(); set => Set(Math.Round(value, 3)); }

        public double ACAvCnt { get => Get<double>(); set => Set(Math.Round(value, 0)); }
        public double ACHz { get => Get<double>(); set => Set(Math.Round(value, 2)); }

        // window height change storyboard parameters
        public double WindowHeight { get => Get<double>(); set => Set(value); }
        public double WindowTop { get => Get<double>(); set => Set(value); }
        public string Transition { get => Get<string>(); set => Set(value); }

        // slider storyboard on configset
        //public double AudioUnit { get => Get<double>(); set => Set(value); }

    }
    
}
