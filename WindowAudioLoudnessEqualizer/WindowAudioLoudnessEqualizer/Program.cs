﻿using System;
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
            JLdebPack.DebugPack.SetWorkDirectory(System.IO.Path.Combine(System.IO.Path.Combine(Environment.ExpandEnvironmentVariables("%userprofile%"), "Documents"), "WaleAudioControl"));
            JLdebPack.DebugPack.Open("WaleLog");
            JLdebPack.DebugPack.Erase(3);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new Wale.WinForm.MainWindow());
            }
            catch (Exception e) { JLdebPack.DebugPack.Log(e.ToString()); MessageBox.Show(e.ToString()); }
        }

    }
}
