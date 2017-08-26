﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Wale.Subclasses;

namespace Wale.WinForm
{//Main Form
    public partial class MainWindow : Form
    {
        #region Form method overrides
        protected override void SetVisibleCore(bool value)
        {
            if (!this.IsHandleCreated)
            {
                this.CreateHandle();
                value = false;
            }
            base.SetVisibleCore(value);
        }/**/
        protected override void OnMouseLeave(EventArgs e)
        {
            if (this.ClientRectangle.Contains(this.PointToClient(Control.MousePosition)))
                return;
            else
            {
                base.OnMouseLeave(e);
            }
        }/**/
        #endregion

        #region Private Variables
        Wale.Properties.Settings settings = Wale.Properties.Settings.Default;
        AudioControl Audio;
        JLdebPack.DebugPackage DP;
        JLdebPack.FormWindowPackage FWP;
        List<Task> _updateTasks;
        object _closelock = new object(), _activelock = new object(), _ntvlock = new object();
        volatile bool _realClose = false, _activated = false, _numberToVol = true;
        bool debug = false, mouseWheelDebug = false, audioDebug = false, updateVolumeDebug = false, updateSessionDebug = false;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            MakeComponents();
            StartApp();
            Log("App Started");
        }

        #region initializing
        private void MakeComponents()
        {
            Point p = new Point();
            p.X = (int)(Screen.PrimaryScreen.WorkingArea.Width - Width);
            p.Y = (int)(Screen.PrimaryScreen.WorkingArea.Height - Height);
            Location = p;
            KeyPreview = true;
            cmsAutoControl.Text = (settings.AutoControl) ? "&AutoControl(On)" : "&AutoControl(Off)";
            tbInterval.Text = settings.MasterVolumeInterval.ToString();
            tabSession.AutoScroll = true;
            this.TopMost = cbAlwaysTop.Checked;
            this.MouseWheel += MainWindow_MouseWheel;
            settings.PropertyChanged += Settings_PropertyChanged;
            DP = new JLdebPack.DebugPackage(debug);
            FWP = new JLdebPack.FormWindowPackage();
            Log("OK1"); DP.DML("OK1");
        }
        private void StartApp()
        {
            Transformation.SetBaseVolume(settings.BaseVolume);
            Audio = new AudioControl(settings.BaseVolume);
            Audio.Start(audioDebug);
            //Audio.AutoControl = Properties.Settings.Default.autoControl;
            //UpdateConnectTask();
            _updateTasks = new List<Task>();
            _updateTasks.Add(new Task(UpdateVolumeTask));
            _updateTasks.Add(new Task(UpdateSessionTask));
            _updateTasks.ForEach(t => t.Start());
            Log("OK2"); DP.DML("OK2");
        }
        #endregion



