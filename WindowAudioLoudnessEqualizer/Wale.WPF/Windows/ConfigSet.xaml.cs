using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Wale.Configs;
using Wale.Controller;
using Wale.Extensions;

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
        private readonly Microsoft.Win32.RegistryKey rkApp = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        /// <summary>
        /// stored setting that users can modify
        /// </summary>
        private readonly General gl;
        /// <summary>
        /// Debug message pack
        /// </summary>
        private readonly JPack.MPack DP;
        /// <summary>
        /// Debug flag for JPack.DebugPack
        /// </summary>
        public bool Debug { get => DP.Enable; set => DP.Enable = value; }

        private readonly DFactors vff;

        //AudioControl Audio;
        private readonly Window Owner;

        private volatile bool NewWindow = false;

        private volatile bool loaded = false;
        private double OriginalMax = 0.1, YMax = 0.1;//, YScale = 0.1;
        #endregion

        #region Initialization
        public ConfigSet() { InitializeComponent(); }
        public ConfigSet(Window owner, bool debug, bool newWindow = false)
        {
            InitializeComponent();

            Owner = owner;
            if (owner is MainWindow m) this.gl = m.gl;
            else if (owner is Configuration c) this.gl = c.gl;
            else throw new ArgumentException("ConfigSet: owner is not a MainWindow or a Configuration");
            this.DataContext = this.gl;

            DP = new JPack.MPack(debug);
            vff = new DFactors(gl);

            MakeComponents(newWindow);
            MakeConfigs();
            MakeFinal();
        }
        private void MakeComponents(bool newWindow)
        {
            NewWindow = newWindow;
            if (NewWindow)
            {
                Owner.Title = ($"WALE - CONFIG v{AppVersion.Version}");//Console.WriteLine("T");

                SaveButton.Content = "Save and Close";//Console.WriteLine("SB C");
                CancelButton.IsEnabled = true;//Console.WriteLine("CB E");
                CancelButton.Visibility = Visibility.Visible;//Console.WriteLine("CB V");

                Owner.KeyDown += ConfigSet_KeyDown;
            }

            //this.Audio = audio;
            //if (Audio == null) { Log("Config Window: Audio controller is not set"); }
            //else { Log($"Config Window: OK. V={Audio.MasterVolume}"); }
        }
        private void MakeConfigs()
        {
            Makes();
            MakeOriginals();
            loaded = true;

            if (!Enum.TryParse(FunctionSelector.SelectedItem.ToString(), true, out DType selectedFunction)) return;
            if (selectedFunction == DType.Reciprocal || selectedFunction == DType.FixedReciprocal) KurtosisBox.IsEnabled = true;
            else KurtosisBox.IsEnabled = false;

            plotView.Model = new PlotModel();//ColorSet.BackColorAltBrush

            //DrawDevideLine();
            DrawGraph("Original");
            DrawBase();
            DrawNew();

            plotView.Model.TextColor = Color(ColorSet.ForeColor);
            plotView.Model.PlotAreaBorderColor = Color(ColorSet.ForeColorAlt);
            //plotView.InvalidateVisual();

            gl.PropertyChanged += Settings_PropertyChanged;

            if (NewWindow) { Owner.Activate(); }
        }
        private void MakeFinal()
        {
            Dispatcher?.Invoke(() =>
            {
                TargetdB.Content = gl.TargetLevel.Level(1);
                LimitdB.Content = gl.LimitLevel.Level(1);
                MinPeakdB.Content = gl.MinPeak.Level(1);
            });
        }
        private void ConfigSet_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F9) gl.AdvancedView = !gl.AdvancedView;
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
        private void LimitLevel_Changed(object sender, TextChangedEventArgs e)
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
            if (!Enum.TryParse((sender as ComboBox).SelectedItem.ToString(), true, out DType selectedFunction)) return;
            //Console.WriteLine($"Fnc Chg{selectedFunction}");
            //if (selectedFunction == Controller.FVol.None.ToString()) { textBox5.Enabled = false; }
            //else { textBox5.Enabled = true; }
            if (selectedFunction == DType.Reciprocal || selectedFunction == DType.FixedReciprocal) KurtosisBox.IsEnabled = true;
            else KurtosisBox.IsEnabled = false;
            DrawNew();
        }

        private void TextBoxesFocus_Enter(object sender, RoutedEventArgs e)
        {
            TextBox t = sender as TextBox;
            t.SelectionStart = t.Text.Length;
            t.SelectionLength = 0;
        }

        private void ResetToDafault_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult dr = MessageBox.Show(Owner, "Do you really want to reset all configurations?", "Warning", MessageBoxButton.YesNo);
            if (dr == MessageBoxResult.Yes)
            {
                gl.Reset();
                Makes();
                JPack.Log.Write("All configs are reset.");
            }
        }
        private async void ConfigSave_Click(object sender, RoutedEventArgs e)
        {
            SavedMessageCancelTKSources.ForEach(TKSource => TKSource.Cancel());
            if (NewWindow)
            {
                Owner.IsEnabled = false;
                Owner.Topmost = false;
                Owner.WindowState = WindowState.Minimized;
            }
            if (Converts() && await Register())
            {
                gl.Save();
                //if (Audio != null) Audio.UpRate = settings.UpRate;
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
                Binding topmostBinding = new Binding
                {
                    Source = gl.AlwaysTop
                };
                BindingOperations.SetBinding(this, Window.TopmostProperty, topmostBinding);
                Owner.WindowState = WindowState.Normal;
            }
            CancellationTokenSource SavedMessageCancelTKSource = new CancellationTokenSource();
            SavedMessageCancelTKSources.Add(SavedMessageCancelTKSource);
            try { await ShowSavedMessage(SavedMessageCancelTKSource.Token); }
            catch (OperationCanceledException) { JPack.M.C("Saved Msg Canceled. This is normal operation"); }
            finally { SavedMessageCancelTKSources.Remove(SavedMessageCancelTKSource); SavedMessageCancelTKSource.Dispose(); }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (NewWindow)
            {
                Owner.DialogResult = false;
                Owner.Close();
            }
        }


        //private volatile bool SavedMessageShowing = false;
        private readonly List<CancellationTokenSource> SavedMessageCancelTKSources = new List<CancellationTokenSource>();
        private async Task ShowSavedMessage(CancellationToken cancellationToken, int keepTime = 1600, int fadeTime = 300)
        {
            Action cancelAction = new Action(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    //SavedNoti.Opacity = 0;
                    //SaveButton.Content = "Save";
                    cancellationToken.ThrowIfCancellationRequested();
                }
            });
            int fadeStage = fadeTime / 10;

            //string SaveButtonContent = SaveButton.Content.ToString();
            SaveButton.Content = string.Empty;
            cancelAction();

            for (int i = 0; i < fadeTime; i += fadeStage)
            {
                double divide = (double)(i + fadeStage) / fadeTime;
                SavedNoti.Opacity = divide;
                //DL.WikiLinkBrush.Opacity = 1.0 - divide;
                try { await Task.Delay(fadeStage, cancellationToken); }
                catch (OperationCanceledException) { cancelAction(); }
            }

            try { await Task.Delay(keepTime, cancellationToken); }
            catch (OperationCanceledException) { cancelAction(); }

            for (int i = 0; i < fadeTime; i += fadeStage)
            {
                double divide = (double)(i + fadeStage) / fadeTime;
                SavedNoti.Opacity = 1.0 - divide;
                //DL.ClipboardCopiedMessageBrush.Opacity = 1.0 - divide;
                try { await Task.Delay(fadeStage, cancellationToken); }
                catch (OperationCanceledException) { cancelAction(); }
            }
            SaveButton.Content = "Save";
        }


        private bool Converts()
        {
            //System.Diagnostics.Stopwatch st = new System.Diagnostics.Stopwatch();
            //Console.WriteLine("Convert");
            bool success = true, auto = gl.AutoControl;
            gl.AutoControl = false;
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
                gl.VFunc = FunctionSelector.SelectedValue.ToString();
            }
            catch { success = false; JPack.Log.Write("Error: Config - Convert failure"); }
            finally { gl.AutoControl = auto; }
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
            catch { success = false; JPack.Log.Write("Error: Config - Register failure"); }
            //Console.WriteLine("resister End");
            return success;
        }


        private void Priority_RadioButton_Click(object sender, RoutedEventArgs e)
        {
            if (!loaded) { e.Handled = true; return; }
            RadioButton s = sender as RadioButton;
            if ((bool)s.IsChecked) SetPriority(s.Content.ToString());
        }
        private void SetPriority(string priority, bool force = false)
        {
            if (gl.ProcessPriority == priority && !force) return;
            Log($"Set process priority {priority}");
            gl.ProcessPriority = priority;
            ProcessPriorityClass ppc = ProcessPriorityClass.Normal;
            switch (priority)
            {
                case "High": ppc = ProcessPriorityClass.High; break;
                case "Above Normal": ppc = ProcessPriorityClass.AboveNormal; break;
                case "Normal": ppc = ProcessPriorityClass.Normal; break;
            }
            //JPack.Debug.CML($"SPP H={DL.ProcessPriorityHigh} A={DL.ProcessPriorityAboveNormal} N={DL.ProcessPriorityNormal}");
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

            FunctionSelector.ItemsSource = Enum.GetValues(typeof(DType));
            if (Enum.TryParse(gl.VFunc, out DType f)) FunctionSelector.SelectedItem = f;
        }
        /// <summary>
        /// Store original setting values
        /// </summary>
        private void MakeOriginals()
        {
            LastValues.UIUpdateInterval = gl.UIUpdateInterval;
            LastValues.AutoControlInterval = gl.AutoControlInterval;
            LastValues.GCInterval = gl.GCInterval;
            LastValues.AudioUnit = gl.AudioUnit;
            LastValues.TargetLevel = gl.TargetLevel;
            LastValues.LimitLevel = gl.LimitLevel;
            LastValues.CompRate = gl.CompRate;
            LastValues.MinPeak = gl.MinPeak;
            LastValues.UpRate = gl.UpRate;
            LastValues.Kurtosis = gl.Kurtosis;
            LastValues.VFunc = gl.VFunc;
            LastValues.AverageTime = gl.AverageTime;
        }
        /// <summary>
        /// Draw new present graph
        /// </summary>
        private void DrawNew()
        {
            try { Owner.Dispatcher?.Invoke(() => DrawGraph("Graph")); }
            catch (TaskCanceledException) { }
        }
        /// <summary>
        /// Draw a graph of decrement function
        /// </summary>
        /// <param name="graphName">Unique identification of a visual graph</param>
        private void DrawGraph(string graphName)
        {
            List<Series> existing = new List<Series>();
            foreach (Series s in plotView.Model.Series) { if (s.Title == graphName) existing.Add(s); }
            foreach (Series s in existing) { plotView.Model.Series.Remove(s); }
            existing.Clear();

            if (!Enum.TryParse(FunctionSelector.SelectedItem.ToString(), out DType f)) { Log("ConfigSet(DrawGraph): Failed to get volume function"); return; }

            FunctionSeries graph = new FunctionSeries(new Func<double, double>(x => f.Calc(x, vff) / vff.URratio), 0, 1, 0.05, graphName);

            plotView.Model.Series.Add(graph);
            plotView.Model.InvalidatePlot(true);

            if (graph.Title == "Original")
            {
                graph.Color = Color(ColorSet.MainColor);
                YMax = OriginalMax = graph.MaxY;
            }
            else
            {
                graph.Color = Color(ColorSet.PeakColor);
                YMax = Math.Max(graph.MaxY, OriginalMax);
            }

            //for (double i = 1.0; i > 0.00001; i /= 2)
            //{
            //    if (YMax > i) { YScale = i * 2.0; break; }
            //}
            //M.D($"{graph.Title} {graph.MaxY} {OriginalMax} {YMax} {YScale}");

            //plotView.Model.DefaultYAxis?.Zoom(yScale);
            //plotView.Model.DefaultYAxis.AbsoluteMaximum = yScale;
            //foreach (var g in plotView.Model.Series) { }
            //plotView.ZoomAllAxes(yScale);
            //if (maxVal > 0.01) chart.ChartAreas["Area"].AxisY.LabelStyle.Format = "G";
            //else chart.ChartAreas["Area"].AxisY.LabelStyle.Format = "#.#E0";
            //myText1.Y = chart.ChartAreas["Area"].AxisY.Maximum;
            //myText2.Y = chart.ChartAreas["Area"].AxisY.Maximum * 0.92;

            //plotView.InvalidatePlot();
            DrawBase();
        }
        /// <summary>
        /// Draw a line of Base(standard) of output level
        /// </summary>
        private void DrawBase()
        {
            try { Owner.Dispatcher?.Invoke(DrawBaseInside); }
            catch (TaskCanceledException) { }
        }
        private void DrawBaseInside()
        {
            List<Series> existing = new List<Series>();
            foreach (Series s in plotView.Model.Series) { if (s.Title == "Base" || s.Title == "Limit") existing.Add(s); }
            foreach (Series s in existing) { plotView.Model.Series.Remove(s); }
            existing.Clear();

            //double y = YScale;// > 0.1 ? 1 : (YScale > 0.01 ? 0.1 : (YScale > 0.001 ? 0.01 : 0.001));

            LineSeries lineSeries1 = new LineSeries { Title = "Base" };
            lineSeries1.Points.Add(new DataPoint(gl.TargetLevel, 0));
            lineSeries1.Points.Add(new DataPoint(gl.TargetLevel, YMax));
            lineSeries1.Color = Color(ColorSet.TargetColor);
            plotView.Model.Series.Add(lineSeries1);

            LineSeries lineSeries2 = new LineSeries { Title = "Limit" };
            lineSeries2.Points.Add(new DataPoint(gl.LimitLevel, 0));
            lineSeries2.Points.Add(new DataPoint(gl.LimitLevel, YMax));
            lineSeries2.Color = Color(ColorSet.LimitColor);
            plotView.Model.Series.Add(lineSeries2);

            plotView.Model.InvalidatePlot(true);
            //plotView.Model.DefaultYAxis.Zoom(y);
            plotView.InvalidatePlot();
        }
        /// <summary>
        /// Draw a line of boundary of section for sliced function
        /// </summary>
        //private void DrawDevideLine()
        //{
        //    LineSeries lineSeries1 = new LineSeries();
        //    lineSeries1.Points.Add(new DataPoint(0, 0));
        //    lineSeries1.Points.Add(new DataPoint(1, 1));
        //    lineSeries1.Color = Color(ColorSet.ForeColor);
        //    plotView.Model.Series.Add(lineSeries1);
        //    plotView.InvalidatePlot();
        //}
        private OxyColor Color(Color color) => OxyColor.FromArgb(color.A, color.R, color.G, color.B);
        #endregion


        #region Logging
        /// <summary>
        /// Write log to log tab on main window. Always prefixed "hh:mm> ".
        /// </summary>
        /// <param name="msg">message to log</param>
        /// <param name="newLine">flag for making newline after the end of the <paramref name="msg"/>.</param>
        /// <param name="caller"><see cref="System.Runtime.CompilerServices.CallerMemberNameAttribute"/></param>
        private void Log(string msg, bool newLine = true, [System.Runtime.CompilerServices.CallerMemberName] string caller = null)
            => LogInvokedEvent?.Invoke(this, new LogEventArgs(msg, newLine, caller));

        public event EventHandler<LogEventArgs> LogInvokedEvent;
        public class LogEventArgs : EventArgs
        {
            public string Message { get; private set; }
            public bool NewLine { get; private set; }
            public string Caller { get; private set; }
            public LogEventArgs(string msg, bool newline = true, string caller = null)
            {
                Message = msg;
                NewLine = newline;
                Caller = caller;
            }
        }
        #endregion
    }
    /// <summary>
    /// Storage class for last configuration values. ConfigSet
    /// </summary>
    public static class LastValues
    {
        public static double UIUpdateInterval { get => Get<double>(); set => Set(value); }
        public static double AutoControlInterval { get => Get<double>(); set => Set(value); }
        public static double GCInterval { get => Get<double>(); set => Set(value); }
        public static double TargetLevel { get => Get<double>(); set => Set(value); }
        public static double LimitLevel { get => Get<double>(); set => Set(value); }
        public static double CompRate { get => Get<double>(); set => Set(value); }
        public static double AverageTime { get => Get<double>(); set => Set(value); }
        public static double UpRate { get => Get<double>(); set => Set(value); }
        public static double Kurtosis { get => Get<double>(); set => Set(value); }
        public static double MinPeak { get => Get<double>(); set => Set(value); }
        public static string VFunc { get => Get<string>(); set => Set(value); }
        public static int AudioUnit { get => Get<int>(); set => Set(value); }

        #region GetSet definition
        public static event System.ComponentModel.PropertyChangedEventHandler StaticPropertyChanged;
        private static void OnStaticPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
            => StaticPropertyChanged?.Invoke(null, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        private static readonly Dictionary<string, object> _properties = new Dictionary<string, object>();
        /// <summary>
        /// Gets the value of a property
        /// </summary>
        /// <typeparam name="T">Type of a property</typeparam>
        /// <param name="name">CallerMemberName</param>
        /// <returns></returns>
        private static T Get<T>([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            Debug.Assert(name != null, "Property CallerMemberName is null");
            if (_properties.TryGetValue(name, out object value))
                return value == null ? default : (T)value;
            return default;
        }

        /// <summary>
        /// Sets the value of a property
        /// </summary>
        /// <typeparam name="T">Type of a property</typeparam>
        /// <param name="value">New value of a property</param>
        /// <param name="name">CallerMemberName</param>
        /// <remarks>Use this overload when implicitly naming the property</remarks>
        private static void Set<T>(T value, [System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            Debug.Assert(name != null, "Property CallerMemberName is null");
            if (Equals(value, Get<T>(name)))
                return;
            _properties[name] = value;
            OnStaticPropertyChanged(name);
        }
        #endregion
    }
}
