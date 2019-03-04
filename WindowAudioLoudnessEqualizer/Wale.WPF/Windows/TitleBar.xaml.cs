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
        private void TitlePanel_MouseDown(object sender, MouseButtonEventArgs e) { titlePosition = e.GetPosition(Owner); }
        private void TitlePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point mouse = PointToScreen(e.GetPosition(this));
                var visource = PresentationSource.FromVisual(Owner).CompositionTarget.TransformToDevice;

                double x = mouse.X / visource.M11 - titlePosition.X;
                double y = mouse.Y / visource.M22 - titlePosition.Y;
                
                var loc = CheckWindowLocation(Owner, x, y);
                
                Owner.Left = loc.Item1;
                Owner.Top = loc.Item2;
            }
        }
        private void Window_LocationAndSizeChanged(object sender, EventArgs e)
        {
            var loc = CheckWindowLocation(Owner, Owner.Left, Owner.Top);
            Owner.Left = loc.Item1;
            Owner.Top = loc.Item2;
        }
        public static Tuple<double, double> CheckWindowLocation(Window win, double left, double top)
        {
            if ((left + win.Width) > System.Windows.SystemParameters.WorkArea.Width) left = System.Windows.SystemParameters.WorkArea.Width - win.Width;
            else if (left < System.Windows.SystemParameters.WorkArea.Left) left = System.Windows.SystemParameters.WorkArea.Left;

            if ((top + win.Height) > System.Windows.SystemParameters.WorkArea.Height) top = System.Windows.SystemParameters.WorkArea.Height - win.Height;
            else if (top < System.Windows.SystemParameters.WorkArea.Top) top = System.Windows.SystemParameters.WorkArea.Top;
            
            return new Tuple<double, double>(left, top);
        }
        #endregion

    }
}
