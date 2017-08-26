using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JLdebPack
{
    public class DebugPackage
    {
        public bool DebugMode = true;

        public void CM(string s) { ConsoleMessage(s); }
        public void CML(string s) { ConsoleMessageLine(s); }
        public void DM(string s) { DebugMessage(s); }
        public void DML(string s) { DebugMessageLine(s); }

        public void ConsoleMessage(string s) { System.Diagnostics.Debug.Write(s); }
        public void ConsoleMessageLine(string s) { System.Diagnostics.Debug.WriteLine(s); }
        public void DebugMessage(string s) { if (DebugMode) { System.Diagnostics.Debug.Write(s); } }
        public void DebugMessageLine(string s) { if (DebugMode) { System.Diagnostics.Debug.WriteLine(s); } }

        public DebugPackage() { }
        public DebugPackage(bool debugMode) { DebugMode = debugMode; }
    }
}
