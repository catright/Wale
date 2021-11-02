using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreTest
{
    public class SessionData : AudioData
    {
        public SessionData(IntPtr ptr) : base() { BasePtr = ptr; }
        public SessionData(IntPtr ptr, string name) : base() { BasePtr = ptr; DisplayName = name; }
        public SessionData(IntPtr ptr, bool b, double v, double p) : base(b, v, p) { BasePtr = ptr; }

        public IntPtr BasePtr { get => Get<IntPtr>(); private set => Set(value); }
        public string DisplayName { get => Get<string>(); private set => Set(value); }
    }
}
