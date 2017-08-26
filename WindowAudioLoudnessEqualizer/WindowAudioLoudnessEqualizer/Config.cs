using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Wale.Subclasses;

namespace Wale.WinForm
{
    public partial class Config : Form
    {
        RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        Wale.Properties.Settings settings = Wale.Properties.Settings.Default;
        System.Windows.Forms.DataVisualization.Charting.Chart chart = new System.Windows.Forms.DataVisualization.Charting.Chart();
        System.Windows.Forms.DataVisualization.Charting.TextAnnotation myText1 = new System.Windows.Forms.DataVisualization.Charting.TextAnnotation(), myText2 = new System.Windows.Forms.DataVisualization.Charting.TextAnnotation();
        bool loaded = false;
        double baseVolume, upRate, skewness, originalMax;
        Color White = Color.LightGray;
        Color Gray = Color.Gray;
        Color Black = Color.DimGray;
        Color Blue = Color.CornflowerBlue;
        Color Red = Color.PaleVioletRed;
        Color Orange = Color.Orange;
        Color Yellow = Color.DarkGoldenrod;

        public Config()
        {
            InitializeComponent();
            titlePanel.BackColor = Blue;
            GraphPanel.Controls.Add(chart);
            chart.Dock = DockStyle.Fill;
            chart.BackColor = this.BackColor;
            chart.Margin = new Padding(0);
            chart.Padding = new Padding(0);

            chart.ChartAreas.Clear();
            chart.ChartAreas.Add("Area");
            chart.ChartAreas["Area"].BackColor = this.BackColor;
            chart.ChartAreas["Area"].BorderColor = White;
            chart.ChartAreas["Area"].AxisX.LineColor = Gray;
            chart.ChartAreas["Area"].AxisY.LineColor = Gray;
            chart.ChartAreas["Area"].AxisX.MajorGrid.LineColor = Black;
            chart.ChartAreas["Area"].AxisY.MajorGrid.LineColor = Black;
            chart.ChartAreas["Area"].AxisX.LabelStyle.ForeColor = Gray;
            chart.ChartAreas["Area"].AxisY.LabelStyle.ForeColor = Gray;
            //chart.ChartAreas["Area"].AxisY.LabelStyle.IsEndLabelVisible = true;
            chart.ChartAreas["Area"].AxisX.LabelAutoFitMinFontSize = 7;
            chart.ChartAreas["Area"].AxisX.LabelAutoFitMaxFontSize = 9;
            chart.ChartAreas["Area"].AxisY.LabelAutoFitMinFontSize = 7;
            chart.ChartAreas["Area"].AxisY.LabelAutoFitMaxFontSize = 9;
            //chart.ChartAreas["Area"].AxisX.LabelStyle.Font = new System.Drawing.Font("Trebuchet MS", 2.25F, System.Drawing.FontStyle.Bold);
            chart.ChartAreas["Area"].AxisX.Minimum = 0;
            chart.ChartAreas["Area"].AxisX.Maximum = 1;
            chart.ChartAreas["Area"].AxisY.Minimum = 0;
            //chart.ChartAreas["Area"].AxisY.Maximum = 0.1;

            myText1.Text = "Original";
            //myText1.ClipToChartArea = chart.ChartAreas["Area"].Name;
            myText1.AxisX = chart.ChartAreas["Area"].AxisX;
            myText1.AxisY = chart.ChartAreas["Area"].AxisY;
            //myText1.AnchorX = 30;
            //myText1.AnchorY = 75;
            myText1.ForeColor = Blue;
            //myText1.Font = new Font(this.Font.FontFamily, 9);
            myText1.X = chart.ChartAreas["Area"].AxisX.Maximum * 0.6;
            //myText1.LineWidth = 2;
            myText1.Visible = true;
            chart.Annotations.Add(myText1);

            myText2.Text = "New";
            //myText2.ClipToChartArea = chart.ChartAreas["Area"].Name;
            myText2.AxisX = chart.ChartAreas["Area"].AxisX;
            myText2.AxisY = chart.ChartAreas["Area"].AxisY;
            //myText2.AnchorX = 30;
            //myText2.AnchorY = 75;
            myText2.ForeColor = Red;
            //myText2.Font = new Font(this.Font.FontFamily, 9);
            myText2.X = chart.ChartAreas["Area"].AxisX.Maximum * 0.6;
            //myText2.LineWidth = 2;
            myText2.Visible = true;
            chart.Annotations.Add(myText2);

            Makes();
            loaded = true;
            try
            {
                if (!string.IsNullOrEmpty(textBox4.Text)) baseVolume = Convert.ToDouble(textBox4.Text);
                if (!string.IsNullOrEmpty(textBox5.Text)) upRate = (Convert.ToDouble(textBox5.Text) * Convert.ToDouble(textBox2.Text) / 1000.0);
                if (!string.IsNullOrEmpty(textBox6.Text)) skewness = Convert.ToDouble(textBox6.Text);
            }
            catch { MessageBox.Show("Invalid First Value"); }
            //DrawDevideLine();
            DrawGraph("Original");
            DrawBase();
            DrawNew();
        }

