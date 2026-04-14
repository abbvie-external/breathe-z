using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cf.TeccViewer
{
    public class Measurement
    {
        public string FileName { get; set; }
        public int Position { get; set; }
        public int Number { get; set; }
        public int Tecc { get; set; }
        public float? R { get; set; }
        public float? G { get; set; }
        public float? Pd1 { get; set; }
        public float? Pd2 { get; set; }
        public float? I { get; set; }
        public string WellAddress { get; set; }
    }
}