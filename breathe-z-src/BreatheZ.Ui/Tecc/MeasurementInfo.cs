using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BreatheZ.Ui.Annotations;

namespace BreatheZ.Ui.Tecc
{
    public class MeasurementInfo : INotifyPropertyChanged
    {
        private string transientComment;
        private int? totalMeasurementTimeS;
        public int Position { get; set; }
        public string MeasurementStep { get; set; }
        public DateTime MeasurementStart { get; set; }
        public double TeccMeasurementTimeS { get; set; }
        public double? ElapsedSinceLastTeccMeasurementS { get; set; }
        public double? ElapsedSinceLastTeccMeasurementSamePlateS { get; set; }
        
        public int MeasurementIndex { get; set; }

        public int? TotalMeasurementTimeS 
        {
            get { return totalMeasurementTimeS; }
            set
            {
                totalMeasurementTimeS = value;
                OnPropertyChanged();
            }
        }

        public string RangeR { get; set; }
        public string RangeRRaw { get; set; }
        public string RangePd { get; set; }
        public string RangeIEq { get; set; }

        public string TransientComment
        {
            get { return transientComment; }
            set
            {
                transientComment = value;
                OnPropertyChanged();
            }
        }

        public string ErrorText { get; set; }
        public string RtxInfo { get; set; }

        public IList<ChannelMeasurement> Measurements { get; private set; }

        public MeasurementInfo()
        {
            Measurements = new List<ChannelMeasurement>();
            TransientComment = string.Empty;
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
