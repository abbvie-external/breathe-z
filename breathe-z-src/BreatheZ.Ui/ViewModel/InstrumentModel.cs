using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutomationEngineering.LoggingHelper;
using BreatheZ.Ui.Annotations;
using BreatheZ.Ui.Engine;
using BreatheZ.Ui.MotionControl;
using BreatheZ.Ui.Properties;
using BreatheZ.Ui.Tecc;
using log4net;

namespace BreatheZ.Ui.ViewModel
{
    public class InstrumentModel : INotifyPropertyChanged
    {
        private static readonly ILog log = LogCreator.ClassLogger();
        private string electrodeManifoldPosition;
        private bool zStageUp;
        private bool electrodeManifoldMoving;
        private bool zStageMoving;

        private bool teccRecording;
        private TeccModel teccModel;
        private readonly List<DeckPosition> positions;
        private string blockingError;
        private string lastError;
        private readonly HighLevelMotionController highLevelMotionController;
        private int offsetTests;

        public InstrumentModel(HighLevelMotionController hlmc, TeccModel teccModel, List<DeckPosition> positions, MotionWarning motionWarning, Action pauseAction)
        {
            highLevelMotionController = hlmc;
            TeccModel = teccModel;
            OffsetTests = 20;
            
            ExperimentRunner = new Runner(motionWarning, pauseAction);

            ExperimentRunner.PropertyChanged += (s, e) => OnPropertyChanged("IsAcceptingCommands");

            this.positions = positions;

            ThreadPool.QueueUserWorkItem(delegate
            {
                Thread.CurrentThread.IsBackground = true;
                Thread.CurrentThread.Name = "Status Update";
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

                while (true)
                {
                    ZStageUp = highLevelMotionController.IsZStageUp;

                    Thread.Sleep(10);
                }
                // ReSharper disable once FunctionNeverReturns
            });
        }

        public List<DeckPosition> Positions { get { return positions; } }

        public int OffsetTests
        {
            get { return offsetTests; }
            set
            {
                if (value == offsetTests) return;
                offsetTests = value;
                OnPropertyChanged();
            }
        }

        public TeccModel TeccModel
        {
            get { return teccModel; }
            private set
            {
                teccModel = value;
                OnPropertyChanged();
            }
        }

        public Runner ExperimentRunner { get; private set; }

        public string BlockingError
        {
            get { return blockingError; }
            private set
            {
                if (value == blockingError) return;
                blockingError = value;
                log.Error("Blocking Error:" + value);
                OnPropertyChanged();
                log.Info("Blocking Error:" + value);
            }
        }
        
        public string ElectrodeManifoldPosition
        {
            get { return electrodeManifoldPosition; }

            private set
            {
                if (value == electrodeManifoldPosition) return;
                electrodeManifoldPosition = value;
                OnPropertyChanged();
                OnPropertyChanged("InMeasurementPosition");
                log.Info("At Position:" + ElectrodeManifoldPosition);
            }
        }

        public bool InMeasurementPosition
        {
            get
            {
                return (!string.IsNullOrWhiteSpace(ElectrodeManifoldPosition));
            }
        }

        public bool ZStageUp
        {
            get { return zStageUp; }

            private set
            {
                if (value.Equals(zStageUp)) return;
                zStageUp = value;
                OnPropertyChanged("ZStageUp");
            }
        }
        
        public bool ElectrodeManifoldMoving
        {
            get { return electrodeManifoldMoving; }
            set
            {
                if (value.Equals(electrodeManifoldMoving)) return;
                electrodeManifoldMoving = value;
                OnPropertyChanged("ElectrodeManifoldMoving");
                OnPropertyChanged("IsAcceptingCommands");

                log.Info("ElectrodeManifoldMoving:" + ElectrodeManifoldMoving);
            }
        }

        public bool ZStageMoving
        {
            get { return zStageMoving; }
            set
            {
                if (value.Equals(zStageMoving)) return;
                zStageMoving = value;
                OnPropertyChanged("ZStageMoving");
                OnPropertyChanged("IsAcceptingCommands");
            }
        }

