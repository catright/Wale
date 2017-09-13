using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale
{
    public static class Lockers
    {
        public static object Sessions = new object();
        public static object Logger = new object();
        public static object AudioObject = new object();
    }
}
