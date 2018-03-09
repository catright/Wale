using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale
{
    /// <summary>
    /// Objects for Lock states of asychronous tasks
    /// </summary>
    public static class Lockers
    {
        public static object Sessions = new object();
        public static object Logger = new object();
        public static object AudioObject = new object();
    }
}
