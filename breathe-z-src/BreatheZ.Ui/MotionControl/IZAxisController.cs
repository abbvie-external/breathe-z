using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreatheZ.Ui.MotionControl
{
    public interface IZAxisController
    {
        bool IsZAxisUp { get; }
        void SetZAxisUp(bool stageStage);
        bool ReverseZAxisSensorLogic { get; set; }
    }
}