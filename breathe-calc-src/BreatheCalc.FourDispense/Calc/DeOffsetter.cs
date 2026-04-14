using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BreatheCalc.FourDispense.Data;

namespace BreatheCalc.FourDispense.Calc
{
    static class DeOffsetter
    {
        public static void CompensateForOffset(IList<Plate> plates)
        {
            var plateOffset = plates.SingleOrDefault(p => p.IsOffset);

            if (plateOffset == null)
            {
                throw new Exception("Could not find offset plate in data set!");
            }

            var ot = new OffsetTable(plateOffset);

            foreach (var plate in plates)
            {
                foreach (var well in plate.Wells)
                {
                    foreach (var measurement in well.Measurements)
                    {
                        measurement.VCorrection = ot.GetOffset(well.WellAddress, measurement.TimeMinutes);
                        measurement.VCorrected = measurement.V - measurement.VCorrection;
                    }
                }
            }
        }
    }
}
