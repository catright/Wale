using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Wale
{
    /// <summary>
    /// Cooley–Tukey FFT
    /// </summary>
    class FFT_CT
    {
        public Complex[] x;                // storage for sample data
        public Complex[] X;                // storage for FFT answer

        // separate even/odd elements to lower/upper halves of array respectively.
        // Due to Butterfly combinations, this turns out to be the simplest way 
        // to get the job done without clobbering the wrong elements.
        void Separate(ref Complex[] a, int m, int n)
        {
            Complex[] b = new Complex[(n - m) / 2];
            for (int i = 0; i < (n - m) / 2; i++)    // copy all odd elements to b
                b[i] = a[m + i * 2 + 1];
            for (int i = 0; i < (n - m) / 2; i++)    // copy all even elements to lower-half of a
                a[m + i] = a[m + i * 2];
            for (int i = 0; i < (n - m) / 2; i++)    // copy all odd (from b) to upper-half of a[]
                a[m + i + (n - m) / 2] = b[i];
        }

        void FFT2(ref Complex[] X, int m, int n) // m: the first element of the array X
                                                 // n - 1: the last element of the array X
        {
            if (n - m < 2)
            {
                // bottom of recursion.
                // Do nothing here, because already X[0] = x[0]
            }
            else
            {
                Separate(ref X, m, n);      // all evens to lower half, all odds to upper half
                FFT2(ref X, m, m + (n - m) / 2);   // recurse even items
                FFT2(ref X, m + (n - m) / 2, n);   // recurse odd  items                                               
                for (int k = 0; k < (n - m) / 2; k++)// combine results of two half recursions
                {
                    Complex e = X[m + k];   // even
                    Complex o = X[m + k + (n - m) / 2];   // odd                                                      
                    Complex w = Complex.Exp(new Complex(0, -2 * Math.PI * k / (n - m))); // w is the "twiddle-factor"
                    X[m + k] = e + w * o;
                    X[m + k + (n - m) / 2] = e - w * o;
                }
            }
        }
        double Signal(double t)
        {
            double[] freq = { 2, 5, 11, 17, 29 }; // known freqs for testing
            double sum = 0;
            for (int j = 0; j < freq.GetLength(0); j++) // sum several known sinusoids into x[]
                sum += Math.Sin(2 * Math.PI * freq[j] * t);
            return sum;
        }

        void Fill()
        {
            // generate samples for testing
            int N = x.Length;
            for (int i = 0; i < N; i++)
            {
                x[i] = Signal((double)i / N);
                X[i] = x[i];  // copy into X[] for FFT work & result
            }
        }

        public FFT_CT(double SampleSeconds, List<double> Sample)
        {
            int N = Sample.Count;
            x = new Complex[N];                // storage for sample data
            X = new Complex[N];                // storage for FFT answer
            Fill();
            FFT2(ref X, 0, N);
        }
    }
}
