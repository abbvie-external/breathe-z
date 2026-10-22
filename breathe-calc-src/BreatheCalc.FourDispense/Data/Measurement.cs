using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BreatheCalc.FourDispense.Data
{
    class Measurement
    {
        public int Index { get; set; }
        public string Stage { get; set; }

        public double TimeMinutes { get; set; }
        public double R { get; set; }
        public double RCorrected { get; set; }
        public double V { get; set; }
        public double G { get; set; }
        public double VCorrected { get; set; }
        public double VCorrection { get; set; }
        
        public double IEq { get; set; }
        public double ValueForAuc { get; set; }
        
        public double? CBaseline { get; set; }
        public double? ValueBaselineCorrected { get; set; }
        public double? SlopeBaseline { get; set; }
        
        public double? CPotentiator { get; set; }
        public double? ValuePotentiator { get; set; }
        public double? ValueForsklin { get; set; }

        public bool IsInitial { get; set; }

        public override string ToString()
        {
            return string.Format("Time = {0}, Index={1}, Stage = {2}, R = {3}, V for AUC = {4}", TimeMinutes,Index, Stage,R,ValueForAuc);
        }
    }
}