using System;
using System.Threading;
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
        readonly bool createdNew, StartRequire = true, mutexOwned = false;
        public App()
        {
            // overloaded mutex constructor which outs a boolean
            // telling if the mutex is new or not.
            // see http://msdn.microsoft.com/en-us/library/System.Threading.Mutex.aspx
            _mutex = new Mutex(true, MutexName, out createdNew);
            if (!createdNew)
            {
                Console.WriteLine("Acquiring.");
                bool needToStop;
                try { needToStop = !_mutex.WaitOne(3000); }
                catch (AbandonedMutexException) { needToStop = false; }
                catch { needToStop = true; }
                if (needToStop)
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
            Configs.Manager.Initialize();
            M.F($"Wale {AppVersion.Version}.{AppVersion.SubVersion}");

            int UICreation = 0;
            try
            {
                MainWindow mw = new MainWindow();
                UICreation = 1;
                mw.Show();
                UICreation = 2;
            }
            catch (Exception uice) { M.F($"Failed to create and show UI on stage {UICreation}, {uice}"); }
        }
        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception.HResult.IsKnown()) { e.Handled = true; return; }
            // Process unhandled exception
            M.F($"{e.Exception.Message} {e.Exception.StackTrace}");
            // Prevent default unhandled exception processing
            //e.Handled = true;
        }
        protected override void OnExit(ExitEventArgs e)
        {
            M.F($"AppExit {e.ApplicationExitCode}");
            if (mutexOwned) _mutex.ReleaseMutex();
            base.OnExit(e);
        }

    }
}