        #region MainWindow and NI events except closing
        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "StayOn" || e.PropertyName == "AlwaysTop") { settings.Save(); }
        }
        private void MainWindow_Load(object sender, EventArgs e)
        {
            DP.DM("Load ");
            Hide();
            Active(false);

            //typeof(Panel).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, this, new object[] { true });

            int style = NativeMethods.GetWindowLong(this.Handle, NativeMethods.GWL_EXSTYLE);
            style |= NativeMethods.WS_EX_COMPOSITED;
            NativeMethods.SetWindowLong(this.Handle, NativeMethods.GWL_EXSTYLE, style);
            /**/
            DP.DML("OK3");
            Log("MainWindow Loaded");
        }
        private void lVolume_Click(object sender, EventArgs e)
        {
            bool now = NTV();
            NTV(!now);
            if (NTV()) lVolume.ForeColor = Color.SteelBlue;//SetForeColor(lVolume, Color.FromArgb(224, 224, 224));
            else lVolume.ForeColor = Color.PaleVioletRed;//SetForeColor(lVolume, Color.PaleVioletRed);
        }
        //Main user input control
        private void MainWindow_MouseWheel(object sender, MouseEventArgs e)
        {
            if (mouseWheelDebug) DP.DML($"MouseWheel Captured:{e.Delta}");
            if (e.Delta > 0) NI_Up_Click(sender, e);
            else if (e.Delta < 0) NI_Down_Click(sender, e);
        }
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            DP.DML($"ShortCut Pressed{e.KeyCode}");
            if (e.KeyCode == Keys.F3) { tabControl1.SelectedTab = tabMain; }
            else if (e.KeyCode == Keys.F4) { tabControl1.SelectedTab = tabSession; }
            else if (e.KeyCode == Keys.F7) { if (settings.AlwaysTop) settings.AlwaysTop = false; else settings.AlwaysTop = true; settings.Save(); }
            else if (e.KeyCode == Keys.F8) { if (settings.StayOn) settings.StayOn = false; else settings.StayOn = true; settings.Save(); }
        }
        //Controlers for main window display
        private void ShowWindow()
        {
            if (!Active())
            {
                Location = FWP.PointFromMouse(-(Width / 2), -Height, JLdebPack.FormWindowPackage.PointMode.AboveTaskbar);
                Show();
                Activate();
                Active(true);
            }
        }
        private void NI_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { DP.DML("IconLeftClick"); Active(false); ShowWindow(); }
        }
        #endregion

        #region Form Close Checks and finalizing
        private void MainWindow_MouseLeave(object sender, EventArgs e) { DP.DML("MouseLeave"); Close(); }
        private async void NI_Exit_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure to terminate Wale completely?", "Exit", MessageBoxButtons.OKCancel);
            if (dialogResult == DialogResult.OK)
            {
                DP.DML("Exit");
                Active(false);
                Rclose(true);
                await Task.WhenAll(_updateTasks);
                Close();
            }
            else
            {
                //do something else
            }
        }
        private bool CloseCheck()
        {
            if (!Rclose())
            {
                if (!settings.StayOn) { Hide(); Active(false); }
                return true;
            }
            else
            {
                tabSession.Controls.Clear();
                return false;
            }
        }
        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e) { DP.DML("Closing"); e.Cancel = CloseCheck(); }
        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Audio != null) Audio.Dispose();
            if (NI != null) NI.Visible = false;
            if (NI != null) NI.Dispose();
            FWP = null;
            Log("Closed"); DP.DML("Closed");
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

        #region title panel control, location and size check events
        private bool titleDrag = false;
        private Point titlePosition;
        private void titlePanel_MouseDown(object sender, MouseEventArgs e) { titleDrag = true; titlePosition = e.Location; }
        private void titlePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (titleDrag)
            {
                //MessageBox.Show($"L={Screen.AllScreens[0].WorkingArea.Left} R={Screen.AllScreens[0].WorkingArea.Right}, T={Screen.AllScreens[0].WorkingArea.Top} B={Screen.AllScreens[0].WorkingArea.Bottom}");
                int x = Location.X + e.Location.X - titlePosition.X;
                if (x + this.Width >= Screen.AllScreens[0].WorkingArea.Right) x = Screen.AllScreens[0].WorkingArea.Right - this.Width;
                else if (x <= Screen.AllScreens[0].WorkingArea.Left) x = Screen.AllScreens[0].WorkingArea.Left;

                int y = Location.Y + e.Location.Y - titlePosition.Y;
                if (y + this.Height >= Screen.AllScreens[0].WorkingArea.Bottom) y = Screen.AllScreens[0].WorkingArea.Bottom - this.Height;
                else if (y <= Screen.AllScreens[0].WorkingArea.Top) y = Screen.AllScreens[0].WorkingArea.Top;
                //MessageBox.Show($"x={x} y={y}");
                Location = new Point(x, y);
            }
        }
        private void titlePanel_MouseUp(object sender, MouseEventArgs e) { titleDrag = false; }
        private void MainWindow_LocationAndSizeChanged(object sender, EventArgs e)
        {
            if ((this.Left + this.Width) > Screen.AllScreens[0].Bounds.Width)
                this.Left = Screen.AllScreens[0].Bounds.Width - this.Width;

            if (this.Left < Screen.AllScreens[0].Bounds.Left)
                this.Left = Screen.AllScreens[0].Bounds.Left;

            if ((this.Top + this.Height) > Screen.AllScreens[0].Bounds.Height)
                this.Top = Screen.AllScreens[0].Bounds.Height - this.Height;

            if (this.Top < Screen.AllScreens[0].Bounds.Top)
                this.Top = Screen.AllScreens[0].Bounds.Top;
        }
        #endregion


        #region Volume control methods and events
        private void NI_Up_Click(object sender, EventArgs e)
        {
            DP.DM("Up ");
            GetInterval();
            updateVolumeDebug = true;
            Audio.VolumeUp(CalcInterval());
            updateVolumeDebug = false;
        }
        private void NI_Down_Click(object sender, EventArgs e)
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
            try { volume = Convert.ToDouble(tbVolume.Text); } catch { Log("fail to convert master volume\n"); MessageBox.Show("Invalid Volume"); return; }
            if (volume != null)
            {
                double? buf = Transformation.Transform((double)volume, Transformation.TransFlow.UserToMachine);
                if (buf != null) Audio.SetVolume((double)buf);
            }
        }
        private void tbVolume_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter) { VolumeSet_Click(sender, e); e.Handled = true; e.SuppressKeyPress = true; }
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
                it = Convert.ToDouble(tbInterval.Text);
                if (it > 0 && it <= 1)
                {
                    settings.MasterVolumeInterval = it;
                    settings.Save();
                }
            }
            catch { Log("fail to get master volume interval\n"); MessageBox.Show("Invalid Interval"); }
        }
        #endregion

        #region Toolstrip menu events
        private void AutoControlMenuItem_Click(object sender, EventArgs e)
        {
            if (settings.AutoControl)
            {
                settings.AutoControl = false;
                settings.Save();
                //Audio.AutoControl = Properties.Settings.Default.autoControl;
                cmsAutoControl.Text = "&AutoControl(Off)";
            }
            else
            {
                settings.AutoControl = true;
                settings.Save();
                //Audio.AutoControl = Properties.Settings.Default.autoControl;
                cmsAutoControl.Text = "&AutoControl(On)";
            }
        }
        private void ConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DP.DM("Settings");
            Config form = new Config();
            form.Location = FWP.PointFromMouse(-(form.Width / 2), -form.Height, JLdebPack.FormWindowPackage.PointMode.AboveTaskbar);
            form.FormClosed += Config_FormClosed;
            form.Show();
        }
        private void Config_FormClosed(object sender, FormClosedEventArgs e)
        {
            Form form = sender as Form;
            if (form.DialogResult == DialogResult.OK)
            {
                Transformation.SetBaseVolume(settings.BaseVolume);
                Audio.SetBaseTo(settings.BaseVolume);
                Audio.UpRate = settings.UpRate;
            }
            form.FormClosed -= Config_FormClosed;
            form.Dispose();
        }
        #endregion



        //Device master volume update.
        private void UpdateVolumeTask()
        {
            Log("Start UpdateVolumeTask");
            while (!Rclose())
            {
                //Task wait = new Task(new Action(() => System.Threading.Thread.Sleep(updateVolumeDelay)));
                //if (updateVolumeDelay > 0) wait.Start();
                if (Active())
                {
                    JLdebPack.DebugPackage VDP = new JLdebPack.DebugPackage(updateVolumeDebug);
                    VDP.DML($"base={settings.BaseVolume} vol={Audio.MasterVolume}({Audio.MasterPeak})");

                    SetText(lBaseVolume, $"{settings.BaseVolume:n}");
                    SetBar(pbBaseVolume, (int)(settings.BaseVolume * 100));
                    //lBaseVolume.Text = $"{Properties.Settings.Default.baseVolume:n}";
                    //pbBaseVolume.Increment((int)(Properties.Settings.Default.baseVolume * 100) - pbBaseVolume.Value);

                    if (NTV()) SetText(lVolume, $"{Transformation.Transform(Audio.MasterVolume, Transformation.TransFlow.MachineToUser):n}");
                    else SetText(lVolume, $"{Audio.MasterPeak * Audio.MasterVolume:n}");
                    SetBar(pbMasterVolume, (int)(Audio.MasterVolume * 100));
                    double lbuf = Audio.MasterVolume * Audio.MasterPeak * 100;
                    if (lbuf > pbMasterLevel.Maximum) lbuf = pbMasterLevel.Maximum;
                    else if (lbuf < pbMasterLevel.Minimum) lbuf = pbMasterLevel.Minimum;
                    SetBar(pbMasterLevel, (int)lbuf);
                    //if (NTV()) lVolume.Text = $"{volume:n}";
                    //else lVolume.Text = $"{Audio.MasterPeak * volume:n}";
                    //pbMasterVolume.Increment((int)(Audio.MasterVolume * 100) - pbMasterVolume.Value);
                    //pbMasterLevel.Increment((int)(Audio.MasterPeak * volume * 100) - pbMasterLevel.Value);
                }
                //if (updateVolumeDelay > 0) await wait;
                System.Threading.Thread.Sleep(settings.UIUpdateInterval);
            }
            Log("End UpdateVolumeTask");
        }
        //making UIs for all sessions.
        private void UpdateSessionTask()
        {
            Log("Start UpdateSessionTask");
            while (!Rclose())
            {
                //Task wait = new Task(new Action(() => System.Threading.Thread.Sleep(updateSessionDelay)));
                //if (updateSessionDelay > 0) wait.Start();
                if (Active()) //do when this.activated
                {
                    JLdebPack.DebugPackage SDP = new JLdebPack.DebugPackage(updateSessionDebug);
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
                            foreach (SessionData sc in Audio.Sessions)
                            {//check and insert new session data as meterset to tabSession.controls
                                if (!sc.Expired)
                                {
                                    bool found = false;
                                    foreach (MeterSet item in tabSession.Controls) { if (sc.ID == item.ID) found = true; }
                                    if (!found)
                                    {
                                        SetTabControl(tabSession, new MeterSet(sc.ID, sc.Name, settings.DetailedView, updateSessionDebug));
                                        reAlign = true;
                                        Log($"New MeterSet:{sc.Name}({sc.ID})");
                                    }
                                }
                            }
                            foreach (MeterSet item in tabSession.Controls)
                            {//check expired session and update not expired session
                                if (!Audio.Sessions.CheckData(item.ID)) { expired.Add(item); reAlign = true; break; }
                                SessionData session = Audio.Sessions.GetData(item.ID);
                                if (session.Expired) { expired.Add(item); reAlign = true; }
                                else// if (session.Active)
                                {
                                    if (settings.DetailedView) item.DetailOn();
                                    else item.DetailOff();
                                    if (item.detailChanged) { reAlign = true; item.detailChanged = false; }
                                    item.UpdateData(session.Volume, session.Peak, session.AveragePeak);
                                    session.Relative = (item.Relative);
                                    session.AutoIncluded = item.AutoIncluded;
                                }
                            }
                        }
                        foreach (MeterSet item in expired) { SetTabControl(tabSession, item, true); Log($"Remove MeterSet:{item.SessionName}({item.ID})"); } //remove expired session as meterset from tabSession.controls
                        expired.Clear(); //clear expire buffer
                                         //realign when there are one or more new set or removed set.
                        if (reAlign)
                        {//re-align when there is(are) added or removed session(s)
                            Log("Re-aligning");
                            int spacing = 24;
                            if (settings.DetailedView) spacing = 36;
                            for (int i = 0; i < tabSession.Controls.Count; i++)
                            {
                                int bottom = spacing * i + 5 + spacing;
                                if (bottom > tabSession.Height)
                                {
                                    Size newSize = this.Size;
                                    newSize.Height += (spacing + 5);
                                    SetSize(this, newSize);
                                    SetLocation(this, FWP.CurrentFormToAboveTaskbar(this.Location, this.Size));
                                    //MainWindow_LocationAndSizeChanged(this, new EventArgs());
                                }
                                else if (bottom < tabSession.Height - (spacing + 5))
                                {
                                    Size newSize = this.Size;
                                    newSize.Height -= (spacing + 5);
                                    SetSize(this, newSize);
                                    SetLocation(this, FWP.CurrentFormToAboveTaskbar(this.Location, this.Size));
                                    //MainWindow_LocationAndSizeChanged(this, new EventArgs());
                                }
                                MeterSet s = tabSession.Controls[i] as MeterSet;
                                s.UpdateLocation(new Point(5, spacing * i + 5));
                                SDP.DML($"MeterSet{s.ID,-5} {s.Location} {s.Size} {s.SessionName}");
                            }
                            Log("Re-aligned");
                        }
                    }//count check enclosure

                }// activated check enclosure
                 //if (updateSessionDelay > 0) await wait;
                System.Threading.Thread.Sleep(settings.UIUpdateInterval);
            }
            Log("End UpdateSessionTask");
        }



        #region Funcion delegates for unsafe UI update
        delegate void ControlSizeConsumer(Control control, Size value);
        private void SetSize(Control control, Size value)
        {
            try
            {
                if (control.InvokeRequired)
                {
                    if (control != null) control.Invoke(new ControlSizeConsumer(SetSize), new object[] { control, value });  // invoking itself
                }
                else
                {
                    control.Size = value;      // the "functional part", executing only on the main thread
                }
            }
            catch { DP.CML($"fail to invoke {control.Name}"); }
        }/**/

        delegate void ControlPointConsumer(Control control, Point loc);
        private void SetLocation(Control control, Point loc)
        {
            try
            {
                if (control.InvokeRequired)
                {
                    if (control != null) control.Invoke(new ControlPointConsumer(SetLocation), new object[] { control, loc });  // invoking itself
                }
                else
                {
                    control.Location = loc;      // the "functional part", executing only on the main thread
                }
            }
            catch { DP.CML($"fail to invoke {control.Name}"); }
        }/**/

        delegate void ControlMeterSetConsumer(Control control, MeterSet set, bool add);
        private void SetTabControl(Control control, MeterSet set, bool remove = false)
        {
            try
            {
                if (control != null)
                {
                    if (control.InvokeRequired)
                    {
                        DP.DML("Invoke Required - MeterSet - SetTabControl");
                        control.Invoke(new ControlMeterSetConsumer(SetTabControl), new object[] { control, set, remove });  // invoking itself
                    }
                    else
                    {
                        if (remove) control.Controls.Remove(set);      // the "functional part", executing only on the main thread
                        else control.Controls.Add(set);
                    }
                }
            }
            catch { Log($"fail to invoke {control.Name}\n"); }
        }/**/

        delegate void ControlStringConsumer(Control control, string text);
        private void SetText(Control control, string text)
        {
            try
            {
                if (control != null)
                {
                    if (control.InvokeRequired)
                    {
                        DP.DML("Invoke Required - MeterSet - SetText");
                        control.Invoke(new ControlStringConsumer(SetText), new object[] { control, text });  // invoking itself
                    }
                    else
                    {
                        control.Text = text;      // the "functional part", executing only on the main thread
                    }
                }
            }
            catch { Log($"fail to invoke {control.Name}\n"); }
        }/**/
        private void AppendText(Control control, string text)
        {
            try
            {
                if (control != null)
                {
                    if (control.InvokeRequired)
                    {
                        DP.DML("Invoke Required - MeterSet - SetText");
                        control.Invoke(new ControlStringConsumer(AppendText), new object[] { control, text });  // invoking itself
                    }
                    else
                    {
                        (control as TextBox).AppendText(text);      // the "functional part", executing only on the main thread
                    }
                }
            }
            catch { Log($"fail to invoke {control.Name}\n"); }
        }/**/

        delegate void ControlIntConsumer(ProgressBar control, int value);
        private void SetBar(ProgressBar control, int value)
        {
            try
            {
                if (control != null)
                {
                    if (control.InvokeRequired)
                    {
                        DP.DML("Invoke Required - MeterSet - SetBar");
                        control.Invoke(new ControlIntConsumer(SetBar), new object[] { control, value });  // invoking itself
                    }
                    else
                    {
                        control.Value = value;      // the "functional part", executing only on the main thread
                    }
                }
            }
            catch { Log($"fail to invoke {control.Name}\n"); }
        }/**/

        //delegate void ControlForeColorConsumer(Control control, Color color);
        /*private void SetForeColor(Control control, Color color)
        {
            try
            {
                if (control.InvokeRequired)
                {
                    control.Invoke(new ControlForeColorConsumer(SetForeColor), new object[] { control, color });  // invoking itself
                }
                else
                {
                    control.ForeColor = color;      // the "functional part", executing only on the main thread
                }
            }
            catch { Log($"fail to invoke {control.Name}\n"); }
        }/**/
        #endregion


        #region Logging
        private void Log(string msg, bool newLine = true)
        {
            JLdebPack.DebugPack.Log($"{DateTime.Now.ToLocalTime()}: {msg}", newLine);
            AppendText(Logs, $"{DateTime.Now.ToLocalTime()}: {msg}");
            if (newLine) AppendText(Logs, "\r\n");
        }
        private void Logs_VisibleChanged(object sender, EventArgs e)
        {
            if (Logs.Visible)
            {
                Logs.SelectionStart = Logs.TextLength;
                Logs.ScrollToCaret();
            }
        }
        #endregion

    }//End Class MainWindow
    #region Native Method related
    //TODO: Don't forget to include using System.Runtime.InteropServices.
    internal static class NativeMethods
    {
        internal static readonly int GWL_EXSTYLE = -20;
        internal static readonly int WS_EX_COMPOSITED = 0x02000000;

        [DllImport("user32")]
        internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32")]
        internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }/**/
    #endregion

    //UI set for session view.
    internal class MeterSet : Panel//, IDisposable
    {
        private JLdebPack.DebugPackage DP;
        private CheckBox cbAutoIncluded;
        private Label lSessionNameLabel, lVolume;
        private Label dlReletive, dlAvPeak, dlPeak;
        private NewProgressBar lVolumeBar, lLevelBar;

        private uint _ID;
        private int _relative = 0;
        private bool _Updated, detailed;//, _disposed = false;
        private LabelMode labelMode = LabelMode.Volume;
        private enum LabelMode { Relative, Volume, Peak, AveragePeak };
        private Color White = Color.LightGray, Blue = Color.CornflowerBlue, Red = Color.PaleVioletRed, Orange = Color.Orange;

        //public variables
        //public List<double> LastPeaks;
        public uint ID { get => _ID; }
        public string SessionName { get => lSessionNameLabel.Text; }
        public double Relative { get => ((double)_relative / 100.0); }
        public bool AutoIncluded { get => cbAutoIncluded.Checked; }
        public bool Updated { get => _Updated; }
        public bool Debug { get => DP.DebugMode; set => DP.DebugMode = value; }
        public bool detailChanged = false;

        //Initialization and init methods
        public MeterSet(uint id, string name, bool detail, bool dbg = false)
        {
            _ID = id;
            DP = new JLdebPack.DebugPackage(dbg);

            Initialization(name);
            detailed = !detail;
            //if (detail) DetailOn();
            //else DetailOff();
        }
        private void Initialization(string name) { SetMake(); Items(); ItemEvents(); ItemLocations(); DetailedItems(); DetailedLocations(); SetFinalMake(name); }
        private void SetMake()
        {
            DP.DM("SetMake");
            //LastPeaks = new List<double>();
            //for (int i = 0; i < 10; i++) { LastPeaks.Add(0); }

            this.AutoSize = false;
            this.Size = new Size(186, 24);
            this.Padding = new Padding(0, 2, 0, 2);
            //this.BorderStyle = BorderStyle.FixedSingle;
        }
        private void Items()
        {
            DP.DM(" - Items");
            cbAutoIncluded = new CheckBox();
            cbAutoIncluded.AutoSize = false;
            cbAutoIncluded.Size = new Size(14, 14);
            cbAutoIncluded.Margin = new Padding(0);
            cbAutoIncluded.ForeColor = White;
            cbAutoIncluded.FlatStyle = FlatStyle.Flat;
            cbAutoIncluded.Checked = true;
            cbAutoIncluded.Enabled = true;
            cbAutoIncluded.Show();
            lSessionNameLabel = new Label();
            lSessionNameLabel.AutoSize = false;
            lSessionNameLabel.Size = new Size(70, 12);
            lSessionNameLabel.Margin = new Padding(0);
            lSessionNameLabel.ForeColor = White;
            lSessionNameLabel.Enabled = true;
            lSessionNameLabel.Show();
            lVolume = new Label();
            lVolume.AutoSize = false;
            lVolume.Size = new Size(35, 12);
            lVolume.Margin = new Padding(0);
            lVolume.ForeColor = Blue;
            lVolume.Enabled = true;
            lVolume.Show();
            lVolumeBar = new NewProgressBar();
            lVolumeBar.Size = new Size(74, 10);
            lVolumeBar.Margin = new Padding(0);
            lVolumeBar.ForeColor = Blue;
            lVolumeBar.Maximum = 100;
            lVolumeBar.Minimum = 0;
            lVolumeBar.Step = 1;
            /*
            int style1 = NativeMethods.GetWindowLong(lVolumeBar.Handle, NativeMethods.GWL_EXSTYLE);
            style1 |= NativeMethods.WS_EX_COMPOSITED;
            NativeMethods.SetWindowLong(lVolumeBar.Handle, NativeMethods.GWL_EXSTYLE, style1);
            /**/
            lVolumeBar.Enabled = true;
            lVolumeBar.Show();
            lLevelBar = new NewProgressBar();
            lLevelBar.Size = new Size(74, 10);
            lLevelBar.Margin = new Padding(0);
            lLevelBar.ForeColor = Red;
            lLevelBar.Maximum = 100;
            lLevelBar.Minimum = 0;
            lLevelBar.Step = 1;
            /*
            int style2 = NativeMethods.GetWindowLong(lLevelBar.Handle, NativeMethods.GWL_EXSTYLE);
            style2 |= NativeMethods.WS_EX_COMPOSITED;
            NativeMethods.SetWindowLong(lLevelBar.Handle, NativeMethods.GWL_EXSTYLE, style2);
            /**/
            lLevelBar.Enabled = true;
            lLevelBar.Show();
            /*pot = new NAudio.Gui.Pot();
            pot.AutoSize = false;
            pot.Size = new Size(74, 10);
            pot.Margin = new Padding(0);
            pot.ForeColor = Color.BlueViolet;
            pot.Maximum = 100;
            pot.Minimum = 0;/**/
        }
        private void ItemEvents()
        {
            DP.DM(" - ItemEvents");
            this.MouseWheel += MeterSet_MouseWheel;
            lSessionNameLabel.Click += LSessionNameLabel_Click;
            lVolume.Click += LVolume_Click;
        }
        private void ItemLocations()
        {
            DP.DM(" - ItemLocations");
            cbAutoIncluded.Location = new Point(0, 3);
            lSessionNameLabel.Location = new Point(13, 4);
            lVolume.Location = new Point(82, 4);
            lVolumeBar.Location = new Point(118, 0);
            lLevelBar.Location = new Point(118, 10);
        }
        private void DetailedItems()
        {
            dlReletive = new Label();
            dlReletive.AutoSize = false;
            dlReletive.Size = new Size(35, 12);
            dlReletive.Margin = new Padding(0);
            dlReletive.ForeColor = White;
            dlReletive.Enabled = true;
            //dlReletive.Show();
            dlReletive.Hide();
            dlAvPeak = new Label();
            dlAvPeak.AutoSize = false;
            dlAvPeak.Size = new Size(35, 12);
            dlAvPeak.Margin = new Padding(0);
            dlAvPeak.ForeColor = Orange;
            dlAvPeak.Enabled = true;
            //dlAvPeak.Show();
            dlAvPeak.Hide();
            dlPeak = new Label();
            dlPeak.AutoSize = false;
            dlPeak.Size = new Size(35, 12);
            dlPeak.Margin = new Padding(0);
            dlPeak.ForeColor = Red;
            dlPeak.Enabled = true;
            //dlPeak.Show();
            dlPeak.Hide();
        }
        private void DetailedLocations()
        {
            DP.DM(" - DetailedItemLocations");
            dlReletive.Location = new Point(13, 22);
            dlAvPeak.Location = new Point(47, 22);
            dlPeak.Location = new Point(82, 22);
        }
        private void SetFinalMake(string name)
        {
            DP.DML(" - SetFinalMake");
            //SetText(lSessionNameLabel, name);
            lSessionNameLabel.Text = name;

            this.Controls.Add(cbAutoIncluded);
            this.Controls.Add(lSessionNameLabel);
            this.Controls.Add(lVolume);
            this.Controls.Add(lVolumeBar);
            this.Controls.Add(lLevelBar);
            this.Controls.Add(dlReletive);
            this.Controls.Add(dlPeak);
            this.Controls.Add(dlAvPeak);
            this.Enabled = true;
            this.Show();
        }
        /*~MeterSet() { Dispose(false); }
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    base.Dispose(true);
                }
                _disposed = true;
            }
        }/**/


        //Item events
        private void LSessionNameLabel_Click(object sender, EventArgs e) { cbAutoIncluded.Checked = !cbAutoIncluded.Checked; }
        private void LVolume_Click(object sender, EventArgs e)
        {
            switch (labelMode)
            {
                case LabelMode.Relative:
                    labelMode = LabelMode.Volume;
                    //lVolume.ForeColor = SystemColors.ActiveCaption;
                    SetForeColor(lVolume, Blue);
                    break;
                case LabelMode.Volume:
                    labelMode = LabelMode.Peak;
                    //lVolume.ForeColor = Color.PaleVioletRed;
                    SetForeColor(lVolume, Red);
                    break;
                case LabelMode.Peak:
                    labelMode = LabelMode.AveragePeak;
                    //lVolume.ForeColor = Color.FromArgb(224, 224, 224);
                    SetForeColor(lVolume, Orange);
                    break;
                case LabelMode.AveragePeak:
                    labelMode = LabelMode.Relative;
                    //lVolume.ForeColor = Color.FromArgb(224, 224, 224);
                    SetForeColor(lVolume, White);
                    break;
            }
        }
        private void MeterSet_MouseWheel(object sender, MouseEventArgs e)
        {
            DP.DM($"{SessionName}({_ID}) MeterSet_MouseWheel {e.Delta}");
            if (e.Delta > 0) { _relative++; if (_relative > 100) _relative = 100; }
            else if (e.Delta < 0) { _relative--; if (_relative < -100) _relative = -100; }
            DP.DML($", {Relative}");
        }



        //public functions
        public void UpdateLocation(Point p) { SetLocation(this, p); }
        public void ResetUpdate() { _Updated = false; }
        public void UpdateData(double vol, double level, double Avl)
        {
            _Updated = true;
            switch (labelMode)
            {
                case LabelMode.Relative:
                    SetText(lVolume, $"{Relative:n}");
                    //lVolume.Text = $"{Relative:n}";
                    break;
                case LabelMode.Volume:
                    SetText(lVolume, string.Format("{0:n}", Transformation.Transform(vol, Transformation.TransFlow.MachineToUser)));
                    //lVolume.Text = $"{Transformation.Transform(vol, Transformation.TransFlow.MachineToUser):n}";
                    break;
                case LabelMode.Peak:
                    SetText(lVolume, $"{level:n}");
                    //lVolume.Text = $"{level:n}";
                    break;
                case LabelMode.AveragePeak:
                    SetText(lVolume, $"{Avl:n}");
                    //lVolume.Text = $"{level:n}";
                    break;
            }
            SetText(dlReletive, $"{Relative:n}");
            SetText(dlPeak, $"{level:n}");
            SetText(dlAvPeak, $"{Avl:n}");
            SetBar(lVolumeBar, (int)(vol * 100));
            //lVolumeBar.Increment((int)(vol * 100) - lVolumeBar.Value);
            double lbuf = Transformation.Transform(vol, Transformation.TransFlow.MachineToUser) * level * 100;
            if (lbuf > lLevelBar.Maximum) lbuf = lLevelBar.Maximum;
            else if (lbuf < lLevelBar.Minimum) lbuf = lLevelBar.Minimum;
            SetBar(lLevelBar, (int)lbuf);
            //lLevelBar.Increment((int)(((vbuf != null) ? vbuf : 1) * level * 100) - lLevelBar.Value);
            //SetBar2(pot, (int)(((vbuf != null) ? vbuf : 1) * level * 100));
            //this.Refresh();
        }

        public void DetailOn()
        {
            if (!detailed)
            {
                lVolume.Click -= LVolume_Click;
                labelMode = LabelMode.Volume;
                SetForeColor(lVolume, Blue);
                SetSize(this, new Size(186, 36));
                ControlShowHide(dlReletive, true);
                ControlShowHide(dlPeak, true);
                ControlShowHide(dlAvPeak, true);
                //ControlAddRemove(dlReletive, true);
                //ControlAddRemove(dlPeak, true);
                //ControlAddRemove(dlAvPeak, true);
                detailed = true;
                detailChanged = true;
            }
        }
        public void DetailOff()
        {
            if (detailed)
            {
                lVolume.Click += LVolume_Click;
                SetSize(this, new Size(186, 24));
                ControlShowHide(dlReletive, false);
                ControlShowHide(dlPeak, false);
                ControlShowHide(dlAvPeak, false);
                //ControlAddRemove(dlReletive, false);
                //ControlAddRemove(dlPeak, false);
                //ControlAddRemove(dlAvPeak, false);
                detailed = false;
                detailChanged = true;
            }
        }


        #region Funcion delegates for MeterSet UI
        delegate void ControlBoolConsumer(Control control, bool value);
        private void ControlShowHide(Control control, bool value)
        {
            try
            {
                if (control.InvokeRequired)
                {
                    if (control != null) control.Invoke(new ControlBoolConsumer(ControlShowHide), new object[] { control, value });  // invoking itself
                }
                else
                {// the "functional part", executing only on the main thread
                    if (value) control.Show();
                    else control.Hide();
                }
            }
            catch { DP.CML($"fail to invoke {control.Name}"); }
        }/**/
        private void ControlAddRemove(Control control, bool value)
        {
            try
            {
                if (control.InvokeRequired)
                {
                    if (control != null) control.Invoke(new ControlBoolConsumer(ControlAddRemove), new object[] { control, value });  // invoking itself
                }
                else
                {// the "functional part", executing only on the main thread
                    if (value) this.Controls.Add(control);
                    else this.Controls.Remove(control);
                }
            }
            catch { DP.CML($"fail to invoke {control.Name}"); }
        }/**/
        delegate void ControlPointConsumer(Control control, Point loc);
        private void SetLocation(Control control, Point loc)
        {
            try
            {
                if (control.InvokeRequired)
                {
                    if (control != null) control.Invoke(new ControlPointConsumer(SetLocation), new object[] { control, loc });  // invoking itself
                }
                else
                {
                    control.Location = loc;      // the "functional part", executing only on the main thread
                }
            }
            catch { DP.CML($"fail to invoke {control.Name}"); }
        }/**/
        delegate void ControlStringConsumer(Control control, string text);
        private void SetText(Control control, string text)
        {
            try
            {
                if (control.InvokeRequired)
                {
                    if (control != null) control.Invoke(new ControlStringConsumer(SetText), new object[] { control, text });  // invoking itself
                }
                else
                {
                    control.Text = text;      // the "functional part", executing only on the main thread
                }
            }
            catch { DP.CML($"fail to invoke {control.Name}"); }
        }/**/
        delegate void ControlIntConsumer(ProgressBar control, int value);
        private void SetBar(ProgressBar control, int value)
        {
            try
            {
                if (control.InvokeRequired)
                {
                    if (control != null) control.Invoke(new ControlIntConsumer(SetBar), new object[] { control, value });  // invoking itself
                }
                else
                {
                    control.Value = value;      // the "functional part", executing only on the main thread
                                                //control.Increment(value);
                }
            }
            catch { DP.CML($"fail to invoke {control.Name}"); }
        }/**/
        delegate void ControlSizeConsumer(Control control, Size value);
        private void SetSize(Control control, Size value)
        {
            try
            {
                if (control.InvokeRequired)
                {
                    if (control != null) control.Invoke(new ControlSizeConsumer(SetSize), new object[] { control, value });  // invoking itself
                }
                else
                {
                    control.Size = value;      // the "functional part", executing only on the main thread
                }
            }
            catch { DP.CML($"fail to invoke {control.Name}"); }
        }/**/
        delegate void ControlColorConsumer(Control control, Color color);
        private void SetForeColor(Control control, Color color)
        {
            try
            {
                if (control.InvokeRequired)
                {
                    if (control != null) control.Invoke(new ControlColorConsumer(SetForeColor), new object[] { control, color });  // invoking itself
                }
                else
                {
                    control.ForeColor = color;      // the "functional part", executing only on the main thread
                }
            }
            catch { DP.CML($"fail to invoke {control.Name}"); }
        }/**/
        #endregion
    }//End Class MeterSet

    #region Custom form controls
    internal class CustomTabControl : TabControl
    {
        private const int TCM_ADJUSTRECT = 0x1328;

        protected override void WndProc(ref Message m)
        {
            //Hide the tab headers at run-time
            if (m.Msg == TCM_ADJUSTRECT)
            {

                RECT rect = (RECT)(m.GetLParam(typeof(RECT)));
                rect.Left = this.Left - this.Margin.Left;
                rect.Right = this.Right + this.Margin.Right;
                rect.Top = this.Top - this.Margin.Top;
                rect.Bottom = this.Bottom + this.Margin.Bottom - 2;
                Marshal.StructureToPtr(rect, m.LParam, true);
                //m.Result = (IntPtr)1;
                //return;
            }
            //else
            // call the base class implementation
            base.WndProc(ref m);
        }

        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }
    }
    internal class NewProgressBar : ProgressBar
    {
        public NewProgressBar()
        {
            this.SetStyle(ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle rec = e.ClipRectangle;

            rec.Width = (int)(rec.Width * Value / Maximum) - 2;
            if (ProgressBarRenderer.IsSupported)
                ProgressBarRenderer.DrawHorizontalBar(e.Graphics, e.ClipRectangle);
            rec.Height -= 2;
            e.Graphics.FillRectangle(new SolidBrush(this.ForeColor), 1, 1, rec.Width, rec.Height);
        }
    }
    #endregion



}//End Namespace WindowAudioLoudnessEqualizer