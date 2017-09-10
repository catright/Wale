using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Wale
{
    public static class Version
    {
        static System.Version versionObject = typeof(Wale.WinForm.Program).Assembly.GetName().Version;
        public static int Major = versionObject.Major;
        public static int Minor = versionObject.Minor;
        public static int Build = versionObject.Build;
        public static string LongVersion = $"{Major}.{Minor}.{Build}";
        public static string Option = "beta";
    }
}
namespace Wale.WinForm
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var mutex = new System.Threading.Mutex(true, "WaleWindowsAudioLoudnessEqualizer", out bool result);
            if (!result)
            {
                MessageBox.Show("The wale is already running.");
                return;
            }

            JDPack.Debug.SetWorkDirectory(System.IO.Path.Combine(System.IO.Path.Combine(Environment.ExpandEnvironmentVariables("%userprofile%"), "Documents"), "WaleAudioControl"));
            JDPack.Debug.Open("WaleLog");
            JDPack.Debug.Erase(3);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new Wale.WinForm.MainWindow());
                GC.KeepAlive(mutex);
            }
            catch (Exception e) { JDPack.Debug.Log(e.ToString()); MessageBox.Show(e.ToString()); }
        }

    }
}
