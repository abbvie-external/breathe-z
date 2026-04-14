using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Cf.Tecc.Controller.Annotations;

namespace Cf.Tecc.Controller
{
    [Serializable]
    public class RtxSetting : INotifyPropertyChanged
    {
        private bool enabled;
        private float thresholdResistance;
        private byte inAmpGain;
        private float currentOutputGain;
        private float stimulusAmplitude;
        private byte currentToVoltageGain;
        private int? rtxIndex;

        [XmlIgnore]
        public int? RtxIndex
        {
            get { return rtxIndex; }
            set
            {
                if (value == rtxIndex) return;
                rtxIndex = value;
                OnPropertyChanged();
            }
        }

        public bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value;
                OnPropertyChanged();
                OnPropertyChanged("AllValid");
            }
        }

        public float ThresholdResistance
        {
            get { return thresholdResistance; }
            set
            {
                thresholdResistance = value;
                OnPropertyChanged();
                OnPropertyChanged("AllValid");
            }
        }

        public byte InAmpGain
        {
            get { return inAmpGain; }
            set
            {
                inAmpGain = value;
                OnPropertyChanged();
                OnPropertyChanged("AllValid");
            }
        }

        public float CurrentOutputGain
        {
            get { return currentOutputGain; }
            set
            {
                currentOutputGain = value;
                OnPropertyChanged();
                OnPropertyChanged("AllValid");
            }
        }

        public float StimulusAmplitude
        {
            get { return stimulusAmplitude; }
            set
            {
                stimulusAmplitude = value;
                OnPropertyChanged();
                OnPropertyChanged("AllValid");
            }
        }

        public byte CurrentToVoltageGain
        {
            get { return currentToVoltageGain; }
            set
            {
                currentToVoltageGain = value;
                OnPropertyChanged();
                OnPropertyChanged("AllValid");
            }
        }

        public RtxSetting()
        {

        }

        public RtxSetting(float rt, byte inampGain, float iogv, float ipp, byte ivgain)
        {
            ThresholdResistance = rt;
            InAmpGain = inampGain;
            CurrentOutputGain = iogv;
            StimulusAmplitude = ipp;
            CurrentToVoltageGain = ivgain;
        }

        public bool AllValid
        {
            get
            {
                return
                    // ReSharper disable CompareOfFloatsByEqualityOperator
                    ThresholdResistance != 0 &&
                    InAmpGain != 0 &&
                    CurrentToVoltageGain != 0 &&
                    StimulusAmplitude != 0 &&
                    CurrentToVoltageGain != 0;
                // ReSharper restore CompareOfFloatsByEqualityOperator
            }
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4}", ThresholdResistance, InAmpGain, CurrentOutputGain,
                StimulusAmplitude, CurrentToVoltageGain);
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