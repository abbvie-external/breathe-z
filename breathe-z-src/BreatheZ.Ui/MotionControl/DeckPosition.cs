using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreatheZ.Ui.MotionControl
{
    public class DeckPosition
    {
        public string Name { get; set; }
        public Func<double> XAxisPosition { get; set; }

        public int TeccPosition { get; set; }

        public bool OffsetOnly { get; set; }
    }
}
