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
using System.Windows.Shapes;
using OxyPlot;
using OxyPlot.Series;
using System.Diagnostics;

namespace Wale.WPF
{
    /// <summary>
    /// Interaction logic for Configuration.xaml
    /// </summary>
    public partial class Configuration : Window
    {
        Microsoft.Win32.RegistryKey rkApp = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        Wale.WPF.Properties.Settings settings = Wale.WPF.Properties.Settings.Default;
        bool loaded = false;
        double originalMax;

        /// <summary>
        /// Initialization when window is poped up. Read all setting values, store all values as original, draw all graphs.
        /// </summary>
        public Configuration()
        {
            InitializeComponent();

            if (string.IsNullOrWhiteSpace(AppVersion.Option)) this.Title = ($"WALE - CONFIG v{AppVersion.LongVersion}");
            else this.Title = ($"WALE - CONFIG v{AppVersion.LongVersion}-{AppVersion.Option}");

            Makes();
            MakeOriginals();
            loaded = true;
            
            string selectedFunction = FunctionSelector.SelectedItem.ToString();
            if (selectedFunction == VFunction.Func.Reciprocal.ToString() || selectedFunction == VFunction.Func.FixedReciprocal.ToString()) { KurtosisBox.IsEnabled = true; }
            else { KurtosisBox.IsEnabled = false; }

            plotView.Model = new PlotModel();
            
            //DrawDevideLine();
            DrawGraph("Original");
            DrawBase();
            DrawNew();

            settings.PropertyChanged += Settings_PropertyChanged;

            Activate();
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!loaded) return;
            DrawBase();
            DrawNew();
        }

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
            LastValues.UIUpdate = settings.UIUpdateInterval;
            LastValues.AutoControlInterval = settings.AutoControlInterval;
            LastValues.GCInterval = settings.GCInterval;
            LastValues.TargetLevel = settings.TargetLevel;
            LastValues.UpRate = settings.UpRate;
            LastValues.Kurtosis = settings.Kurtosis;
            LastValues.AverageTime = settings.AverageTime;
            LastValues.MinPeak = settings.MinPeak;
            LastValues.VFunc = settings.VFunc;
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
                    graph = new FunctionSeries(new Func<double, double>((x) => {
                        double res = VFunction.Linear(x, settings.UpRate);// * 1000 / settings.AutoControlInterval;
                        //if (res > 1) { res = 1; } else if (res < 0) { res = 0; }
                        return res;
                    }), 0, 1, 0.05, graphName);
                    break;
                case VFunction.Func.SlicedLinear:
                    VFunction.FactorsForSlicedLinear sliceFactors = VFunction.GetFactorsForSlicedLinear(settings.UpRate, settings.TargetLevel);
                    graph = new FunctionSeries(new Func<double, double>((x) => {
                        double res = VFunction.SlicedLinear(x, settings.UpRate, settings.TargetLevel, sliceFactors.A, sliceFactors.B);// * 1000 / settings.AutoControlInterval;
                        //if (res > 1) { res = 1; } else if (res < 0) { res = 0; }
                        return res;
                    }), 0, 1, 0.05, graphName);
                    break;
                case VFunction.Func.Reciprocal:
                    graph = new FunctionSeries(new Func<double, double>((x) => {
                        double res = VFunction.Reciprocal(x, settings.UpRate, settings.Kurtosis);// * 1000 / settings.AutoControlInterval;
                        //if (res > 1) { res = 1; } else if (res < 0) { res = 0; }
                        return res;
                    }), 0, 1, 0.05, graphName);
                    break;
                case VFunction.Func.FixedReciprocal:
                    graph = new FunctionSeries(new Func<double, double>((x) => {
                        double res = VFunction.FixedReciprocal(x, settings.UpRate, settings.Kurtosis);// * 1000 / settings.AutoControlInterval;
                        //if (res > 1) { res = 1; } else if (res < 0) { res = 0; }
                        return res;
                    }), 0, 1, 0.05, graphName);
                    break;
                default:
                    graph = new FunctionSeries(new Func<double, double>((x) => {
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
                //settings.TargetLevel = Convert.ToDouble(textBox4.Text);
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
                if (x + this.Width > System.Windows.SystemParameters.WorkArea.Width) x = System.Windows.SystemParameters.WorkArea.Width - this.Width;
                else if (x < 0) x = 0;

                double y = loc.Y - titlePosition.Y;
                if (y + this.Height > System.Windows.SystemParameters.WorkArea.Height) y = System.Windows.SystemParameters.WorkArea.Height - this.Height;
                else if (y < 0) y = 0;
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


        /*
        private void textBoxForDigits_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
            }
        }*/
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
        /*
        private void textboxes_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
            {
                TextBox tb = sender as TextBox;
                int idx = -1, tidx = -1, init = tb.Name.IndexOfAny("1234567890".ToArray());
                //MessageBox.Show($"{tb.Name.Substring(init, tb.Name.Length - init)}");
                try { idx = Convert.ToInt32(tb.Name.Substring(init, tb.Name.Length - init)); }
                catch { MessageBox.Show("Arrow Key Cast Error"); return; }

                if (e.KeyCode == Keys.Down) tidx = idx + 1;
                else tidx = idx - 1;
                if (tidx == 7) tidx = 1;
                else if (tidx == 0) tidx = 6;
                if (tidx == -1) { MessageBox.Show("Something's wrong when catch to arrow key"); return; }

                foreach (object c in groupBox1.Controls)
                {
                    if (c is TextBox)
                    {
                        if ((c as TextBox).Name == $"textBox{tidx}") (c as TextBox).Focus();
                    }
                }
            }
        }*/
        private void textBoxesFocus_Enter(object sender, RoutedEventArgs e)
        {
            TextBox t = sender as TextBox;
            t.SelectionStart = t.Text.Length;
            t.SelectionLength = 0;
        }

        private void resetToDafault_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult dr = MessageBox.Show(this, "Do you really want to reset all configurations?", "Warning", MessageBoxButton.YesNo);
            if (dr == MessageBoxResult.Yes)
            {
                settings.Reset();
                Makes();
                JDPack.FileLog.Log("All configs are reset.");
            }
        }
        private async void ConfigSave_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            this.Topmost = false;
            this.WindowState = WindowState.Minimized;
            if (Converts() && await Register())
            {
                settings.Save();
                this.DialogResult = true;
                Close();
            }
            else { MessageBox.Show("Can not save Changes", "ERROR"); }
            this.IsEnabled = true;
            Binding topmostBinding = new Binding();
            topmostBinding.Source = settings.AlwaysTop;
            BindingOperations.SetBinding(this, Window.TopmostProperty, topmostBinding);
            this.WindowState = WindowState.Normal;
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { this.Close(); }
        }

        private void ProcessPriorityAboveNormal_Unchecked(object sender, RoutedEventArgs e)
        {
            using (System.Diagnostics.Process p = System.Diagnostics.Process.GetCurrentProcess())
            {
                p.PriorityClass = System.Diagnostics.ProcessPriorityClass.Normal;
            }
        }
        private void ProcessPriorityAboveNormal_Checked(object sender, RoutedEventArgs e)
        {
            using (System.Diagnostics.Process p = System.Diagnostics.Process.GetCurrentProcess())
            {
                p.PriorityClass = System.Diagnostics.ProcessPriorityClass.AboveNormal;
            }
        }

        private void Priority_RadioButton_Click(object sender, RoutedEventArgs e)
        {
            RadioButton s = sender as RadioButton;
            if ((bool)s.IsChecked) SetPriority(s.Content.ToString());
        }
        private void SetPriority(string priority)
        {
            settings.ProcessPriority = priority;
            ProcessPriorityClass ppc = ProcessPriorityClass.Normal;
            switch (priority)
            {
                case "High": ppc = ProcessPriorityClass.High; settings.ProcessPriorityHigh = true;break; settings.ProcessPriorityAboveNormal = false; settings.ProcessPriorityNormal = false; break;
                case "Above Normal": ppc = ProcessPriorityClass.AboveNormal; settings.ProcessPriorityHigh = false; settings.ProcessPriorityAboveNormal = true;break; settings.ProcessPriorityNormal = false; break;
                case "Normal": ppc = ProcessPriorityClass.Normal; settings.ProcessPriorityHigh = false; settings.ProcessPriorityAboveNormal = false; settings.ProcessPriorityNormal = true; break;
            }
            //settings.Save();
            using (Process p = Process.GetCurrentProcess())
            {
                p.PriorityClass = ppc;
            }
        }
    }

