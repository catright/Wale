using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.Subclasses
{
    public class SessionDataPack : List<SessionData>
    {
        public int CountNotExpired { get { int c = 0; this.ForEach(sc => { if (!sc.Expired) c++; }); return c; } }
        public uint GetID(int idx) { return GetData(idx).ID; }
        public int GetIndex(uint id) { return GetData(id).Index; }
        public bool CheckData(int idx) { return this.Exists(x => x.Index == idx); }
        public bool CheckData(uint id) { return this.Exists(x => x.ID == id); }
        public SessionData GetData(int idx) { return this.Find(sc => sc.Index == idx); }
        public SessionData GetData(uint id) { return this.Find(sc => sc.ID == id); }
        public double GetRelative(int idx) { return GetData(idx).Relative; }
        public double GetRelative(uint id) { return GetData(id).Relative; }
        //public bool Included(int idx) { SessionData sc = GetData(idx); return (!sc.Expired && sc.AutoIncluded); }
        //public bool Included(uint id) { SessionData sc = GetData(id); return (!sc.Expired && sc.AutoIncluded); }
        //public bool Expired(int idx) { return GetData(idx).Expired; }
        //public bool Expired(uint id) { return GetData(id).Expired; }
    }//Class session pack
}
