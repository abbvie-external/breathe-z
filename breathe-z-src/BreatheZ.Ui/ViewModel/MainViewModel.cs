using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using AutomationEngineering.CommonTools.ExtensionMethods;
using AutomationEngineering.CommonTools.Gui;
using AutomationEngineering.LoggingHelper;
using AutomationEngineering.NewportEsp300;
using BreatheZ.Ui.Annotations;
using BreatheZ.Ui.MotionControl;
using BreatheZ.Ui.Properties;
using BreatheZ.Ui.Tecc;
using log4net;

namespace BreatheZ.Ui.ViewModel
{
    class MainViewModel : INotifyPropertyChanged
    {
        private static readonly ILog log = LogCreator.ClassLogger();

        private bool isTestWindowVisible;
        private InstrumentModel instrumentModel;
        
        public InstrumentModel InstrumentModel
        {
            get { return instrumentModel; }
            
            private set
            {
                instrumentModel = value;
                OnPropertyChanged();
            }
        }

        public bool IsTestWindowVisible
        {
            get { return isTestWindowVisible; }
            set
            {
                if (value.Equals(isTestWindowVisible)) return;
                isTestWindowVisible = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel(Action pauseAction)
        {
            var debug = Settings.Default.SimulateMotionControl;

            var daqOp = new DaqOperations();

            var motionWarning = new MotionWarning(debug ? null : daqOp);

            var controller = (debug)
                ? new DummyZAxisController() as IZAxisController
                : new RealZAxisController(daqOp);

            try
            {
                controller.SetZAxisUp(true);
            }
            catch (Exception ex)
            {
                log.Error("Could not init daq stuff",ex);
                controller = new DummyZAxisController();
                
                QuickMessage.DisplayMessage(null,"Could not initialize DAQ system, switching to debug!",4000);
            }

            var highLevelMotion = new HighLevelMotionController(controller, new Esp300(Properties.Settings.Default.XAxisPort, debug));

            var dataPath = Settings.Default.DataFolder;

            if (string.IsNullOrWhiteSpace(dataPath) || !Directory.Exists(dataPath))
            {
                dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "TECC Raw Data Files");

                Settings.Default.DataFolder = dataPath;
                Settings.Default.Save();
            }

            var tecc = new TeccModel {DataPath = dataPath};

            if (!Directory.Exists(tecc.DataPath))
            {
                Directory.CreateDirectory(tecc.DataPath);
            }

            var positions = new List<DeckPosition>
                            {
                                new DeckPosition {Name = PositionName.Offset, XAxisPosition = ()=>Settings.Default.PositionOffset, TeccPosition = 0, OffsetOnly = true},
                                new DeckPosition {Name = PositionName.Wash, XAxisPosition = ()=>Settings.Default.PositionWash, OffsetOnly = true},
                                new DeckPosition {Name = PositionName.CellPlate1, XAxisPosition = ()=>Settings.Default.PositionCellPlate1, TeccPosition = 1},
                                new DeckPosition {Name = PositionName.CellPlate2, XAxisPosition = ()=>Settings.Default.PositionCellPlate2, TeccPosition = 2},
                            };

            InstrumentModel = new InstrumentModel(highLevelMotion, tecc, positions, motionWarning, pauseAction);
            
            Settings.Default.PropertyChanged += Default_PropertyChanged;
        }

        void Default_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.IsAnyOf("PlateFormat", "ExpectedBoardCount"))
            {
                InstrumentModel.TeccModel.IsInitialized = false;
            }
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
