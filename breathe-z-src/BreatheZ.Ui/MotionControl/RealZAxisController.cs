using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BreatheZ.Ui.MotionControl
{
    public class RealZAxisController : IZAxisController
    {
        private readonly DaqOperations daqOperations;

        public RealZAxisController(DaqOperations daqOperations)
        {
            this.daqOperations = daqOperations;
        }

        private const int zAxisOutLine = 0;
        private const int zAxisUpInLine = 1;
        
        public void SetZAxisUp(bool stageStage)
        {
            daqOperations.SetSingleDigOut(DaqOperations.DaqRelay, 0, zAxisOutLine, !stageStage);
        }

        public bool ReverseZAxisSensorLogic { get; set; }

        public bool IsZAxisUp
        {
            get
            {
                var values = daqOperations.GetMultiDigIn(DaqOperations.DaqDigIo, 0, 0, 3);
                var up = (ReverseZAxisSensorLogic) ? !values[zAxisUpInLine] : values[zAxisUpInLine];
                return up;
            }
        }
    }
}
