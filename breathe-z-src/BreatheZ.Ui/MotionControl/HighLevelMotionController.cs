using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AutomationEngineering.LoggingHelper;
using AutomationEngineering.NewportEsp300;
using BreatheZ.Ui.Annotations;
using BreatheZ.Ui.Properties;
using log4net;

namespace BreatheZ.Ui.MotionControl
{
    public class HighLevelMotionController : INotifyPropertyChanged
    {
        private static readonly ILog log = LogCreator.ClassLogger();
        private readonly IZAxisController zAxis;
        private readonly Esp300 xAxis;
        private bool xIsInitialized;

        public bool IsZStageUp
        {
            get { return zAxis.IsZAxisUp; }
            set
            {
                if (value == zAxis.IsZAxisUp) return;
                OnPropertyChanged();
            }
        }

        public void SetZAxisUp(bool stageStage)
        {
            zAxis.SetZAxisUp(stageStage);

            log.Info("Waiting for Z-Axis to move " + (stageStage ? "Up" : "Down"));
            
            while (IsZStageUp != stageStage)
            {
                Task.Delay(100).ConfigureAwait(false);
            }

            log.Info("Z-Axis Has moved to " + (stageStage ? "Up" : "Down"));
        }

        public void SetStagePosition(double position)
        {
            log.Info("Setting Stage Position to " + position);
            SetZAxisUp(true);

            if (!xIsInitialized)
            {
                Home();
            }

            OnPropertyChanged("");

            bool completed = false;

            var task = Task.Run(
                () =>
                {
                    xAxis.AxisMove(Settings.Default.XAxisChannel, position);

                    if (xAxis.GetDebugMode())
                    {
                        var shrt = Settings.Default.DebugShortSample;
                        Task.Delay(shrt ? 100 : 1500).Wait();
                    }

                    completed = true;
                });

            task.Wait(30000);

            if (!completed)
            {
                throw new Exception("Timed out waiting for stage move to " + position);
            }

            if (!xAxis.GetAxisPower(Settings.Default.XAxisChannel))
            {
                throw new Exception("Robot could not move. Clear obstacle, click 'Home' button on left, then restart run. Peace be your journey...");
            }
            
            OnPropertyChanged("");
            log.Info("Stage Position:" + position);
        }

        public void Home()
        {
            log.Info("Homing XAxis");
            SetZAxisUp(true);

            OnPropertyChanged("");
            Init();

            xAxis.FindHome(Settings.Default.XAxisChannel, HomeSearchMethod.FindHomeSignal);
            OnPropertyChanged("");

            xIsInitialized = true;
        }

        public void Init()
        {
            xAxis.Initialize();
            xAxis.SetAxisPower(Settings.Default.XAxisChannel, true);
            xAxis.SetVelocity(Settings.Default.XAxisChannel, Settings.Default.XAxisSpeed);
        }

        public HighLevelMotionController(IZAxisController zAxis, Esp300 xAxis)
        {
            this.zAxis = zAxis;
            this.xAxis = xAxis;
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