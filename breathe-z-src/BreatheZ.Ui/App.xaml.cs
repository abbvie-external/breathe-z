using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BreatheZ.Ui.View;

namespace BreatheZ.Ui
{
    public partial class App : Application
    {
        private Mutex instanceMutex;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // check that there is only one instance of the control panel running...
            bool createdNew;

            instanceMutex = new Mutex(true, @"Global\BreatheZ!", out createdNew);

            if (!createdNew)
            {
                instanceMutex = null;
                Shutdown();
            }
            else
            {
                (new MainView()).Show();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (instanceMutex != null)
            {
                instanceMutex.ReleaseMutex();
            }

            base.OnExit(e);
        }
    }
}
