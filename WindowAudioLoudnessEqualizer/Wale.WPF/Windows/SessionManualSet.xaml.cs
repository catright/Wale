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
using Wale.CoreAudio;

namespace Wale.WPF
{
    /// <summary>
    /// Interaction logic for SessionManualSet.xaml
    /// </summary>
    public partial class SessionManualSet : Window
    {
        public SMSDatalink DL = new SMSDatalink();
        public double Relative = 0;

        public SessionManualSet()
        {
            InitializeComponent();
            Init1();
        }
        public SessionManualSet(Window owner, string sessionName, double rel=0)
        {
            InitializeComponent();
            Init1();
            Init2(owner, sessionName, rel);
        }
        private void Init1() { DataContext = DL; this.KeyDown += SessionManualSet_KeyDown; }
        private void SessionManualSet_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { DialogResult = false; Close(); }
        }

        private void Init2(Window owner, string sessionName, double rel)
        {
            this.Owner = owner;
            DL.SessionName = sessionName;
            DL.RelativeTooltop = $"{Wale.Configuration.Audio.RelativeEndInv}~{Wale.Configuration.Audio.RelativeEnd}";
            Relative = DL.Relative = rel;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Relative = DL.Relative;
            DialogResult = true;
            Close();
        }

        private void Relative_TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return) { OK_Click(sender, new RoutedEventArgs()); }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { DialogResult = false; Close(); }
        }
    }

    public class SMSDatalink : JPack.NotifyPropertyChanged
    {
        public SMSDatalink()
        {
            SessionName = "Session Name";
        }

        public string SessionName { get => Get<string>(); set => Set(value); }

        public string RelativeTooltop { get => Get<string>(); set => Set(value); }
        public double Relative { get => Get<double>(); set => Set(Math.Round(value, 3)); }
    }

}
