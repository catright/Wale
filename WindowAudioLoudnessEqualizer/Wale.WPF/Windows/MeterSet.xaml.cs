using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Wale.Extensions;

namespace Wale.WPF
{
    public enum LabelMode { Relative, Volume, Peak, AveragePeak };

    /// <summary>
    /// Interaction logic for MeterSet.xaml
    /// </summary>
    public partial class MeterSet : UserControl, IComparable<MeterSet>, INotifyPropertyChanged
    {
        #region system variables and events
        private readonly JPack.MPack DP;
#pragma warning disable IDE1006 // Naming Styles
        public Configs.General gl { get; private set; }
        public CoreAudio.Session s { get; private set; }
#pragma warning restore IDE1006 // Naming Styles

        internal Window Owner;

        public event EventHandler<string> Logged;
        private void Log(string msg, [CallerMemberName] string caller = null) => Logged?.Invoke(this, M.Message(msg, gl.VerboseLog ? caller : null, true));
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public event EventHandler SessionChanged;
        public event EventHandler<Guid> SessionExpired;
        #endregion
        #region public variables
        private string lastName, lastTooltip;
        private LabelMode _LMode = LabelMode.AveragePeak;
        public LabelMode LMode { get => _LMode; set { _LMode = value; NotifyPropertyChanged(); } }
        public bool Debug { get => DP.Enable; set => DP.Enable = value; }
        public Guid ID => s.ID;
        public int ProcessID => s.ProcessID;
        public string SessionName => s.Name;
        public string SessionIdent => s.SessionIdentifier;
        public bool Updated { get; private set; }
        #endregion

        #region Initialization and init methods
        public MeterSet() => throw new NotImplementedException();
        public MeterSet(Window owner, CoreAudio.Session session, bool dbg = false)
        {
            Owner = owner;
            if (owner is MainWindow m) gl = m.gl;
            else throw new ArgumentException("MeterSet: owner is not a MainWindow");
            gl.PropertyChanged += (sender, e) => Gl_PropertyChanged(sender, e);
            s = session;
            s.SessionExpired += (sender, id) => SessionExpired?.Invoke(sender, id);
            // set debug
            DP = new JPack.MPack(dbg);
//#if DEBUG
//            if (dbg) Console.WriteLine($@"make new meterset with: {s.ProcessID}, {s.Name}, {s.Icon}, {gl.AdvancedView}, {s.Auto}, {dbg}");
//#endif
            InitializeComponent();
            DataContext = gl;
#if DEBUG
            if (dbg) Console.WriteLine(@"meterset init ok");
#endif

            // get session icon
            string iconpath = s.Icon;
            if (iconpath.StartsWith("@"))
            {
                iconpath = iconpath.Substring(iconpath.IndexOf('@') + 1, iconpath.LastIndexOf(",-", StringComparison.Ordinal) - 1);
                //Console.WriteLine(@"@sub ok");
                if (iconpath.Contains("%SystemRoot%"))
                {
                    iconpath = iconpath.Replace("%SystemRoot%", Environment.GetFolderPath(Environment.SpecialFolder.Windows));
                    //Console.WriteLine(@"%path% ok");
                }
            }
            else if (string.IsNullOrWhiteSpace(iconpath))
            {
                iconpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "AudioSrv.Dll");
                //Console.WriteLine("Default Icon ok");
            }
            if (!string.IsNullOrWhiteSpace(iconpath)) iconpath = Path.GetFullPath(Regex.Replace(iconpath, @"\r\n?|\n", ""));
            if (File.Exists(iconpath))
            {
                //Console.WriteLine(iconpath);
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(iconpath);

                if (icon != null)
                    Icon.Source = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, new Int32Rect(0, 0, icon.Width, icon.Height), BitmapSizeOptions.FromEmptyOptions());
            }

            // check auto
            NameLabel.Foreground = s.Auto ? ColorSet.ForeColorBrush : ColorSet.MainColorBrush;
            // check averaging
            LMode = gl.Averaging ? LabelMode.AveragePeak : LabelMode.Peak;

            // make proper name, not update
            if (gl.VerboseLog) Log($"Make proper name of {s.Name}({s.ProcessID})");
            SetName();

