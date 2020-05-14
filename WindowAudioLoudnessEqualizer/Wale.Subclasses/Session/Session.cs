using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;

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
                "",//asc2.Process.ProcessName,
                "",//asc2.Process?.MainWindowTitle,
                asc2.DisplayName,
                asc2.SessionIdentifier
            );
            //AutoIncluded = ExcList.Contains(NameSet.Name) ? false : true;
            if (ExcList == null) { JPack.FileLog.Log($"Error: ExcList is null. {NameSet.SessionIdentifier}"); }
            if (ExcList != null && ExcList.Contains(NameSet.Name)){
                //LastIncluded = false;
                AutoIncluded = false;
            }
            else
            {
                //LastIncluded = true;
                //AutoIncluded = SoundEnabled;
                AutoIncluded = true;
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
        private void MakeNameSet()
        {
            try
            {
                NameSet = new NameSet(
                    ProcessID,
                    IsSystemSoundSession,
                    ProcessName,
                    MainWindowTitle,
                    DisplayName,
                    SessionIdentifier
                );
            }
            catch
            {
                string l = $"Error: failed to re-make NameSet of {Name}";
                Console.WriteLine(l);
                JPack.FileLog.Log(l);
                return;
            }
            NameSet.MakeName();
        }
        /// <summary>
        /// Human readable process name
        /// </summary>
        public string Name { get { return (NameSet != null) ? NameSet.Name : name; } set { MakeNameSet(); name = value; } }
        /// <summary>
        /// ProcessID
        /// </summary>
        //public uint PID { get => (uint)ProcessID; }
        
        public int ProcessID { get { try { return (int)asc2?.ProcessID; } catch { return -1; } } }
        public string GroupParam { get { try { return asc2?.GroupingParam.ToString(); } catch { return Guid.Empty.ToString(); } } }
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
        public string Icon
        {
            get
            {
                string path = asc2?.IconPath;
                if (string.IsNullOrWhiteSpace(path))
                {
                    Console.WriteLine($"PID({ProcessID}):there is no icon information. try to get it from process");
                    JPack.FileLog.Log($"PID({ProcessID}):there is no icon information. try to get it from process");
                    try { path = asc2?.Process.MainModule.FileName; } catch { Console.WriteLine($"PID({ProcessID}):FAILED to get icon from process"); JPack.FileLog.Log($"PID({ProcessID}):FAILED to get icon from process"); }
                    if (string.IsNullOrWhiteSpace(path)) { Console.WriteLine($"PID({ProcessID}):FAILED to get icon"); }
                    else { Console.WriteLine($"PID({ProcessID}):Suceed to get Icon from process"); }
                }
                else { Console.WriteLine($"PID({ProcessID}):Suceed to get Icon from session control"); }
                return path;
            }
        }
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
        private bool _SoundEnabled;
        public bool SoundEnabled
        {
            get { using (var v = asc2?.QueryInterface<CSCore.CoreAudioAPI.SimpleAudioVolume>()) { try { _SoundEnabled = !v.IsMuted; } catch { _SoundEnabled = false; } } return _SoundEnabled; }
            set { using (var v = asc2?.QueryInterface<CSCore.CoreAudioAPI.SimpleAudioVolume>()) {
                    bool buffer = !value;
                    try { v.IsMuted = buffer;
                        //if (v.IsMuted) { LastIncluded = AutoIncluded; AutoIncluded = false; } else { AutoIncluded = LastIncluded; }
                    } catch { return; }
                    _SoundEnabled = buffer;
                }
            }
        }
        #endregion

        #region Customized Datas
        private float _Relative = 0f;
        /// <summary>
        /// Final volume would be multiplied by 2^Relative. This value is kept in -1~1.
        /// </summary>
        public float Relative { get => _Relative; set { _Relative = (value > Wale.Configuration.Audio.RelativeEnd) ? Wale.Configuration.Audio.RelativeEnd : ((value < Wale.Configuration.Audio.RelativeEndInv) ? Wale.Configuration.Audio.RelativeEndInv : (Math.Abs(value) < 0.00001) ? 0 : value); } }
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
        public bool AutoIncluded { get; set; } = true;
        //public bool AutoIncluded { get=> _AutoIncluded; set { _AutoIncluded = value; LastIncluded = value; } }
        //private bool _AutoIncluded = true;
        //private bool LastIncluded = true;
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
        private uint AvCount = 0;
        private List<double> Peaks = new List<double>();

        /// <summary>
        /// Set stacking time for average calculation.
        /// </summary>
        /// <param name="averagingTime">Total stacking time.[ms](=AvTime)</param>
        /// <param name="unitTime">Passing time when calculate the average once.[ms]</param>
        public void SetAvTime(double averagingTime, double unitTime)
        {
            AvCount = (uint)Convert.ToUInt32(averagingTime / unitTime);
            //Console.WriteLine($"Average Time Updated Cnt:{AvCount}");
            //JPack.FileLog.Log($"Average Time Updated Cnt:{AvCount}");
            ResetAverage();
        }
        /// <summary>
        /// Clear all stacked peak values and set average to 0.
        /// </summary>
        public void ResetAverage() { Peaks.Clear(); AveragePeak = 0; }// JPack.FileLog.Log("Average Reset"); }//Console.WriteLine("Average Reset");
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
        public void SetAverage2()
        {
            if (Peaks.Count > AvCount) Peaks.RemoveAt(0);
            Peaks.Add(Peak);
            AveragePeak = Peaks.Average();
            //Console.WriteLine($"Av={AveragePeak}, PC={Peaks.Count}, AvT={AvTime}");
        }
        #endregion


        #region SessionAutoControl
        private CancellationTokenSource cTokenS = null;
        private bool AutoControlEnabled = false;
        private Task ControlTask, AverageTask;
        private int AutoInterval => Wale.Configuration.Audio.ACInterval;
        public void AutoControlEnable()
        {
            AutoControlEnabled = true;
            cTokenS = new CancellationTokenSource();
            CancellationToken cT = cTokenS.Token;
            ControlTask = AutoController(cT);
            AverageTask = Averaging(cT);
        }
        public async void AutoControlDisable()
        {
            AutoControlEnabled = false;
            await AutoControlDisableI();
        }
        private async Task AutoControlDisableI()
        {
            try
            {
                await Task.WhenAny(ControlTask, AverageTask, Task.Delay(100));
                if (cTokenS != null) { cTokenS.Cancel(); }
                await Task.WhenAll(ControlTask, AverageTask);
            }
            catch (OperationCanceledException e)
            {
                string msg = $"{nameof(OperationCanceledException)} thrown with message: {e.Message}"; JPack.FileLog.Log(msg); JPack.Debug.DML(msg);
            }
            finally { cTokenS.Dispose(); }
        }
        private async Task Averaging(CancellationToken cT)
        {
            while (AutoControlEnabled)
            {
                if (_SoundEnabled && AutoIncluded)
                {
                    await Task.Run(SetAverage2, cT);
                    if (cT.IsCancellationRequested) { cT.ThrowIfCancellationRequested(); }
                    await Task.Delay(AutoInterval, cT);
                }
            }
        }
        private async Task AutoController(CancellationToken cT)
        {
            while (AutoControlEnabled)
            {
                if (_SoundEnabled && AutoIncluded)
                {
                    await AutoControl(cT);
                    if (cT.IsCancellationRequested) { cT.ThrowIfCancellationRequested(); }
                    await Task.Delay(AutoInterval, cT);
                }
            }
        }
        private Task AutoControl(CancellationToken cT)
        {
            cT.ThrowIfCancellationRequested();

            StringBuilder dm = new StringBuilder().Append($"AutoVolume:{Name}({ProcessID}), inc={AutoIncluded}");

            // Control session(=s) when s is not in exclude list, auto included, active, and not muted
            if (AutoIncluded && State == SessionState.Active && _SoundEnabled)
            {
                double peak = Peak;
                // math 2^0=1 but skip math calculation and set relFactor to 1 for calc speed when relative is 0
                double relFactor = (Relative == 0 ? 1 : Math.Pow(4, Relative));
                double volume = Volume / relFactor;
                dm.Append($" P:{peak:n3} V:{volume:n3}");

                if (cT.IsCancellationRequested) { cT.ThrowIfCancellationRequested(); }// Cancellation Check
                // control volume when audio session makes sound
                if (peak > Wale.Configuration.Audio.MinPeak)
                {
                    double tVol, UpLimit;

                    // update average
                    if (Wale.Configuration.Audio.Averaging) SetAverage(peak);

                    // when averaging, lower volume once if current peak exceeds average or set volume along average.
                    if (Wale.Configuration.Audio.Averaging && peak < AveragePeak) tVol = Wale.Configuration.Audio.TargetLevel / AveragePeak;
                    else tVol = Wale.Configuration.Audio.TargetLevel / peak;

                    // calc upLimit by vfunc
                    switch (Wale.Configuration.Audio.VFunc)
                    {
                        case VFunction.Func.Linear:
                            UpLimit = VFunction.Linear(volume, Wale.Configuration.Audio.UpRate) + volume;
                            break;
                        case VFunction.Func.SlicedLinear:
                            UpLimit = VFunction.SlicedLinear(volume, Wale.Configuration.Audio.UpRate, Wale.Configuration.Audio.TargetLevel, Wale.Configuration.Audio.SliceFactors.A, Wale.Configuration.Audio.SliceFactors.B) + volume;
                            break;
                        case VFunction.Func.Reciprocal:
                            UpLimit = VFunction.Reciprocal(volume, Wale.Configuration.Audio.UpRate, Wale.Configuration.Audio.Kurtosis) + volume;
                            break;
                        case VFunction.Func.FixedReciprocal:
                            UpLimit = VFunction.FixedReciprocal(volume, Wale.Configuration.Audio.UpRate, Wale.Configuration.Audio.Kurtosis) + volume;
                            break;
                        default:
                            UpLimit = Wale.Configuration.Audio.UpRate + volume;
                            break;
                    }

                    if (cT.IsCancellationRequested) { cT.ThrowIfCancellationRequested(); }// Cancellation Check
                    // set volume
                    dm.Append($" T={tVol:n3} UL={UpLimit:n3}");//Console.WriteLine($" T={tVol:n3} UL={UpLimit:n3}");
                    Volume = (float)((tVol > UpLimit ? UpLimit : tVol) * relFactor);
                }
                JPack.Debug.DML(dm.ToString());// print debug message
            }
            return Task.FromResult(0);
        }
        #endregion


        public int CompareTo(Session other)
        {
            // A null value means that this object is greater.
            if (other == null) return 1;
            else return this.ProcessID.CompareTo(other.ProcessID);
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
                    Task.WaitAll(AutoControlDisableI());
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
                JPack.FileLog.Log($"Error(GetSession): ArgumentNullException");
            }
            catch (NullReferenceException)
            {
                JPack.FileLog.Log($"Error(GetSession): NullReferenceException");
            }
            catch (Exception e)
            {
                JPack.FileLog.Log($"Error(GetSession): {e.ToString()}");
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
        /// <returns>Session object if it found or null.</returns>
        public Session GetSession(int pid)
        {
            try { return this.Find(sc => sc.ProcessID == pid); }
            catch (ArgumentNullException)
            {
                JPack.FileLog.Log($"Error(GetSession): ArgumentNullException");
            }
            catch (NullReferenceException)
            {
                JPack.FileLog.Log($"Error(GetSession): NullReferenceException");
            }
            catch (Exception e)
            {
                JPack.FileLog.Log($"Error(GetSession): {e.ToString()}");
            }
            return null;
        }
        /// <summary>
        /// Get Relative value with <code>GetSession</code> by its process id.
        /// </summary>
        /// <param name="pid">ProcessId</param>
        /// <returns>Relative value if session is found or 0.0.</returns>
        public double GetRelative(int pid)
        {
            try { return GetSession(pid).Relative; }
            catch (NullReferenceException)
            {
                JPack.FileLog.Log($"Error(GetRelative): NullReferenceException");
            }
            catch (Exception e)
            {
                JPack.FileLog.Log($"Error(GetRelative): {e.ToString()}");
            }
            return 0.0;
        }


        /// <summary>
        /// Find session by grouping param.
        /// <para>Log($"Error(GetSession): ArgumentNullException") when ArgumentNullException. 
        /// Log($"Error(GetSession): NullReferenceException") when NullReferenceException. 
        /// Log($"Error(GetSession): {(Exception)e.ToString()}") when Exception</para>
        /// </summary>
        /// <param name="grparam">Grouping Param</param>
        /// <returns>Session object if it found or null.</returns>
        public Session GetSession(string grparam)
        {
            try { return this.Find(sc => sc.GroupParam == grparam); }
            catch (ArgumentNullException)
            {
                JPack.FileLog.Log($"Error(GetSession): ArgumentNullException");
            }
            catch (NullReferenceException)
            {
                JPack.FileLog.Log($"Error(GetSession): NullReferenceException");
            }
            catch (Exception e)
            {
                JPack.FileLog.Log($"Error(GetSession): {e.ToString()}");
            }
            return null;
        }
        /// <summary>
        /// Get Relative value with <code>GetSession</code> by grouping param.
        /// </summary>
        /// <param name="grparam">Grouping Param</param>
        /// <returns>Relative value if session is found or 0.0.</returns>
        public double GetRelative(string grparam)
        {
            try { return GetSession(grparam).Relative; }
            catch (NullReferenceException)
            {
                JPack.FileLog.Log($"Error(GetRelative): NullReferenceException");
            }
            catch (Exception e)
            {
                JPack.FileLog.Log($"Error(GetRelative): {e.ToString()}");
            }
            return 0.0;
        }


        /// <summary>
        /// Find session by SessionIdentifier.
        /// <para>Log($"Error(GetSession): ArgumentNullException") when ArgumentNullException. 
        /// Log($"Error(GetSession): NullReferenceException") when NullReferenceException. 
        /// Log($"Error(GetSession): {(Exception)e.ToString()}") when Exception</para>
        /// </summary>
        /// <param name="sid">SessionIdentifier</param>
        /// <returns>Session object if it found or null.</returns>
        public List<Session> GetSessionBySID(string sid)
        {
            try { return this.FindAll(sc => sc.SessionIdentifier == sid); }
            catch (ArgumentNullException)
            {
                JPack.FileLog.Log($"Error(GetSession): ArgumentNullException");
            }
            catch (NullReferenceException)
            {
                JPack.FileLog.Log($"Error(GetSession): NullReferenceException");
            }
            catch (Exception e)
            {
                JPack.FileLog.Log($"Error(GetSession): {e.ToString()}");
            }
            return null;
        }
    }
}//End namespace Wale.Subclasses