        public string LastError
        {
            get { return lastError; }
            set
            {
                if (value == lastError) return;
                lastError = value;
                OnPropertyChanged();
            }
        }
      
        public bool TeccRecording
        {
            get { return teccRecording; }
            set
            {
                if (value.Equals(teccRecording)) return;
                teccRecording = value;
                OnPropertyChanged("TeccRecording");
                OnPropertyChanged("IsAcceptingCommands");
                log.Info("TECC Recordning:" + TeccRecording);
            }
        }

        public bool IsAcceptingCommands
        {
            get { return (!ElectrodeManifoldMoving && !TeccRecording && !ZStageMoving && !ExperimentRunner.IsRunning); }
        }

        public void HomeSystem()
        {
            if (ElectrodeManifoldMoving) throw new Exception("Electrode Already Moving");

            ElectrodeManifoldMoving = true;

            try
            {
                log.Info("Homing");

                SetZStageUp(true);

                highLevelMotionController.Home();

                ElectrodeManifoldPosition = "I am Home";
            }
            finally
            {
                ElectrodeManifoldMoving = false;
            }
        }


        public void MoveAndMeasure(string position, int? sampleTimeS, string stepName, bool? voltageOnly, int waitTimeS)
        {
            var pos = MapTextToPosition(position);
            MoveElectrodeManifold(pos);
            SetZStageUp(false);

            if (string.IsNullOrWhiteSpace(TeccModel.CurrentFile) || !TeccModel.IsInitialized)
            {
                TeccModel.StartNewFile();
                ConfigureTecc();
            }

            TakeTeccSample(pos.TeccPosition, sampleTimeS, stepName, voltageOnly ?? pos.OffsetOnly, waitTimeS);
            SetZStageUp(true);
        }

        public void MoveElectrodeManifold(string position)
        {
            var pos = MapTextToPosition(position);
            MoveElectrodeManifold(pos);
        }

        public void MoveElectrodeManifold(DeckPosition deckPosition)
        {
            if (ElectrodeManifoldMoving) throw new Exception("Electrode Already Moving");

            ElectrodeManifoldMoving = true;

            try
            {
                log.Info("Moving Electrode Manifold to position " + deckPosition.Name);

                SetZStageUp(true);

                highLevelMotionController.SetStagePosition(deckPosition.XAxisPosition());

                ElectrodeManifoldPosition = deckPosition.Name;

            }
            catch (Exception ex)
            {
                log.Error("Error when moving to desk position " + deckPosition, ex);
                throw;
            }
            finally
            {
                ElectrodeManifoldMoving = false;
            }
        }


        private DeckPosition MapTextToPosition(string position)
        {

            var deckPos = Positions
                .SingleOrDefault(p => p.Name.Equals(position, StringComparison.InvariantCultureIgnoreCase));

            if (deckPos == null)
            {
                throw new KeyNotFoundException("Could not find position for " + position);
            }

            return deckPos;
        }

        public void ConfigureTecc()
        {
            try
            {
                TeccRecording = true;
                TeccModel.ConfigureTeccFull();
            }
            finally
            {
                TeccRecording = false;
            }
        }

        public void SetZStageUp(bool up)
        {
            if (ZStageMoving) throw new Exception("ZStage Already Moving");

            try
            {
                ZStageMoving = true;
                highLevelMotionController.SetZAxisUp(up);
            }
            finally
            {
                ZStageMoving = false;

                if (up)
                {
                    Task.Delay(100).Wait();
                }
            }
        }

        public void ClearBlockingError()
        {
            BlockingError = null;
        }

