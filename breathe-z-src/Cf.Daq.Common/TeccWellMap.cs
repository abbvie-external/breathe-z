using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cf.Daq.Common
{
    public class TeccWellMap
    {
        public int TeccNumber { get; set; }
        public int WellNumber { get; set; }
        public string WellAddress { get; set; }
        public string WellRow { get; set; }
        public int WellRowNumeric { get; set; }
        public int WellColumn { get; set; }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4}", TeccNumber, WellNumber, WellRow, WellColumn, WellAddress);
        }
    }
}
