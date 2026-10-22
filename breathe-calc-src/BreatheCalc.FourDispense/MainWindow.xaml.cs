using AutomationEngineering.CommonTools.ExtensionMethods;
using BreatheCalc.FourDispense.Calc;
using BreatheCalc.FourDispense.Data;
using BreatheCalc.FourDispense.IO;
using BreatheCalc.FourDispense.Output;
using BreatheCalc.FourDispense.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Measurement = BreatheCalc.FourDispense.Data.Measurement;

namespace BreatheCalc.FourDispense
{
    public partial class MainWindow
    {
        private Experiment experiment;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            DisableAllButtons();

            UseG.Checked += ResetFile_Handler;
            UseG.Unchecked += ResetFile_Handler;

            CorrectOnMin.Checked += ResetFile_Handler;
            CorrectOnMin.Unchecked += ResetFile_Handler;

            UseRfumsAuc.Checked += ResetFile_Handler;
            UseRfumsAuc.Unchecked += ResetFile_Handler;

            RCorrection.TextChanged += GeneralChange_TextChanged;
        }

        private void GeneralChange_TextChanged(object sender, TextChangedEventArgs e)
        {
            ResetFile();
        }

        private void PickFile_OnClick(object sender, RoutedEventArgs e)
        {
            var browser = new OpenFileDialog
            {
                Filter = "CSV, TXT, LOG, DAT|*.csv;*.tsv;*.log;*.dat",
            };

            browser.ShowDialog(GetWindow(this));

            if (string.IsNullOrWhiteSpace(browser.FileName))
            {
                return;
            }

            FilePath.Text = browser.FileName;

            LoadFile();

            DisableAllButtons();
            EnableQc();
        }

        private void LoadFile()
        {
            var fn = FilePath.Text;

            if (!File.Exists(fn))
            {
                MessageBox.Show(this, "File does not exist!", "BREATHE", MessageBoxButton.OK);
            }

            var ext = System.IO.Path.GetExtension(fn)?.ToUpper();

            if (ext.IsAnyOf(".DAT", ".LOG"))
            {
                int measurementCount = 0;

                Experiment expWithoutStageInfo;

                if (ext == ".LOG")
                {
                    var loader = new TeccDataLoaderLog();
                    expWithoutStageInfo = loader.LoadData(fn, Settings.Default.RCorrection, Settings.Default.RowWise, null);
                }
                else
                {
                    var loader = new TeccDataLoaderDat();
                    expWithoutStageInfo = loader.LoadData(fn, Settings.Default.RCorrection, Settings.Default.RowWise, null);
                }

                var wellMeasurementCounts = expWithoutStageInfo.Plates
                    .Where(p => !p.IsOffset)
                    .SelectMany(p => p.Wells)
                    .Select(w => w.Measurements.Count)
                    .ToArray();

                var minNumber = wellMeasurementCounts.Min();

                var teccInput = new StepInputWindow(minNumber) { Owner = this };
                teccInput.ShowDialog();

                if (teccInput.IsCancelled)
                {
                    FilePath.Text = string.Empty;
                    return;
                }

                var stageCount = new List<StageCount>
                                 {
                                     new StageCount{Stage = "MeasurementCourse", Count = Settings.Default.Baseline},
                                     new StageCount{Stage = Stages.StageBenzamil, Count = Settings.Default.Benzamil},
                                     new StageCount{Stage = Stages.StageForskolin, Count = Settings.Default.Forksolin},
                                     new StageCount{Stage = Stages.StagePotentiator, Count = Settings.Default.Potentiator},
                                     new StageCount{Stage = Stages.StageBumetanide, Count = Settings.Default.Bumetinide},
                                 };

                if (ext == ".LOG")
                {
                    var loader = new TeccDataLoaderLog();
                    experiment = loader.LoadData(fn, Settings.Default.RCorrection, Settings.Default.RowWise, stageCount);
                }
                else
                {
                    var loader = new TeccDataLoaderDat();
                    experiment = loader.LoadData(fn, Settings.Default.RCorrection, Settings.Default.RowWise, stageCount);
                }
            }
            else
            {
                var loader = new BreatheDataReader();
                experiment = loader.LoadData(fn, 0, Settings.Default.RCorrection);
            }

            DeOffsetter.CompensateForOffset(experiment.Plates);

            var funG = new Func<Measurement, double>(a => a.G);
            var funI = new Func<Measurement, double>(a => a.IEq);

            Func<Measurement, double> selector;

            if (UseG.IsChecked == true)
            {
                selector = funG;
                experiment.AnalysisType = "G";
            }
            else
            {
                selector = funI;
                experiment.AnalysisType = "I";
            }

            var measCalc = new MeasurmentCalculator();
            measCalc.CalcPerWellParameters(experiment.Plates, selector);

            var stager = new StageAssigner();
            stager.AssignStages(experiment);

            var cross = new CrossMeasurmentCalc();
            cross.CalcBaseLine(experiment, 3, Settings.Default.CorrectOnMinimum);
            cross.CalcForskolinComponents(experiment, Settings.Default.ForskolinCorrectionPts);
            cross.CalcForskolinAuc(experiment, Settings.Default.UseRfumsAuc);
            cross.CalcPotentiatorAuc(experiment);
            cross.CalcForskolinPotentiatorAuc(experiment);
            cross.CalcEntireAuc(experiment);
            cross.CalcDeltas(experiment);
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            Settings.Default.Save();
        }

        private void LoadData_OnClick(object sender, RoutedEventArgs e)
        {
            var export = new Exporter();
            var tes = export.CreateRawTes(experiment.AssayPlates);
            export.CopyTesToClipboard(tes, true);
        }

        private void LoadSummary_OnClick(object sender, RoutedEventArgs e)
        {
            var export = new Exporter();
            var tes = export.CreateSummaryTes(experiment.AssayPlates);
            export.CopyTesToClipboard(tes, false);
        }
        
        private void QcData_OnClick(object sender, RoutedEventArgs e)
        {
            var qc = new PlateQc(experiment) { Owner = this };
            qc.ShowDialog();

            EnableExport();
        }

        private void ResetFile_Handler(object sender, RoutedEventArgs e)
        {
            ResetFile();
        }

        private void DisableAllButtons()
        {
            btnCopySF.IsEnabled = false;
            btnCopySummary.IsEnabled = false;
            btnQC.IsEnabled = false;
        }

        private void EnableQc()
        {
            btnQC.IsEnabled = true;
        }

        private void EnableExport()
        {
            btnCopySF.IsEnabled = true;
            btnCopySummary.IsEnabled = true;
        }

        private void ResetFile()
        {
            experiment = null;
            FilePath.Text = string.Empty;
            DisableAllButtons();
        }
    }
}