            SoundOnCBox.IsChecked = !s.Muted;
            AutoIncludeCBox.IsChecked = s.Auto;
        }

        private void Gl_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Averaging")
            {
                if (gl.Averaging) LMode = LabelMode.AveragePeak;
                else LMode = LabelMode.Peak;
            }
        }

        private void SetName(bool update = false)
        {
            try
            {
                if (update) s.UpdateProcessInfo();
                string mt = (s.DisplayName != s.MainWindowTitle ? s.MainWindowTitle : ""),
                    name = (gl.MainTitleforAppname ? $"{s.Name} {mt}" : s.Name),
                    pname = s.ProcessName;
                if (gl.PnameForAppname)
                {
                    name = (gl.MainTitleforAppname ? $"{s.ProcessName} {mt}" : s.ProcessName);
                    pname = s.Name;
                }
                if (Debug) Console.WriteLine($"{name}({s.ProcessID}) {s.DisplayName} / {s.ProcessName} / {s.Icon} / {mt} / {s.SessionIdentifier}"); // debug msg with proper name
                string tooltip = $"{pname}({s.ProcessID}) {mt}";
                if (Debug) Console.WriteLine(tooltip);

                if (lastName != name) { Dispatcher?.Invoke(() => NameLabel.Text = name); lastName = name; }
                if (lastTooltip != tooltip) { Dispatcher?.Invoke(() => NameToolTip.Content = tooltip); lastTooltip = tooltip; }
            }
            catch (Exception e) { if (e.HResult.IsUnknown() && gl.VerboseLog) Log(e.Message); }
        }
        #endregion


        #region Item events
        private void LSessionNameLabel_Click(object sender, MouseButtonEventArgs e) => SoundOnCBox.IsChecked = !SoundOnCBox.IsChecked;
        private void AutoIncludedCBox_Click(object sender, RoutedEventArgs e)
        {
            bool auto = s.Auto;
            string name = "";
            lock (s.Locker) { name = s.Name; }

            NameLabel.Foreground = auto ? ColorSet.ForeColorBrush : ColorSet.MainColorBrush;
            if (auto && gl.ExcList.Contains(name)) { gl.ExcList.RemoveAll(x => x == name); gl.Save(); }
            else if (!auto && !gl.ExcList.Contains(name)) { gl.ExcList.Add(name); gl.Save(); }
            SessionChanged?.Invoke(this, null);
        }
        private void LSessionValueLabel_Click(object sender, MouseButtonEventArgs e)
        {
            if (!gl.AdvancedView)
            {
                switch (LMode)
                {
                    case LabelMode.Relative:
                        LMode = LabelMode.Volume;
                        break;
                    case LabelMode.Volume:
                        LMode = LabelMode.Peak;
                        break;
                    case LabelMode.Peak:
                        LMode = LabelMode.AveragePeak;
                        break;
                    case LabelMode.AveragePeak:
                        LMode = LabelMode.Relative;
                        break;
                }
            }
        }
        private void MeterSet_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            DP.D($"{s.Name}({ProcessID}) MeterSet_MouseWheel {e.Delta}", newLine: false);
            if (e.Delta > 0) { lock (s.Locker) { s.Relative += 0.05f; } }
            else if (e.Delta < 0) { lock (s.Locker) { s.Relative -= 0.05f; } }
            DP.D($", {s.Relative}");
        }
        private void RelBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => SessionChanged?.Invoke(this, null);
        private void ManualSet_Click(object sender, RoutedEventArgs e)
        {
            SessionManualSet sms = new SessionManualSet(Owner, $"PID{ProcessID}:{s.Name}", s.Relative);
            var stay = gl.StayOn;
            gl.StayOn = true;
            if (sms.ShowDialog() == true)
            {
                //if (this.AutoIncluded) { this.AutoIncluded = false; this.AutoIncludedChanged = true; }
                lock (s.Locker) { s.Relative = (float)sms.Relative; }
            }
            gl.StayOn = stay;
        }
        #endregion



        //public void UpdateLocation(Thickness p) => Dispatcher?.Invoke(() => this.Margin = p);
        //public void ResetUpdate() => Updated = false;
        /// <returns><c>true</c> when session expired or error has occured</returns>
        public bool UpdateData(string DeviceId, bool updateName = false)
        {
            try
            {
                if (s == null || DeviceId != s.DeviceID) { return true; }// || s.State.IsExpired() SessionExpired?.Invoke(this, ID);
                SetName(updateName);
                //if (Debug) { Console.WriteLine($"{vol}, {peak}, {avp}, {s.Name}, {lastTooltip}"); }
            }
            catch (Exception e)
            {
                if (e.HResult.IsUnknown()) Log(e.Message);
                return true;
            }
            return false;
        }



        public int CompareTo(MeterSet other)
        {
            // A null value means that this object is greater.
            if (other == null) return 1;
            else return s.Name.CompareTo(other.s.Name);
        }
        public int CompareTo(string otherName)
        {
            // A null value means that this object is greater.
            if (string.IsNullOrEmpty(otherName)) return 1;
            else return s.Name.CompareTo(otherName);
        }
    }
}
