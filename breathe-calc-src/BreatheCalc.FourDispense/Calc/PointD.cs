using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BreatheCalc.FourDispense.Calc
{
    class PointD
    {
        public double X { get; set; }
        public double Y { get; set; }

        public PointD(double x, double y)
        {
            X = x;
            Y = y;
        }

        public PointD()
        {
        }

        public override string ToString()
        {
            return string.Format("{0},{1}", X, Y);
        }
    }
}
