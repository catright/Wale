using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.CoreAudio
{
    public class NameSet
    {
        public string ProcessName, MainWindowTitle, DisplayName, SessionIdentifier, Name;
        public bool IsSystemSoundSession;

        public NameSet(bool issystem, string pname, string mwtitle, string dispname, string sessider)
        {
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
            else { return ProcessName; }
        }
    }
}