    public static class LastValues
    {
        public static event System.ComponentModel.PropertyChangedEventHandler StaticPropertyChanged;

        private static void OnStaticPropertyChanged(string propertyName)
        {
            var handler = StaticPropertyChanged;
            if (handler != null)
            {
                handler(null, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
        public static int UIUpdate { get => _UIUpdate; set { _UIUpdate = value; OnStaticPropertyChanged("UIUpdate"); } }
        public static int AutoControlInterval { get => _AutoControlInterval; set { _AutoControlInterval = value; OnStaticPropertyChanged("AutoControlInterval"); } }
        public static int GCInterval { get => _GCInterval; set { _GCInterval = value; OnStaticPropertyChanged("GCInterval"); } }
        public static double TargetLevel { get => _TargetLevel; set { _TargetLevel = value; OnStaticPropertyChanged("TargetLevel"); } }
        public static double AverageTime { get => _AverageTime; set { _AverageTime = value; OnStaticPropertyChanged("AverageTime"); } }
        public static double UpRate { get => _UpRate; set { _UpRate = value; OnStaticPropertyChanged("UpRate"); } }
        public static double Kurtosis { get => _Kurtosis; set { _Kurtosis = value; OnStaticPropertyChanged("Kurtosis"); } }
        public static double MinPeak { get => _MinPeak; set { _MinPeak = value; OnStaticPropertyChanged("MinPeak"); } }
        public static string VFunc { get => _VFunc; set { _VFunc = value; OnStaticPropertyChanged("VFunc"); } }
        
        public static int _UIUpdate, _AutoControlInterval, _GCInterval;
        public static double _TargetLevel, _AverageTime, _UpRate, _Kurtosis, _MinPeak;
        public static string _VFunc;
        /*
        public static int UIUpdate, AutoControlInterval, GCInterval;
        public static double TargetLevel, AverageTime, UpRate, Kurtosis, MinPeak;
        public static string VFunc;*/
    }
}
