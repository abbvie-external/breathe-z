using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BreatheZ.Ui.Annotations;

namespace BreatheZ.Ui.MotionControl
{
    public class MotionWarning : INotifyPropertyChanged
    {
        private readonly DaqOperations daqOperations;
        private bool warningOn;

        public MotionWarning(DaqOperations daqOp)
        {
            daqOperations = daqOp;
        }

        public bool WarningOn
        {
            get { return warningOn; }
            set
            {
                if (daqOperations != null)
                {
                    daqOperations.SetSingleDigOut(DaqOperations.DaqRelay, 0, 1, !value);
                }

                warningOn = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
