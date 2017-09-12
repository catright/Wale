using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

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

            JDPack.FileLog.SetWorkDirectory(System.IO.Path.Combine(System.IO.Path.Combine(Environment.ExpandEnvironmentVariables("%userprofile%"), "Documents"), "WaleAudioControl"));
            JDPack.FileLog.Open("WaleLog");
            JDPack.FileLog.Erase(3);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new Wale.WinForm.MainWindow());
                GC.KeepAlive(mutex);
            }
            catch (Exception e) { JDPack.FileLog.Log(e.ToString()); MessageBox.Show(e.ToString()); }
        }

    }
}
