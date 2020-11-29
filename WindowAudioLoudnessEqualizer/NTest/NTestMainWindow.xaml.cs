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
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio;

namespace NTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class NTestMainWindow : Window
    {
        private void DW(object o) => System.Diagnostics.Debug.WriteLine(o);
        public NTestMainWindow()
        {
            InitializeComponent();
            L = new Linker();
            DataContext = L;
            C = new Core(L);
            //Test();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            C.Unload();
        }
        private Linker L;
        private Core C;

        private void Test()
        {
            L.MMD = new NAudio.CoreAudioApi.MMDeviceEnumerator().GetDefaultAudioEndpoint(NAudio.CoreAudioApi.DataFlow.Render, NAudio.CoreAudioApi.Role.Multimedia);
            if (L.MMD == null) return;
        }



    }
}
