using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using AutomationEngineering.CommonTools.ExtensionMethods;
using BreatheZ.Ui.Util;
using BreatheZ.Ui.ViewModel;

namespace BreatheZ.Ui.Engine
{
    public class StartUpMessage : UnitOfWork
    {
        public override bool Display
        {
            get { return false; }
        }

        public StartUpMessage(InstrumentModel instrumentModel)
            : base(instrumentModel, false)
        {
            WorkDescription = string.Format("Message");
        }
        
        public override void Run()
        {
            Status = WorkStatus.Running;

            var sayings = new[]
                                  {
                                      "Tell Torben to Crank it Up!",
                                      "Let us blow this thing and go home!",
                                      "Die noo mite!",
                                      "Let us get this party started",
                                  };

            sayings.Shuffle();

            Task.Run(() =>
            {
                using (var reader = Talker.GetSpeechSynthesizer())
                {
                    reader.Rate = -2;
                    reader.Speak("Starting Run. . " + sayings.First());
                }
            });

            Status = WorkStatus.Completed;
        }
    }
}