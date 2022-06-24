using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Wale.WPF
{
    /// <summary>
    /// Interaction logic for TitleBar.xaml
    /// </summary>
    public partial class TitleBar : UserControl
    {
        readonly Window Owner;

        public TitleBar()
        {
            InitializeComponent();
        }
        public TitleBar(Window owner)
        {
            InitializeComponent();
            this.Owner = owner;
            this.DataContext = Owner;
            Owner.LocationChanged += Window_LocationAndSizeChanged;
        }


        #region title panel control, location and size check events
        private Point titlePosition;
        private void TitlePanel_MouseDown(object sender, MouseButtonEventArgs e) => titlePosition = e.GetPosition(Owner);
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
            if ((left + win.Width) > SystemParameters.WorkArea.Width) left = SystemParameters.WorkArea.Width - win.Width;
            else if (left < SystemParameters.WorkArea.Left) left = SystemParameters.WorkArea.Left;

            if ((top + win.Height) > SystemParameters.WorkArea.Height) top = SystemParameters.WorkArea.Height - win.Height;
            else if (top < SystemParameters.WorkArea.Top) top = SystemParameters.WorkArea.Top;

            return new Tuple<double, double>(left, top);
        }
        #endregion

    }
}
