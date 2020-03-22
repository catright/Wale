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
using CSCore.Codecs;
using CSCore.SoundOut;
using CSCore.Streams.Effects;
using System.Threading;

namespace EqualizerTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        private void Equal()
        {
            //CSCore.SoundOut.WaveOutDevice.DefaultDevice;
            const string filename = @"C:\Temp\test.mp3";
            EventWaitHandle waitHandle = new AutoResetEvent(false);

            try
            {
                //create a source which provides audio data
                using (var source = CodecFactory.Instance.GetCodec(filename))
                {
                    //create the equalizer.
                    //You can create a custom eq with any bands you want, or you can just use the default 10 band eq.
                    Equalizer equalizer = Equalizer.Create10BandEqualizer(FluentExtensions.ToSampleSource(source));

                    //create a soundout to play the source
                    ISoundOut soundOut;
                    if (WasapiOut.IsSupportedOnCurrentPlatform)
                    {
                        soundOut = new WasapiOut();
                    }
                    else
                    {
                        soundOut = new DirectSoundOut();
                    }

                    soundOut.Stopped += (s, e) => waitHandle.Set();

                    IWaveSource finalSource = equalizer.ToWaveSource(16); //since the equalizer is a samplesource, you have to convert it to a raw wavesource
                    soundOut.Initialize(finalSource); //initialize the soundOut with the previously created finalSource
                    soundOut.Play();

                    /*
                     * You can change the filter configuration of the equalizer at any time.
                     */
                    equalizer.SampleFilters[0].AverageGainDB = 20; //eq set the gain of the first filter to 20dB (if needed, you can set the gain value for each channel of the source individually)

                    //wait until the playback finished
                    //of course that is optional
                    waitHandle.WaitOne();

                    //remember to dispose and the soundout and the source
                    soundOut.Dispose();
                }
            }
            catch (NotSupportedException ex)
            {
                Console.WriteLine("Fileformat not supported: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected exception: " + ex.Message);
            }
        }
        public double[] FFT(double[] data)
        {
            System.Numerics.Complex[] dfc = new System.Numerics.Complex[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                dfc[i] = new System.Numerics.Complex(data[i], 0);
            }
#pragma warning disable CS0618 // Type or member is obsolete
            System.Numerics.Complex[] result = MathNet.Numerics.IntegralTransforms.Fourier.NaiveForward(dfc, MathNet.Numerics.IntegralTransforms.FourierOptions.Default);
#pragma warning restore CS0618 // Type or member is obsolete
            double[] final = new double[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                final[i] = result[i].Real;
            }
            return final;
        }
        public List<double> FFT(List<double> data)
        {
            System.Numerics.Complex[] dfc = new System.Numerics.Complex[data.Count];
            for (int i = 0; i < data.Count; i++)
            {
                dfc[i] = new System.Numerics.Complex(data[i], 0);
            }
#pragma warning disable CS0618 // Type or member is obsolete
            System.Numerics.Complex[] result = MathNet.Numerics.IntegralTransforms.Fourier.NaiveForward(dfc, MathNet.Numerics.IntegralTransforms.FourierOptions.Default);
#pragma warning restore CS0618 // Type or member is obsolete
            List<double> final = new List<double>();
            foreach (System.Numerics.Complex d in result)
            {
                final.Add(d.Real);
            }
            return final;
        }


        
    }
}
