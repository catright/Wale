using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JDPack
{
    public static class Debug
    {
        public static bool DebugMode = true;

        public static void CM(string s) { ConsoleMessage(s); }
        public static void CML(string s) { ConsoleMessageLine(s); }
        public static void DM(string s) { DebugMessage(s); }
        public static void DML(string s) { DebugMessageLine(s); }

        public static void ConsoleMessage(string s) { System.Diagnostics.Debug.Write(s); }
        public static void ConsoleMessageLine(string s) { System.Diagnostics.Debug.WriteLine(s); }
        public static void DebugMessage(string s) { if (DebugMode) { System.Diagnostics.Debug.Write(s); } }
        public static void DebugMessageLine(string s) { if (DebugMode) { System.Diagnostics.Debug.WriteLine(s); } }
        
    }
}
