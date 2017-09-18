using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Wale.WinForm
{
    public partial class MeterSet : Panel
    {
        private JDPack.DebugPack DP;
        private CheckBox cbAutoIncluded;
        private Label lSessionNameLabel, lVolume;
        private Label dlReletive, dlAvPeak, dlPeak;
        private JDPack.ProgressBarColored lVolumeBar, lLevelBar;
        private Color foreColor = ColorSet.ForeColor, mainColor = ColorSet.MainColor, peakColor = ColorSet.PeakColor, averageColor = ColorSet.AverageColor;
        private enum LabelMode { Relative, Volume, Peak, AveragePeak };

        private uint _ID;
        private int _relative = 0;
        private bool _Updated, detailed;//, _disposed = false;
        private LabelMode labelMode = LabelMode.Volume;

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
        public MeterSet()
        {
            InitializeComponent();
        }
        public MeterSet(uint id, string name, bool detail, bool dbg = false)
        {
            InitializeComponent();
            _ID = id;
            DP = new JDPack.DebugPack(dbg);

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
            cbAutoIncluded.ForeColor = foreColor;
            cbAutoIncluded.FlatStyle = FlatStyle.Flat;
            cbAutoIncluded.Checked = true;
            cbAutoIncluded.Enabled = true;
            cbAutoIncluded.Show();
            lSessionNameLabel = new Label();
            lSessionNameLabel.AutoSize = false;
            lSessionNameLabel.Size = new Size(70, 12);
            lSessionNameLabel.Margin = new Padding(0);
            lSessionNameLabel.ForeColor = foreColor;
            lSessionNameLabel.Enabled = true;
            lSessionNameLabel.Show();
            lVolume = new Label();
            lVolume.AutoSize = false;
            lVolume.Size = new Size(35, 12);
            lVolume.Margin = new Padding(0);
            lVolume.ForeColor = mainColor;
            lVolume.Enabled = true;
            lVolume.Show();
            lVolumeBar = new JDPack.ProgressBarColored();
            lVolumeBar.Size = new Size(74, 10);
            lVolumeBar.Margin = new Padding(0);
            lVolumeBar.ForeColor = mainColor;
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
            lLevelBar = new JDPack.ProgressBarColored();
            lLevelBar.Size = new Size(74, 10);
            lLevelBar.Margin = new Padding(0);
            lLevelBar.ForeColor = peakColor;
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
            //lVolume.Click += LVolume_Click;
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
            dlReletive.ForeColor = foreColor;
            dlReletive.Enabled = true;
            //dlReletive.Show();
            dlReletive.Hide();
            dlAvPeak = new Label();
            dlAvPeak.AutoSize = false;
            dlAvPeak.Size = new Size(35, 12);
            dlAvPeak.Margin = new Padding(0);
            dlAvPeak.ForeColor = averageColor;
            dlAvPeak.Enabled = true;
            //dlAvPeak.Show();
            dlAvPeak.Hide();
            dlPeak = new Label();
            dlPeak.AutoSize = false;
            dlPeak.Size = new Size(35, 12);
            dlPeak.Margin = new Padding(0);
            dlPeak.ForeColor = peakColor;
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
                    SetForeColor(lVolume, mainColor);
                    break;
                case LabelMode.Volume:
                    labelMode = LabelMode.Peak;
                    SetForeColor(lVolume, peakColor);
                    break;
                case LabelMode.Peak:
                    labelMode = LabelMode.AveragePeak;
                    SetForeColor(lVolume, averageColor);
                    break;
                case LabelMode.AveragePeak:
                    labelMode = LabelMode.Relative;
                    SetForeColor(lVolume, foreColor);
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
                    break;
                case LabelMode.Volume:
                    SetText(lVolume, $"{vol:n}");//string.Format("{0:n}", Transformation.Transform(vol, Transformation.TransFlow.MachineToUser)));
                    break;
                case LabelMode.Peak:
                    SetText(lVolume, $"{level:n}");
                    break;
                case LabelMode.AveragePeak:
                    SetText(lVolume, $"{Avl:n}");
                    break;
            }
            if (detailed)
            {
                SetText(dlReletive, $"{Relative:n}");
                SetText(dlPeak, $"{level:n}");
                SetText(dlAvPeak, $"{Avl:n}");
            }
            SetBar(lVolumeBar, (int)(vol * 100));
            //lVolumeBar.Increment((int)(vol * 100) - lVolumeBar.Value);
            double lbuf = Wale.Transformation.Transform(vol, Wale.Transformation.TransFlow.MachineToUser) * level * 100;
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
                SetForeColor(lVolume, mainColor);
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

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
        }
    }
}
