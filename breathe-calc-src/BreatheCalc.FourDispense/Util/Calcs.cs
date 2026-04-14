using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreatheCalc.FourDispense.Util
{
    class Calcs
    {
        public static double LinearInterpolation(double x, double x0, double x1, double y0, double y1)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if ((x1 - x0) == 0)
            {
                return (y0 + y1) / 2;
            }
            return y0 + (x - x0) * (y1 - y0) / (x1 - x0);
        }
    }
}
