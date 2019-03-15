using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using OxyPlot;
using OxyPlot.Series;

namespace Wale.WPF
{
    /// <summary>
    /// Interaction logic for ConfigSet.xaml
    /// </summary>
    public partial class ConfigSet : UserControl
    {
        #region Variables
        /// <summary>
        /// registry key for start at windows startup
        /// </summary>
        Microsoft.Win32.RegistryKey rkApp = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        /// <summary>
        /// stored setting that users can modify
        /// </summary>
        Wale.WPF.Properties.Settings settings = Wale.WPF.Properties.Settings.Default;
        /// <summary>
        /// datalink between MVVM
        /// </summary>
        Datalink DL = new Datalink();
        /// <summary>
        /// Debug message pack
        /// </summary>
        JDPack.DebugPack DP;
        /// <summary>
        /// Debug flag for JDPack.DebugPack
        /// </summary>
        public bool Debug { get => DP.DebugMode; set => DP.DebugMode = value; }

        AudioControl Audio;
        Window Owner;

        volatile bool NewWindow = false;

        volatile bool loaded = false;
        double originalMax;
        #endregion

        #region Initialization
        public ConfigSet()
        {
            InitializeComponent();
        }
        public ConfigSet(AudioControl audio, Datalink dl, Window owner, bool debug, bool newWindow = false)
        {
            InitializeComponent();
            MakeComponents(audio, dl, owner, debug, newWindow);
            MakeConfigs();
            MakeFinal();
        }
        private void MakeComponents(AudioControl audio, Datalink dl, Window owner, bool debug, bool newWindow)
        {
            this.DL = dl;
            this.DataContext = this.DL;

            DP = new JDPack.DebugPack(debug);

            this.Owner = owner;

            NewWindow = newWindow;
            if (NewWindow)
            {
                Owner.Title = ($"WALE - CONFIG v{AppVersion.Version}");//Console.WriteLine("T");

                SaveButton.Content = "Save and Close";//Console.WriteLine("SB C");
                CancelButton.IsEnabled = true;//Console.WriteLine("CB E");
                CancelButton.Visibility = Visibility.Visible;//Console.WriteLine("CB V");

                Owner.KeyDown += ConfigSet_KeyDown;
            }

            this.Audio = audio;
            if (Audio == null) { Log("Config Window: Audio controller is not set"); }
            else { Log($"Config Window: OK. V={Audio.MasterVolume}"); }
        }
        private void MakeConfigs()
        {
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

            settings.PropertyChanged += Settings_PropertyChanged;

            if (NewWindow) { Owner.Activate(); }
        }
        private void MakeFinal()
        {
            TargetdB.Content = VFunction.Level(settings.TargetLevel, 1);
            MinPeakdB.Content = VFunction.Level(settings.MinPeak, 1);
        }

