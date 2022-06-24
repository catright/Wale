using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Wale.WPF
{
    /// <summary>
    /// Interaction logic for Help.xaml
    /// </summary>
    public partial class Help : Window
    {
        readonly Datalink DL = new Datalink();
        public Help()
        {
            InitializeComponent();
            this.DataContext = DL;

            MainGrid.Children.RemoveAt(0);
            MainGrid.Children.Insert(0, new TitleBar(this));

            this.Title = "Wale " + Localization.Interpreter.Current.Help;
            message.Text = Localization.Interpreter.Current.HelpMsg;
        }


        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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
            public string Wiki => @"https://github.com/catright/Wale/wiki";

            private Brush _WikiLinkBrush = ColorSet.ForeColorBrush;
            public Brush WikiLinkBrush
            {
                get => _WikiLinkBrush;
                set { _WikiLinkBrush = value; OnPropertyChanged(); }
            }
            private Brush _ClipboardCopiedMessageBrush = ColorSet.ForeColorBrush;
            public Brush ClipboardCopiedMessageBrush
            {
                get => _ClipboardCopiedMessageBrush;
                set { _ClipboardCopiedMessageBrush = value; OnPropertyChanged(); }
            }

            public Datalink() { _ClipboardCopiedMessageBrush.Opacity = 0; }
            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
