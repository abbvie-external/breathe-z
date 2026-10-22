using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BreatheZ.Ui.MotionControl
{
    public class MotionControllerException : Exception
    {
        public MotionControllerException (string msg) : base(msg)
        {
        }
    }
}
