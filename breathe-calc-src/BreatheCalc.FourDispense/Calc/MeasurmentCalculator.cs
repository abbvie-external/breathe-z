using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BreatheCalc.FourDispense.Data;

namespace BreatheCalc.FourDispense.Calc
{
    class MeasurmentCalculator
    {
        public void CalcPerWellParameters(IList<Plate> plates, Func<Measurement,double> aucMeasurementSelector)
        {
            var measurements = plates.SelectMany(p => p.Wells).SelectMany(w => w.Measurements);

            foreach (var measurement in measurements)
            {
                measurement.IEq = (Math.Abs(measurement.RCorrected) < 0.001) ? 0 : 1000 * measurement.VCorrected/measurement.RCorrected;
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                measurement.G = (measurement.RCorrected == 0) ? 0 : 1000/measurement.RCorrected;
                measurement.ValueForAuc = aucMeasurementSelector(measurement);
            }
        }
    }
}
