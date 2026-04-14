using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BreatheCalc.FourDispense.Data
{
    class Well
    {
        #region Private Fields

        private string wellAddress;
        private List<string> wellAddresses;

        #endregion Private Fields

        #region Public Constructors

        public Well()
        {
            Measurements = new List<Measurement>();
            IsValid = true;
        }

        #endregion Public Constructors

        #region Public Properties

        public double Baseline { get; set; }
        public double? BaselineRt { get; set; }
        public double? BaseLineShift { get; set; }
        public double? BenzamilDelta { get; set; }
        public double? BumetanideDelta { get; set; }
        public string Col { get; set; }
        public int ColNumber
        {
            get
            {
                return int.Parse(Col);
            }
        }

        public Decimal? ConcUm { get; set; }
        public string CurveReference { get; set; }
        public string DispenseType { get; set; }
        public double EntireAuc { get; set; }
        public double? ForkolinDelta { get; set; }
        public double ForskolinAuc { get; set; }
        public double ForskolinPotentiatorAuc { get; set; }
        public double ForskolinSlope { get; set; }
        public bool IsValid { get; set; }
        public string LogicalCompound { get; set; }
        public Decimal? Lot { get; set; }
        public List<Measurement> Measurements { get; set; }

        public double? PostBenzamilRt { get; set; }

        public double PotentiatorAuc { get; set; }

        public double? PotentiatorDelta { get; set; }

        public string Row { get; set; }

        public int RowNumber
        {
            get
            {
                var rowChar = Row.ToCharArray()[0];
                return rowChar - 64;
            }
        }

        public int Tecc { get; set; }

        public string WellAddress
        {
            get { return wellAddress; }
            set
            {
                wellAddresses = new List<string>();

                wellAddress = value;
                wellAddresses.Add(wellAddress);

                var regEx = new Regex(@"([A-Z])0(\d+)");

                if (regEx.IsMatch(wellAddress))
                {
                    var alt = regEx.Replace(wellAddress,"$1$2");
                    wellAddresses.Add(alt);
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public bool Match(string address)
        {
            return wellAddresses.Contains(address);
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}, Measurements={1}", Tecc,WellAddress, Measurements.Count);
        }

        #endregion Public Methods
    }
}
