using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AutomationEngineering.LoggingHelper;
using SciChart.Charting.Visuals;

namespace BreatheCalc.FourDispense
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            LoggingConfiguration.Configure("BREATHE Data Cruncher",AppenderType.File);

            WinformLoggingConfiguration.SetupWinformsEventHandlers();

            SciChartSurface.SetRuntimeLicenseKey("THIS IS NOT A KEY - Need to license SciChart");

            var props = FourDispense.Properties.Settings.Default;

            if (props.CallRestoreSettings)
            {
                props.Upgrade();
                props.CallRestoreSettings = false;
                props.Save();
            }

        }
    }
}