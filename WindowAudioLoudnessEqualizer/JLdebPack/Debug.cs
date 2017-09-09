using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JDPack
{
    public static class Debug
    {
        private static object loggerLock = new object();
        private static DateTime now;
        private static string fileName;

        public static bool DebugMode = true;
        public static System.IO.DirectoryInfo WorkDirectory;
        public static System.IO.FileInfo File;


        public static void CM(string s) { ConsoleMessage(s); }
        public static void CML(string s) { ConsoleMessageLine(s); }
        public static void DM(string s) { DebugMessage(s); }
        public static void DML(string s) { DebugMessageLine(s); }

        public static void ConsoleMessage(string s) { System.Diagnostics.Debug.Write(s); }
        public static void ConsoleMessageLine(string s) { System.Diagnostics.Debug.WriteLine(s); }
        public static void DebugMessage(string s) { if (DebugMode) { System.Diagnostics.Debug.Write(s); } }
        public static void DebugMessageLine(string s) { if (DebugMode) { System.Diagnostics.Debug.WriteLine(s); } }


        public static void SetWorkDirectory(string workDirectory)
        {
            WorkDirectory = System.IO.Directory.CreateDirectory(workDirectory);
        }
        public static void Erase(int eraseDays)
        {
            if (now != null)
            {
                foreach (System.IO.FileInfo fi in WorkDirectory.EnumerateFiles())
                {
                    if (fi.Name.Contains(fileName))
                    {
                        int first = fi.Name.IndexOf("-") + 1, last = fi.Name.Length - first;
                        if (fi.Name.EndsWith(".txt")) last = fi.Name.LastIndexOf(".txt") - first;
                        string f = fi.Name.Substring(first, last);

                        DateTime ftime;
                        ftime = new DateTime(
                            Convert.ToInt32(f.Substring(0, 4)),
                            Convert.ToInt32(f.Substring(4, 2)),
                            Convert.ToInt32(f.Substring(6, 2)),
                            Convert.ToInt32(f.Substring(9, 2)),
                            Convert.ToInt32(f.Substring(11, 2)),
                            Convert.ToInt32(f.Substring(13, 2))
                            );
                        TimeSpan dif = ftime - now;
                        if (-dif.Days >= eraseDays) System.IO.File.Delete(fi.FullName);
                    }
                }
            }
        }
        public static void Open(string name)
        {
            fileName = name;
            now = DateTime.Now;
            string fullName = string.Format(@"{0}\{1}-{2:d4}{3:d2}{4:d2}-{5:d2}{6:d2}{7:d2}.txt", WorkDirectory.FullName, fileName, now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
            File = new System.IO.FileInfo(fullName);
        }
        public static void Log(string msg, bool newLine = true)
        {
            string content = $"{DateTime.Now.ToLocalTime()}: {msg}";
            if (newLine) content += "\r\n";
            lock (loggerLock)
            {
                System.IO.StreamWriter writer = new System.IO.StreamWriter(path: File.FullName, append: true, encoding: System.Text.Encoding.UTF8);
                writer.Write(content);
                writer.Close();
                writer.Dispose();

                IEnumerable<string> lines = System.IO.File.ReadLines(File.FullName);
                int lineCount = lines.Count(), critCount = 3000;
                if (lineCount > critCount) System.IO.File.WriteAllLines(File.FullName, lines.Skip(lineCount - critCount));
            }
        }
    }
}
