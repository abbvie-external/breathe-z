using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cf.Tecc.Controller
{
    public struct ChannelValue
    {
        public int Channel;
        public int TeccBoard;
        public int BoardChannel;
        public double Resistance;
        public double ResistanceRaw;
        public double VoltagePreMv;
        public double VoltagePostMv;
        public string WellAddress;
        public string WellRow;
        public int WellColumn;
        public int WellNumber;

        public bool StatusOscillation;
        public bool StatusVoltageOverflowOnV1;
        public bool StatusVoltageOverflowOnV2;
        public bool StatusCurrentOverflowOnRt;
        public bool StatusVoltageOverflowOnRt;

        public byte CurrentRtxIndex;
        public byte NextRtxIndex;

        public string BoardWell { get { return TeccBoard + "/" + Channel; } }

        public bool HasIssues
        {
            get
            {
                return RtxChange || Overflow || StatusOscillation;
            }
        }

        public bool Overflow
        {
            get
            {
                return StatusVoltageOverflowOnRt || StatusVoltageOverflowOnV1 ||
                       StatusVoltageOverflowOnV2 || StatusCurrentOverflowOnRt;
            }
        }


        public bool RtxChange
        {
            get { return CurrentRtxIndex != NextRtxIndex; }
        }

        public override string ToString()
        {
            return string.Format("Channel={0}, V={1}, V1={2}, V2={3}", Channel, Resistance, VoltagePreMv, VoltagePostMv);
        }
    }
}
