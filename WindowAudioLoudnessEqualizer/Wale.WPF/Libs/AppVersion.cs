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
        public static int Build = versionObject.Build;
        public static string LongVersion = $"{Major}.{Minor}.{Build}";
        public static string Option
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
        }
    }
}