        private void ConfigSet_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F9) settings.AdvancedView = !settings.AdvancedView;
        }
        #endregion


        #region Config Events
        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!loaded) return;
            DrawBase();
            DrawNew();
            MakeFinal();
        }

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

        private void textBoxesFocus_Enter(object sender, RoutedEventArgs e)
        {
            TextBox t = sender as TextBox;
            t.SelectionStart = t.Text.Length;
            t.SelectionLength = 0;
        }

        private void resetToDafault_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult dr = MessageBox.Show(Owner, "Do you really want to reset all configurations?", "Warning", MessageBoxButton.YesNo);
            if (dr == MessageBoxResult.Yes)
            {
                settings.Reset();
                Makes();
                JDPack.FileLog.Log("All configs are reset.");
            }
        }
        private async void ConfigSave_Click(object sender, RoutedEventArgs e)
        {
            if (NewWindow)
            {
                Owner.IsEnabled = false;
                Owner.Topmost = false;
                Owner.WindowState = WindowState.Minimized;
            }
            if (Converts() && await Register())
            {
                settings.Save();
                if (Audio != null) Audio.UpRate = settings.UpRate;
                if (!NewWindow) MakeOriginals();
                if (NewWindow)
                {
                    Owner.DialogResult = true;
                    Owner.Close();
                }
            }
            else { MessageBox.Show("Can not save Changes", "ERROR"); }
            if (NewWindow)
            {
                this.IsEnabled = true;
                Binding topmostBinding = new Binding();
                topmostBinding.Source = settings.AlwaysTop;
                BindingOperations.SetBinding(this, Window.TopmostProperty, topmostBinding);
                Owner.WindowState = WindowState.Normal;
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (NewWindow)
            {
                Owner.DialogResult = false;
                Owner.Close();
            }
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
        /// <summary>
        /// Write setting values to windows registry if it's necessary. Return (bool)true when success
        /// </summary>
        /// <returns></returns>
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

        #region Drawing
        /// <summary>
        /// Get setting values are not stored in properties.settings from registry or app at runtime.
        /// </summary>
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
        /// <summary>
        /// Store original setting values
        /// </summary>
        private void MakeOriginals()
        {
            LastValues.UIUpdateInterval = settings.UIUpdateInterval;
            LastValues.AutoControlInterval = settings.AutoControlInterval;
            LastValues.GCInterval = settings.GCInterval;
            LastValues.TargetLevel = settings.TargetLevel;
            LastValues.UpRate = settings.UpRate;
            LastValues.Kurtosis = settings.Kurtosis;
            LastValues.AverageTime = settings.AverageTime;
            LastValues.MinPeak = settings.MinPeak;
            LastValues.VFunc = settings.VFunc;
            LastValues.AudioUnit = settings.AudioUnit;
        }
        /// <summary>
        /// Draw new present graph
        /// </summary>
        private void DrawNew() { DrawGraph("Graph"); }
        /// <summary>
        /// Draw a graph of decrement function
        /// </summary>
        /// <param name="graphName">Unique identification of a visual graph</param>
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
                    graph = new FunctionSeries(new Func<double, double>((x) =>
                    {
                        double res = VFunction.Linear(x, settings.UpRate);// * 1000 / settings.AutoControlInterval;
                        //if (res > 1) { res = 1; } else if (res < 0) { res = 0; }
                        return res;
                    }), 0, 1, 0.05, graphName);
                    break;
                case VFunction.Func.SlicedLinear:
                    VFunction.FactorsForSlicedLinear sliceFactors = VFunction.GetFactorsForSlicedLinear(settings.UpRate, settings.TargetLevel);
                    graph = new FunctionSeries(new Func<double, double>((x) =>
                    {
                        double res = VFunction.SlicedLinear(x, settings.UpRate, settings.TargetLevel, sliceFactors.A, sliceFactors.B);// * 1000 / settings.AutoControlInterval;
                        //if (res > 1) { res = 1; } else if (res < 0) { res = 0; }
                        return res;
                    }), 0, 1, 0.05, graphName);
                    break;
                case VFunction.Func.Reciprocal:
                    graph = new FunctionSeries(new Func<double, double>((x) =>
                    {
                        double res = VFunction.Reciprocal(x, settings.UpRate, settings.Kurtosis);// * 1000 / settings.AutoControlInterval;
                        //if (res > 1) { res = 1; } else if (res < 0) { res = 0; }
                        return res;
                    }), 0, 1, 0.05, graphName);
                    break;
                case VFunction.Func.FixedReciprocal:
                    graph = new FunctionSeries(new Func<double, double>((x) =>
                    {
                        double res = VFunction.FixedReciprocal(x, settings.UpRate, settings.Kurtosis);// * 1000 / settings.AutoControlInterval;
                        //if (res > 1) { res = 1; } else if (res < 0) { res = 0; }
                        return res;
                    }), 0, 1, 0.05, graphName);
                    break;
                default:
                    graph = new FunctionSeries(new Func<double, double>((x) =>
                    {
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
        /// <summary>
        /// Draw a line of Base(standard) of output level
        /// </summary>
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
        /// <summary>
        /// Draw a line of boundary of section for sliced function
        /// </summary>
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


        #region Logging
        /// <summary>
        /// Write log to log tab on main window. Always prefixed "hh:mm> ".
        /// </summary>
        /// <param name="msg">message to log</param>
        /// <param name="newLine">flag for making newline after the end of the <paramref name="msg"/>.</param>
        private void Log(string msg, bool newLine = true)
        {
            //JDPack.FileLog.Log(msg, newLine);
            //DateTime t = DateTime.Now.ToLocalTime();
            //string content = $"{t.Hour:d2}:{t.Minute:d2}>{msg}";
            //if (newLine) content += "\r\n";
            //AppendText(LogScroll, content);
            LogInvoke(msg);
            //DP.DMML(content);
            //DP.CMML(content);
            //bAllowPaintLog = true;
        }

        public event EventHandler<LogEventArgs> LogInvokedEvent;
        private void LogInvoke(string msg) { LogInvokedEvent?.Invoke(this, new LogEventArgs(msg)); }
        /// <summary>
        /// pass string msg
        /// </summary>
        public class LogEventArgs : EventArgs
        {
            public string msg;
            public LogEventArgs(string msg)
            {
                this.msg = msg;
            }
        }
        #endregion
    }
    /// <summary>
    /// Storage class for last configuration values. ConfigSet
    /// </summary>
    public static class LastValues
    {
        public static event System.ComponentModel.PropertyChangedEventHandler StaticPropertyChanged;
        private static void OnStaticPropertyChanged(string propertyName)
        {
            StaticPropertyChanged?.Invoke(null, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        public static double UIUpdateInterval { get => _UIUpdate; set { _UIUpdate = value; OnStaticPropertyChanged("UIUpdateInterval"); } }
        public static double AutoControlInterval { get => _AutoControlInterval; set { _AutoControlInterval = value; OnStaticPropertyChanged("AutoControlInterval"); } }
        public static double GCInterval { get => _GCInterval; set { _GCInterval = value; OnStaticPropertyChanged("GCInterval"); } }
        public static double TargetLevel { get => _TargetLevel; set { _TargetLevel = value; OnStaticPropertyChanged("TargetLevel"); } }
        public static double AverageTime { get => _AverageTime; set { _AverageTime = value; OnStaticPropertyChanged("AverageTime"); } }
        public static double UpRate { get => _UpRate; set { _UpRate = value; OnStaticPropertyChanged("UpRate"); } }
        public static double Kurtosis { get => _Kurtosis; set { _Kurtosis = value; OnStaticPropertyChanged("Kurtosis"); } }
        public static double MinPeak { get => _MinPeak; set { _MinPeak = value; OnStaticPropertyChanged("MinPeak"); } }
        public static string VFunc { get => _VFunc; set { _VFunc = value; OnStaticPropertyChanged("VFunc"); } }
        public static int AudioUnit { get => _AudioUnit; set { _AudioUnit = value; OnStaticPropertyChanged("AudioUnit"); } }

        private static double _UIUpdate, _AutoControlInterval, _GCInterval;
        private static double _TargetLevel, _AverageTime, _UpRate, _Kurtosis, _MinPeak;
        private static string _VFunc;
        private static int _AudioUnit;
        /*
        public static int UIUpdate, AutoControlInterval, GCInterval;
        public static double TargetLevel, AverageTime, UpRate, Kurtosis, MinPeak;
        public static string VFunc;*/

        //public static bool AdvancedView { get; set; }
    }
}
