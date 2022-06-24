using CSCore.CoreAudioAPI;

namespace Wale.Extensions
{
    /// <summary>
    /// Identification of audio session
    /// </summary>
    public static class Namer
    {
        private static bool IsSystemSound { get; set; }
        private static int ProcessID { get; set; }
        private static string ProcessName { get; set; }
        private static string MainWindowTitle { get; set; }
        private static string DisplayName { get; set; }
        private static string SessionIdentifier { get; set; }

        /// <summary>
        /// Deside appropriate name for an audio session to properly identify the session
        /// </summary>
        /// <returns>Appropriate name</returns>
        private static string MakeName()
        {
            if (IsSystemSound)
                return "System Sound";
            else if (SessionIdentifier.EndsWith("{5E081B13-649D-48BC-9F67-4DBF51759BD8}"))
                return "Windows Shell Experience Host";
            else if (SessionIdentifier.EndsWith("{ABC33D23-135D-4C00-B1BF-A9FA4C7916D4}"))
                return "Microsoft Edge";
            else if (!string.IsNullOrWhiteSpace(DisplayName))
                return DisplayName;
            //else if (!string.IsNullOrWhiteSpace(MainWindowTitle))
            //    return MainWindowTitle;
            else if (!string.IsNullOrWhiteSpace(ProcessName))
                return ProcessName;
            else
            {
                string name = SessionIdentifier;
                int startidx = name.IndexOf("|"), endidx = name.IndexOf("%b");
                name = name.Substring(startidx, endidx - startidx + 2);
                if (name == "|#%b") name = "System";
                else
                {
                    startidx = name.LastIndexOf("\\") + 1; endidx = name.IndexOf("%b");
                    name = name.Substring(startidx, endidx - startidx);
                    if (name.EndsWith(".exe")) name = name.Substring(0, name.LastIndexOf(".exe"));
                }
                return name;
            }
        }

        /// <inheritdoc cref="MakeName"/>
        //public static string Make(NAudio.CoreAudioApi.AudioSessionControl asc, bool accessProcess = false)
        //{
        //    DisplayName = asc.DisplayName;
        //    ProcessID = (int)asc.GetProcessID;
        //    SessionIdentifier = asc.GetSessionIdentifier;
        //    IsSystemSound = asc.IsSystemSoundsSession;
        //    if (accessProcess)
        //    {
        //        using (var p = System.Diagnostics.Process.GetProcessById(ProcessID))
        //        {
        //            ProcessName = accessProcess ? p.ProcessName : string.Empty;
        //            MainWindowTitle = accessProcess ? p.MainWindowTitle : string.Empty;
        //        }
        //    }
        //    return MakeName();
        //}
        /// <inheritdoc cref="MakeName"/>
        public static string Make(AudioSessionControl asc, bool accessProcess = false)
        {
            DisplayName = asc.DisplayName;
            using (var asc2 = asc.QueryInterface<AudioSessionControl2>())
            {
                ProcessID = asc2.ProcessID;
                SessionIdentifier = asc2.SessionIdentifier;
                IsSystemSound = asc2.IsSystemSoundSession;
                ProcessName = accessProcess ? asc2.Process.ProcessName : string.Empty;
                MainWindowTitle = accessProcess ? asc2.Process.MainWindowTitle : string.Empty;
            }
            return MakeName();
        }
        /// <inheritdoc cref="MakeName"/>
        public static string Make(AudioSessionControl2 asc2, bool accessProcess = false)
        {
            DisplayName = asc2.DisplayName;
            ProcessID = asc2.ProcessID;
            SessionIdentifier = asc2.SessionIdentifier;
            IsSystemSound = asc2.IsSystemSoundSession;
            ProcessName = accessProcess ? asc2.Process.ProcessName : string.Empty;
            MainWindowTitle = accessProcess ? asc2.Process.MainWindowTitle : string.Empty;
            return MakeName();
        }
        /// <inheritdoc cref="MakeName"/>
        public static string Make(string dispname, int pid, string ssIdent, bool isSystem, string procname = null, string proctitle = null)
        {
            DisplayName = dispname;
            ProcessID = pid;
            SessionIdentifier = ssIdent;
            IsSystemSound = isSystem;
            ProcessName = procname ?? string.Empty;
            MainWindowTitle = proctitle ?? string.Empty;
            return MakeName();
        }

    }
}