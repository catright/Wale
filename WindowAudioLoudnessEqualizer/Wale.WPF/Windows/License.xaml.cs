using System.Windows;

namespace Wale.WPF
{
    /// <summary>
    /// Interaction logic for License.xaml
    /// </summary>
    public partial class License : Window
    {
        public License()
        {
            InitializeComponent();
            MainGrid.Children.RemoveAt(0);
            MainGrid.Children.Insert(0, new TitleBar(this));

            this.Title = "Wale " + Localization.Interpreter.Current.License;
        }


        private void Button_Click(object sender, RoutedEventArgs e) => this.Close();

    }
}
