using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Wale.CoreAudio
{
    /// <summary>
    /// Data container for audio session
    /// </summary>
    public class Session : IDisposable, IComparable<Session>
    {
        private CSCore.CoreAudioAPI.AudioSessionControl2 asc2;
        public Session(CSCore.CoreAudioAPI.AudioSessionControl2 asc2, List<string> ExcList, double AvgTime, double AcInterval)
        {
            this.asc2 = asc2;
            NameSet = new NameSet(
                asc2.ProcessID,
                asc2.IsSystemSoundSession,
                "",//asc2.Process.ProcessName
                "",//asc2.Process.MainWindowTitle
                asc2.DisplayName,
                asc2.SessionIdentifier
            );
            //AutoIncluded = ExcList.Contains(NameSet.Name) ? false : true;
            if (ExcList.Contains(NameSet.Name)){
                LastIncluded = false;
                AutoIncluded = false;
            }
            else
            {
                LastIncluded = true;
                AutoIncluded = SoundEnabled;
            }
            SetAvTime(AvgTime, AcInterval);
        }

        #region API Default Datas
        /// <summary>
        /// Converted from CoreAudioApi
        /// </summary>
        public SessionState State {
            get
            {
                try
                {
                    switch (asc2?.SessionState)
                    {
                        case CSCore.CoreAudioAPI.AudioSessionState.AudioSessionStateActive: return SessionState.Active;
                        case CSCore.CoreAudioAPI.AudioSessionState.AudioSessionStateInactive: return SessionState.Inactive;
                        default: return SessionState.Expired;
                    }
                }
                catch { return SessionState.Expired; }
            }
        }

        private string name;
        public NameSet NameSet { get; set; }
        /// <summary>
        /// Human readable process name
        /// </summary>
        public string Name { get { if (NameSet != null) return NameSet.Name; return name; } set => name = value; }
        /// <summary>
        /// ProcessID
        /// </summary>
        //public uint PID { get => (uint)ProcessID; }
        
        public int ProcessID { get { try { return (int)asc2?.ProcessID; } catch { return -1; } } }
        public string DisplayName { get { try { return asc2?.DisplayName; } catch { return string.Empty; } } }
        /// <summary>
        /// Take VERY LONG TIME when read this property. Because you will access process object when you use this.
        /// </summary>
        public string ProcessName { get { try { return asc2?.Process.ProcessName; } catch { return string.Empty; } } }
        /// <summary>
        /// Take VERY LONG TIME when read this property. Because you will access process object when you use this.
        /// </summary>
        public string MainWindowTitle { get { try { return asc2?.Process.MainWindowTitle; } catch { return string.Empty; } } }
        public string SessionIdentifier { get { try { return asc2?.SessionIdentifier; } catch { return string.Empty; } } }
        public bool IsSystemSoundSession { get { try { return (bool)asc2?.IsSystemSoundSession; } catch { return false; } } }

        /// <summary>
        /// Read or write volume of audio session. Always use RV when write volume.
        /// RV makes final volume = inputVolume * Pow(2, Reletive)
        /// </summary>
        public float Volume
        {
            get { using (var v = asc2?.QueryInterface<CSCore.CoreAudioAPI.SimpleAudioVolume>()) { try { return v.MasterVolume; } catch { return -1; } } }
            set { using (var v = asc2?.QueryInterface<CSCore.CoreAudioAPI.SimpleAudioVolume>()) { try { v.MasterVolume = (value > 1 ? 1 : (value < 0 ? 0 : value)); } catch { } } }
        }
        public float Peak
        {
            get
            {
                using (var p = asc2?.QueryInterface<CSCore.CoreAudioAPI.AudioMeterInformation>())
                {
                    try
                    {
                        //return p.GetMeteringChannelCount() > 1 ? p.GetChannelsPeakValues().Average() : p.PeakValue;
                        return p.PeakValue;
                    }
                    catch { return -1; }
                }
            }
        }
        public bool SoundEnabled
        {
            get { using (var v = asc2?.QueryInterface<CSCore.CoreAudioAPI.SimpleAudioVolume>()) { try { return !v.IsMuted; } catch { return false; } } }
            set { using (var v = asc2?.QueryInterface<CSCore.CoreAudioAPI.SimpleAudioVolume>()) { try { v.IsMuted = !value;
                        if (v.IsMuted) { LastIncluded = AutoIncluded; AutoIncluded = false; } else { AutoIncluded = LastIncluded; }
                    } catch { } } }
        }
        #endregion

        #region Customized Datas
        private float _Relative = 0f;
        /// <summary>
        /// Final volume would be multiplied by 2^Relative. This value is kept in -1~1.
        /// </summary>
        public float Relative { get => _Relative; set { _Relative = (value > 1) ? 1 : ((value < -1) ? -1 : value); } }
        /// <summary>
        /// Minimum volume. Session volume is kept above this value.
        /// </summary>
        public float MinVol { get; set; } = 0.01f;
        /// <summary>
        /// Make final volume with Relative. final volume would be <paramref name="vol"/> * Pow(2, Reletive)
        /// </summary>
        /// <param name="vol"></param>
        /// <returns>(float)Final volume</returns>
        /*private float RV(float vol)
        {
            float r = (float)Math.Pow(2, Relative);
            float v = vol * r;Console.WriteLine($"{vol:n5}, {v:n5}({r:n5},{Relative:n3})");
            return (v > 1) ? 1 : ((v < MinVol) ? MinVol : v);
        }*/
        /// <summary>
        /// The session is included to Auto controller when this flag is True. Default is True.
        /// </summary>
        public bool AutoIncluded { get; set; }
        //public bool AutoIncluded { get=> _AutoIncluded; set { _AutoIncluded = value; LastIncluded = value; } }
        //private bool _AutoIncluded = true;
        private bool LastIncluded = true;
        /// <summary>
        /// Average Calculation is enabled when this flag is True. Default is True.
        /// </summary>
        //public bool Averaging { get; set; } = true;
        #endregion


        #region Averaging
        /// <summary>
        /// Average peak value within AvTime.
        /// </summary>
        public double AveragePeak { get; private set; }
        /// <summary>
        /// Total stacking time for averaging.
        /// </summary>
        public double AvTime { get; private set; }//ms
        private uint AvCount = 0;
        private List<double> Peaks = new List<double>();

        /// <summary>
        /// Set stacking time for average calculation.
        /// </summary>
        /// <param name="averagingTime">Total stacking time.[ms](=AvTime)</param>
        /// <param name="unitTime">Passing time when calculate the average once.[ms]</param>
        public void SetAvTime(double averagingTime, double unitTime)
        {
            AvTime = averagingTime;
            AvCount = (uint)Convert.ToUInt32(averagingTime / unitTime);
            //Console.WriteLine($"Average Time Updated Cnt:{AvCount}");
            //JDPack.FileLog.Log($"Average Time Updated Cnt:{AvCount}");
            ResetAverage();
        }
        /// <summary>
        /// Clear all stacked peak values and set average to 0.
        /// </summary>
        public void ResetAverage() { Peaks.Clear(); AveragePeak = 0; }// JDPack.FileLog.Log("Average Reset"); }//Console.WriteLine("Average Reset");
        /// <summary>
        /// Add new peak value to peaks container and re-calculate AveragePeak value.
        /// </summary>
        /// <param name="peak"></param>
        public void SetAverage(double peak)
        {
            if (Peaks.Count > AvCount) Peaks.RemoveAt(0);
            Peaks.Add(peak);
            AveragePeak = Peaks.Average();
            //Console.WriteLine($"Av={AveragePeak}, PC={Peaks.Count}, AvT={AvTime}");
        }
        #endregion


        public int CompareTo(Session other)
        {
            // A null value means that this object is greater.
            if (other == null) return 1;
            else return this.Name.CompareTo(other.Name);
        }
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SessionData() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }//End class Session
    /// <summary>
    /// List object of SessionData. List&lt;Session&gt;
    /// </summary>
    public class SessionList : List<Session>
    {
        public SessionList() { this.Clear(); }
        /// <summary>
        /// Find session by its process id.
        /// <para>
        /// <list type="bullet">
        /// <item><description>Log($"Error(GetSession): ArgumentNullException") when ArgumentNullException.</description></item>
        /// <item><description>Log($"Error(GetSession): NullReferenceException") when NullReferenceException.</description></item>
        /// <item><description>Log($"Error(GetSession): {(Exception)e.ToString()}") when Exception</description></item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="pid">ProcessId</param>
        /// <returns>SessionData when find SessionData successfully or null.</returns>
        /*public Session GetSession(uint pid)
        {
            try { return this.Find(sc => sc.PID == pid); }
            catch (ArgumentNullException)
            {
                JDPack.FileLog.Log($"Error(GetSession): ArgumentNullException");
            }
            catch (NullReferenceException)
            {
                JDPack.FileLog.Log($"Error(GetSession): NullReferenceException");
            }
            catch (Exception e)
            {
                JDPack.FileLog.Log($"Error(GetSession): {e.ToString()}");
            }
            return null;
        }*/
        /// <summary>
        /// Find session by its process id.
        /// <para>Log($"Error(GetSession): ArgumentNullException") when ArgumentNullException. 
        /// Log($"Error(GetSession): NullReferenceException") when NullReferenceException. 
        /// Log($"Error(GetSession): {(Exception)e.ToString()}") when Exception</para>
        /// </summary>
        /// <param name="pid">ProcessId</param>
        /// <returns>SessionData when find SessionData successfully or null.</returns>
        public Session GetSession(int pid)
        {
            try { return this.Find(sc => sc.ProcessID == pid); }
            catch (ArgumentNullException)
            {
                JDPack.FileLog.Log($"Error(GetSession): ArgumentNullException");
            }
            catch (NullReferenceException)
            {
                JDPack.FileLog.Log($"Error(GetSession): NullReferenceException");
            }
            catch (Exception e)
            {
                JDPack.FileLog.Log($"Error(GetSession): {e.ToString()}");
            }
            return null;
        }
        /// <summary>
        /// Get Relative value with <code>GetSession</code> by its process id.
        /// </summary>
        /// <param name="pid">ProcessId</param>
        /// <returns>Relative value when find Relative value successfully or 0.0.</returns>
        public double GetRelative(int pid)
        {
            try { return GetSession(pid).Relative; }
            catch (NullReferenceException)
            {
                JDPack.FileLog.Log($"Error(GetRelative): NullReferenceException");
            }
            catch (Exception e)
            {
                JDPack.FileLog.Log($"Error(GetRelative): {e.ToString()}");
            }
            return 0.0;
        }

    }
}//End namespace Wale.Subclasses
