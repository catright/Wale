using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Wale.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {// give the mutex a  unique name
        private const string MutexName = "WaleWindowsAudioLoudnessEqualizer";
        // declare the mutex
        private readonly Mutex _mutex;
        // overload the constructor
        bool createdNew;
        public App()
        {
            // overloaded mutex constructor which outs a boolean
            // telling if the mutex is new or not.
            // see http://msdn.microsoft.com/en-us/library/System.Threading.Mutex.aspx
            _mutex = new Mutex(true, MutexName, out createdNew);
            if (!createdNew)
            {
                // if the mutex already exists, notify and quit
                MessageBox.Show("The Wale is already breathing");
                Application.Current.Shutdown(0);
            }

        }
        protected override void OnStartup(StartupEventArgs e)
        {
            if (!createdNew) return;
            // overload the OnStartup so that the main window 
            // is constructed and visible

            JDPack.FileLog.SetWorkDirectory(System.IO.Path.Combine(Environment.ExpandEnvironmentVariables("%appdata%"), "WaleAudioControl"));
            JDPack.FileLog.Open("WaleLog");
            JDPack.FileLog.Erase(7);

            MainWindow mw = new MainWindow();
            mw.Show();
        }
    }
}
