using System;
using System.Text;
using Wale.Configs;
using Wale.CoreAudio;

namespace Wale.Controller
{
    internal class SessionControl
    {
        public bool Debug { get; set; }
        private readonly General gl;
        private readonly DFactors dfac;
        public SessionControl(General gl, bool debug = false)
        {
            this.Debug = debug;
            this.gl = gl;
            gl.PropertyChanged += Gl_PropertyChanged;

            if (VolumeFunctionExtension.gl == null) VolumeFunctionExtension.gl = this.gl;
            dfac = new DFactors(gl);

            if (Enum.TryParse(gl.DFunc, out DType buffer)) Delay = buffer;
            else { M.F($"SessionControl: Can NOT parse delay function. Use {DType.None}"); Delay = DType.None; }
        }

        internal DType Delay;
        private void Gl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "DFunc":
                    Delay = Enum.TryParse(gl.DFunc, out DType buffer) ? buffer : Delay;
                    break;
            }
        }

        public event EventHandler<Session> SessionAdded;
        //public event EventHandler<Guid> SessionRemoved;

        public void Run(Session s, string deviceId)
        {
            try
            {
                // Session removed event
                if (s == null || deviceId != s.DeviceID) { s?.Dispose(); return; }// || s.State.IsExpired() SessionRemoved?.Invoke(this, s.ID);
                // Session added event
                if (s.NewlyAdded) { s.NewlyAdded = false; SessionAdded?.Invoke(this, s); }

                // Get session name
                string name;
                lock (s.Locker) { name = s.Name; }
                // Forced disposal of session when its associated process has some problems, this might not be occurred because session now has a PID cache.
                if (s?.ProcessID < 0) { M.F($"{name} is changed. Dispose session"); s.Dispose(); return; }//SessionRemoved?.Invoke(this, s);

                // Control session(=s) when it is not in exclude list, auto included, and not muted
                if (!gl.ExcList.Contains(name) && s.Auto && !s.Muted)
                {
                    // static control when in static mode
                    if (gl.StaticMode) Passive(s);
                    // active control only when session is active
                    else Active(s);
                }
            }
            catch (Exception e) { M.F($"SessionControl: Run: Unknown\r\n\t{e}"); }
        }
        public void Passive(Session s)
        {
            try
            {
                lock (s.Locker) { s.Volume = (float)(gl.TargetLevel * s.Relfactor); }
            }
            catch { M.F("SessionControl: Passive: Unknown"); }
        }
        public void Active(Session s)
        {
            try
            {
                double rfac = s.Relfactor, peak = s.Peak, apeak = s.AveragePeak, volume = s.Volume / rfac;
#if DEBUG
                StringBuilder dm = new StringBuilder();
                if (Debug) { lock (s.Locker) { dm.Append($"AutoVolume:{s.Name}({s.ProcessID}), inc={s.Auto}"); } }
                if (Debug) dm.Append($" P:{peak:n3} A:{apeak:n3} V:{volume:n3}");
#endif
                //if (volume == 0) volume = 0.001;
                // control volume when audio session makes sound, re-check session activity
                if (peak > gl.MinPeak)// || s.State.IsActive()
                {
                    //double tVol = VType.CompAR.Next(peak, 0, new CompFactor(s.AveragePeakAttack, s.AveragePeakRelease, volume));
                    double vn = VType.CompressDB.Next(peak, apeak);
                    double cut = volume + Delay.Calc(vn, dfac);
#if DEBUG
                    if (Debug) dm.Append($" T={vn:n3} UC={cut:n3}");
#endif
                    // set volume
                    //if (s.State.IsNotActive()) return;//Check again session activity before actual volume update
                    lock (s.Locker) { s.Volume = (float)(Math.Min(vn, cut) * rfac); }
                }
#if DEBUG
                if (Debug) M.D(dm);// print debug message
#endif
            }
            catch { M.F("SessionControl: Active: Unknown"); }
        }

        public void Reset(Session s)
        {
            try
            {
                lock (s.Locker) { if (gl.ExcList.Contains(s.Name)) return; }
                if (s.Auto && !s.Active && s.Volume != 0.01f)
                {
                    lock (s.Locker)
                    {
                        s.Volume = 0.01f;
                        s.ResetAverage();
                    }
                }
            }
            catch (Exception e) { if (e.HResult.IsUnknown()) M.F($"SessionControl: Reset: Unknown. HRESULT={(byte)e.HResult}"); }
        }

    }
}
