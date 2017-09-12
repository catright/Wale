using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.WinForm
{
    public static class AppVersion
    {
        static System.Version versionObject = typeof(Wale.WinForm.Program).Assembly.GetName().Version;
        public static int Major = versionObject.Major;
        public static int Minor = versionObject.Minor;
        public static int Build = versionObject.Build;
        public static string LongVersion = $"{Major}.{Minor}.{Build}";
        public static string Option
        {
            get
            {
                string opt = string.Empty;
                int rev = versionObject.Revision;
                if (rev == 0) opt = "";
                else if (rev == 1) opt = "beta";
                else if (rev == 2) opt = "alpha";
                else opt = "pre-alpha";
                return opt;
            }
        }
    }
}
