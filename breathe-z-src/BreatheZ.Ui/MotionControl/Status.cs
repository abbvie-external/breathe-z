using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreatheZ.Ui.MotionControl
{
    public class Status
    {
        public bool Busy { get; set; }

        public bool ReadyForWork
        {
            get { return !Busy; }
        }

        public bool ZAxisUp { get; set; }

        public Status Clone()
        {
            return new Status
                   {
                       Busy = Busy,
                       ZAxisUp = ZAxisUp
                   };
        }

        public override string ToString()
        {
            return string.Format("Busy={0},ReadyForWork={1},ZAxisUp={2}", Busy, ReadyForWork, ZAxisUp);
        }
    }
}
