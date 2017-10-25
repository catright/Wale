using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.CoreAudio
{
    public class NameSet
    {
        public int ProcessID;
        public string ProcessName, MainWindowTitle, DisplayName, SessionIdentifier, Name;
        public bool IsSystemSoundSession;

        public NameSet(int pid, bool issystem, string pname, string mwtitle, string dispname, string sessider)
        {
            ProcessID = pid;
            IsSystemSoundSession = issystem;
            ProcessName = pname;
            MainWindowTitle = mwtitle;
            DisplayName = dispname;
            SessionIdentifier = sessider;
            Name = MakeName();
        }

        private string MakeName()
        {
            if (IsSystemSoundSession) { return "System Sound"; }
            else if (SessionIdentifier.EndsWith("{5E081B13-649D-48BC-9F67-4DBF51759BD8}")) { return "Windows Shell Experience Host"; }
            else if (SessionIdentifier.EndsWith("{ABC33D23-135D-4C00-B1BF-A9FA4C7916D4}")) { return "Microsoft Edge"; }
            else if (!string.IsNullOrWhiteSpace(DisplayName)) { return DisplayName; }
            else if (!string.IsNullOrWhiteSpace(ProcessName)) { return ProcessName; }
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
    }
}