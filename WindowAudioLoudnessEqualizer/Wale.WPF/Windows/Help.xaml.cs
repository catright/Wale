using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for Help.xaml
    /// </summary>
    public partial class Help : Window
    {
        Datalink DL = new Datalink();
        public Help()
        {
            InitializeComponent();
            this.DataContext = DL;
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
                if (x + this.Width >= System.Windows.SystemParameters.WorkArea.Width) x = System.Windows.SystemParameters.WorkArea.Width - this.Width;
                else if (x <= 0) x = 0;

                double y = loc.Y - titlePosition.Y;
                if (y + this.Height >= System.Windows.SystemParameters.WorkArea.Height) y = System.Windows.SystemParameters.WorkArea.Height - this.Height;
                else if (y <= 0) y = 0;
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) this.Close();
        }

        private void WikiLinkTextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) { System.Diagnostics.Process.Start(DL.Wiki); }
        }
        private async void WikiLinkTextBlock_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(DL.Wiki);
            await ShowClipboardCopiedMessage();
        }

        private async Task ShowClipboardCopiedMessage()
        {
            int keepTime = 3000, fadeTime = 300, fadeStage = fadeTime / 10;
            
            for (int i = 0; i < fadeTime; i += fadeStage)
            {
                double divide = (double)(i + fadeStage) / fadeTime;
                DL.ClipboardCopiedMessageBrush.Opacity = divide;
                DL.WikiLinkBrush.Opacity = 1.0 - divide;
                await Task.Delay(fadeStage);
            }

            await Task.Delay(keepTime);

            for (int i = 0; i < fadeTime; i += fadeStage)
            {
                double divide = (double)(i + fadeStage) / fadeTime;
                DL.WikiLinkBrush.Opacity = divide;
                DL.ClipboardCopiedMessageBrush.Opacity = 1.0 - divide;
                await Task.Delay(fadeStage);
            }
        }

        class Datalink : INotifyPropertyChanged
        {
            public string Wiki { get => "https://github.com/catright/Wale/wiki"; }

            private Brush _WikiLinkBrush = ColorSet.ForeColorBrush;
            public Brush WikiLinkBrush { get => _WikiLinkBrush; set { _WikiLinkBrush = value; Changed("WikiLinkBrush"); } }
            private Brush _ClipboardCopiedMessageBrush = ColorSet.ForeColorBrush;
            public Brush ClipboardCopiedMessageBrush { get=> _ClipboardCopiedMessageBrush; set { _ClipboardCopiedMessageBrush = value; Changed("ClipboardCopiedMessageBrush"); } }

            public Datalink() { _ClipboardCopiedMessageBrush.Opacity = 0; }
            public event PropertyChangedEventHandler PropertyChanged;
            private void Changed(string name) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
        }
    }
}
