using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CSCore;
using CSCore.CoreAudioAPI;
using System.Threading;

namespace CoreTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class CoreTestMainWindow : Window
    {
        private void DW(object o) => System.Diagnostics.Debug.WriteLine(o);
        public CoreTestMainWindow()
        {
            InitializeComponent();
            L = new Linker();
            DataContext = L;
            //C = new Core(L);
            Test();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //C.Unload();
        }
        public Linker L;
        //public Core C;



        private AudioSessionManager2 ASM;
        private void Test()
        {
            L.MMD = CSCore.CoreAudioAPI.MMDeviceEnumerator.TryGetDefaultAudioEndpoint(CSCore.CoreAudioAPI.DataFlow.Render, CSCore.CoreAudioAPI.Role.Multimedia);
            if (L.MMD == null) return;

            DW(1); DW(Thread.CurrentThread.GetHashCode());
            TryGetSs();
        }
        private void TryGetSs()
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA) GetSs();
            else
            {
                using (ManualResetEvent waitHandle = new ManualResetEvent(false))
                {
                    ThreadPool.QueueUserWorkItem(
                        (o) =>
                        {
                            try
                            {
                                GetSs();
                            }
                            finally
                            {
                                waitHandle.Set();
                            }
                        });
                    waitHandle.WaitOne();
                }
            }
        }
        private List<AudioSessionControl> SL = new List<AudioSessionControl>();
        private List<Session> SL2 = new List<Session>();
        private void GetSs()
        {
            if (ASM == null && L.MMD != null) ASM = AudioSessionManager2.FromMMDevice(L.MMD);
            ASM.SessionCreated += ASM_SessionCreated;
            DW(2); DW(Thread.CurrentThread.GetHashCode());
            foreach (var asc in ASM.GetSessionEnumerator()) AddSession(asc);
        }
        private void AddSession(AudioSessionControl asc, bool safe = true)
        {
            var n = asc.DisplayName;
            DW(31); DW(Thread.CurrentThread.GetHashCode());
            if (!safe)
            {
                DW(Dispatcher);
                Dispatcher?.Invoke(() =>
                {
                    DW(32); DW(Thread.CurrentThread.GetHashCode());
                    L.SNames.Add(n);
                });
            }
            else
            {
                DW(32); DW(Thread.CurrentThread.GetHashCode());
                L.SNames.Add(n);
            }
            DW(3); DW(Thread.CurrentThread.GetHashCode());
            //asc.SessionDisconnected += Asc_SessionDisconnected;
            //asc.StateChanged += Asc_StateChanged;
            var s = new Session(asc.BasePtr, L);
            s.Offline += S_Offline;
            SL2.Add(s);
        }

        private void S_Offline(object sender, EventArgs e)
        {
            try { L.SNames.Remove(L.SNames.FirstOrDefault(sn => sn == (sender as AudioSessionControl).DisplayName)); }
            catch (NotSupportedException)
            {
                Dispatcher?.Invoke(() =>
                {
                    L.SNames.Remove(L.SNames.FirstOrDefault(sn => sn == (sender as AudioSessionControl).DisplayName));
                });
            }
            DW(7); DW(Thread.CurrentThread.GetHashCode());
            SL2.Remove(sender as Session);
        }

        private void Asc_StateChanged(object sender, AudioSessionStateChangedEventArgs e)
        {
            if (e.NewState != AudioSessionState.AudioSessionStateActive)
            {//Dispatcher?.Invoke(() =>
             //{
                L.SNames.Remove(L.SNames.FirstOrDefault(sn => sn == (sender as AudioSessionControl).DisplayName));
                //});
                DW(5); DW(Thread.CurrentThread.GetHashCode());
                SL.Remove(sender as AudioSessionControl);
            }
        }

        private void ASM_SessionCreated(object sender, SessionCreatedEventArgs e) => AddSession(e.NewSession, false);
        private void Asc_SessionDisconnected(object sender, AudioSessionDisconnectedEventArgs e)
        {
            //Dispatcher?.Invoke(() =>
            //{
                L.SNames.Remove(L.SNames.FirstOrDefault(sn => sn == (sender as AudioSessionControl).DisplayName));
            //});
            DW(4); DW(Thread.CurrentThread.GetHashCode());
            SL.Remove(sender as AudioSessionControl);
        }

        private void VolButton_Click(object sender, RoutedEventArgs e)
        {
            //if (L.MD.Volume == 1) C.SetVol(0.5);
            //else C.SetVol(1);
        }

    }

}
