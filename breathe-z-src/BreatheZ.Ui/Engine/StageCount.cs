using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreatheZ.Ui.Engine
{
    class StageCount
    {
        public string Stage { get; set; }
        public int Count { get; set; }

        public override string ToString()
        {
            return string.Format("{0}, {1}", Stage, Count);
        }
    }
}
