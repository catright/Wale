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
        private bool Dev = true;
        #region Variables
        Microsoft.Win32.RegistryKey rkApp = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        Wale.WPF.Properties.Settings settings = Wale.WPF.Properties.Settings.Default;
        Datalink DL = new Datalink();
        System.Windows.Forms.NotifyIcon NI;
        AudioControl Audio;
        JDPack.DebugPack DP;
        List<Task> _updateTasks;
        bool debug = false, mouseWheelDebug = false, audioDebug = false, updateVolumeDebug = false, updateSessionDebug = false;
        object _closelock = new object(), _activelock = new object(), _ntvlock = new object();
        volatile bool FirstLoad = true, _realClose = false, _activated = false, _numberToVol = true;
        bool Rclose { get { bool val; lock (_closelock) { val = _realClose; } return val; } set { lock (_closelock) { _realClose = value; } } }
        bool Active { get { bool val; lock (_activelock) { val = _activated; } return val; } set { lock (_activelock) { _activated = value; } } }
        bool NTV { get { bool val; lock (_ntvlock) { val = _numberToVol; } return val; } set { lock (_ntvlock) { _numberToVol = value; } } }
        bool loaded = false;
        double originalMax;
        #endregion

        #region initializing
        public MainWindow()
        {
            InitializeComponent();
            MakeComponents();
            MakeNI();
            MakeConfigs();
            StartApp();
            Log("AppStarted");
        }
        private void MakeComponents()
        {
            LogScroll.Content = string.Empty;
            //DL.ACDebugShow = Dev ? Visibility.Visible : Visibility.Hidden;
            DL.ACHz = 1.0 / (2.0 * settings.AutoControlInterval / 1000.0);
            DL.ACAvCnt = settings.AverageTime / settings.AutoControlInterval;
            this.DataContext = DL;
            DP = new JDPack.DebugPack(debug);
            settings.PropertyChanged += Settings_PropertyChanged;

            //set process priority
            SetPriority(settings.ProcessPriority);

            if (string.IsNullOrWhiteSpace(AppVersion.Option)) this.Title = ($"WALE v{AppVersion.LongVersion}");
            else this.Title = ($"WALE v{AppVersion.LongVersion}");//-{AppVersion.Option}
            settings.AppTitle = this.Title;

            Left = System.Windows.SystemParameters.WorkArea.Width - this.Width;
            Top = System.Windows.SystemParameters.WorkArea.Height - this.Height;
            Log("OK1");
        }
        private void MakeNI()
        {
            //this.Visibility = Visibility.Hidden;
            NI = new System.Windows.Forms.NotifyIcon();
            NI.Text = $"WALE v{AppVersion.LongVersion}";
            NI.Icon = Properties.Resources.WaleLeftOn;
            NI.Visible = true;

            List<System.Windows.Forms.MenuItem> items = new List<System.Windows.Forms.MenuItem>();
            items.Add(new System.Windows.Forms.MenuItem("Configuration", ConfigToolStripMenuItem_Click));
            items.Add(new System.Windows.Forms.MenuItem("Device Map", deviceMapToolStripMenuItem_Click));
            items.Add(new System.Windows.Forms.MenuItem("Open Log", openLogDirectoryToolStripMenuItem_Click));
            items.Add(new System.Windows.Forms.MenuItem("-"));
            items.Add(new System.Windows.Forms.MenuItem("Help", helpToolStripMenuItem_Click));
            items.Add(new System.Windows.Forms.MenuItem("License", licensesToolStripMenuItem_Click));
            items.Add(new System.Windows.Forms.MenuItem("E&xit", OnProgramShutdown));
            NI.ContextMenu = new System.Windows.Forms.ContextMenu(items.ToArray());

            this.NI.MouseClick += new System.Windows.Forms.MouseEventHandler(NI_MouseClick);
        }
        /// <summary>
        /// Initialization when window is poped up. Read all setting values, store all values as original, draw all graphs.
        /// </summary>
        private void MakeConfigs()
        {
            //if (string.IsNullOrWhiteSpace(AppVersion.Option)) this.Title = ($"WALE v{AppVersion.LongVersion}");
            //else this.Title = ($"WALE v{AppVersion.LongVersion}-{AppVersion.Option}");

            Makes();
            MakeOriginals();
            loaded = true;

            string selectedFunction = FunctionSelector.SelectedItem.ToString();
            if (selectedFunction == VFunction.Func.Reciprocal.ToString() || selectedFunction == VFunction.Func.FixedReciprocal.ToString()) { KurtosisBox.IsEnabled = true; }
            else { KurtosisBox.IsEnabled = false; }

            plotView.Model = new PlotModel();//ColorSet.BackColorAltBrush
            
            //DrawDevideLine();
            DrawGraph("Original");
            DrawBase();
            DrawNew();
            
            plotView.Model.TextColor = Color(ColorSet.ForeColor);
            plotView.Model.PlotAreaBorderColor = Color(ColorSet.ForeColorAlt);
            //plotView.InvalidateVisual();

        }
        /// <summary>
        /// Read necessary setting values and start audio controller and all tasks
        /// </summary>
        private void StartApp()
        {
            Wale.Transformation.SetBaseLevel(settings.TargetLevel);
            Audio = new AudioControl(settings.TargetLevel, DL);
            while (Audio.MasterVolume == -1)
            {
                Audio.Dispose();
                Audio = new AudioControl(settings.TargetLevel, DL);
            }
            Audio.Start(audioDebug);
            //Audio.AutoControl = Properties.Settings.Default.autoControl;
            //UpdateConnectTask();
            _updateTasks = new List<Task>();
            _updateTasks.Add(new Task(CheckFirstLoadTask));
            _updateTasks.Add(new Task(UpdateStateTask));
            _updateTasks.Add(new Task(UpdateVolumeTask));
            _updateTasks.Add(new Task(UpdateSessionTask));
            _updateTasks.ForEach(t => t.Start());
            Log("OK2");
        }
        #endregion


        #region Update Tasks
        //Check First Load.
        private void CheckFirstLoadTask()
        {
            Log("Start CheckFirstLoadTask");
            while (!Rclose && FirstLoad)
            {
                if (this.Visibility == Visibility.Visible) CheckFirstLoad();
                else FirstLoad = false;
                System.Threading.Thread.Sleep(new TimeSpan((long)(settings.AutoControlInterval * 10000)));
            }
            Log("End CheckFirstLoadTask");
        }
        private void CheckFirstLoad()
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(new WindowConsumer(CheckFirstLoad));  // invoking itself
                }
                else
                {
                    this.Visibility = Visibility.Hidden;
                }
            }
            catch { DP.DMML($"fail to invoke CheckFirstLoad"); }
        }
        //Check device state.
        private void UpdateStateTask()
        {
            Log("Start UpdateStateTask");
            bool On = true;
            //Console.WriteLine($"V={Audio.MasterVolume}, I={NI.Icon.Equals(Properties.Resources.WaleLeftOn)}");
            while (!Rclose)
            {
                //Console.WriteLine($"V={Audio.MasterVolume}, I={NI.Icon.GetHashCode()}");
                //Console.WriteLine("USTask");
                if (Audio.MasterVolume < 0 && On)
                {
                    //Console.WriteLine("InsideErrorState");
                    On = false;
                    NI.Icon = Properties.Resources.WaleRightOff;
                    //MessageBox.Show("IconOff");
                }
                else if (Audio.MasterPeak >= 0 && !On)
                {
                    //Console.WriteLine("InsideGoodState");
                    On = true;
                    NI.Icon = Properties.Resources.WaleLeftOn;
                    //MessageBox.Show("IconOn");
                }

                System.Threading.Thread.Sleep(new TimeSpan((long)(settings.UIUpdateInterval * 10000)));
            }
            Log("End UpdateStateTask");
        }
        /// <summary>
        /// Update task for master output level of a device is currently used
        /// </summary>
        private void UpdateVolumeTask()
        {
            Log("Start UpdateVolumeTask");
            while (!Rclose)
            {
                //Task wait = Task.Delay(settings.UIUpdateInterval);
                //if (updateVolumeDelay > 0) wait.Start();
                if (Active)
                {
                    JDPack.DebugPack VDP = new JDPack.DebugPack(updateVolumeDebug);
                    VDP.DMML($"base={settings.TargetLevel} vol={Audio.MasterVolume}({Audio.MasterPeak})");

                    //lBaseVolume.Text = $"{Properties.Settings.Default.baseVolume:n}";
                    //pbBaseVolume.Increment((int)(Properties.Settings.Default.baseVolume * 100) - pbBaseVolume.Value);
                    
                    double vbuf = /*(Audio.MasterVolume / settings.BaseLevel) */ Audio.MasterVolume;// Console.WriteLine($"{vbuf}");
                    //SetBar(MasterVolumeBar, vbuf);
                    DL.MasterVolume = vbuf;

                    double lbuf = (Audio.MasterVolume / settings.TargetLevel) * Audio.MasterPeak;// Console.WriteLine($"{lbuf}");
                    //SetBar(MasterPeakBar, lbuf);
                    DL.MasterPeak = lbuf;

                    /*if (NTV())*/ SetText(MasterLabel, $"{vbuf:n3}");//Transformation.Transform(Audio.MasterVolume, Transformation.TransFlow.MachineToUser)
                    /*else*/ SetText(MasterPeakLabel, $"{lbuf:n3}");
                    //Console.WriteLine($"{DL.MasterVolume:n3} {DL.MasterPeak:n3}");
                    //if (NTV()) lVolume.Text = $"{volume:n}";
                    //else lVolume.Text = $"{Audio.MasterPeak * volume:n}";
                    //pbMasterVolume.Increment((int)(Audio.MasterVolume * 100) - pbMasterVolume.Value);
                    //pbMasterLevel.Increment((int)(Audio.MasterPeak * volume * 100) - pbMasterLevel.Value);
                }
                //if (updateVolumeDelay > 0) await wait;
                //bAllowPaintMaster = true;
                System.Threading.Thread.Sleep(new TimeSpan((long)(settings.UIUpdateInterval * 10000)));
                //await wait;
            }
            Log("End UpdateVolumeTask");
        }
        //making UIs for all sessions.
        /// <summary>
        /// Update task to make UIs of all sessions
        /// </summary>
        private void UpdateSessionTask()
        {
            Log("Start UpdateSessionTask");
            Dispatcher.Invoke(() => { SessionGrid.Children.Clear(); });
            while (!Rclose)
            {
                //Task wait = Task.Delay(settings.UIUpdateInterval);
                //if (updateSessionDelay > 0) wait.Start();
                if (Active) //do when this.activated
                {
                    UpdateSession(SessionGrid);
                }// activated check enclosure
                 //if (updateSessionDelay > 0) await wait;
                //bAllowPaintSession = true;
                System.Threading.Thread.Sleep(new TimeSpan((long)(settings.UIUpdateInterval * 10000)));
                //await wait;
            }/**/
            Log("End UpdateSessionTask");
        }
        private void UpdateSession(Grid grid)
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(new GridConsumer(UpdateSession), new object[] { grid });  // invoking itself
                }
                else
                {
                    // the "functional part", executing only on the main thread
                    JDPack.DebugPack SDP = new JDPack.DebugPack(updateSessionDebug);
                    SDP.DMM("Getting Sessions");
                    int count = 0;
                    lock (Lockers.Sessions)
                    {
                        count = Audio.Sessions.Count; //all count
                    }
                    SDP.DMML("  Count:" + count);
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
                                    foreach (MeterSet item in SessionGrid.Children) { if (sc.PID == item.ID) found = true; }
                                    if (!found)
                                    {
                                        //SessionGrid.RowDefinitions.Add(new RowDefinition());
                                        MeterSet set = new MeterSet(sc.PID, sc.Name, settings.DetailedView, sc.AutoIncluded, updateSessionDebug);
                                        SessionGrid.Children.Add(set);
                                        //SetTabControl(SessionGrid, set);
                                        reAlign = true;
                                        Log($"New MeterSet:{sc.Name}({sc.PID})");
                                    }
                                }
                            }

                            foreach (MeterSet item in SessionGrid.Children)
                            {//check expired session and update not expired session
                                if (Audio.Sessions.GetSession(item.ID) == null) { expired.Add(item); reAlign = true; break; }
                                var session = Audio.Sessions.GetSession(item.ID);
                                if (session.State == Wale.CoreAudio.SessionState.Expired) { expired.Add(item); reAlign = true; }
                                else// if (session.Active)
                                {
                                    if (settings.DetailedView) item.DetailOn();
                                    else item.DetailOff();
                                    if (item.detailChanged) { reAlign = true; item.detailChanged = false; }
                                    item.UpdateData(session.Volume, session.Peak, session.AveragePeak, session.Name);
                                    session.Relative = (float)(item.Relative / 100.0);
                                    session.AutoIncluded = item.AutoIncluded;
                                }
                            }
                        }
                        foreach (MeterSet item in expired) { SetTabControl(SessionGrid, item, true); Log($"Remove MeterSet:{item.SessionName}({item.ID})"); } //remove expired session as meterset from tabSession.controls
                        expired.Clear(); //clear expire buffer
                                         //realign when there are one or more new set or removed set.
                        if (reAlign)
                        {//re-align when there is(are) added or removed session(s)
                            Log("Re-aligning");
                            double spacing = AppDatas.SessionBlockHeightNormal, lastHeight = this.Height;
                            if (settings.DetailedView) { spacing = AppDatas.SessionBlockHeightDetail; }
                            //spacing += 0;
                            for (int i = 0; i < SessionGrid.Children.Count; i++)
                            {
                                MeterSet s = SessionGrid.Children[i] as MeterSet;
                                s.UpdateLocation(new Thickness(0, spacing * i, 0, 0));
                                SDP.DMML($"MeterSet{s.ID,-5} {s.Margin} {s.Height} {s.SessionName}");
                            }
                            double fsgHeight = (double)(SessionGrid.Children.Count) * spacing + 60 + 2, dif = fsgHeight - lastHeight;
                            if (fsgHeight < this.MinHeight) { fsgHeight = AppDatas.MainWindowHeightDefault; dif = fsgHeight - lastHeight; }
                            //Console.WriteLine($"fsgH:{fsgHeight},DF:{dif}");
                            this.Height = fsgHeight;
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
        private void OnProgramShutdown(object sender, EventArgs e) { OnProgramShutdown(sender, new RoutedEventArgs()); }
        private async void OnProgramShutdown(object sender, RoutedEventArgs e)
        {
            MessageBoxResult dialogResult = MessageBox.Show("Are you sure to terminate Wale completely?", "Exit", MessageBoxButton.OKCancel);
            if (dialogResult == MessageBoxResult.OK)
            {
                DP.DMML("Exit");
                Active = false;
                Rclose = true;
                NI.Visible = false;
                NI.Dispose();
                await Task.WhenAll(_updateTasks);
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
            Configuration form = new Configuration() { Icon = this.Icon };
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
                Transformation.SetBaseLevel(settings.TargetLevel);
                Audio.SetBaseTo(settings.TargetLevel);
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
            System.Diagnostics.Process.Start("explorer", JDPack.FileLog.WorkDirectory.FullName);
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
                Audio.SetVolume((double)volume);
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
            double? buf = Transformation.Transform(it, Transformation.TransFlow.IntervalUserToMachine);
            if (buf != null) it = (double)buf;
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
            settings.Save();
        }
        #endregion

        #region NI events
        private void NI_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left) { DP.DMML("IconLeftClick"); Active = true; this.Visibility = Visibility.Visible; this.Activate(); }
        }
        #endregion

        #region Window Events

        private void DevView_CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if(Dev) DL.ACDebugShow = DL.ACDebugShow == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
        }
        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!loaded) return;
            DrawBase();
            DrawNew();
            if (settings.AutoControlInterval != LastValues.AutoControlInterval || settings.AverageTime != LastValues.AverageTime)
            {
                Audio?.UpdateAverageParam();
                JDPack.FileLog.Log($"Update Avr Param {settings.AverageTime:n3}ms({settings.AverageTime / settings.AutoControlInterval:n0}), {settings.AutoControlInterval:n3}ms");
                DL.ACHz = 1.0 / (2.0 * settings.AutoControlInterval / 1000.0);
                DL.ACAvCnt = settings.AverageTime / settings.AutoControlInterval;
            }
        }

        private void window_Deactivated(object sender, EventArgs e)
        {
            if (!settings.StayOn)
            {
                Active = false;
                this.Visibility = Visibility.Hidden;
            }
        }
        private void Window_LostFocus(object sender, RoutedEventArgs e)
        {
            //if (!settings.StayOn)
            //{
                //Active(false);
                //this.Visibility = Visibility.Hidden;
            //}
        }
        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            //if (!settings.StayOn)
            //{
                //Console.WriteLine($"X:{e.GetPosition(this).X}({this.ActualWidth}),Y:{e.GetPosition(this).Y}({this.ActualHeight})");
                //double crit = 10;
                //if (e.GetPosition(this).X <= crit || e.GetPosition(this).X >= this.Width - crit - 15 || e.GetPosition(this).Y <= crit || e.GetPosition(this).Y >= this.Height - crit - 15)
                //{
                    //Active(false);
                    //this.Visibility = Visibility.Hidden;
                //}
            //}
        }
        private void MasterLabel_Click(object sender, MouseButtonEventArgs e)
        {
            bool now = NTV;
            NTV = !now;
            if (NTV) MasterLabel.Foreground = ColorSet.MainColorBrush;//SetForeColor(lVolume, Color.FromArgb(224, 224, 224));
            else MasterLabel.Foreground = ColorSet.PeakColorBrush;//SetForeColor(lVolume, Color.PaleVioletRed);
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
            RemakeConf();
            lastHeightForConfigTab = this.Height;
            heightDeffForConfigTab = 450 - this.Height;
            //Dispatcher.Invoke(new Action(() =>
            //{
                this.Height = 450;
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
            foreach (TabItem tab in (sender as TabControl).Items)
            {
                if (tab.IsSelected)
                {
                    if (tab.Header.ToString().Contains("Config") && !nowConfig) { ConfigTab_GotFocus(sender, e); nowConfig = true; }
                    else if (!tab.Header.ToString().Contains("Config") && nowConfig) { ConfigTab_LostFocus(sender, e); nowConfig = false; }
                    if (tab.Header.ToString().Contains("Log")) { LogScroll.ScrollToEnd(); }
                }
            }
        }

        private void RemakeConf() {
            Makes();
            MakeOriginals();
            DrawGraph("Original");
            DrawNew();
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
                case "High": ppc = ProcessPriorityClass.High; settings.ProcessPriorityHigh = true;break; settings.ProcessPriorityAboveNormal = false; settings.ProcessPriorityNormal = false; break;
                case "Above Normal": ppc = ProcessPriorityClass.AboveNormal; settings.ProcessPriorityHigh = false; settings.ProcessPriorityAboveNormal = true;break; settings.ProcessPriorityNormal = false; break;
                case "Normal": ppc = ProcessPriorityClass.Normal; settings.ProcessPriorityHigh = false; settings.ProcessPriorityAboveNormal = false; settings.ProcessPriorityNormal = true; break;
            }JDPack.Debug.CML($"SPP H={settings.ProcessPriorityHigh} A={settings.ProcessPriorityAboveNormal} N={settings.ProcessPriorityNormal}");
            //settings.Save();
            using (Process p = Process.GetCurrentProcess())
            {
                p.PriorityClass = ppc;
            }
        }
        #endregion

        #region Config Events

        private void TargetLevel_Changed(object sender, TextChangedEventArgs e)
        {
            if (!loaded) return;
            DrawBase();
            DrawNew();
        }
        private void UpRate_Changed(object sender, TextChangedEventArgs e)
        {
            if (!loaded) return;
            DrawNew();
        }
        private void Kurtosis_Changed(object sender, TextChangedEventArgs e)
        {
            if (!loaded) return;
            DrawNew();
        }
        private void Function_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!loaded) return;
            string selectedFunction = (sender as ComboBox).SelectedItem.ToString();
            //Console.WriteLine($"Fnc Chg{selectedFunction}");
            //if (selectedFunction == VFunction.Func.None.ToString()) { textBox5.Enabled = false; }
            //else { textBox5.Enabled = true; }
            if (selectedFunction == VFunction.Func.Reciprocal.ToString() || selectedFunction == VFunction.Func.FixedReciprocal.ToString()) { KurtosisBox.IsEnabled = true; }
            else { KurtosisBox.IsEnabled = false; }
            DrawNew();
        }

        private void resetToDafault_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult dr = MessageBox.Show(this, "Do you really want to reset all configurations?", "Warning", MessageBoxButton.YesNo);
            if (dr == MessageBoxResult.Yes)
            {
                settings.Reset();
                Makes();
                JDPack.FileLog.Log("All configs are reset.");
            }
        }
        private async void ConfigSave_Click(object sender, RoutedEventArgs e)
        {
            //this.IsEnabled = false;
            //this.Topmost = false;
            //this.WindowState = WindowState.Minimized;
            if (Converts() && await Register())
            {
                settings.Save();
                Transformation.SetBaseLevel(settings.TargetLevel);
                Audio.SetBaseTo(settings.TargetLevel);
                Audio.UpRate = settings.UpRate;
                MakeOriginals();
                //this.DialogResult = true;
                //Close();
            }
            else { MessageBox.Show("Can not save Changes", "ERROR"); }
            //this.IsEnabled = true;
            //Binding topmostBinding = new Binding();
            //topmostBinding.Source = settings.AlwaysTop;
            //BindingOperations.SetBinding(this, Window.TopmostProperty, topmostBinding);
            //this.WindowState = WindowState.Normal;
        }
        private bool Converts()
        {
            //System.Diagnostics.Stopwatch st = new System.Diagnostics.Stopwatch();
            //Console.WriteLine("Convert");
            bool success = true, auto = settings.AutoControl;
            settings.AutoControl = false;
            try
            {
                //settings.UIUpdateInterval = Convert.ToInt16(textBox1.Text);
                //settings.AutoControlInterval = Convert.ToInt16(textBox2.Text);
                //settings.GCInterval = Convert.ToInt16(textBox3.Text);
                //settings.BaseLevel = Convert.ToDouble(textBox4.Text);
                //settings.UpRate = Convert.ToDouble(textBox5.Text);
                //settings.Kurtosis = Convert.ToDouble(textBox6.Text);
                //settings.AverageTime = Convert.ToDouble(textBox7.Text) * 1000;
                //settings.MinPeak = Convert.ToDouble(textBox8.Text);
                //settings.VFunc = comboBox1.SelectedValue.ToString();
            }
            catch { success = false; JDPack.FileLog.Log("Error: Config - Convert failure"); }
            finally { settings.AutoControl = auto; }
            //Console.WriteLine("Convert End");
            return success;
        }
        private async Task<bool> Register()
        {
            //Console.WriteLine("Resister");
            bool success = true;
            try
            {
                if (runAtWindowsStartup.IsChecked.Value)
                {
                    // Add the value in the registry so that the application runs at startup
                    if (rkApp.GetValue("WALEWindowAudioLoudnessEqualizer") == null)
                    {
                        await Task.Run(() => { rkApp.SetValue("WALEWindowAudioLoudnessEqualizer", System.Reflection.Assembly.GetExecutingAssembly().Location); });
                    }
                }
                else
                {
                    // Remove the value from the registry so that the application doesn't start
                    if (rkApp.GetValue("WALEWindowAudioLoudnessEqualizer") != null)
                    {
                        rkApp.DeleteValue("WALEWindowAudioLoudnessEqualizer", false);
                    }
                }
            }
            catch { success = false; JDPack.FileLog.Log("Error: Config - Register failure"); }
            //Console.WriteLine("resister End");
            return success;
        }


        #endregion

        #region Drawing
        private void Makes()
        {
            if (rkApp.GetValue("WALEWindowAudioLoudnessEqualizer") == null)
            {
                // The value doesn't exist, the application is not set to run at startup
                runAtWindowsStartup.IsChecked = false;
            }
            else
            {
                // The value exists, the application is set to run at startup
                runAtWindowsStartup.IsChecked = true;
            }

            FunctionSelector.ItemsSource = Enum.GetValues(typeof(VFunction.Func));
            if (Enum.TryParse(settings.VFunc, out VFunction.Func f)) FunctionSelector.SelectedItem = f;
        }
        private void MakeOriginals()
        {
            LastValues.UIUpdate = settings.UIUpdateInterval;
            LastValues.AutoControlInterval = settings.AutoControlInterval;
            LastValues.GCInterval = settings.GCInterval;
            LastValues.TargetLevel = settings.TargetLevel;
            LastValues.UpRate = settings.UpRate;
            LastValues.Kurtosis = settings.Kurtosis;
            LastValues.AverageTime = settings.AverageTime;
            LastValues.MinPeak = settings.MinPeak;
            LastValues.VFunc = settings.VFunc;
        }
        private void DrawNew() { DrawGraph("Graph"); }
        private void DrawGraph(string graphName)
        {
            List<Series> exc = new List<Series>();
            foreach (Series s in plotView.Model.Series) { if (s.Title == graphName) exc.Add(s); }
            foreach (Series s in exc) { plotView.Model.Series.Remove(s); }
            exc.Clear();

            VFunction.Func f;
            Enum.TryParse<VFunction.Func>(FunctionSelector.SelectedItem.ToString(), out f);

            Series exclude = null;
            foreach (var g in plotView.Model.Series) { if (g.Title == graphName) exclude = g; }
            if (exclude != null) { plotView.Model.Series.Remove(exclude); /*Console.WriteLine("Plot removed");*/ }

            FunctionSeries graph;

            switch (f)
            {
                case VFunction.Func.Linear:
                    graph = new FunctionSeries(new Func<double, double>((x) => {
                        double res = VFunction.Linear(x, settings.UpRate);// * 1000 / settings.AutoControlInterval;
                        //if (res > 1) { res = 1; } else if (res < 0) { res = 0; }
                        return res;
                    }), 0, 1, 0.05, graphName);
                    break;
                case VFunction.Func.SlicedLinear:
                    VFunction.FactorsForSlicedLinear sliceFactors = VFunction.GetFactorsForSlicedLinear(settings.UpRate, settings.TargetLevel);
                    graph = new FunctionSeries(new Func<double, double>((x) => {
                        double res = VFunction.SlicedLinear(x, settings.UpRate, settings.TargetLevel, sliceFactors.A, sliceFactors.B);// * 1000 / settings.AutoControlInterval;
                        //if (res > 1) { res = 1; } else if (res < 0) { res = 0; }
                        return res;
                    }), 0, 1, 0.05, graphName);
                    break;
                case VFunction.Func.Reciprocal:
                    graph = new FunctionSeries(new Func<double, double>((x) => {
                        double res = VFunction.Reciprocal(x, settings.UpRate, settings.Kurtosis);// * 1000 / settings.AutoControlInterval;
                        //if (res > 1) { res = 1; } else if (res < 0) { res = 0; }
                        return res;
                    }), 0, 1, 0.05, graphName);
                    break;
                case VFunction.Func.FixedReciprocal:
                    graph = new FunctionSeries(new Func<double, double>((x) => {
                        double res = VFunction.FixedReciprocal(x, settings.UpRate, settings.Kurtosis);// * 1000 / settings.AutoControlInterval;
                        //if (res > 1) { res = 1; } else if (res < 0) { res = 0; }
                        return res;
                    }), 0, 1, 0.05, graphName);
                    break;
                default:
                    graph = new FunctionSeries(new Func<double, double>((x) => {
                        double res = settings.UpRate;// * 1000 / settings.AutoControlInterval;
                        //if (res > 1) { res = 1; } else if (res < 0) { res = 0; }
                        return res;
                    }), 0, 1, 0.05, graphName);
                    break;
            }

            double maxVal = 0, yScale = 1;
            if (graph.Title == "Original")
            {
                graph.Color = Color(ColorSet.MainColor);
                originalMax = graph.MaxY;
            }
            else
            {
                graph.Color = Color(ColorSet.PeakColor);
                maxVal = Math.Max(graph.MaxY, originalMax);
            }

            plotView.Model.Series.Add(graph);
            plotView.Model.InvalidatePlot(true);

            for (double i = 1.0; i > 0.00001; i /= 2)
            {
                if (maxVal > i) { yScale = i * 2.0; break; }
            }
            //plotView.Model.DefaultYAxis.AbsoluteMaximum = yScale;
            //foreach (var g in plotView.Model.Series) { }
            //plotView.ZoomAllAxes(yScale);
            //if (maxVal > 0.01) chart.ChartAreas["Area"].AxisY.LabelStyle.Format = "G";
            //else chart.ChartAreas["Area"].AxisY.LabelStyle.Format = "#.#E0";
            //myText1.Y = chart.ChartAreas["Area"].AxisY.Maximum;
            //myText2.Y = chart.ChartAreas["Area"].AxisY.Maximum * 0.92;
            plotView.InvalidatePlot();
        }
        private void DrawBase()
        {
            List<Series> exc = new List<Series>();
            foreach (Series s in plotView.Model.Series) { if (s.Title == "Base") exc.Add(s); }
            foreach (Series s in exc) { plotView.Model.Series.Remove(s); }
            exc.Clear();

            LineSeries lineSeries1 = new LineSeries();
            lineSeries1.Title = "Base";
            lineSeries1.Points.Add(new DataPoint(settings.TargetLevel, 0));
            lineSeries1.Points.Add(new DataPoint(settings.TargetLevel, 1));
            lineSeries1.Color = Color(ColorSet.TargetColor);
            plotView.Model.Series.Add(lineSeries1);
            plotView.InvalidatePlot();
        }
        private void DrawDevideLine()
        {
            LineSeries lineSeries1 = new LineSeries();
            lineSeries1.Points.Add(new DataPoint(0, 0));
            lineSeries1.Points.Add(new DataPoint(1, 1));
            lineSeries1.Color = Color(ColorSet.ForeColor);
            plotView.Model.Series.Add(lineSeries1);
            plotView.InvalidatePlot();
        }
        private OxyColor Color(Color color) { return OxyColor.FromArgb(color.A, color.R, color.G, color.B); }
        #endregion

        #region title panel control, location and size check events
        private Point titlePosition;
        private void titlePanel_MouseDown(object sender, MouseButtonEventArgs e) { titlePosition = e.GetPosition(this); }
        private void titlePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point loc = PointToScreen(e.GetPosition(this));
                //MessageBox.Show($"L={Screen.PrimaryScreen.WorkingArea.Left} R={Screen.PrimaryScreen.WorkingArea.Right}, T={Screen.PrimaryScreen.WorkingArea.Top} B={Screen.PrimaryScreen.WorkingArea.Bottom}");
                //Console.WriteLine($"W:{Left},M:{loc.X},LM:{titlePosition.X},SW:{System.Windows.SystemParameters.PrimaryScreenWidth}");
                double x = loc.X - titlePosition.X;
                if (x + this.Width >= System.Windows.SystemParameters.WorkArea.Width) x = System.Windows.SystemParameters.WorkArea.Width - this.Width;
                else if (x <= 0) x = 0;

                double y = loc.Y - titlePosition.Y;
                if (y + this.Height >= System.Windows.SystemParameters.WorkArea.Height) y = System.Windows.SystemParameters.WorkArea.Height - this.Height;
                else if (y <= 0) y = 0;
                //MessageBox.Show($"x={x} y={y}");
                Left = x;
                Top = y;
            }
        }
        private void Window_LocationAndSizeChanged(object sender, EventArgs e)
        {
            if ((this.Left + this.Width) > System.Windows.SystemParameters.WorkArea.Width)
                this.Left = System.Windows.SystemParameters.WorkArea.Width - this.Width;

            if (this.Left < System.Windows.SystemParameters.WorkArea.Left)
                this.Left = System.Windows.SystemParameters.WorkArea.Left;


            if ((this.Top + this.Height) > System.Windows.SystemParameters.WorkArea.Height)
                this.Top = System.Windows.SystemParameters.WorkArea.Height - this.Height;

            if (this.Top < System.Windows.SystemParameters.WorkArea.Top)
                this.Top = System.Windows.SystemParameters.WorkArea.Top;
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

        delegate void GridMeterSetConsumer(Grid control, MeterSet set, bool add);
        private void SetTabControl(Grid control, MeterSet set, bool remove = false)
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
        private double _MasterVolume = 0;
        public double MasterVolume { get => _MasterVolume; set { _MasterVolume = value; Notify("MasterVolume"); } }
        
        private double _MasterPeak = 0;
        public double MasterPeak { get => _MasterPeak; set { _MasterPeak = value; Notify("MasterPeak"); } }


        private Visibility _ACDebugShow = Visibility.Hidden;
        public Visibility ACDebugShow { get => _ACDebugShow; set { _ACDebugShow = value; Notify("ACDebugShow"); } }
        private double _ACElapsed = 0;
        public double ACElapsed { get => _ACElapsed; set { _ACElapsed = Math.Round(value, 3); Notify("ACElapsed"); } }
        private double _ACWaited = 0;
        public double ACWaited { get => _ACWaited; set { _ACWaited = Math.Round(value, 3); Notify("ACWaited"); } }
        private double _ACEWdif = 0;
        public double ACEWdif { get => _ACEWdif; set { _ACEWdif = Math.Round(value, 3); Notify("ACEWdif"); } }

        private double _ACAvCnt = 0;
        public double ACAvCnt { get => _ACAvCnt; set { _ACAvCnt = Math.Round(value, 0); Notify("ACAvCnt"); } }
        private double _ACHz = 0;
        public double ACHz { get => _ACHz; set { _ACHz = Math.Round(value, 2); Notify("ACHz"); } }


        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string Name) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name)); }
    }
}
