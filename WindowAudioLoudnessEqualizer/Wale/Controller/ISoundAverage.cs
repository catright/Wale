using System.ComponentModel;

namespace Wale.Controller
{
    internal interface ISoundAverage : INotifyPropertyChanged
    {
        double AveragePeak { get; }
        //void SetAvTime(double chunkTime = 100, double averageTime = -1, double unitTime = -1);
        void ResetAverage();
        void SetAverage(double peak);
    }
}
