using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JDPack
{
    public class NotifyPropertyChanged : System.ComponentModel.INotifyPropertyChanged
    {
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberNameAttribute] string propertyName = null)
        {
            var propertyChanged = PropertyChanged;
            if (propertyChanged != null)
            {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    }
}