        public void TakeTeccSample(int position, int? sampleTimeS, string stepName, bool voltageOnly, int minWaitTimeS = 0)
        {
            try
            {
                var tmr = Stopwatch.StartNew();

                TeccRecording = true;

                TeccModel.WriteResults(position, stepName, sampleTimeS, voltageOnly);

                while (tmr.Elapsed.TotalSeconds < minWaitTimeS)
                {
                    Task.Delay(100).Wait();
                }
            }
            finally
            {
                TeccRecording = false;
            }
        }
        
        public void CreateNewExperiment()
        {
            var list = ExperimentRunner.WorkList; 
            
            list.Clear();
            ExperimentRunner.CurrentUnit = null;
            ExperimentRunner.LastTimeCheck = null;
            ExperimentRunner.CurrentStatus = string.Empty;

            var settings = Settings.Default;

            var stageCount = new List<StageCount>
                                 {
                                     new StageCount{Stage = "Benzamil", Count = settings.Benzamil},
                                     new StageCount{Stage = "Forskolin", Count = settings.Forksolin},
                                     new StageCount{Stage = "Potentiator", Count = settings.Potentiator},
                                     new StageCount{Stage = "Bumetanide", Count = settings.Bumetinide},
                                     new StageCount{Stage = "UTP", Count = settings.Utp},
                                 }.Where(sc=>sc.Count > 0);

            var plates = (settings.Use2Plates) ? new[] { "CellPlate_1", "CellPlate_2" } : new[] { "CellPlate_1"};

            list.Add(new StartUpMessage(this));

            list.Add(new Initialization(this,false));

            for (int i = 0; i < 5; i++)
            {
                list.Add(new OffsetMeasurement(this, false, null, false));
            }

            int subStep = 1;
            for (int i = 0; i < settings.Baseline; i++)
            {
                var onFirst = true;
                foreach (var plate in plates)
                {
                    list.Add(new OffsetMeasurement(this, false, null,onFirst));
                    onFirst = false;

                    list.Add(new CellMeasurement(this, plate, "MeasurementCourse", false, subStep));
                }

                subStep++;
            }

            foreach (var count in stageCount)
            {
                subStep = 1;
                var onFirst = true;
                foreach (var plate in plates)
                {
                    var msg = string.Format("Golden Fingers, Dispense '{0}' into '{1}'", count.Stage, plate);

                    list.Add(new OffsetMeasurement(this, false, msg, onFirst));
                    onFirst = false;

                    list.Add(new Message(this, "Run Paused"));
                    list.Add(new CellMeasurement(this, plate, count.Stage, true, subStep));
                }

                subStep++;

                for (int i = 1; i < count.Count; i++)
                {
                    onFirst = true;
                    foreach (var plate in plates)
                    {
                        list.Add(new OffsetMeasurement(this, false, null, onFirst));
                        onFirst = false;

                        list.Add(new CellMeasurement(this, plate, "MeasurementCourse", false, subStep));
                    }

                    subStep++;
                }
            }

            list.Add(new OffsetMeasurement(this, false, null, false));
            list.Add(new Message(this, "Experiment Complete.  It is Miller time."));

            var plateMeasCount = new Dictionary<string, int>();

            foreach (var p in plates)
            {
                plateMeasCount[p] = 1;
            }

            int index = 1;
            foreach (var unitOfWork in list.Skip(1))
            {
                unitOfWork.Step = index;

                var cellMeasurement = unitOfWork as CellMeasurement;

                if (cellMeasurement != null)
                {
                    var count = plateMeasCount[cellMeasurement.Position];
                    cellMeasurement.MeasurementStep = count;
                    plateMeasCount[cellMeasurement.Position] = count + 1;
                }

                index++;
            }
        }

        public void CreateOffsetExperiment()
        {
            var list = ExperimentRunner.WorkList;

            list.Clear();

            if (OffsetTests < 1) return;

            list.Add(new Message(this, "Starting Offset Measurements"));

            list.Add(new Initialization(this,false));

            for (int i = 0; i < OffsetTests; i++)
            {
                list.Add(new OffsetMeasurement(this, false, null, false));
            }

            list.Add(new Message(this,"Done. Is that all you got?"));
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}