        //title panel control
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
        private void Config_LocationChanged(object sender, EventArgs e)
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


        private void Makes()
        {
            if (rkApp.GetValue("WALEWindowAudioLoudnessEqualizer") == null)
            {
                // The value doesn't exist, the application is not set to run at startup
                runAtWindowsStartup.Checked = false;
            }
            else
            {
                // The value exists, the application is set to run at startup
                runAtWindowsStartup.Checked = true;
            }
            originalUIInterval.Text = textBox1.Text = settings.UIUpdateInterval.ToString();
            originalACInterval.Text = textBox2.Text = settings.AutoControlInterval.ToString();
            originalGCInterval.Text = textBox3.Text = settings.GCInterval.ToString();
            originalBaseVolume.Text = textBox4.Text = settings.BaseVolume.ToString();
            originalUpRate.Text = textBox5.Text = settings.UpRate.ToString();
            originalSkewness.Text = textBox6.Text = settings.Skewness.ToString();
            originalAvTime.Text = textBox7.Text = (settings.AverageTime / 1000).ToString();
            originalMinPeak.Text = textBox8.Text = settings.MinPeak.ToString();
            comboBox1.DataSource = Enum.GetValues(typeof(VFunction.Func));
            comboBox1.SelectedItem = settings.VFunc;
            originalFunction.Text = comboBox1.SelectedText;
        }
        private void DrawNew() { DrawGraph("Graph"); }
        private void DrawGraph(string graphName)
        {
            VFunction.Func f;
            Enum.TryParse<VFunction.Func>(comboBox1.SelectedValue.ToString(), out f);
            //chart.ChartAreas["Area"].RecalculateAxesScale();
            /*switch (f)
            {
                case VFunction.Func.Linear:
                case VFunction.Func.SlicedLinear:
                case VFunction.Func.Reciprocal:
                case VFunction.Func.FixedReciprocal:
                    chart.ChartAreas["Area"].AxisY.Minimum = 0;
                    chart.ChartAreas["Area"].AxisY.Maximum = 1;
                    break;
                default:
                    break;
            }/**/

            System.Windows.Forms.DataVisualization.Charting.Series graph = chart.Series.FindByName(graphName);
            if (graph == null) graph = chart.Series.Add(graphName);
            graph.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            if (graph.Name == "Original") graph.Color = Blue;
            else graph.Color = Red;
            graph.Points.Clear();
            double val = 0, maxVal = 0, yScale = 1;
            switch (f)
            {
                case VFunction.Func.Linear:
                    for (double x = 0; x < 1.05; x += 0.05)
                    {
                        val = VFunction.Linear(x, upRate) * 1000 / settings.AutoControlInterval;
                        maxVal = Math.Max(maxVal, val);
                        graph.Points.AddXY(x, val);
                    }
                    break;
                case VFunction.Func.SlicedLinear:
                    VFunction.FactorsForSlicedLinear sliceFactors = VFunction.GetFactorsForSlicedLinear(upRate, baseVolume);
                    for (double x = 0; x < 1.05; x += 0.05)
                    {
                        val = VFunction.SlicedLinear(x, upRate, baseVolume, sliceFactors.A, sliceFactors.B) * 1000 / settings.AutoControlInterval;
                        maxVal = Math.Max(maxVal, val);
                        graph.Points.AddXY(x, val);
                    }
                    break;
                case VFunction.Func.Reciprocal:
                    for (double x = 0; x < 1.05; x += 0.05)
                    {
                        val = VFunction.Reciprocal(x, upRate, skewness) * 1000 / settings.AutoControlInterval;
                        maxVal = Math.Max(maxVal, val);
                        graph.Points.AddXY(x, val);
                    }
                    break;
                case VFunction.Func.FixedReciprocal:
                    for (double x = 0; x < 1.05; x += 0.05)
                    {
                        val = VFunction.FixedReciprocal(x, upRate, skewness) * 1000 / settings.AutoControlInterval;
                        maxVal = Math.Max(maxVal, val);
                        graph.Points.AddXY(x, val);
                    }
                    break;
                default:
                    break;
            }
            //MessageBox.Show($"{maxVal}");
            if (graph.Name == "Original") originalMax = maxVal;
            else maxVal = Math.Max(maxVal, originalMax);
            for (double i = 1.0; i > 0.00001; i /= 2)
            {
                if (maxVal > i) { yScale = i * 2.0; break; }
            }
            chart.ChartAreas["Area"].AxisY.Maximum = yScale;
            if (maxVal > 0.01) chart.ChartAreas["Area"].AxisY.LabelStyle.Format = "G";
            else chart.ChartAreas["Area"].AxisY.LabelStyle.Format = "#.#E0";
            myText1.Y = chart.ChartAreas["Area"].AxisY.Maximum;
            myText2.Y = chart.ChartAreas["Area"].AxisY.Maximum * 0.92;
        }
        private void DrawBase()
        {
            System.Windows.Forms.DataVisualization.Charting.Series graph = chart.Series.FindByName("Base");
            if (graph == null) graph = chart.Series.Add("Base");
            graph.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            graph.Color = Yellow;
            graph.Points.Clear();

            for (double y = 0; y < 99; y++)
            {
                graph.Points.AddXY(baseVolume, y);
            }
        }
        private void DrawDevideLine()
        {
            System.Windows.Forms.DataVisualization.Charting.Series graph = chart.Series.FindByName("DevideLine");
            if (graph == null) graph = chart.Series.Add("DevideLine");
            graph.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            graph.Color = Gray;
            graph.Points.Clear();

            for (double x = 0; x < 99; x++)
            {
                graph.Points.AddXY(x, x);
            }
        }
        private bool Converts()
        {
            bool success = true, auto = settings.AutoControl;
            settings.AutoControl = false;
            try
            {
                settings.BaseVolume = Convert.ToDouble(textBox4.Text);
                settings.UIUpdateInterval = Convert.ToInt16(textBox1.Text);
                settings.AutoControlInterval = Convert.ToInt16(textBox2.Text);
                settings.GCInterval = Convert.ToInt16(textBox3.Text);
                settings.UpRate = Convert.ToDouble(textBox5.Text);
                settings.Skewness = Convert.ToDouble(textBox6.Text);
                settings.AverageTime = Convert.ToDouble(textBox7.Text) * 1000;
                settings.MinPeak = Convert.ToDouble(textBox8.Text);
                VFunction.Func f;
                Enum.TryParse<VFunction.Func>(comboBox1.SelectedValue.ToString(), out f);
                settings.VFunc = f;
            }
            catch { success = false; }
            finally { settings.AutoControl = auto; }
            return success;
        }
        private bool Register()
        {
            try
            {
                if (runAtWindowsStartup.Checked)
                {
                    // Add the value in the registry so that the application runs at startup
                    rkApp.SetValue("WALEWindowAudioLoudnessEqualizer", Application.ExecutablePath);
                }
                else
                {
                    // Remove the value from the registry so that the application doesn't start
                    rkApp.DeleteValue("WALEWindowAudioLoudnessEqualizer", false);
                }
            }
            catch { return false; }
            return true;
        }



