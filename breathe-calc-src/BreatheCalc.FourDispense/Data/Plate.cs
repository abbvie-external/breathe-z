using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BreatheCalc.FourDispense.Data
{
    class Plate
    {
        public string FileName { get; set; }
        public string FileFolder { get; set; }

        public int Platform { get; set; }
        public List<Well> Wells { get; set; }

        public bool IsOffset { get; set; }

        public Plate()
        {
            Wells = new List<Well>();
        }

        public override string ToString()
        {
            return string.Format("{0}, Wells={1}", Platform,Wells.Count);
        }
    }
}
