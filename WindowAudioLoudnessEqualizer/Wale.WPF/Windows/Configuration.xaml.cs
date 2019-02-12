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

namespace Wale.WPF
{
    /// <summary>
    /// Interaction logic for Configuration.xaml
    /// </summary>
    public partial class Configuration : Window
    {
        /// <summary>
        /// Initialization when window is poped up. Read all setting values, store all values as original, draw all graphs.
        /// </summary>
        public Configuration(AudioControl audio, Datalink dl)
        {
            InitializeComponent();

            MainGrid.Children.Clear();

            MainGrid.Children.Add(new TitleBar(this));

            ConfigSet cs = new ConfigSet(audio, dl, this, false, true);
            cs.LogInvokedEvent += Cs_LogInvokedEvent;
            cs.Margin = new Thickness(0, 35, 0, 0);
            MainGrid.Children.Add(cs);
            this.Height = cs.Height + AppDatas.TitleBarHeight;
        }

        private void Cs_LogInvokedEvent(object sender, ConfigSet.LogEventArgs e)
        {
            //Console.WriteLine(e.msg);
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


        /*private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { this.Close(); }
        }*/

    }
}
