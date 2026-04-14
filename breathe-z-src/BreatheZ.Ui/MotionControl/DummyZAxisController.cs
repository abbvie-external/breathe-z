using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BreatheZ.Ui.Properties;

namespace BreatheZ.Ui.MotionControl
{
    public class DummyZAxisController : IZAxisController
    {
        public bool IsZAxisUp { get; private set; }

        public void SetZAxisUp(bool stageState)
        {
            var shrt = Settings.Default.DebugShortSample;

                var action = new Action(
                    () =>
                    {
                        if (stageState != IsZAxisUp)
                        {
                            Task.Delay(shrt ? 100 : 1500).Wait();
                        }

                        IsZAxisUp = stageState;
                    });

                Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public bool ReverseZAxisSensorLogic { get; set; }
    }
}