using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.Subclasses
{
    public static class Lockers
    {
        public static object Sessions = new object();
        public static object Logger = new object();
    }
}
