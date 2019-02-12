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
    /// Interaction logic for TitleBar.xaml
    /// </summary>
    public partial class TitleBar : UserControl
    {
        Window Owner;

        public TitleBar()
        {
            InitializeComponent();
        }
        public TitleBar(Window owner) { InitializeComponent(); this.Owner = owner; this.DataContext = Owner; Owner.LocationChanged += Window_LocationAndSizeChanged; }


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
                if (x + Owner.Width >= System.Windows.SystemParameters.WorkArea.Width) x = System.Windows.SystemParameters.WorkArea.Width - Owner.Width;
                else if (x <= 0) x = 0;

                double y = loc.Y - titlePosition.Y;
                if (y + Owner.Height >= System.Windows.SystemParameters.WorkArea.Height) y = System.Windows.SystemParameters.WorkArea.Height - Owner.Height;
                else if (y <= 0) y = 0;
                //MessageBox.Show($"x={x} y={y}");
                Owner.Left = x;
                Owner.Top = y;
            }
        }
        private void Window_LocationAndSizeChanged(object sender, EventArgs e)
        {
            if ((Owner.Left + Owner.Width) > System.Windows.SystemParameters.WorkArea.Width)
                Owner.Left = System.Windows.SystemParameters.WorkArea.Width - Owner.Width;

            if (Owner.Left < System.Windows.SystemParameters.WorkArea.Left)
                Owner.Left = System.Windows.SystemParameters.WorkArea.Left;


            if ((Owner.Top + Owner.Height) > System.Windows.SystemParameters.WorkArea.Height)
                Owner.Top = System.Windows.SystemParameters.WorkArea.Height - Owner.Height;

            if (Owner.Top < System.Windows.SystemParameters.WorkArea.Top)
                Owner.Top = System.Windows.SystemParameters.WorkArea.Top;
        }

        #endregion

    }
}
