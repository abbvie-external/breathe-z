using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BreatheZ.Ui.Annotations;

namespace BreatheZ.Ui.Tecc
{
    public class ChannelMeasurement : INotifyPropertyChanged
    {
        public int TeccChannel { get; set; }
        public int BoardChannel { get; set; }
        public int TeccBoard { get; set; }
        public string WellAddress { get; set; }
        public double Rt { get; set; }
        public double Pd1 { get; set; }
        public double Pd2 { get; set; }
        public double IEq { get; set; }

        public string BoardWell { get; set; }

        public bool HasIssues { get; set; }

        public bool RtxChange { get; set; }

        public float? NextRtxR { get; set; }
        public float? NextRtxIndexMsg { get { return (NextRtxR == CurrentRtxR) ? null : NextRtxR; } }
        public float? CurrentRtxR { get; set; }

        public bool VoltageOnly { get; set; }

        public double RtRaw { get; set; }
        public bool StatusOscillation { get; set; }
        public bool StatusVoltageOverflowOnV1 { get; set; }
        public bool StatusVoltageOverflowOnV2 { get; set; }
        public bool StatusCurrentOverflowOnRt { get; set; }
        public bool StatusVoltageOverflowOnRt { get; set; }

        public string StatusOscillationMsg
        {
            get { return StatusOscillation ? "Osc" : string.Empty; }
        }

        public string StatusVoltageOverflowOnV1Msg
        {
            get { return StatusVoltageOverflowOnV1 ? "OF" : string.Empty; }
        }

        public string StatusVoltageOverflowOnV2Msg
        {
            get { return StatusVoltageOverflowOnV2 ? "OF" : string.Empty; }
        }

        public string StatusCurrentOverflowOnRtMsg
        {
            get { return StatusCurrentOverflowOnRt ? "OF" : string.Empty; }
        }

        public string StatusVoltageOverflowOnRtMsg
        {
            get { return StatusVoltageOverflowOnRt ? "OF" : string.Empty; }
        }

        public int OutputLine { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
