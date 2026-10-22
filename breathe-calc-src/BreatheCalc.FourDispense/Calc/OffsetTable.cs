using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BreatheCalc.FourDispense.Data;
using BreatheCalc.FourDispense.Util;

namespace BreatheCalc.FourDispense.Calc
{
    class OffsetTable
    {
        private readonly Dictionary<string, PointD[]> offsetLists;

        public double GetOffset(string address, double timeMin)
        {
            var offsetList = offsetLists[address];

            if (offsetList.First().X > timeMin)
            {
                return offsetList.First().Y;
            }

            if (offsetList.Last().X <= timeMin)
            {
                return offsetList.Last().Y;
            }

            for (int i = 0; i < offsetList.Length - 1; i++)
            {
                var m0 = offsetList[i];
                var m1 = offsetList[i + 1];
                
                if (timeMin >= m0.X && timeMin < m1.X)
                {
                    var interp = Calcs.LinearInterpolation(timeMin, m0.X, m1.X, m0.Y, m1.Y);
                    return interp;
                }
            }

            throw new Exception(string.Format("Could not find valid reference for {0} time {1}", address,timeMin));
        }

        public OffsetTable(Plate offsetPlate)
        {
            offsetLists = new Dictionary<string, PointD[]>();

            foreach (var well in offsetPlate.Wells)
            {
                var offsetList = new PointD[well.Measurements.Count];
                offsetLists[well.WellAddress] = offsetList;

                for (int i = 0; i < well.Measurements.Count; i++)
                {
                    offsetList[i] = new PointD
                                    {
                                        X = well.Measurements[i].TimeMinutes,
                                        Y = well.Measurements[i].V
                                    };
                }
            }
        }
    }
}
