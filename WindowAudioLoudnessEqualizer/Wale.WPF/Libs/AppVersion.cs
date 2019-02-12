using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.WPF
{
    public static class AppVersion
    {
        static System.Version versionObject = typeof(Wale.WPF.App).Assembly.GetName().Version;
        public static int Major = versionObject.Major;
        public static int Minor = versionObject.Minor;
        public static string Version = $"0.{Major}.{Minor}";

        private static int SysBuild = versionObject.Build;
        private static int SysRevision = versionObject.Revision;
        private static DateTime buildDate = new DateTime(2000, 1, 1).AddDays(SysBuild).AddSeconds(SysRevision * 2);
        private static TimeSpan critDate = buildDate.Subtract(new DateTime(2017, 8, 20));
        public static int Build = (int)critDate.TotalDays;
        public static int Revision = (int)critDate.Subtract(new TimeSpan(Build, 0, 0, 0)).TotalSeconds / 10;

        public static string SubVersion = $"{Build}.{Revision}";
        /*public static string Option
        {
            get
            {
                // 3>=release, 2=beta, 1=alpha, 0=pre-alpha
                string opt = string.Empty;
                int rev = versionObject.Revision;
                if (rev == 0) opt = "pre-alpha";
                else if (rev == 1) opt = "alpha";
                else if (rev == 2) opt = "beta";
                else opt = "";
                return opt;
            }
        }*/
    }
}
