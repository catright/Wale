using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Wale.WPF
{
    /// <summary>
    /// Interaction logic for MeterSet.xaml
    /// </summary>
    public partial class MeterSet : UserControl, IComparable<MeterSet>
    {
        private readonly JPack.DebugPack DP;
        private readonly Brush foreColor = ColorSet.ForeColorBrush, mainColor = ColorSet.MainColorBrush, peakColor = ColorSet.PeakColorBrush, averageColor = ColorSet.AverageColorBrush;
        private enum LabelMode { Relative, Volume, Peak, AveragePeak };

        private bool detailed;
        private LabelMode labelMode = LabelMode.AveragePeak;
        private string lastName, lastTooltip;
        public Window Owner;

        #region public variables
        //public List<double> LastPeaks;
        public int ProcessID { get; }
        public string SessionName { get => Dispatcher?.Invoke(() => NameLabel.Text.ToString()); }
        public double Relative { get; set; } = 0;
        public bool AutoIncluded { get => (bool)Dispatcher?.Invoke(() => AutoIncludeCBox.IsChecked.Value); set => Dispatcher?.Invoke(() => AutoIncludeCBox.IsChecked = value); }
        public bool AutoIncludedChanged { get; set; } = false;
        public bool Updated { get; private set; }
        public bool Debug { get => DP.DebugMode; set => DP.DebugMode = value; }
        public bool detailChanged = false;
        public bool SoundEnabled { get => (bool)Dispatcher?.Invoke(() => SoundOnCBox.IsChecked.Value); set => Dispatcher?.Invoke(() => SoundOnCBox.IsChecked = value); }
        public bool SoundEnableChanged { get; set; } = false;
        public int AudioUnit { get; set; } = 0;
        #endregion

        #region Initialization and init methods
        public MeterSet()
        {
            InitializeComponent();
        }
        public MeterSet(Window owner, int pid, string name, string iconpath, bool detail, bool autoinc, bool dbg = false, string tooltip = null)
        {
            this.Owner = owner;
            Console.WriteLine($@"make new meterset with: {pid}, {name}, {iconpath}, {detail}, {autoinc}, {dbg}, {tooltip}");
            InitializeComponent();
            Console.WriteLine(@"meterset init ok");
            ProcessID = pid;
            Console.WriteLine(@"meterset pid ok");

            // get session icon
            if (iconpath.StartsWith("@"))
            {
                iconpath = iconpath.Substring(iconpath.IndexOf('@') + 1, iconpath.LastIndexOf(",-", StringComparison.Ordinal) - 1);
                Console.WriteLine(@"@sub ok");
                if (iconpath.Contains("%SystemRoot%"))
                {
                    iconpath = iconpath.Replace("%SystemRoot%", Environment.GetFolderPath(Environment.SpecialFolder.Windows));
                    Console.WriteLine(@"%path% ok");
                }
            }
            else if (string.IsNullOrWhiteSpace(iconpath))
            {
                iconpath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "AudioSrv.Dll");
                Console.WriteLine("Default Icon ok");
            }
            if (!string.IsNullOrWhiteSpace(iconpath)) iconpath = Path.GetFullPath(Regex.Replace(iconpath, @"\r\n?|\n", ""));
            if (File.Exists(iconpath))
            {
                //Console.WriteLine(iconpath);
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(iconpath);

                if (icon != null)
                    Icon.Source = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, new Int32Rect(0, 0, icon.Width, icon.Height), BitmapSizeOptions.FromEmptyOptions());
            }
            //Icon.Source = new BitmapImage(new Uri(iconpath, UriKind.Relative));

            DP = new JPack.DebugPack(dbg);

            AutoIncluded = autoinc;
            detailed = detail;
            if (!detailed) HideDetailedItems();

            SetFinalMake(name, tooltip);
            NameLabel.Foreground = AutoIncluded ? ColorSet.ForeColorBrush : ColorSet.MainColorBrush;
            //if (detail) DetailOn();
            //else DetailOff();

        }
        //private void Initialization(string name, string tooltip) { DetailedItems(); SetFinalMake(name, tooltip); }
        private void HideDetailedItems()
        {
            //RelLabel.Visibility = Visibility.Hidden;
            //PeakLabel.Visibility = Visibility.Hidden;
            //AvPeakLabel.Visibility = Visibility.Hidden;
            labelMode = LabelMode.AveragePeak;
            SetForeColor(SessionLabel, averageColor);
            SetHeight(Wale.Configuration.Visual.SessionBlockHeightNormal);
            ControlShowHide(SessionLabel, Visibility.Visible);
            ControlShowHide(RelLabel, Visibility.Hidden);
            ControlShowHide(PeakLabel, Visibility.Hidden);
            ControlShowHide(AvPeakLabel, Visibility.Hidden);
        }
        private void SetFinalMake(string name, string tooltip)
        {
            DP.DML(" - SetFinalMake");
            //SetText(lSessionNameLabel, name);
            NameLabel.Text = name;
            NameToolTip.Content = string.IsNullOrEmpty(tooltip) ? name : tooltip;
            //Console.WriteLine($"{LevelBar.DesiredSize}");
        }
        #endregion


        #region Item events
        private void LSessionNameLabel_Click(object sender, MouseButtonEventArgs e) { SoundEnabled = !SoundEnabled; SoundEnableChanged = true; }
        private void SoundOnCBox_Click(object sender, RoutedEventArgs e) { SoundEnableChanged = true; }
        private void AutoIncludedCBox_Click(object sender, RoutedEventArgs e) { AutoIncludedChanged = true; NameLabel.Foreground = AutoIncluded ? ColorSet.ForeColorBrush : ColorSet.MainColorBrush; }
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
            if (e.Delta > 0) { Relative += 0.05; if (Relative > Wale.Configuration.Audio.RelativeEnd) Relative = Wale.Configuration.Audio.RelativeEnd; }
            else if (e.Delta < 0) { Relative -= 0.05; if (Relative < Wale.Configuration.Audio.RelativeEndInv) Relative = Wale.Configuration.Audio.RelativeEndInv; }
            DP.DML($", {Relative}");
        }
        private void RelBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Relative = e.NewValue > Wale.Configuration.Audio.RelativeEnd ? Wale.Configuration.Audio.RelativeEnd : (e.NewValue < Wale.Configuration.Audio.RelativeEndInv ? Wale.Configuration.Audio.RelativeEndInv : e.NewValue);
        }
        #endregion



        #region public functions
        public void UpdateLocation(Thickness p) { SetLocation(p); }
        public void ResetUpdate() { Updated = false; }
        public void UpdateData(double vol, double level, double Avl, string name, string tooltip = null)
        {
            Updated = true;
            switch (labelMode)
            {
                case LabelMode.Relative:
                    SetLabelText(SessionLabel, $"{VFunction.Level(Relative, AudioUnit)}");
                    break;
                case LabelMode.Volume:
                    SetLabelText(SessionLabel, $"{VFunction.Level(vol, AudioUnit)}");//string.Format("{0:n}", Transformation.Transform(vol, Transformation.TransFlow.MachineToUser)));
                    break;
                case LabelMode.Peak:
                    SetLabelText(SessionLabel, $"{VFunction.Level(level, AudioUnit)}");
                    break;
                case LabelMode.AveragePeak:
                    SetLabelText(SessionLabel, $"{VFunction.Level(Avl, AudioUnit)}");
                    break;
            }
            if (detailed)
            {
                SetLabelText(VolumeLabel, $"{VFunction.Level(vol, AudioUnit)}");
                SetLabelText(PeakLabel, $"{VFunction.Level(level, AudioUnit)}");
                SetLabelText(AvPeakLabel, $"{VFunction.Level(Avl, AudioUnit)}");
                SetLabelText(RelLabel, $"{VFunction.RelLv(Relative, AudioUnit)}");
            }
            SetBar(VolumeBar, vol);
            SetBar(RelBar, Relative);
            //lVolumeBar.Increment((int)(vol * 100) - lVolumeBar.Value);
            //double lbuf = /*Wale.Transformation.Transform(vol, Wale.Transformation.TransFlow.MachineToUser) */ level;
            SetBar(LevelBar, level);
            SetBar(AvLevelBar, Avl);
            //lLevelBar.Increment((int)(((vbuf != null) ? vbuf : 1) * level * 100) - lLevelBar.Value);
            //SetBar2(pot, (int)(((vbuf != null) ? vbuf : 1) * level * 100));
            if (lastName != name) { SetTextBlockText(NameLabel, name); lastName = name; }
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
                SetHeight(Wale.Configuration.Visual.SessionBlockHeightDetail);
                ControlShowHide(SessionLabel, Visibility.Hidden);
                ControlShowHide(RelLabel, Visibility.Visible);
                ControlShowHide(PeakLabel, Visibility.Visible);
                ControlShowHide(AvPeakLabel, Visibility.Visible);
                //ControlAddRemove(dlReletive, true);
                //ControlAddRemove(dlPeak, true);
                //ControlAddRemove(dlAvPeak, true);
                detailed = true;
                detailChanged = true;
                DetailChanged?.Invoke(this, new EventArgs());
            }
        }
        public void DetailOff()
        {
            if (detailed)
            {
                //SessionLabel.MouseDown += LVolume_Click;
                labelMode = LabelMode.AveragePeak;
                SetForeColor(SessionLabel, averageColor);
                SetHeight(Wale.Configuration.Visual.SessionBlockHeightNormal);
                ControlShowHide(SessionLabel, Visibility.Visible);
                ControlShowHide(RelLabel, Visibility.Hidden);
                ControlShowHide(PeakLabel, Visibility.Hidden);
                ControlShowHide(AvPeakLabel, Visibility.Hidden);
                //ControlAddRemove(dlReletive, false);
                //ControlAddRemove(dlPeak, false);
                //ControlAddRemove(dlAvPeak, false);
                detailed = false;
                detailChanged = true;
                DetailChanged?.Invoke(this, new EventArgs());
            }
        }
        public event EventHandler DetailChanged;
        #endregion


        #region Funcion delegates for MeterSet UI
        delegate void ControlVisibilityConsumer(Control control, Visibility value);
        private void ControlShowHide(Control control, Visibility value)
        {
            try
            {
                if (control?.Visibility == null || control.Visibility == value) return;
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(new ControlVisibilityConsumer(ControlShowHide), control, value);  // invoking itself
                }
                else
                {// the "functional part", executing only on the main thread
                    control.Visibility = value;
                }
            }
            catch { if (control != null) DP.CML($"fail to invoke {control.Name}"); }
        }/**/

        delegate void PointConsumer(Thickness loc);
        private void SetLocation(Thickness loc)
        {
            try
            {
                if (this.Margin == loc) return;
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(new PointConsumer(SetLocation), loc);  // invoking itself
                }
                else
                {
                    // the "functional part", executing only on the main thread
                    this.Margin = loc;
                }
            }
            catch { DP.CML($"fail to invoke SetLocation"); }
        }/**/

        delegate void TextBlockStringConsumer(TextBlock control, string text);
        private void SetTextBlockText(TextBlock control, string text)
        {
            try
            {
                if (control?.Text == null || control.Text == text) return;
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(new TextBlockStringConsumer(SetTextBlockText), control, text);  // invoking itself
                }
                else
                {
                    control.Text = text;      // the "functional part", executing only on the main thread
                }
            }
            catch { if (control != null) DP.CML($"fail to invoke {control.Name}"); }
        }/**/
        delegate void ControlStringConsumer(Label control, string text);
        private void SetLabelText(Label control, string text)
        {
            try
            {
                if (control?.Content == null || (string)control.Content == text) return;
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(new ControlStringConsumer(SetLabelText), control, text);  // invoking itself
                }
                else
                {
                    control.Content = text;      // the "functional part", executing only on the main thread
                }
            }
            catch { if (control != null) DP.CML($"fail to invoke {control.Name}"); }
        }/**/
        delegate void ControlTooltipConsumer(ToolTip control, string text);
        private void SetTooltip(ToolTip control, string text)
        {
            try
            {
                if (control?.Content == null || (string)control.Content == text) return;
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(new ControlTooltipConsumer(SetTooltip), control, text);  // invoking itself
                }
                else
                {
                    control.Content = text;// the "functional part", executing only on the main thread
                }
            }
            catch { if (control != null) DP.CML($"fail to invoke {control.Name}"); }
        }

        delegate void ControldoubleConsumer(ProgressBar control, double value);
        private void SetBar(ProgressBar control, double value)
        {
            try
            {
                if (control?.Value == null || Math.Abs(control.Value - value) == 0) return;
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(new ControldoubleConsumer(SetBar), control, value);  // invoking itself
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
                if (control?.Value == null || Math.Abs(control.Value - value) == 0) return;
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(new ControlSliderConsumer(SetBar), control, value);  // invoking itself
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
                if (Math.Abs(Height - height) == 0) return;
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(new SetdoubleConsumer(SetHeight), height);  // invoking itself
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
                if (control?.Foreground == null || control.Foreground == color) return;
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(new ControlColorConsumer(SetForeColor), control, color);  // invoking itself
                }
                else
                {
                    control.Foreground = color;      // the "functional part", executing only on the main thread
                }
            }
            catch { if (control != null) DP.CML($"fail to invoke {control.Name}"); }
        }/**/

        private void ManualSet_Click(object sender, RoutedEventArgs e)
        {
            SessionManualSet sms = new SessionManualSet(this.Owner, $"PID{ProcessID}:{SessionName}", Relative);
            var stay = Properties.Settings.Default.StayOn;
            Properties.Settings.Default.StayOn = true;
            if (sms.ShowDialog() == true)
            {
                if (this.AutoIncluded) { this.AutoIncluded = false; this.AutoIncludedChanged = true; }

                this.Relative = sms.Relative;
            }
            Properties.Settings.Default.StayOn = stay;
        }

        #endregion

        public int CompareTo(MeterSet other)
        {
            // A null value means that this object is greater.
            if (other == null) return 1;
            //else return this.ProcessID.CompareTo(other.ProcessID);
            //else return this.SessionName.CompareTo(other.SessionName);
            else
            {
                if (this.ProcessID == 0) { return 1; }
                int res = this.SessionName.CompareTo(other.SessionName);
                //if (res != 0) res = this.ProcessID.CompareTo(other.ProcessID);
                return res;
            }
        }
        /*protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
        }/**/
    }
}
