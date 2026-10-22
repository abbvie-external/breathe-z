using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using BreatheZ.Ui.MotionControl;
using BreatheZ.Ui.Properties;
using BreatheZ.Ui.Util;
using BreatheZ.Ui.ViewModel;

namespace BreatheZ.Ui.Engine
{
    public class OffsetMeasurement : UnitOfWork
    {
        private readonly string onMoveMessage;

        public OffsetMeasurement(InstrumentModel instrumentModel, bool pauseOnStart, string onMoveMessage, bool isTimeCheck)
            : base(instrumentModel, pauseOnStart)
        {
            WorkDescription = string.Format("Offset Measurement");
            this.onMoveMessage = onMoveMessage;
            TimeCheckPoint = isTimeCheck;
        }
        
        public override void Run()
        {
            Status = WorkStatus.Running;
            var shrt = Settings.Default.DebugShortSample;

            InstrumentModel.MoveElectrodeManifold(PositionName.Offset);
            InstrumentModel.SetZStageUp(false);

            if (!string.IsNullOrWhiteSpace(onMoveMessage))
            {
                InstrumentModel.ExperimentRunner.CurrentStatus = onMoveMessage;

                Task.Run(() =>
                {
                    using (var reader = Talker.GetSpeechSynthesizer())
                    {
                        reader.Rate = -2;
                        reader.Speak(onMoveMessage.Replace('_', ' '));
                    }
                });
            }

            InstrumentModel.TakeTeccSample(0, shrt ? 5 : 12, PositionName.Offset, true, shrt ? 0 : 3);
            
            Status = WorkStatus.Completed;
        }
    }
}
