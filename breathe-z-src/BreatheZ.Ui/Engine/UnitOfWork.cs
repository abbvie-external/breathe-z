using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BreatheZ.Ui.Annotations;
using BreatheZ.Ui.ViewModel;

namespace BreatheZ.Ui.Engine
{
    public abstract class UnitOfWork : INotifyPropertyChanged
    {
        private WorkStatus status;
        private string statusDescription;
        protected InstrumentModel InstrumentModel { get; private set; }
        public bool PauseOnStart { get; set; }

        public string WorkDescription { get; protected set; }
        public string Location { get; protected set; }
        public string MiscDescription { get; protected set; }

        public int? Step { get; set; }
        public int? SubStep { get; set; }
        public int? MeasurementStep { get; set; }

        public string StatusMessage { get; protected set; }

        public bool TimeCheckPoint { get; set; }

        public virtual bool Display
        {
            get { return true; }
        }

        public WorkStatus Status
        {
            get { return status; }
            set
            {
                if (value == status) return;
                status = value;
                OnPropertyChanged();
            }
        }

        public string HadError { get; protected set; }

        public string StatusDescription
        {
            get { return statusDescription; }
            protected set
            {
                if (value == statusDescription) return;
                statusDescription = value;
                OnPropertyChanged();
            }
        }

        protected UnitOfWork(InstrumentModel instrumentModel, bool pauseOnStart)
        {
            InstrumentModel = instrumentModel;
            PauseOnStart = pauseOnStart;
        }
        
        public abstract void Run();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}