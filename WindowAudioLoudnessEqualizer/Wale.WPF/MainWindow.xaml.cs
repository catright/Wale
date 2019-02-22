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
        private bool Dev = true;
        /// <summary>
        /// Debug flag for main window
        /// </summary>
        private bool debug = false;

        private bool mouseWheelDebug = false;
        /// <summary>
        /// Debug flag for audio controller
        /// </summary>
        private bool audioDebug = false;
        /// <summary>
        /// Debug flag for master update task
        /// </summary>
        private bool updateVolumeDebug = false;
        /// <summary>
        /// Debug flag for session update task
        /// </summary>
        private bool updateSessionDebug = false;
        #endregion
        #region Variables
        // objects
        /// <summary>
        /// stored setting that users can modify
        /// </summary>
        Wale.WPF.Properties.Settings settings = Wale.WPF.Properties.Settings.Default;
        /// <summary>
        /// datalink between MVVM
        /// </summary>
        Datalink DL = new Datalink();
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
        JDPack.DebugPack DP;
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
        #endregion

        #region initializing
        public MainWindow()
        {
            InitializeComponent();
            MakeComponents();
            MakeNI();
            StartApp();
            MakeConfigs();
            Log("AppStarted");
        }
        /// <summary>
        /// Make UI components
        /// </summary>
        private void MakeComponents()
        {
            DP = new JDPack.DebugPack(debug);

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
            List<System.Windows.Forms.MenuItem> items = new List<System.Windows.Forms.MenuItem>
            {
                new System.Windows.Forms.MenuItem("Configuration", ConfigToolStripMenuItem_Click),
                new System.Windows.Forms.MenuItem("Device Map", deviceMapToolStripMenuItem_Click),
                new System.Windows.Forms.MenuItem("Open Log", openLogDirectoryToolStripMenuItem_Click),
                new System.Windows.Forms.MenuItem("-"),
                new System.Windows.Forms.MenuItem("Help", helpToolStripMenuItem_Click),
                new System.Windows.Forms.MenuItem("License", licensesToolStripMenuItem_Click),
                new System.Windows.Forms.MenuItem("E&xit", OnProgramShutdown)
            };
            NI.ContextMenu = new System.Windows.Forms.ContextMenu(items.ToArray());

            // icon click event
            this.NI.MouseClick += new System.Windows.Forms.MouseEventHandler(NI_MouseClick);
        }
        /// <summary>
        /// Read necessary setting values and start audio controller and all tasks
        /// </summary>
        private void StartApp()
        {
            // start audio controller
            Audio = new AudioControl(DL) { UpRate = settings.UpRate };
            while (Audio.MasterVolume == -1)
            {
                Audio.Dispose();
                Audio = new AudioControl(DL) { UpRate = settings.UpRate };
            }
            Audio.Start(audioDebug);

            // start window update tasks
            UpdateTasks = new List<Task>();
            UpdateTasks.Add(new Task(CheckFirstLoadTask));
            UpdateTasks.Add(new Task(UpdateStateTask));
            UpdateTasks.Add(new Task(UpdateMasterDeviceTask));
            UpdateTasks.Add(new Task(UpdateSessionTask));
            UpdateTasks.ForEach(t => t.Start());

            Log("OK2");
        }
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
                    JDPack.DebugPack VDP = new JDPack.DebugPack(updateVolumeDebug);
                    VDP.DMML($"base={settings.TargetLevel} vol={Audio.MasterVolume}({Audio.MasterPeak})");

                    Tuple<string, string> nbuf = Audio.GetDeviceName();
                    if (DL.CurrentDevice != nbuf.Item1) { DL.CurrentDevice = nbuf.Item1; DL.CurrentDeviceLong = nbuf.Item2; }

                    double vbuf = Audio.MasterVolume;// Console.WriteLine($"{vbuf}");
                    DL.MasterVolume = vbuf;

                    double lbuf = Audio.MasterPeak * vbuf;// Console.WriteLine($"{lbuf}");
                    DL.MasterPeak = lbuf;

                    Dispatcher?.Invoke(() =>
                    {
                        double hbuf = MasterPanel.Height + AppDatas.MainWindowBaseHeight;
                        hbuf = hbuf > AppDatas.MainWindowHeightDefault ? hbuf : AppDatas.MainWindowHeightDefault;
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
            while (!FinishApp)
            {
                if (Active) { UpdateSession(SessionPanel); }
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
                    JDPack.DebugPack SDP = new JDPack.DebugPack(updateSessionDebug);
                    SDP.DMM("Getting Sessions");
                    int count = 0;
                    lock (Lockers.Sessions) { count = Audio.Sessions.Count; }// count of all sessions
                    SDP.DMML("  Count:" + count);

                    // do when there is session
                    if (count > 0)
                    {
                        bool reAlign = false; // re-alignment flag
                        List<MeterSet> expired = new List<MeterSet>(); //expired tabSession.controls buffer
                        lock (Lockers.Sessions)
                        {
                            foreach (var sc in Audio.Sessions)
                            {//check and insert new session data as meterset to tabSession.controls
                                if (sc.State != Wale.CoreAudio.SessionState.Expired)
                                {
                                    bool found = false;
                                    foreach (MeterSet item in SessionPanel.Children) { if (sc.ProcessID == item.ProcessID) found = true; }
                                    if (!found)
                                    {
                                        //Console.WriteLine($"{sc.Name}({sc.ProcessID}) {sc.DisplayName} / {sc.ProcessName} / {sc.MainWindowTitle} / {sc.SessionIdentifier}");
                                        //string stooltip = string.IsNullOrEmpty(sc.MainWindowTitle) ? $"{sc.Name}({sc.ProcessID})" : $"{sc.Name}({sc.ProcessID}) - {sc.MainWindowTitle}";
                                        string stooltip = $"{sc.Name}({sc.ProcessID})";
                                        MeterSet set = new MeterSet(sc.ProcessID, sc.Name, settings.DetailedView, sc.AutoIncluded, updateSessionDebug, stooltip);
                                        int idx = SessionPanel.Children.Count;
                                        foreach (MeterSet item in SessionPanel.Children) { if (set.CompareTo(item) < 0) { idx = SessionPanel.Children.IndexOf(item); break; } }
                                        if (idx < SessionPanel.Children.Count) SessionPanel.Children.Insert(idx, set);
                                        else SessionPanel.Children.Add(set);
                                        reAlign = true;
                                        Log($"New MeterSet:{sc.Name}({sc.ProcessID})");
                                    }
                                }
                            }

                            foreach (MeterSet item in SessionPanel.Children)
                            {//check expired session and update not expired session
                                var session = Audio.Sessions.GetSession(item.ProcessID);
                                if (session == null || session.State == Wale.CoreAudio.SessionState.Expired) { expired.Add(item); reAlign = true; }
                                else
                                {
                                    if (settings.DetailedView) item.DetailOn();
                                    else item.DetailOff();
                                    if (item.detailChanged) { reAlign = true; item.detailChanged = false; }
                                    //string stooltip = string.IsNullOrEmpty(session.MainWindowTitle) ? $"{session.Name}({session.ProcessID})" : $"{session.Name}({session.ProcessID}) - {session.MainWindowTitle}";
                                    string stooltip = $"{session.Name}({session.ProcessID})";
                                    item.UpdateData(session.Volume, session.Peak, session.AveragePeak, session.Name, stooltip);
                                    session.Relative = (float)item.Relative;
                                    // Sound mute check
                                    if (item.SoundEnableChanged) { session.SoundEnabled = item.SoundEnabled; item.SoundEnableChanged = false; }
                                    if (session.SoundEnabled != item.SoundEnabled) item.SoundEnabled = session.SoundEnabled;
                                    // Auto include check
                                    if (item.AutoIncludedChanged) { session.AutoIncluded = item.AutoIncluded; item.AutoIncludedChanged = false; }
                                    if (session.AutoIncluded != item.AutoIncluded) item.AutoIncluded = session.AutoIncluded;
                                }
                            }
                        }
                        foreach (MeterSet item in expired) { SetTabControl(SessionPanel, item, true); Log($"Remove MeterSet:{item.SessionName}({item.ProcessID})"); } //remove expired session as meterset from tabSession.controls
                        expired.Clear(); //clear expire buffer
                                         //realign when there are one or more new set or removed set.
                        if (reAlign)
                        {//re-align when there is(are) added or removed session(s)
                            Log("Re-aligning");
                            double lastHeight = this.Height, spacing = settings.DetailedView ? AppDatas.SessionBlockHeightDetail : AppDatas.SessionBlockHeightNormal;
                            double newHeight = (double)(SessionPanel.Children.Count) * spacing + 60 + 2, dif = newHeight - lastHeight;
                            if (newHeight < this.MinHeight) { newHeight = AppDatas.MainWindowHeightDefault; dif = newHeight - lastHeight; }
                            //Console.WriteLine($"fsgH:{fsgHeight},DF:{dif}");
                            this.Height = newHeight;
                            this.Top -= dif;
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
            MessageBoxResult dialogResult = MessageBox.Show("Are you sure to terminate Wale completely?", "Exit", MessageBoxButton.OKCancel);
            if (dialogResult == MessageBoxResult.OK)
            {
                DP.DMML("Exit");
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

        private void ConfigToolStripMenuItem_Click(object sender, EventArgs e) { ConfigToolStripMenuItem_Click(sender, new RoutedEventArgs()); }
        private void ConfigToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DP.DMM("Settings");
            JDPack.FormPack FWP = new JDPack.FormPack();
            Configuration form = new Configuration(Audio, DL) { Icon = this.Icon };
            System.Drawing.Point p = FWP.PointFromMouse(-(int)(form.Width / 2), -(int)form.Height, JDPack.FormPack.PointMode.AboveTaskbar);
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
            JDPack.FormPack FWP = new JDPack.FormPack();
            DeviceMap form = new DeviceMap() { Icon = this.Icon };
            System.Drawing.Point p = FWP.PointFromMouse(-(int)(form.Width / 2), -(int)form.Height, JDPack.FormPack.PointMode.AboveTaskbar);
            form.Left = p.X;
            form.Top = p.Y;
            form.ShowDialog();
        }
        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DP.DMM("Help");
            JDPack.FormPack FWP = new JDPack.FormPack();
            Help form = new Help() { Icon = this.Icon };
            System.Drawing.Point p = FWP.PointFromMouse(-(int)(form.Width / 2), -(int)form.Height, JDPack.FormPack.PointMode.AboveTaskbar);
            form.Left = p.X;
            form.Top = p.Y;
            form.ShowDialog();
        }
        private void licensesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DP.DMM("Licenses");
            JDPack.FormPack FWP = new JDPack.FormPack();
            License form = new License { Icon = this.Icon };
            System.Drawing.Point p = FWP.PointFromMouse(-(int)(form.Width / 2), -(int)form.Height, JDPack.FormPack.PointMode.AboveTaskbar);
            form.Left = p.X;
            form.Top = p.Y;
            form.ShowDialog();
        }

        private void openLogDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(JDPack.FileLog.WorkDirectory.FullName);
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
            TargetLabel.Content = settings.TargetLevel.ToString();
            //if (settings.BaseLevel.ToString().Length > 4) { settings.BaseLevel = Math.Round(settings.BaseLevel, 2); }
            LastValues.TargetLevel = settings.TargetLevel;
            settings.Save();
        }

        private void MasterVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Audio.SetMasterVolume(e.NewValue);
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
            settings.DetailedView = !settings.DetailedView;
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

        private double lastHeightForConfigTab, heightDeffForConfigTab;
        private void ConfigTab_GotFocus(object sender, RoutedEventArgs e)
        {
            //RemakeConf();
            lastHeightForConfigTab = this.Height;
            heightDeffForConfigTab = AppDatas.MainWindowConfigHeight - this.Height;
            //Dispatcher.Invoke(new Action(() =>
            //{
                this.Height = AppDatas.MainWindowConfigHeight;
                this.Top -= heightDeffForConfigTab;
            //}), System.Windows.Threading.DispatcherPriority.ContextIdle, null);
        }
        private void ConfigTab_LostFocus(object sender, RoutedEventArgs e)
        {
            
            //Dispatcher.Invoke(new Action(() =>
            //{
                this.Top += heightDeffForConfigTab;
                this.Height = lastHeightForConfigTab;
            //}), System.Windows.Threading.DispatcherPriority.ContextIdle, null);
        }
        private bool nowConfig = false;
        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //ConfigSet cs = null;
            foreach (TabItem tab in (sender as TabControl).Items)
            {
                if (tab.IsSelected)
                {
                    if (tab.Header.ToString().Contains("Config") && !nowConfig)
                    {
                        //cs = new ConfigSet(Audio, DL, debug);
                        //cs.LogInvokedEvent += Cs_LogInvokedEvent;
                        //tab.Content = cs;
                        ConfigTab_GotFocus(sender, e); nowConfig = true;
                    }
                    else if (!tab.Header.ToString().Contains("Config") && nowConfig)
                    {
                        //if (cs != null) cs.LogInvokedEvent -= Cs_LogInvokedEvent;
                        //cs = null;
                        ConfigTab_LostFocus(sender, e); nowConfig = false;
                    }
                    if (tab.Header.ToString().Contains("Log")) { LogScroll.ScrollToEnd(); }
                }
            }
        }
        private void Cs_LogInvokedEvent(object sender, ConfigSet.LogEventArgs e)
        {
            Log(e.msg);
        }

        /*private void RemakeConf() {
            Makes();
            MakeOriginals();
            DrawGraph("Original");
            DrawNew();
        }*/


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
            JDPack.Debug.CML($"SPP H={DL.ProcessPriorityHigh} A={DL.ProcessPriorityAboveNormal} N={DL.ProcessPriorityNormal}");
            //settings.Save();
            using (Process p = Process.GetCurrentProcess()) { p.PriorityClass = ppc; }
        }
        #endregion

        #region title panel control, location and size check events
        private Point titlePosition;
        private void titlePanel_MouseDown(object sender, MouseButtonEventArgs e) { titlePosition = e.GetPosition(this); }
        private void titlePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point mouse = PointToScreen(e.GetPosition(this));
                //MessageBox.Show($"L={Screen.PrimaryScreen.WorkingArea.Left} R={Screen.PrimaryScreen.WorkingArea.Right}, T={Screen.PrimaryScreen.WorkingArea.Top} B={Screen.PrimaryScreen.WorkingArea.Bottom}");
                //Console.WriteLine($"W:{Left},M:{loc.X},LM:{titlePosition.X},SW:{System.Windows.SystemParameters.PrimaryScreenWidth}");
                double x = mouse.X - titlePosition.X;
                //if (x + this.Width >= System.Windows.SystemParameters.WorkArea.Width) x = System.Windows.SystemParameters.WorkArea.Width - this.Width;
                //else if (x <= 0) x = 0;

                double y = mouse.Y - titlePosition.Y;
                //if (y + this.Height >= System.Windows.SystemParameters.WorkArea.Height) y = System.Windows.SystemParameters.WorkArea.Height - this.Height;
                //else if (y <= 0) y = 0;
                var loc = CheckWindowLocation(x, y);
                //MessageBox.Show($"x={x} y={y}");
                Left = loc.Item1;
                Top = loc.Item2;
            }
        }
        private void Window_LocationAndSizeChanged(object sender, EventArgs e)
        {
            //if ((this.Left + this.Width) > System.Windows.SystemParameters.WorkArea.Width) this.Left = System.Windows.SystemParameters.WorkArea.Width - this.Width;
            //else if (this.Left < System.Windows.SystemParameters.WorkArea.Left) this.Left = System.Windows.SystemParameters.WorkArea.Left;

            //if ((this.Top + this.Height) > System.Windows.SystemParameters.WorkArea.Height) this.Top = System.Windows.SystemParameters.WorkArea.Height - this.Height;
            //else if (this.Top < System.Windows.SystemParameters.WorkArea.Top) this.Top = System.Windows.SystemParameters.WorkArea.Top;
            var loc = CheckWindowLocation(this.Left, this.Top);
            this.Left = loc.Item1;
            this.Top = loc.Item2;
        }
        private Tuple<double, double> CheckWindowLocation(double left, double top)
        {
            if ((left + this.Width) > System.Windows.SystemParameters.WorkArea.Width) left = System.Windows.SystemParameters.WorkArea.Width - this.Width;
            else if (left < System.Windows.SystemParameters.WorkArea.Left) left = System.Windows.SystemParameters.WorkArea.Left;

            if ((top + this.Height) > System.Windows.SystemParameters.WorkArea.Height) top = System.Windows.SystemParameters.WorkArea.Height - this.Height;
            else if (top < System.Windows.SystemParameters.WorkArea.Top) top = System.Windows.SystemParameters.WorkArea.Top;
            return new Tuple<double, double>(left, top);
        }
        #endregion

        #region Funcion delegates for unsafe UI update
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
            JDPack.FileLog.Log(msg, newLine);
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
    public class Datalink : INotifyPropertyChanged
    {
        private string _SubVersion = "";
        public string SubVersion { get => _SubVersion; set => SetData(ref _SubVersion, value); }
        private Visibility _AppUpdateMsg = Visibility.Hidden;
        public Visibility AppUpdateMsg { get => _AppUpdateMsg; set => SetData(ref _AppUpdateMsg, value); }
        private string _UpdateLink = "";
        public string UpdateLink { get => _UpdateLink; set => SetData(ref _UpdateLink, value); }

        private bool _ProcessPriorityHigh = false;
        public bool ProcessPriorityHigh { get => _ProcessPriorityHigh; set => SetData(ref _ProcessPriorityHigh, value); }
        private bool _ProcessPriorityAboveNormal = false;
        public bool ProcessPriorityAboveNormal { get => _ProcessPriorityAboveNormal; set => SetData(ref _ProcessPriorityAboveNormal, value); }
        private bool _ProcessPriorityNormal = false;
        public bool ProcessPriorityNormal { get => _ProcessPriorityNormal; set => SetData(ref _ProcessPriorityNormal, value); }


        private string _CurrentDevice = "";
        public string CurrentDevice { get => _CurrentDevice; set => SetData(ref _CurrentDevice, value); }
        private string _CurrentDeviceLong = "";
        public string CurrentDeviceLong { get => _CurrentDeviceLong; set => SetData(ref _CurrentDeviceLong, value); }

        private double _MasterVolume = 0;
        public double MasterVolume { get => _MasterVolume; set => SetData(ref _MasterVolume, Math.Round(value, 3)); }
        private double _MasterPeak = 0;
        public double MasterPeak { get => _MasterPeak; set => SetData(ref _MasterPeak, Math.Round(value, 3)); }


        private Visibility _ACDevShow = Visibility.Hidden;
        public Visibility ACDevShow { get => _ACDevShow; set => SetData(ref _ACDevShow, value); }
        private double _ACElapsed = 0;
        public double ACElapsed { get => _ACElapsed; set => SetData(ref _ACElapsed, Math.Round(value, 3)); }
        private double _ACWaited = 0;
        public double ACWaited { get => _ACWaited; set => SetData(ref _ACWaited, Math.Round(value, 3)); }
        private double _ACEWdif = 0;
        public double ACEWdif { get => _ACEWdif; set => SetData(ref _ACEWdif, Math.Round(value, 3)); }

        private double _ACAvCnt = 0;
        public double ACAvCnt { get => _ACAvCnt; set => SetData(ref _ACAvCnt, Math.Round(value, 0)); }
        private double _ACHz = 0;
        public double ACHz { get => _ACHz; set => SetData(ref _ACHz, Math.Round(value, 2)); }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void Notify([System.Runtime.CompilerServices.CallerMemberName]string name = null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
        protected bool SetData<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            Notify(name);
            return true;
        }
    }
}
