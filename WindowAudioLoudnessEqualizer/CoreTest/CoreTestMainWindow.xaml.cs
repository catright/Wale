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
        Linker L;
        Core C;
        public CoreTestMainWindow()
        {
            InitializeComponent();
            L = new Linker();
            DataContext = L;

            C = new Core(L, false);
            C.SessionAdded += C_SessionAdded;
            C.SessionRemoved += C_SessionRemoved;
            C.Load();
            //Test();
        }

        private async void C_SessionAdded(object sender, SessionEventArgs e)
        {
            await Dispatcher?.InvokeAsync(() =>
            {
                SSPanel.Children.Add(new TextBlock() { Text = L.SSD.FirstOrDefault(s => s.BasePtr == e.BasePtr).DisplayName });
            });
        }
        private async void C_SessionRemoved(object sender, SessionEventArgs e)
        {
            await Dispatcher?.InvokeAsync(() =>
            {
                SSPanel.Children.Remove(new TextBlock() { Text = L.SSD.FirstOrDefault(s => s.BasePtr == e.BasePtr).DisplayName });
            });
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            C.SessionAdded -= C_SessionAdded;
            C.SessionRemoved -= C_SessionRemoved;
            C.Unload();
            SSPanel.Children.Clear();
        }



        private AudioSessionManager2 ASM;
        private void Test()
        {
            L.MMD = CSCore.CoreAudioAPI.MMDeviceEnumerator.TryGetDefaultAudioEndpoint(CSCore.CoreAudioAPI.DataFlow.Render, CSCore.CoreAudioAPI.Role.Multimedia);
            if (L.MMD == null) return;

            M.D(1); M.D(Thread.CurrentThread.GetHashCode());
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
            M.D(2); M.D(Thread.CurrentThread.GetHashCode());
            foreach (var asc in ASM.GetSessionEnumerator()) AddSession(asc);
        }
        private void AddSession(AudioSessionControl asc, bool safe = true)
        {
            var n = asc.DisplayName;
            M.D(31); M.D(Thread.CurrentThread.GetHashCode());
            if (!safe)
            {
                M.D(Dispatcher);
                Dispatcher?.Invoke(() =>
                {
                    M.D(32); M.D(Thread.CurrentThread.GetHashCode());
                    L.SSN.Add(n);
                });
            }
            else
            {
                M.D(32); M.D(Thread.CurrentThread.GetHashCode());
                L.SSN.Add(n);
            }
            M.D(3); M.D(Thread.CurrentThread.GetHashCode());
            //asc.SessionDisconnected += Asc_SessionDisconnected;
            //asc.StateChanged += Asc_StateChanged;
            var s = new Session(asc.BasePtr, ref L);
            s.Offline += S_Offline;
            SL2.Add(s);
        }

        private void S_Offline(object sender, EventArgs e)
        {
            //try { L.SSN.Remove(L.SSN.FirstOrDefault(sn => sn == (sender as AudioSessionControl).DisplayName)); }
            //catch (NotSupportedException)
            //{
                Dispatcher?.Invoke(() =>
                {
                    L.SSN.Remove(L.SSN.FirstOrDefault(sn => sn == (sender as AudioSessionControl).DisplayName));
                });
            //}
            M.D(7); M.D(Thread.CurrentThread.GetHashCode());
            SL2.Remove(sender as Session);
        }

        private void Asc_StateChanged(object sender, AudioSessionStateChangedEventArgs e)
        {
            if (e.NewState != AudioSessionState.AudioSessionStateActive)
            {//Dispatcher?.Invoke(() =>
             //{
                L.SSN.Remove(L.SSN.FirstOrDefault(sn => sn == (sender as AudioSessionControl).DisplayName));
                //});
                M.D(5); M.D(Thread.CurrentThread.GetHashCode());
                SL.Remove(sender as AudioSessionControl);
            }
        }

        private void ASM_SessionCreated(object sender, SessionCreatedEventArgs e) => AddSession(e.NewSession, false);
        private void Asc_SessionDisconnected(object sender, AudioSessionDisconnectedEventArgs e)
        {
            //Dispatcher?.Invoke(() =>
            //{
                L.SSN.Remove(L.SSN.FirstOrDefault(sn => sn == (sender as AudioSessionControl).DisplayName));
            //});
            M.D(4); M.D(Thread.CurrentThread.GetHashCode());
            SL.Remove(sender as AudioSessionControl);
        }

        private void VolButton_Click(object sender, RoutedEventArgs e)
        {
            M.D(99, "UI: VolButton_Click");
            //if (L.MD.Volume == 1) C.SetVol(0.5);
            //else C.SetVol(1);
        }

    }

}
