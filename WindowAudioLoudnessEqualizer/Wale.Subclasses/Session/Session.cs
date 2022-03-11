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
            if (ExcList != null && ExcList.Contains(NameSet.Name)) {
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
            //SetAvTimeAR(1000, 100, AvgTime, 100, AcInterval);
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
        private string processName;
        /// <summary>
        /// Take VERY LONG TIME when read this property. Because you will access process object when you use this.
        /// </summary>
        public string ProcessName
        {
            get
            {
                try
                {
                    return processName ?? (processName = asc2?.Process?.ProcessName);
                }
                catch { return string.Empty; }
            }
        }
        // we can not update main title if we using a cache.
        private string mainWindowTitle;
        // Force update main window title when needed. 
        public void ForceGetMainTitle() { try { mainWindowTitle = asc2?.Process?.MainWindowTitle; } catch { } }
        /// <summary>
        /// Take VERY LONG TIME when read this property. Because you will access process object when you use this.
        /// </summary>
        public string MainWindowTitle
        {
            get
            {
                try
                {
                    return mainWindowTitle ?? (mainWindowTitle = asc2?.Process?.MainWindowTitle);
                    //return asc2?.Process.MainWindowTitle;
                }
                catch { return string.Empty; }
            }
        }
        public string SessionIdentifier { get { try { return asc2?.SessionIdentifier; } catch { return string.Empty; } } }
        public bool IsSystemSoundSession { get { try { return (bool)asc2?.IsSystemSoundSession; } catch { return false; } } }
        public string Icon
        {
            get
            {
                string path = string.Empty;
                try { path = asc2?.IconPath; }
                catch (Exception e) { JPack.FileLog.Log($"PID({ProcessID}):FAILED to get iconPath from session control\n{e.Message}"); }
                if (string.IsNullOrWhiteSpace(path))
                {
                    Console.WriteLine($"PID({ProcessID}):there is no icon information. try to get it from process");
                    JPack.FileLog.Log($"PID({ProcessID}):there is no icon information. try to get it from process");
                    try
                    {
                        if (asc2?.Process?.MainModule != null) path = asc2?.Process?.MainModule.FileName;
                    }
                    catch
                    {
                        Console.WriteLine($"PID({ProcessID}):FAILED to get icon from process");
                        JPack.FileLog.Log($"PID({ProcessID}):FAILED to get icon from process");
                    }
                    Console.WriteLine(string.IsNullOrWhiteSpace(path)
                        ? $"PID({ProcessID}):FAILED to get icon"
                        : $"PID({ProcessID}):Suceed to get Icon from process");
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
            get { try { using (var v = asc2?.QueryInterface<CSCore.CoreAudioAPI.SimpleAudioVolume>()) { return v.MasterVolume; } } catch { return -1; } }
            set { try { using (var v = asc2?.QueryInterface<CSCore.CoreAudioAPI.SimpleAudioVolume>()) { v.MasterVolume = (value > 1 ? 1 : (value < 0 ? 0 : value)); } } catch { } }
        }
        public float Peak
        {
            get
            {
                try
                {
                    using (var p = asc2?.QueryInterface<CSCore.CoreAudioAPI.AudioMeterInformation>())
                    {
                        //return p.GetMeteringChannelCount() > 1 ? p.GetChannelsPeakValues().Average() : p.PeakValue;
                        return p.PeakValue;
                    }
                }
                catch { return -1; }
            }
        }
        private bool _SoundEnabled;
        public bool SoundEnabled
        {
            get { try { using (var v = asc2?.QueryInterface<CSCore.CoreAudioAPI.SimpleAudioVolume>()) { _SoundEnabled = !v.IsMuted; } } catch { _SoundEnabled = false; } return _SoundEnabled; }
            set
            {
                bool buffer = !value;
                try
                {
                    using (var v = asc2?.QueryInterface<CSCore.CoreAudioAPI.SimpleAudioVolume>())
                    {
                        v.IsMuted = buffer;
                        //if (v.IsMuted) { LastIncluded = AutoIncluded; AutoIncluded = false; } else { AutoIncluded = LastIncluded; }
                    }
                }
                catch { return; }
                _SoundEnabled = buffer;
            }
        }
        #endregion

        #region Customized Datas
        public object Locker { get; set; } = new object();
        public bool NewlyAdded { get; set; } = true;
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
        private uint AvCount = 0, ChunkSize = 5;
        private List<double> Peaks = new List<double>();
        private List<double> PeaksBuffer = new List<double>();
        public double AveragePeakAttack { get; private set; }
        public double AveragePeakRelease { get; private set; }
        private uint AttackCount = 0, AttackChunk = 5, ReleaseCount = 0, ReleaseChunk = 5;
        private List<double> PeaksAttack = new List<double>(), PeaksAttackBuffer = new List<double>();
        private List<double> PeaksRelease = new List<double>(), PeaksReleaseBuffer = new List<double>();

        /// <summary>
        /// Set stacking time for average calculation.
        /// </summary>
        /// <param name="averageTime">Total stacking time.[ms](=AvTime)</param>
        /// <param name="unitTime">Passing time when calculate the average once.[ms]</param>
        public void SetAvTime(double averageTime, double unitTime, double chunkTime = 100)
        {
            if (averageTime < 0) { throw new Exception("Invalid averageTime"); }
            if (unitTime < 0) { throw new Exception("Invalid unitTime"); }
            if (chunkTime < 0) { throw new Exception("Invalid chunkTime"); }
            //if (chunkTime < unitTime) { throw new Exception("chunkTime is smaller than unitTime"); }

            ChunkSize = Chunk(chunkTime, unitTime);
            AvCount = Count(averageTime, unitTime, ChunkSize);
            //Console.WriteLine($"Average Time Updated Cnt:{AvCount}");
            //JPack.FileLog.Log($"Average Time Updated Cnt:{AvCount}");
            ResetAverage();
        }
        /// <summary>
        /// Set stacking time for average calculation.
        /// </summary>
        /// <param name="attackTime">Total stacking time for attack.[ms]</param>
        /// <param name="releaseTime">Total stacking time for release.[ms]</param>
        /// <param name="unitTime">Passing time when calculate the average once.[ms]</param>
        public void SetAvTimeAR(double attackTime, double atChunkTime, double releaseTime, double relChunkTime, double unitTime)
        {
            if (attackTime < 0) { throw new Exception("Invalid attackTime"); }
            if (releaseTime < 0) { throw new Exception("Invalid releaseTime"); }
            if (unitTime < 0) { throw new Exception("Invalid unitTime"); }

            AttackChunk = Chunk(atChunkTime, unitTime);
            ReleaseChunk = Chunk(relChunkTime, unitTime);

            AttackCount = Count(attackTime, unitTime, AttackChunk);
            ReleaseCount = Count(releaseTime, unitTime, ReleaseChunk);

            ResetAverage();
        }
        private uint Chunk(double chunkTime, double unitTime)
        {
            int chunk = Convert.ToInt32(chunkTime / unitTime);
            return (chunk > 1 ? (uint)chunk : 1);
        }
        private uint Count(double countTime, double unitTime, uint chunkSize)
        {
            return Convert.ToUInt32((countTime / unitTime) / Convert.ToDouble(chunkSize));
        }

        /// <summary>
        /// Clear all stacked peak values and set average to 0.
        /// </summary>
        public void ResetAverage()
        {
            AveragePeak = 0; Peaks.Clear(); PeaksBuffer.Clear();
            // JPack.FileLog.Log("Average Reset"); }//Console.WriteLine("Average Reset");
            AveragePeakAttack = 0; PeaksAttack.Clear(); PeaksAttackBuffer.Clear();
            AveragePeakRelease = 0; PeaksRelease.Clear(); PeaksReleaseBuffer.Clear();
        }
        /// <summary>
        /// Add new peak value to peaks container and re-calculate AveragePeak value.
        /// </summary>
        /// <param name="peak"></param>
        //public void SetAverage(double peak) => Average_AR(peak);
        //public void SetAverage(double peak, double sigma = 4) => Average_Sigma(peak, sigma);
        public void SetAverage(double peak) => Average_Chunk(peak);

        private void Average_AR(double peak)
        {
            PeaksAttackBuffer.Add(peak);
            if (PeaksAttackBuffer.Count > AttackChunk)
            {
                PeaksAttack.AddRange(PeaksAttackBuffer);
                PeaksAttackBuffer.Clear();
                AveragePeakAttack = PeaksAttack.Average();
            }
            PeaksReleaseBuffer.Add(peak);
            if (PeaksReleaseBuffer.Count > ReleaseChunk)
            {
                PeaksRelease.AddRange(PeaksReleaseBuffer);
                PeaksReleaseBuffer.Clear();
                AveragePeakRelease = PeaksRelease.Average();
            }
        }
        private double low, high;
        private void Average_Sigma(double peak, double sigma)
        {
            if (sigma < 1) sigma = 1;
            //Peaks.Add(peak);
            //if (Peaks.Count > AvCount) Peaks.RemoveAt(0);
            if (AveragePeak == 0) PeaksBuffer.Add(peak);
            else
            {
                if (peak >= low && peak <= high) PeaksBuffer.Add(peak);
            }
            if (PeaksBuffer.Count > ChunkSize) {
                Peaks.AddRange(PeaksBuffer);
                PeaksBuffer.Clear();
                AveragePeak = Peaks.Average();
                low = AveragePeak / sigma;
                high = AveragePeak * sigma;
            }
        }
        private void Average_Chunk(double peak)
        {
            if (PeaksBuffer.Count >= ChunkSize)
            {
                if (Peaks.Count() > AvCount) Peaks.RemoveAt(0);
                Peaks.Add(PeaksBuffer.Average());
                PeaksBuffer.Clear();
                AveragePeak = Peaks.Average();
            }
            PeaksBuffer.Add(peak);
            //Console.WriteLine($"Av={AveragePeak}, PC={Peaks.Count}, AvT={AvTime}");
        }
        public void SetAverage2()
        {
            if (PeaksBuffer.Count >= ChunkSize)
            {
                if (Peaks.Count() > AvCount) Peaks.RemoveAt(0);
                Peaks.Add(PeaksBuffer.Average());
                PeaksBuffer.Clear();
                AveragePeak = Peaks.Average();
            }
            else PeaksBuffer.Add(Peak);
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
            cTokenS = new CancellationTokenSource();
            //CancellationToken cT = cTokenS.Token;
            ControlTask = AutoController(cTokenS.Token);
            AverageTask = Averaging(cTokenS.Token);
            AutoControlEnabled = true;
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
            finally { cTokenS?.Dispose(); }
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
        public bool Disposed => disposedValue;
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (AutoControlEnabled) AutoControlDisable();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                asc2?.Dispose();
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
        public void DisposedCheck() { int i = this.RemoveAll(s => s.Disposed == true); }

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
