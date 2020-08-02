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
        //private const string MutexModi = "RS";
        // declare the mutex
        private readonly Mutex _mutex;
        // overload the constructor
        bool createdNew, StartRequire = true, mutexOwned = false;
        public App()
        {
            // overloaded mutex constructor which outs a boolean
            // telling if the mutex is new or not.
            // see http://msdn.microsoft.com/en-us/library/System.Threading.Mutex.aspx
            _mutex = new Mutex(true, MutexName, out createdNew);
            if (!createdNew)
            {
                Console.WriteLine("Acquiring.");
                if (!_mutex.WaitOne(3000))
                {
                    StartRequire = false;
                    // if the mutex already exists, notify and quit
                    MessageBox.Show("The Wale is already breathing");
                    Application.Current.Shutdown(0);
                }
                else mutexOwned = true;
            }

        }
        protected override void OnStartup(StartupEventArgs e)
        {
            if (!StartRequire) return;
            // overload the OnStartup so that the main window 
            // is constructed and visible

            //foreach (string arg in e.Args) { Console.WriteLine(arg); }
            //string installedPath = System.AppDomain.CurrentDomain.BaseDirectory;
            //string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string path = Conf.WorkingPath;
            JPack.FileLog.SetWorkDirectory(path);
            JPack.FileLog.Open("WaleLog");
            JPack.FileLog.Erase(7);
            JPack.FileLog.Log($"Wale {AppVersion.Version}.{AppVersion.SubVersion}");
            JPack.FileLog.Log(path);

            int UICreation = 0;
            try
            {
                MainWindow mw = new MainWindow();
                UICreation = 1;
                mw.Show();
                UICreation = 2;
            }
            catch (Exception uice) { JPack.FileLog.Log($"Failed to create and show UI on stage {UICreation}, {uice.ToString()}"); }
        }
        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // Process unhandled exception
            JPack.FileLog.Log($"{e.Exception.Message} {e.Exception.StackTrace}");
            // Prevent default unhandled exception processing
            //e.Handled = true;
        }
        protected override void OnExit(ExitEventArgs e)
        {
            if (mutexOwned) _mutex.ReleaseMutex();
            base.OnExit(e);
        }

    }

    public static class Conf
    {
        public static Wale.Configuration.General settings = new Wale.Configuration.General();
        public static string WorkingPath = settings.WorkingPath;
    }
}