        private void textBoxForDigits_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
            }
        }
        private void BaseVolume_Changed(object sender, EventArgs e)
        {
            if (!loaded) return;
            try
            {
                if (!string.IsNullOrEmpty(textBox4.Text) && textBox4.Text != ".") baseVolume = Convert.ToDouble(textBox4.Text);
            }
            catch { MessageBox.Show("Invalid BaseVolume Value"); }

            DrawBase();
            DrawNew();
        }
        private void UpRate_Changed(object sender, EventArgs e)
        {
            if (!loaded) return;
            try
            {
                if (!string.IsNullOrEmpty(textBox5.Text) && textBox5.Text != ".") upRate = (Convert.ToDouble(textBox5.Text) * Convert.ToDouble(textBox2.Text) / 1000.0);
            }
            catch { MessageBox.Show("Invalid UpRate Value"); }
            DrawNew();
        }
        private void Skewness_Changed(object sender, EventArgs e)
        {
            if (!loaded) return;
            try
            {
                if (!string.IsNullOrEmpty(textBox6.Text) && textBox6.Text != ".") skewness = Convert.ToDouble(textBox6.Text);
            }
            catch { MessageBox.Show("Invalid Skewness Value"); }
            DrawNew();
        }
        private void Function_Changed(object sender, EventArgs e)
        {
            if (!loaded) return;
            DrawNew();
        }

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
                    if(c is TextBox)
                    {
                        if ((c as TextBox).Name == $"textBox{tidx}") (c as TextBox).Focus();
                    }
                }
            }
        }
        private void textBoxesFocus_Enter(object sender, EventArgs e)
        {
            TextBox t = sender as TextBox;
            t.SelectionStart = t.Text.Length;
            t.SelectionLength = 0;
        }

        private void Submit_Click(object sender, EventArgs e)
        {
            if (Converts() && Register())
            {
                settings.Save();
                this.DialogResult = DialogResult.OK;
                Close();
            }
            else { MessageBox.Show("Can not save Changes", "ERROR"); }
        }
        private void Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

    }
}
