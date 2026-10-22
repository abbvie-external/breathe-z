using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutomationEngineering.LoggingHelper;
using BreatheZ.Ui.MotionControl;
using BreatheZ.Ui.Properties;
using BreatheZ.Ui.ViewModel;
using log4net;

namespace BreatheZ.Ui.Engine
{
    class CellMeasurement : UnitOfWork
    {
        private static ILog log = LogCreator.ClassLogger();
        public string Position { get; private set; }
        public string MeasurementType { get; private set; }

        public CellMeasurement(InstrumentModel instrumentModel, string position, string measurementType, bool pauseOnStart, int subStep)
            : base(instrumentModel, pauseOnStart)
        {
            Position = position;
            MeasurementType = measurementType;
            SubStep = subStep;

            WorkDescription = "Cell Measurement";
            MiscDescription = (MeasurementType == "MeasurementCourse") ? "" : MeasurementType;
            Location = Position;

            StatusMessage = string.Format("Paused for Dispense of '{0}' into '{1}'", measurementType, position);
        }
        
        public override void Run()
        {
            Status = WorkStatus.Running;

            log.Info("Starting CellMeasurement on #" + Position);

            var shrt = Settings.Default.DebugShortSample;
            InstrumentModel.MoveAndMeasure(Position, shrt ? 0 : 28,MeasurementType, false, shrt ? 0 : 3);

            log.Info("Moving to Offset to hang out");
            InstrumentModel.MoveElectrodeManifold(PositionName.Offset);
            InstrumentModel.SetZStageUp(false);

            Task.Delay(1000).Wait();

            Status = WorkStatus.Completed;

            log.Info("Done with CellMeasurement on #" + Position);
        }
    }
}