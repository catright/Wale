using System;
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

        private const int WM_PAINT = 0x000F;
        protected override void WndProc(ref Message m)
        {
            if (m.Msg != WM_PAINT)
            {
                base.WndProc(ref m);
            }
            else if (bAllowPaintMaster && m.Msg == WM_PAINT)
            {
                base.WndProc(ref m);
                bAllowPaintMaster = false;
            }
            else if (bAllowPaintSession && m.Msg == WM_PAINT)
            {
                base.WndProc(ref m);
                bAllowPaintSession = false;
            }
            else if (bAllowPaintLog && m.Msg == WM_PAINT)
            {
                base.WndProc(ref m);
                bAllowPaintLog = false;
            }
        }
        #endregion

        #region Private Variables
        Properties.Settings settings = Properties.Settings.Default;
        AudioControl Audio;
        JDPack.DebugPack DP;
        JDPack.FormPack FWP;
        List<Task> _updateTasks;
        object _closelock = new object(), _activelock = new object(), _ntvlock = new object();
        volatile bool _realClose = false, _activated = false, _numberToVol = true;
        bool debug = false, mouseWheelDebug = false, audioDebug = false, updateVolumeDebug = false, updateSessionDebug = true;
        bool bAllowPaintMaster = true, bAllowPaintSession = true, bAllowPaintLog = true;
        #endregion

        #region Public Vatiables

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            MakeComponents();
            ColorBindings();
            StartApp();
            Log("App Started");
        }

        #region initializing
        private void MakeComponents()
        {
            NI.Icon = this.Icon = Properties.Resources.speaker_white1;
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
            DP = new JDPack.DebugPack(debug);
            FWP = new JDPack.FormPack();
            Log("OK1"); DP.DML("OK1");
        }
        private void ColorBindings()
        {
            this.ForeColor = ColorSet.ForeColor;
            this.BackColor = ColorSet.BackColor;
            
            titlePanel.BackColor = ColorSet.MainColor;

            pbMasterVolume.ForeColor = ColorSet.MainColor;
            pbBaseLevel.ForeColor = ColorSet.BaseColor;
            pbMasterPeak.ForeColor = ColorSet.PeakColor;

            bVolumeSet.BackColor = ColorSet.BackColorAlt;
            bVolumeSet.FlatAppearance.BorderColor = ColorSet.ForeColor;

            lVolume.ForeColor = ColorSet.MainColor;

            JDPack.FormPack2.Bind(tabMain, "BackColor", this, "BackColor");
            JDPack.FormPack2.Bind(tabSession, "BackColor", this, "BackColor");
            JDPack.FormPack2.Bind(tabLog, "BackColor", this, "BackColor");
        }
        private void StartApp()
        {
            Wale.Transformation.SetBaseLevel(settings.BaseLevel);
            Audio = new AudioControl(settings.BaseLevel);
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
                Location = FWP.PointFromMouse(-(Width / 2), -Height, JDPack.FormPack.PointMode.AboveTaskbar);
                Show();
                Activate();
                Active(true);
            }
        }
        private void NI_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { DP.DML("IconLeftClick"); Active(false); ShowWindow(); }
        }
        //ToolTip Messeges

        private ToolTip tt;
        private void textBox_Enter(object sender, EventArgs e)
        {
            TextBox targetTextBox = (sender as TextBox);
            tt = new ToolTip();
            tt.InitialDelay = 0;
            tt.IsBalloon = false;
            tt.Show(string.Empty, targetTextBox);
            string msg = "";
            if (targetTextBox.Name == tbInterval.Name) { msg = "Step of master volume"; }
            else if (targetTextBox.Name == tbVolume.Name) { msg = "Target master volume to set"; }
            tt.Show(msg, targetTextBox, 0);
        }
        private void textBox_Leave(object sender, EventArgs e)
        {
            tt.Dispose();
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
                //double? buf = Transformation.Transform((double)volume, Transformation.TransFlow.UserToMachine);
                //if (buf != null) Audio.SetVolume((double)buf);
                Audio.SetVolume((double)volume);
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
            form.Location = FWP.PointFromMouse(-(form.Width / 2), -form.Height, JDPack.FormPack.PointMode.AboveTaskbar);
            form.FormClosed += Config_FormClosed;
            form.Show();
        }
        private void Config_FormClosed(object sender, FormClosedEventArgs e)
        {
            Form form = sender as Form;
            if (form.DialogResult == DialogResult.OK)
            {
                Transformation.SetBaseLevel(settings.BaseLevel);
                Audio.SetBaseTo(settings.BaseLevel);
                Audio.UpRate = settings.UpRate;
            }
            form.FormClosed -= Config_FormClosed;
            form.Dispose();
        }

        private void deviceMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DP.DM("DeviceMap");
            DeviceMap form = new DeviceMap();
            form.Location = FWP.PointFromMouse(-(form.Width / 2), -form.Height, JDPack.FormPack.PointMode.AboveTaskbar);

            form.ShowDialog();
        }
        
        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DP.DM("Help");
            Help form = new Help();
            form.Location = FWP.PointFromMouse(-(form.Width / 2), -form.Height, JDPack.FormPack.PointMode.AboveTaskbar);

            form.ShowDialog();
        }
        private void licensesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DP.DM("Licenses");
            Licenses form = new Licenses();
            form.Location = FWP.PointFromMouse(-(form.Width / 2), -form.Height, JDPack.FormPack.PointMode.AboveTaskbar);
            
            form.ShowDialog();
        }
        #endregion



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

                    SetText(lBaseVolume, $"{settings.BaseLevel:n}");
                    SetBar(pbBaseLevel, (int)(settings.BaseLevel * 100));
                    //lBaseVolume.Text = $"{Properties.Settings.Default.baseVolume:n}";
                    //pbBaseVolume.Increment((int)(Properties.Settings.Default.baseVolume * 100) - pbBaseVolume.Value);

                    if (NTV()) SetText(lVolume, $"{Audio.MasterVolume:n}");//Transformation.Transform(Audio.MasterVolume, Transformation.TransFlow.MachineToUser)
                    else SetText(lVolume, $"{Audio.MasterPeak * Audio.MasterVolume:n}");
                    SetBar(pbMasterVolume, (int)(Audio.MasterVolume * 100));
                    double lbuf = Audio.MasterVolume * Audio.MasterPeak * 100;
                    if (lbuf > pbMasterPeak.Maximum) lbuf = pbMasterPeak.Maximum;
                    else if (lbuf < pbMasterPeak.Minimum) lbuf = pbMasterPeak.Minimum;
                    SetBar(pbMasterPeak, (int)lbuf);
                    //if (NTV()) lVolume.Text = $"{volume:n}";
                    //else lVolume.Text = $"{Audio.MasterPeak * volume:n}";
                    //pbMasterVolume.Increment((int)(Audio.MasterVolume * 100) - pbMasterVolume.Value);
                    //pbMasterLevel.Increment((int)(Audio.MasterPeak * volume * 100) - pbMasterLevel.Value);
                }
                //if (updateVolumeDelay > 0) await wait;
                bAllowPaintMaster = true;
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
                            foreach (Wale.CoreAudio.Session2 sc in Audio.Sessions)
                            {//check and insert new session data as meterset to tabSession.controls
                                if (sc.State != Wale.CoreAudio.SessionState.Expired)
                                {
                                    bool found = false;
                                    foreach (MeterSet item in tabSession.Controls) { if (sc.PID == item.ID) found = true; }
                                    if (!found)
                                    {
                                        SetTabControl(tabSession, new MeterSet(sc.PID, sc.Name, settings.DetailedView, updateSessionDebug));
                                        reAlign = true;
                                        Log($"New MeterSet:{sc.Name}({sc.PID})");
                                    }
                                }
                            }
                            foreach (MeterSet item in tabSession.Controls)
                            {//check expired session and update not expired session
                                if (Audio.Sessions.GetSession(item.ID) == null) { expired.Add(item); reAlign = true; break; }
                                Wale.CoreAudio.Session2 session = Audio.Sessions.GetSession(item.ID);
                                if (session.State == Wale.CoreAudio.SessionState.Expired) { expired.Add(item); reAlign = true; }
                                else// if (session.Active)
                                {
                                    if (settings.DetailedView) item.DetailOn();
                                    else item.DetailOff();
                                    if (item.detailChanged) { reAlign = true; item.detailChanged = false; }
                                    item.UpdateData(session.Volume, session.Peak, session.AveragePeak);
                                    session.Relative = (float)item.Relative;
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
                bAllowPaintSession = true;
                System.Threading.Thread.Sleep(settings.UIUpdateInterval);
                //await wait;
            }/**/
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
            JDPack.FileLog.Log(msg, newLine);
            DateTime t = DateTime.Now.ToLocalTime();
            string content = $"{t.Hour:d2}:{t.Minute:d2}>{msg}";
            if (newLine) content += "\r\n";
            AppendText(Logs, content);
            bAllowPaintLog = true;
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
    
}//End Namespace WindowAudioLoudnessEqualizer