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
    public partial class MeterSet : UserControl
    {
        private JDPack.DebugPack DP;
        private Brush foreColor = ColorSet.ForeColorBrush, mainColor = ColorSet.MainColorBrush, peakColor = ColorSet.PeakColorBrush, averageColor = ColorSet.AverageColorBrush;
        private enum LabelMode { Relative, Volume, Peak, AveragePeak };

        private int _relative = 0;
        private bool detailed;//, _disposed = false;
        private LabelMode labelMode = LabelMode.AveragePeak;
        private string lastName;

        //public variables
        //public List<double> LastPeaks;
        public uint ID { get; }
        public string SessionName { get => NameLabel.Content.ToString(); }
        public double Relative { get => ((double)_relative / 100.0); }
        public bool AutoIncluded { get => AutoIncludeCBox.IsChecked.Value; private set => AutoIncludeCBox.IsChecked = value; }
        public bool Updated { get; private set; }
        public bool Debug { get => DP.DebugMode; set => DP.DebugMode = value; }
        public bool detailChanged = false;

        //Initialization and init methods
        public MeterSet()
        {
            InitializeComponent();
        }
        public MeterSet(uint id, string name, bool detail, bool autoinc, bool dbg = false)
        {
            InitializeComponent();
            ID = id;
            DP = new JDPack.DebugPack(dbg);

            Initialization(name);
            detailed = !detail;
            AutoIncluded = autoinc;
            //if (detail) DetailOn();
            //else DetailOff();
        }
        private void Initialization(string name) { DetailedItems(); SetFinalMake(name); }
        private void DetailedItems()
        {
            RelLabel.Visibility = Visibility.Hidden;
            PeakLabel.Visibility = Visibility.Hidden;
            AvPeakLabel.Visibility = Visibility.Hidden;
        }
        private void SetFinalMake(string name)
        {
            DP.DML(" - SetFinalMake");
            //SetText(lSessionNameLabel, name);
            NameToolTip.Content = name;
            //Console.WriteLine($"{LevelBar.DesiredSize}");
        }


        //Item events
        private void LSessionNameLabel_Click(object sender, MouseButtonEventArgs e) { AutoIncludeCBox.IsChecked = !AutoIncludeCBox.IsChecked; }
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
            DP.DM($"{SessionName}({ID}) MeterSet_MouseWheel {e.Delta}");
            if (e.Delta > 0) { _relative++; if (_relative > 100) _relative = 100; }
            else if (e.Delta < 0) { _relative--; if (_relative < -100) _relative = -100; }
            DP.DML($", {Relative}");
        }



        //public functions
        public void UpdateLocation(Thickness p) { SetLocation(p); }
        public void ResetUpdate() { Updated = false; }
        public void UpdateData(double vol, double level, double Avl, string name)
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
            //lVolumeBar.Increment((int)(vol * 100) - lVolumeBar.Value);
            //double lbuf = /*Wale.Transformation.Transform(vol, Wale.Transformation.TransFlow.MachineToUser) */ level;
            SetBar(LevelBar, level);
            SetBar(AvLevelBar, Avl);
            //lLevelBar.Increment((int)(((vbuf != null) ? vbuf : 1) * level * 100) - lLevelBar.Value);
            //SetBar2(pot, (int)(((vbuf != null) ? vbuf : 1) * level * 100));
            if (lastName != name)
            {
                SetLabelText(NameLabel, name);
                SetTooltip(NameLabel, name);
            }
            lastName = name;
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
        private void SetTooltip(Control control, string text)
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    if (control != null) Dispatcher.Invoke(new ControlStringConsumer(SetTooltip), new object[] { control, text });  // invoking itself
                }
                else
                {
                    NameToolTip.Content = text;// the "functional part", executing only on the main thread
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

        /*protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
        }/**/
    }
}
