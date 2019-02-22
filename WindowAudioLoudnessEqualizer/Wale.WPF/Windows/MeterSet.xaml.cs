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
    /// Interaction logic for MeterSet.xaml
    /// </summary>
    public partial class MeterSet : UserControl, IComparable<MeterSet>
    {
        private JDPack.DebugPack DP;
        private Brush foreColor = ColorSet.ForeColorBrush, mainColor = ColorSet.MainColorBrush, peakColor = ColorSet.PeakColorBrush, averageColor = ColorSet.AverageColorBrush;
        private enum LabelMode { Relative, Volume, Peak, AveragePeak };

        private bool detailed;
        private LabelMode labelMode = LabelMode.AveragePeak;
        private string lastName, lastTooltip;

        //public variables
        //public List<double> LastPeaks;
        public int ProcessID { get; }
        public string SessionName { get => NameLabel.Content.ToString(); }
        public double Relative { get; private set; } = 0;
        public bool AutoIncluded { get => AutoIncludeCBox.IsChecked.Value; set => AutoIncludeCBox.IsChecked = value; }
        public bool AutoIncludedChanged { get; set; } = false;
        public bool Updated { get; private set; }
        public bool Debug { get => DP.DebugMode; set => DP.DebugMode = value; }
        public bool detailChanged = false;
        public bool SoundEnabled { get => SoundOnCBox.IsChecked.Value; set { SoundOnCBox.IsChecked = value; } }
        public bool SoundEnableChanged { get; set; } = false;

        //Initialization and init methods
        public MeterSet()
        {
            InitializeComponent();
        }
        public MeterSet(int pid, string name, bool detail, bool autoinc, bool dbg = false, string tooltip = null)
        {
            InitializeComponent();
            ProcessID = pid;
            DP = new JDPack.DebugPack(dbg);

            Initialization(name, tooltip);
            detailed = !detail;
            AutoIncluded = autoinc;
            //if (detail) DetailOn();
            //else DetailOff();
        }
        private void Initialization(string name, string tooltip) { DetailedItems(); SetFinalMake(name, tooltip); }
        private void DetailedItems()
        {
            RelLabel.Visibility = Visibility.Hidden;
            PeakLabel.Visibility = Visibility.Hidden;
            AvPeakLabel.Visibility = Visibility.Hidden;
        }
        private void SetFinalMake(string name, string tooltip)
        {
            DP.DML(" - SetFinalMake");
            //SetText(lSessionNameLabel, name);
            NameLabel.Content = name;
            NameToolTip.Content = string.IsNullOrEmpty(tooltip) ? name : tooltip;
            //Console.WriteLine($"{LevelBar.DesiredSize}");
        }


        //Item events
        private void LSessionNameLabel_Click(object sender, MouseButtonEventArgs e) { SoundEnabled = !SoundEnabled; SoundEnableChanged = true; }
        private void SoundOnCBox_Click(object sender, RoutedEventArgs e) { SoundEnableChanged = true; }
        private void AutoIncludedCBox_Click(object sender, RoutedEventArgs e) { AutoIncludedChanged = true; }
        private void LSessionLabel_Click(object sender, MouseButtonEventArgs e)
        {
            if (!detailed)
            {
                switch (labelMode)
                {
                    case LabelMode.Relative:
                        labelMode = LabelMode.Volume;
                        SetForeColor(SessionLabel, mainColor);
                        break;
                    case LabelMode.Volume:
                        labelMode = LabelMode.Peak;
                        SetForeColor(SessionLabel, peakColor);
                        break;
                    case LabelMode.Peak:
                        labelMode = LabelMode.AveragePeak;
                        SetForeColor(SessionLabel, averageColor);
                        break;
                    case LabelMode.AveragePeak:
                        labelMode = LabelMode.Relative;
                        SetForeColor(SessionLabel, foreColor);
                        break;
                }
            }
        }
        private void MeterSet_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            DP.DM($"{SessionName}({ProcessID}) MeterSet_MouseWheel {e.Delta}");
            if (e.Delta > 0) { Relative += 0.05; if (Relative > 1) Relative = 1; }
            else if (e.Delta < 0) { Relative -= 0.05; if (Relative < -1) Relative = -1; }
            DP.DML($", {Relative}");
        }
        private void RelBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Relative = e.NewValue > 1 ? 1 : (e.NewValue < -1 ? -1 : e.NewValue);
        }




        //public functions
        public void UpdateLocation(Thickness p) { SetLocation(p); }
        public void ResetUpdate() { Updated = false; }
        public void UpdateData(double vol, double level, double Avl, string name, string tooltip = null)
        {
            Updated = true;
            switch (labelMode)
            {
                case LabelMode.Relative:
                    SetLabelText(SessionLabel, $"{Relative:n3}");
                    break;
                case LabelMode.Volume:
                    SetLabelText(SessionLabel, $"{vol:n3}");//string.Format("{0:n}", Transformation.Transform(vol, Transformation.TransFlow.MachineToUser)));
                    break;
                case LabelMode.Peak:
                    SetLabelText(SessionLabel, $"{level:n3}");
                    break;
                case LabelMode.AveragePeak:
                    SetLabelText(SessionLabel, $"{Avl:n3}");
                    break;
            }
            if (detailed)
            {
                SetLabelText(VolumeLabel, $"{vol:n3}");
                SetLabelText(PeakLabel, $"{level:n3}");
                SetLabelText(AvPeakLabel, $"{Avl:n3}");
                SetLabelText(RelLabel, $"{Relative:n3}");
            }
            SetBar(VolumeBar, vol);
            SetBar(RelBar, Relative);
            //lVolumeBar.Increment((int)(vol * 100) - lVolumeBar.Value);
            //double lbuf = /*Wale.Transformation.Transform(vol, Wale.Transformation.TransFlow.MachineToUser) */ level;
            SetBar(LevelBar, level);
            SetBar(AvLevelBar, Avl);
            //lLevelBar.Increment((int)(((vbuf != null) ? vbuf : 1) * level * 100) - lLevelBar.Value);
            //SetBar2(pot, (int)(((vbuf != null) ? vbuf : 1) * level * 100));
            if (lastName != name) { SetLabelText(NameLabel, name); lastName = name; }
            if (tooltip != null && lastTooltip != tooltip) { SetTooltip(NameToolTip, tooltip); lastTooltip = tooltip; }
            //lastName = name;
            //this.Refresh();
        }

        public void DetailOn()
        {
            if (!detailed)
            {
                //SessionLabel.MouseDown -= LVolume_Click;
                labelMode = LabelMode.Volume;
                SetForeColor(SessionLabel, mainColor);
                SetHeight(AppDatas.SessionBlockHeightDetail);
                ControlShowHide(SessionLabel, Visibility.Hidden);
                ControlShowHide(RelLabel, Visibility.Visible);
                ControlShowHide(PeakLabel, Visibility.Visible);
                ControlShowHide(AvPeakLabel, Visibility.Visible);
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
                //SessionLabel.MouseDown += LVolume_Click;
                labelMode = LabelMode.AveragePeak;
                SetForeColor(SessionLabel, averageColor);
                SetHeight(AppDatas.SessionBlockHeightNormal);
                ControlShowHide(SessionLabel, Visibility.Visible);
                ControlShowHide(RelLabel, Visibility.Hidden);
                ControlShowHide(PeakLabel, Visibility.Hidden);
                ControlShowHide(AvPeakLabel, Visibility.Hidden);
                //ControlAddRemove(dlReletive, false);
                //ControlAddRemove(dlPeak, false);
                //ControlAddRemove(dlAvPeak, false);
                detailed = false;
                detailChanged = true;
            }
        }


        #region Funcion delegates for MeterSet UI
        delegate void ControlVisibilityConsumer(Control control, Visibility value);
        private void ControlShowHide(Control control, Visibility value)
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    if (control != null) Dispatcher.Invoke(new ControlVisibilityConsumer(ControlShowHide), new object[] { control, value });  // invoking itself
                    return;
                }
                else
                {// the "functional part", executing only on the main thread
                    control.Visibility = value;
                }
            }
            catch { DP.CML($"fail to invoke {control.Name}"); }
        }/**/
        
        delegate void PointConsumer(Thickness loc);
        private void SetLocation(Thickness loc)
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(new PointConsumer(SetLocation), new object[] { loc });  // invoking itself
                }
                else
                {
                    // the "functional part", executing only on the main thread
                    this.Margin = loc;
                }
            }
            catch { DP.CML($"fail to invoke SetLocation"); }
        }/**/

        delegate void ControlStringConsumer(Label control, string text);
        private void SetLabelText(Label control, string text)
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    if (control != null) Dispatcher.Invoke(new ControlStringConsumer(SetLabelText), new object[] { control, text });  // invoking itself
                }
                else
                {
                    control.Content = text;      // the "functional part", executing only on the main thread
                }
            }
            catch { DP.CML($"fail to invoke {control.Name}"); }
        }/**/
        delegate void ControlTooltipConsumer(ToolTip control, string text);
        private void SetTooltip(ToolTip control, string text)
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    if (control != null) Dispatcher.Invoke(new ControlTooltipConsumer(SetTooltip), new object[] { control, text });  // invoking itself
                }
                else
                {
                    control.Content = text;// the "functional part", executing only on the main thread
                }
            }
            catch { DP.CML($"fail to invoke {control.Name}"); }
        }

        delegate void ControldoubleConsumer(ProgressBar control, double value);
        private void SetBar(ProgressBar control, double value)
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    if (control != null) Dispatcher.Invoke(new ControldoubleConsumer(SetBar), new object[] { control, value });  // invoking itself
                }
                else
                {
                    if (value > control.Maximum) value = control.Maximum;
                    else if (value < control.Minimum) value = control.Minimum;
                    control.Value = value;      // the "functional part", executing only on the main thread
                                                //control.Increment(value);
                }
            }
            catch { DP.CML($"fail to invoke {control.Name}"); }
        }/**/
        delegate void ControlSliderConsumer(Slider control, double value);
        private void SetBar(Slider control, double value)
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    if (control != null) Dispatcher.Invoke(new ControlSliderConsumer(SetBar), new object[] { control, value });  // invoking itself
                }
                else
                {
                    if (value > control.Maximum) value = control.Maximum;
                    else if (value < control.Minimum) value = control.Minimum;
                    control.Value = value;      // the "functional part", executing only on the main thread
                                                //control.Increment(value);
                }
            }
            catch { DP.CML($"fail to invoke {control.Name}"); }
        }/**/

        delegate void SetdoubleConsumer(double height);

        private void SetHeight(double height)
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(new SetdoubleConsumer(SetHeight), new object[] { height });  // invoking itself
                }
                else
                {
                    // the "functional part", executing only on the main thread
                    this.Height = height;
                }
            }
            catch { DP.CML($"fail to invoke MeterSet"); }
        }/**/

        delegate void ControlColorConsumer(Control control, Brush color);
        private void SetForeColor(Control control, Brush color)
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    if (control != null) Dispatcher.Invoke(new ControlColorConsumer(SetForeColor), new object[] { control, color });  // invoking itself
                }
                else
                {
                    control.Foreground = color;      // the "functional part", executing only on the main thread
                }
            }
            catch { DP.CML($"fail to invoke {control.Name}"); }
        }/**/

        #endregion

        public int CompareTo(MeterSet other)
        {
            // A null value means that this object is greater.
            if (other == null) return 1;
            else return this.SessionName.CompareTo(other.SessionName);
        }
        /*protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
        }/**/
    }
}
