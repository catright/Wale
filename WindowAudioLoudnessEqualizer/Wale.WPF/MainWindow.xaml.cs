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

namespace Wale.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables
        Wale.WPF.Properties.Settings settings = Wale.WPF.Properties.Settings.Default;
        System.Windows.Forms.NotifyIcon NI;
        AudioControl Audio;
        JDPack.DebugPack DP;
        List<Task> _updateTasks;
        bool debug = false, mouseWheelDebug = false, audioDebug = false, updateVolumeDebug = false, updateSessionDebug = false;
        object _closelock = new object(), _activelock = new object(), _ntvlock = new object();
        volatile bool FirstLoad = true, _realClose = false, _activated = false, _numberToVol = true;
        #endregion

        #region initializing
        public MainWindow()
        {
            InitializeComponent();
            MakeComponents();
            StartApp();
            Log("AppStarted");
        }
        private void MakeComponents()
        {
            //this.Visibility = Visibility.Hidden;
            NI = new System.Windows.Forms.NotifyIcon();
            NI.Text = "Wale";
            NI.Icon = Properties.Resources.WaleLeftOn;
            NI.Visible = true;

            List<System.Windows.Forms.MenuItem> items = new List<System.Windows.Forms.MenuItem>();
            items.Add(new System.Windows.Forms.MenuItem("Configuration",ConfigToolStripMenuItem_Click));
            items.Add(new System.Windows.Forms.MenuItem("Device Map", deviceMapToolStripMenuItem_Click));
            items.Add(new System.Windows.Forms.MenuItem("Open Log", openLogDirectoryToolStripMenuItem_Click));
            items.Add(new System.Windows.Forms.MenuItem("-"));
            items.Add(new System.Windows.Forms.MenuItem("Help", helpToolStripMenuItem_Click));
            items.Add(new System.Windows.Forms.MenuItem("License", licensesToolStripMenuItem_Click));
            items.Add(new System.Windows.Forms.MenuItem("Exit", OnProgramShutdown));
            NI.ContextMenu = new System.Windows.Forms.ContextMenu(items.ToArray());

            this.NI.MouseClick += new System.Windows.Forms.MouseEventHandler(NI_MouseClick);

            Left = System.Windows.SystemParameters.WorkArea.Width - this.Width;
            Top = System.Windows.SystemParameters.WorkArea.Height - this.Height;
            DP = new JDPack.DebugPack(debug);
            Log("OK1"); DP.DML("OK1");
        }
        private void StartApp()
        {
            Wale.Transformation.SetBaseLevel(settings.BaseLevel);
            Audio = new AudioControl(settings.BaseLevel);
            while (Audio.MasterVolume == -1)
            {
                Audio.Dispose();
                Audio = new AudioControl(settings.BaseLevel);
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
            Log("OK2"); DP.DML("OK2");
        }
        #endregion


        #region Update Tasks
        //Check First Load.
        private void CheckFirstLoadTask()
        {
            Log("Start CheckFirstLoadTask");
            while (!Rclose() && FirstLoad)
            {
                if (this.Visibility == Visibility.Visible) CheckFirstLoad();
                else FirstLoad = false;
                System.Threading.Thread.Sleep(settings.AutoControlInterval);
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
            catch { DP.CML($"fail to invoke CheckFirstLoad"); }
        }
        //Check device state.
        private void UpdateStateTask()
        {
            Log("Start UpdateStateTask");
            bool On = true;
            //Console.WriteLine($"V={Audio.MasterVolume}, I={NI.Icon.Equals(Properties.Resources.WaleLeftOn)}");
            while (!Rclose())
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

                System.Threading.Thread.Sleep(settings.UIUpdateInterval);
            }
            Log("End UpdateStateTask");
        }
        //Device master volume update.
        private void UpdateVolumeTask()
        {
            Log("Start UpdateVolumeTask");
            while (!Rclose())
            {
                //Task wait = Task.Delay(settings.UIUpdateInterval);
                //if (updateVolumeDelay > 0) wait.Start();
                if (Active())
                {
                    JDPack.DebugPack VDP = new JDPack.DebugPack(updateVolumeDebug);
                    VDP.DML($"base={settings.BaseLevel} vol={Audio.MasterVolume}({Audio.MasterPeak})");

                    //lBaseVolume.Text = $"{Properties.Settings.Default.baseVolume:n}";
                    //pbBaseVolume.Increment((int)(Properties.Settings.Default.baseVolume * 100) - pbBaseVolume.Value);
                    
                    double vbuf = /*(Audio.MasterVolume / settings.BaseLevel) */ Audio.MasterVolume;// Console.WriteLine($"{vbuf}");
                    SetBar(MasterVolumeBar, vbuf);

                    double lbuf = (Audio.MasterVolume / settings.BaseLevel) * Audio.MasterPeak;// Console.WriteLine($"{lbuf}");
                    SetBar(MasterPeakBar, lbuf);

                    /*if (NTV())*/ SetText(MasterLabel, $"{vbuf:n3}");//Transformation.Transform(Audio.MasterVolume, Transformation.TransFlow.MachineToUser)
                    /*else*/ SetText(MasterPeakLabel, $"{lbuf:n3}");

                    //if (NTV()) lVolume.Text = $"{volume:n}";
                    //else lVolume.Text = $"{Audio.MasterPeak * volume:n}";
                    //pbMasterVolume.Increment((int)(Audio.MasterVolume * 100) - pbMasterVolume.Value);
                    //pbMasterLevel.Increment((int)(Audio.MasterPeak * volume * 100) - pbMasterLevel.Value);
                }
                //if (updateVolumeDelay > 0) await wait;
                //bAllowPaintMaster = true;
                System.Threading.Thread.Sleep(settings.UIUpdateInterval);
                //await wait;
            }
            Log("End UpdateVolumeTask");
        }
        //making UIs for all sessions.
        private void UpdateSessionTask()
        {
            Log("Start UpdateSessionTask");
            while (!Rclose())
            {
                //Task wait = Task.Delay(settings.UIUpdateInterval);
                //if (updateSessionDelay > 0) wait.Start();
                if (Active()) //do when this.activated
                {
                    UpdateSession(SessionGrid);
                }// activated check enclosure
                 //if (updateSessionDelay > 0) await wait;
                //bAllowPaintSession = true;
                System.Threading.Thread.Sleep(settings.UIUpdateInterval);
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
                    SDP.DM("Getting Sessions");
                    int count = 0;
                    lock (Lockers.Sessions)
                    {
                        count = Audio.Sessions.Count; //all count
                    }
                    SDP.DML("  Count:" + count);
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
                                    session.Relative = (float)item.Relative;
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
                            double spacing = 24, lastHeight = this.Height;
                            if (settings.DetailedView) spacing = 36;
                            spacing += 0;
                            for (int i = 0; i < SessionGrid.Children.Count; i++)
                            {
                                MeterSet s = SessionGrid.Children[i] as MeterSet;
                                s.UpdateLocation(new Thickness(0, spacing * i, 0, 0));
                                SDP.DML($"MeterSet{s.ID,-5} {s.Margin} {s.Height} {s.SessionName}");
                            }
                            double fsgHeight = (double)(SessionGrid.Children.Count) * spacing + 72 + 2, dif = fsgHeight - lastHeight;
                            if (fsgHeight < this.MinHeight) { fsgHeight = 200; dif = fsgHeight - lastHeight; }
                            //Console.WriteLine($"fsgH:{fsgHeight},DF:{dif}");
                            this.Height = fsgHeight;
                            this.Top -= dif;
                            //Console.WriteLine($"WH:{this.Height},({SystemParameters.WorkArea.Width},{SystemParameters.WorkArea.Height})");
                            Log("Re-aligned");
                        }
                    }//count check enclosure

                }
            }
            catch { DP.CML($"fail to invoke UpdateSession"); }
        }
        #endregion



        #region flag control methods
        private bool Rclose() { bool val; lock (_closelock) { val = _realClose; } return val; }
        private void Rclose(bool val) { lock (_closelock) { _realClose = val; } }
        private bool Active() { bool val; lock (_activelock) { val = _activated; } return val; }
        private void Active(bool val) { lock (_activelock) { _activated = val; } }
        private bool NTV() { bool val; lock (_ntvlock) { val = _numberToVol; } return val; }
        private void NTV(bool val) { lock (_ntvlock) { _numberToVol = val; } }
        #endregion

        #region Toolstrip menu events
        private void OnProgramShutdown(object sender, EventArgs e) { OnProgramShutdown(sender, new RoutedEventArgs()); }
        private async void OnProgramShutdown(object sender, RoutedEventArgs e)
        {
            MessageBoxResult dialogResult = MessageBox.Show("Are you sure to terminate Wale completely?", "Exit", MessageBoxButton.OKCancel);
            if (dialogResult == MessageBoxResult.OK)
            {
                DP.DML("Exit");
                Active(false);
                Rclose(true);
                NI.Visible = false;
                NI.Dispose();
                await Task.WhenAll(_updateTasks);
                this.Close();
                Audio.Dispose();
                Log("Closed"); DP.DML("Closed");
            }
        }

        private void ConfigToolStripMenuItem_Click(object sender, EventArgs e) { ConfigToolStripMenuItem_Click(sender, new RoutedEventArgs()); }
        private void ConfigToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DP.DM("Settings");
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
                Transformation.SetBaseLevel(settings.BaseLevel);
                Audio.SetBaseTo(settings.BaseLevel);
                Audio.UpRate = settings.UpRate;
            }
            form.Closed -= Config_FormClosed;
        }

        private void deviceMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DP.DM("DeviceMap");
            JDPack.FormPack FWP = new JDPack.FormPack();
            DeviceMap form = new DeviceMap() { Icon = this.Icon };
            System.Drawing.Point p = FWP.PointFromMouse(-(int)(form.Width / 2), -(int)form.Height, JDPack.FormPack.PointMode.AboveTaskbar);
            form.Left = p.X;
            form.Top = p.Y;
            form.ShowDialog();
        }
        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DP.DM("Help");
            JDPack.FormPack FWP = new JDPack.FormPack();
            Help form = new Help() { Icon = this.Icon };
            System.Drawing.Point p = FWP.PointFromMouse(-(int)(form.Width / 2), -(int)form.Height, JDPack.FormPack.PointMode.AboveTaskbar);
            form.Left = p.X;
            form.Top = p.Y;
            form.ShowDialog();
        }
        private void licensesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DP.DM("Licenses");
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
            DP.DM("Up ");
            GetInterval();
            updateVolumeDebug = true;
            Audio.VolumeUp(CalcInterval());
            updateVolumeDebug = false;
        }
        private void Down_Click(object sender, EventArgs e)
        {
            DP.DM("Down ");
            GetInterval();
            updateVolumeDebug = true;
            Audio.VolumeDown(CalcInterval());
            updateVolumeDebug = false;
        }
        private void VolumeSet_Click(object sender, EventArgs e)
        {
            DP.DM("Set ");
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
            DP.DM(" CalcInterval");
            double it = settings.MasterVolumeInterval;
            double? buf = Transformation.Transform(it, Transformation.TransFlow.IntervalUserToMachine);
            if (buf != null) it = (double)buf;
            DP.DM($"={it} ");
            return it;
        }
        private void GetInterval()
        {
            DP.DM(" GetInterval");
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
        #endregion

        #region NI events
        private void NI_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left) { DP.DML("IconLeftClick"); Active(true); this.Visibility = Visibility.Visible; this.Activate(); }
        }
        #endregion

        #region Window Events
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
            if (!settings.StayOn)
            {
                //Console.WriteLine($"X:{e.GetPosition(this).X}({this.ActualWidth}),Y:{e.GetPosition(this).Y}({this.ActualHeight})");
                double crit = 10;
                if (e.GetPosition(this).X <= crit || e.GetPosition(this).X >= this.Width - crit - 15 || e.GetPosition(this).Y <= crit || e.GetPosition(this).Y >= this.Height - crit - 15)
                {
                    Active(false);
                    this.Visibility = Visibility.Hidden;
                }
            }
        }
        private void MasterLabel_Click(object sender, MouseButtonEventArgs e)
        {
            bool now = NTV();
            NTV(!now);
            if (NTV()) MasterLabel.Foreground = ColorSet.MainColorBrush;//SetForeColor(lVolume, Color.FromArgb(224, 224, 224));
            else MasterLabel.Foreground = ColorSet.PeakColorBrush;//SetForeColor(lVolume, Color.PaleVioletRed);
        }
        private void MasterTab_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (mouseWheelDebug) DP.DML($"MouseWheel Captured:{e.Delta}");
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
        private void ShiftToMasterTab(object sender, ExecutedRoutedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() => Tabs.SelectedIndex = 0));
        }
        private void ShiftToSessionTab(object sender, ExecutedRoutedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() => Tabs.SelectedIndex = 1));
        }

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
        delegate void TextBlockStringConsumer(TextBlock control, string text);
        private void AppendText(TextBlock control, string text)
        {
            try
            {
                if (control != null)
                {
                    if (!Dispatcher.CheckAccess())
                    {
                        DP.DML("Invoke Required - AppendText");
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
        private void SetText(Label control, string text)
        {
            try
            {
                if (control != null)
                {
                    if (!Dispatcher.CheckAccess())
                    {
                        DP.DML("Invoke Required - MeterSet - SetText");
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
        private void SetBar(ProgressBar control, double value)
        {
            try
            {
                if (control != null)
                {
                    if (!Dispatcher.CheckAccess())
                    {
                        DP.DML("Invoke Required - MeterSet - SetBar");
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
                        DP.DML("Invoke Required - MeterSet - SetTabControl");
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
            catch { DP.CML($"fail to invoke WindowSize"); }
        }/**/

        delegate void GridConsumer(Grid grid);//MeterSet Update Delegate
        delegate void WindowConsumer();//CheckFirstLoad Delegate

        #endregion

        #region Logging
        private void Log(string msg, bool newLine = true)
        {
            JDPack.FileLog.Log(msg, newLine);
            DateTime t = DateTime.Now.ToLocalTime();
            string content = $"{t.Hour:d2}:{t.Minute:d2}>{msg}";
            if (newLine) content += "\r\n";
            AppendText(Logs, content);
            //bAllowPaintLog = true;
        }
        private void Logs_VisibleChanged(object sender, RoutedEventArgs e)
        {
            if (Logs.IsVisible)
            {
                LogScroll.ScrollToEnd();
                //Logs.SelectionStart = Logs.TextLength;
                //Logs.ScrollToCaret();
            }
        }
        #endregion
    }
}
