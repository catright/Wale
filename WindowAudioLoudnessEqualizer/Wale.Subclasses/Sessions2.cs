using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.CoreAudio
{
    public class Sessions2 : List<Session2>
    {
        public Sessions2() { this.Clear(); }
        public Session2 GetSession(uint id) { try { return this.Find(sc => sc.PID == id); } catch (ArgumentNullException) { return null; } }
        public double GetRelative(uint id) { return GetSession(id).Relative; }
        
    }
}
