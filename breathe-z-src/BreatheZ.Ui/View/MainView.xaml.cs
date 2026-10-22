using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using AutomationEngineering.CommonTools;
using AutomationEngineering.CommonTools.Gui;
using AutomationEngineering.LoggingHelper;
using BreatheZ.Ui.Properties;
using BreatheZ.Ui.Util;
using BreatheZ.Ui.ViewModel;
using Cf.Tecc.Controller;
using log4net;
using log4net.Core;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace BreatheZ.Ui.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainView
    {
        private readonly MainViewModel model;
        private static readonly ILog log = LogCreator.ClassLogger();
        
        public MainView()
        {
            InitializeComponent();
            
            LoggingConfiguration.Configure("BREATHE-Z Instrument",AppenderType.ActivityLogger | AppenderType.File,"0.4");
            LoggingConfiguration.StartSession();

            Configuration.DefaultWindowCaption = Title;

            log.LogWithFlag("Starting", Level.Info, FlagType.Audit);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            Dispatcher.UnhandledException += OnDispatcherUnhandledException;

            var pauseAction = new Action(() =>

                                         {
                                             Dispatcher.Invoke(
                                                 () =>
                                                 {
                                                     if (WindowState == WindowState.Minimized)
                                                     {
                                                         WindowState = WindowState.Normal;
                                                     }

                                                     Activate();
                                                     PauseButton.Focus();
                                                 });

                                             var tmr = Stopwatch.StartNew();
                                             var tmrBeep = Stopwatch.StartNew();

                                             var firstTime = true;

                                             var delay = 4;

                                             var wasDelay = false;

                                             while(!model.InstrumentModel.ExperimentRunner.IsRunning)
                                             {
                                                 Task.Delay(10).Wait();

                                                 if (tmr.Elapsed.TotalSeconds > 60 && tmrBeep.Elapsed.Seconds > delay)
                                                 {
                                                     if (firstTime)
                                                     {
                                                         firstTime = false;
                                                         wasDelay = true;

                                                         using (var reader = Talker.GetSpeechSynthesizer())
                                                         {
                                                             reader.Rate = -2;
                                                             reader.Speak("Hey, this isn't California Time Here");
                                                         }

                                                         delay = 2;
                                                     }
                                                     else
                                                     {
                                                         SystemSounds.Exclamation.Play();
                                                     }

                                                     tmrBeep.Restart();
                                                 }
                                             }

                                             using (var reader = Talker.GetSpeechSynthesizer())
                                             {
                                                 reader.Rate = -2;
                                                 reader.Speak(wasDelay ? "Better late than never" : "Rock in Roll");
                                             }
                                         });

            model = new MainViewModel(pauseAction);

            var rtx = Settings.Default.RtxSettings;

            if (rtx == null || !rtx.Settings.Any())
            {
                rtx = new RtxSettingGroup();

                for (int i = 0; i < 2; i++)
                {
                    rtx.Settings.Add(new RtxSetting(0, 0, 0, 0, 0) { Enabled = false });
                }

                rtx.Settings.Add(new RtxSetting(40000, 2, 4, 1, 20) { Enabled = true });
                rtx.Settings.Add(new RtxSetting(10000, 2, 4, 4, 20) { Enabled = true });
                rtx.Settings.Add(new RtxSetting(1000, 2, 5, 6, 10) { Enabled = true });
                rtx.Settings.Add(new RtxSetting(80, 4, 4, 10, 10) { Enabled = true });

                for (int i = 0; i < 30; i++)
                {
                    rtx.Settings.Add(new RtxSetting(0, 0, 0, 0, 0) { Enabled = false });
                }

                Settings.Default.RtxSettings = rtx;
                Settings.Default.Save();
            }

            foreach (var rtxSetting in Settings.Default.RtxSettings.Settings)
            {
                rtxSetting.PropertyChanged += rtxSetting_PropertyChanged;
            }

            ResetRtxNumbering();

            DataContext = model;
            
            model.IsTestWindowVisible = true;
        }

        void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            string errorMessage = string.Format("An unhandled exception occurred: {0}", e.Exception.Message);
            MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            log.Error(e.Exception);

            if (e.Exception.InnerException != null)
            {
                log.Error("Inner", e.Exception.InnerException);
            }

            e.Handled = true;
        }

        void rtxSetting_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Enabled")
            {
                return;
            }

            ResetRtxNumbering();
        }

        private void ResetRtxNumbering()
        {
            int i = 1;
            foreach (var rtxSetting in Settings.Default.RtxSettings.Settings.OrderByDescending(r=>r.ThresholdResistance))
            {
                if (rtxSetting.Enabled)
                {
                    rtxSetting.RtxIndex = i;
                    i++;
                }
                else
                {
                    rtxSetting.RtxIndex = null;
                }
            }
        }

        void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                var exceptionFlat = e.Exception.Flatten();

                var inners = exceptionFlat.InnerExceptions.Select(se => new { se.Message, se.StackTrace });

                log.Error(inners,exceptionFlat);

            }
            catch (Exception ex)
            {
                log.Info("Exception when handling unobserved task exception", ex);
            }

            e.SetObserved();
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                log.Error(e.ExceptionObject);

            }
            catch (Exception ex)
            {
                log.Info("Exception when handling unobserved task exception", ex);
            }
        }
       
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow = this;
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!MainTab.IsSelected)
            {
                SizeToContent = SizeToContent.Manual;
            }
        }

        private void SaveSettings_OnClick(object sender, RoutedEventArgs e)
        {
            var rtx = Settings.Default.RtxSettings.Settings
                .Where(s=>s.AllValid && s.Enabled)
                .ToList();

            var valid = rtx.Count > 0 && rtx.Count < 16;

            if (!valid)
            {
                QuickMessage.DisplayMessage(null, "RTX Settings Invalid!", 3000);
                return;
            }

            Settings.Default.Save();
            model.InstrumentModel.TeccModel.TeccSettingsDirty = true;
        }

        private void CopyDataToClipboard_Click(object sender, RoutedEventArgs e)
        {
            var data = model.InstrumentModel.TeccModel.GetCurrentFileContents();
            Clipboard.Clear();

            if (data != null)
            {
                Clipboard.SetText(data);
            }
        }
        
        private async void SendManifoldPostion_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            
            var pos = button.Tag.ToString();

            try
            {
                await Task.Run(() => model.InstrumentModel.MoveElectrodeManifold(pos));
            }
            catch (Exception ex)
            {
                log.Error("Error moving XAxis", ex);
                throw;
            }
        }

        private async void ZAxisUp_OnClick(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => model.InstrumentModel.SetZStageUp(true));
        }

        private async void ZAxisDown_OnClick(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => model.InstrumentModel.SetZStageUp(false));
        }
        
        private async void TeccMeasurement_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var pos = button.Tag.ToString();

            var task = Task.Run(() => model.InstrumentModel.MoveAndMeasure(pos, Settings.Default.DebugShortSample ? 2 : 28, "Manual Measurement", null, 3));

            await task;
        }

        private void TeccNewFile_OnClick(object sender, RoutedEventArgs e)
        {
            model.InstrumentModel.TeccModel.StartNewFile();
        }

        private void ClearError_OnClick(object sender, RoutedEventArgs e)
        {
            model.InstrumentModel.ClearBlockingError();
        }
       
        private void TeccFileReName_OnClick(object sender, RoutedEventArgs e)
        {
            var old = model.InstrumentModel.TeccModel.CurrentFileSanExtension;

            var result = TextInputView.Show(this, "Enter new filename without extension", old);

            if (result.IsCancelled)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(result.Text))
            {
                return;
            }

            var fn = result.Text.Trim();

            if (fn.Contains("."))
            {
                QuickMessage.DisplayMessage(null, "Cannot contain a .", 3000);
                return;
            }

            if (fn.Contains("\\"))
            {
                QuickMessage.DisplayMessage(null, "Cannot contain a \\", 3000);
                return;
            }

            if (Path.GetInvalidPathChars().Any(fn.Contains))
            {
                QuickMessage.DisplayMessage(null, "File contains invalid characters!", 3000);
                return;
            }

            if (fn == old.Trim())
            {
                QuickMessage.DisplayMessage(null, "File name is same as old one!", 3000);
                return;
            }

            model.InstrumentModel.TeccModel.RenameFile(fn);
        }

        private void EditComment_Click(object sender, RoutedEventArgs e)
        {
            var measure = model.InstrumentModel.TeccModel.SelectedMeasurement;

            if (measure == null) return;

            var resp = TextInputView.Show(this, "Enter New Comment", measure.TransientComment);

            if (resp.IsCancelled)
            {
                return;
            }

            model.InstrumentModel.TeccModel.ResetComment(measure,resp.Text);
        }

        private void ChangeBaseFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(model.InstrumentModel.TeccModel.CurrentFile))
            {
                var resp = MessageBox.Show(this, "This will clear the current data file and terminate experiment.\n\nThis cool?", "BREATHE", MessageBoxButton.OKCancel);

                if (resp != MessageBoxResult.OK)
                {
                    return;
                }
            }

            var fbd = new FolderBrowserDialog();

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                model.InstrumentModel.TeccModel.CurrentFileSanExtension = string.Empty;
                model.InstrumentModel.TeccModel.DataPath = fbd.SelectedPath;
                Settings.Default.DataFolder = fbd.SelectedPath;
                Settings.Default.Save();
            }
        }

        private void CreateNewExperiment(object sender, RoutedEventArgs e)
        {
            if (!VerifyExpClear()) return;

            var win = new StepInputWindow {Owner = this};
            win.ShowDialog();

            Settings.Default.Save();

            if (win.IsCancelled) return;

            log.Info("Manually Creating New Experiment");
            model.InstrumentModel.CreateNewExperiment();
        }

        private void StartExperiment(object sender, RoutedEventArgs e)
        {
            log.Info("Manually Starting Experiment");
            model.InstrumentModel.ExperimentRunner.IsRunning = true;
        }

        private void PauseExperiment(object sender, RoutedEventArgs e)
        {
            log.Info("Manually Pausing Experiment");
            model.InstrumentModel.ExperimentRunner.IsRunning = false;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!model.InstrumentModel.ExperimentRunner.IsRunning && e.Key == Key.Space)
            {
                model.InstrumentModel.ExperimentRunner.IsRunning = true;
                e.Handled = true;
            }
        }

        private async void HomeSystem_OnClick(object sender, RoutedEventArgs e)
        {
            log.Info("Manually Homing System");
            await Task.Run(() => model.InstrumentModel.HomeSystem());
        }

        private async void InitTecc_OnClick(object sender, RoutedEventArgs e)
        {
            log.Info("Manually Initializing TECC");
            await Task.Run(() => model.InstrumentModel.ConfigureTecc());
        }

        private void ClearExperiment(object sender, RoutedEventArgs e)
        {
            if (!VerifyExpClear()) return;

            log.Info("Manually Clearing Experiment");

            model.InstrumentModel.ExperimentRunner.Clear();
        }

        private void CreateOffsetExperiment(object sender, RoutedEventArgs e)
        {
            if (!VerifyExpClear()) return;

            log.Info("Manually Creating Offset Experiment");

            model.InstrumentModel.CreateOffsetExperiment();
        }

        private bool VerifyExpClear()
        {
            if (model.InstrumentModel.ExperimentRunner.PendingWorkList.Any())
            {
                var result = MessageBox.Show(this, "Are you sure you want to nuke the current experiment?", "BREATHE-Z", MessageBoxButton.OKCancel);

                return (result == MessageBoxResult.OK);
            }

            return true;
        }

        private void MainView_OnClosing(object sender, CancelEventArgs e)
        {
            if (model.InstrumentModel.ExperimentRunner.PendingWorkList.Any())
            {
                var result = MessageBox.Show(this, "Close with pending data to collect???", "BREATHE-Z", MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    log.Info("User chose to close");
                    e.Cancel = false;
                }
                else
                {
                    log.Info("User chose to cancel close");
                    e.Cancel = true;
                }
            }
            else
            {
                log.Info("No pending experiments so accept close without cancel");
            }
        }
    }
}
