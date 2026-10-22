using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BreatheZ.Ui.ViewModel;

namespace BreatheZ.Ui.Engine
{
    class Initialization : UnitOfWork
    {
        public Initialization(InstrumentModel instrumentModel, bool pauseOnStart) : base(instrumentModel, pauseOnStart)
        {
            WorkDescription = "Create New File";
        }

        public override void Run()
        {
            Status = WorkStatus.Running;

            var task = Task.Run(() => {
                InstrumentModel.ConfigureTecc();
                InstrumentModel.TeccModel.StartNewFile();
            });

            InstrumentModel.HomeSystem();

            task.Wait(60000);

            Status = WorkStatus.Completed;
        }
    }
}
