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

            SciChartSurface.SetRuntimeLicenseKey("MbFjugr9o+XLv5UyxXU0X+WHlb8TngOWHrXTOk0vIOzzCi6J6goLfYjx12U1h6CYfiOnrvaqe6SOk1N7fDZTW+/Lk5KQR1n7usgw/g3mX0F1Z0XzkCXJg6Fzp03Azs2cbNUHjttZW0CortHbB4zLt6LqNwPjBxmR8Eu9HSRqHH+HorolJ7WCvbR4gF20zHLKz8QCBzAg6lNlzkTT75mHgyaVcPI4IVbHKrKatG6EHeulVtcJhEyLjJEXOwdo2qQ9gRvKQT/ThtptT2Oz9RZ9jTd8xzrgNRzfiBk7yPIbavQFORvkmPEGSC+3XbmRfKXh/r6yiVItat5OmwAbqMtAiofAaAW7QVhuomyT36gD1WMTAj5Ce2lh6cPmAbGi5kx+xUq1wZzuaD1ba7Y0XcNWZv44dnW0NqgWbBrUYwRSITuqB715i5OMzfEyt/xMptzVm+f6ImCBc2dqCewxEBpvxKFuU1W1djM0Uz/e");

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