using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using BreatheZ.Ui.Util;
using BreatheZ.Ui.ViewModel;

namespace BreatheZ.Ui.Engine
{
    public class Message : UnitOfWork
    {
        private readonly string message;

        public override bool Display
        {
            get { return false; }
        }

        public Message(InstrumentModel instrumentModel,string message)
            : base(instrumentModel, false)
        {
            WorkDescription = string.Format("Message");
            this.message= message;
        }
        
        public override void Run()
        {
            Status = WorkStatus.Running;
            
            if (!string.IsNullOrWhiteSpace(message))
            {
                InstrumentModel.ExperimentRunner.CurrentStatus = message;

                Task.Run(() =>
                {
                    using (var reader = Talker.GetSpeechSynthesizer())
                    {
                        reader.Rate = -2;
                        reader.Speak(message.Replace('_', ' '));
                    }
                });
            }

            Status = WorkStatus.Completed;
        }
    }
}