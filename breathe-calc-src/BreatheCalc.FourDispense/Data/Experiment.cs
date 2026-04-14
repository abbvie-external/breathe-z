using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreatheCalc.FourDispense.Data
{
    class Experiment
    {
        public IList<Plate> Plates { get; set; }
        public string Protocol { get; set; }
        public string AnalysisType { get; set; }
        public double ResistanceCompensation { get; set; }

        public int MinNumberMeasurements { get; set; }
        public int MaxNumberMeasurements { get; set; }

        public IList<Plate> AssayPlates
        {
            get { return Plates.Where(p => !p.IsOffset).ToList(); }
        }

        public bool Mapped { get; set; }
    }
}
