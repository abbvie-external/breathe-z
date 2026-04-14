using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AutomationEngineering.CommonTools.Tables;
using BreatheCalc.FourDispense.Data;
using Experiment = BreatheCalc.FourDispense.Data.Experiment;

namespace BreatheCalc.FourDispense.Output
{
    internal class Exporter
    {
        public TableEntryStore CreateRawTes(IList<Plate> plates)
        {
            var tes = new TableEntryStore();

            foreach (var plate in plates)
            {
                foreach (var well in plate.Wells)
                {
                    foreach (var measurement in well.Measurements)
                    {
                        tes.AddCell("File Name", plate.FileName, typeof (string));
                        tes.AddCell("Platform", plate.Platform, typeof (int));
                        tes.AddCell("Well", well.WellAddress, typeof (string));
                        tes.AddCell("Row", well.Row, typeof (string));
                        tes.AddCell("Column", well.Col, typeof (int));

                        tes.AddCell("Valid", well.IsValid, typeof(bool));
                        tes.AddCell("ANumber", well.LogicalCompound, typeof(string));
                        tes.AddCell("Lot", well.Lot, typeof(int));
                        tes.AddCell("Conc", well.ConcUm, typeof(double));
                        
                        tes.AddCell("TECC", well.Tecc, typeof (int));
                        tes.AddCell("Time(min)", measurement.TimeMinutes, typeof (double));
                        tes.AddCell("Index", measurement.Index, typeof (int));
                        tes.AddCell("Stage", measurement.Stage, typeof (string));
                        tes.AddCell("R", measurement.RCorrected, typeof (int));
                        tes.AddCell("V Uncorrected", measurement.V, typeof (double));
                        tes.AddCell("V Correction", measurement.VCorrection, typeof (double));
                        tes.AddCell("V", measurement.VCorrected, typeof (double));
                        tes.AddCell("I", R(measurement.IEq, 1), typeof(double));
                        tes.AddCell("G", R(measurement.G,1), typeof (double));
                        tes.AddCell("Value for AUC", R(measurement.ValueForAuc,1), typeof (double));
                        tes.AddCell("Baseline Corrected", R(measurement.ValueBaselineCorrected,1), typeof (double));
                        tes.AddCell("Baseline Correction", R(measurement.CBaseline,1), typeof (double));
                        tes.AddCell("Baseline Slope", R(measurement.SlopeBaseline,1), typeof (double));
                        tes.AddCell("Forskolin", R(measurement.ValueForsklin,1), typeof (double));
                        tes.AddCell("Potentiator", R(measurement.ValuePotentiator,1), typeof (double));
                        tes.AddCell("Potentiator Correction", R(measurement.CPotentiator,1), typeof (double));

                        tes.AddCell("Baseline", R(well.Baseline, 1), typeof(double));
                        tes.AddCell("Baseline Rt", R(well.BaselineRt, 1), typeof(double));
                        tes.AddCell("Delta Benza", R(well.BenzamilDelta, 1), typeof(double));
                        tes.AddCell("Post Benzamil RT", R(well.PostBenzamilRt, 1), typeof(double));
                        tes.AddCell("Delta Fsk", R(well.ForkolinDelta, 1), typeof(double));
                        tes.AddCell("Delta Potentiator", R(well.PotentiatorDelta, 1), typeof(double));
                        tes.AddCell("Total Fsk+Pot", R(well.ForkolinDelta + well.PotentiatorDelta, 1), typeof(double));
                        tes.AddCell("Delta Bumetanide", R(well.BumetanideDelta, 1), typeof(double));
                        tes.AddCell("AUC Fsk", R(well.ForskolinAuc, 1), typeof(double));
                        tes.AddCell("AUC Potentiator", R(well.PotentiatorAuc, 1), typeof(double));
                        tes.AddCell("AUC Fsk+Pot", R(well.ForskolinPotentiatorAuc, 1), typeof(double));
                        tes.AddCell("Entire AUC", R(well.EntireAuc, 1), typeof(double));
                        tes.AddCell("Baseline Shift", R(well.BaseLineShift, 2), typeof(double));

                        tes.IncrementRow();
                    }
                }
            }

            return tes;
        }

        public TableEntryStore CreateSummaryTes(IList<Plate> plates)
        {
            var tes = new TableEntryStore();

            foreach (var plate in plates)
            {
                foreach (var well in plate.Wells)
                {
                    tes.AddCell("File Name", plate.FileName, typeof (string));
                    tes.AddCell("Platform", plate.Platform, typeof (int));
                    tes.AddCell("Well", well.WellAddress, typeof (string));

                    tes.AddCell("ANumber", well.LogicalCompound, typeof(string));
                    tes.AddCell("Lot", well.Lot, typeof(int));
                    tes.AddCell("Conc", well.ConcUm, typeof(double));

                    var b = !well.IsValid;

                    tes.AddCell("Baseline", b ? null :  R(well.Baseline, 1), typeof(double));
                    tes.AddCell("Baseline Rt", b ? null : R(well.BaselineRt, 1), typeof(double));
                    tes.AddCell("Delta Benza", b ? null : R(well.BenzamilDelta, 1), typeof(double));
                    tes.AddCell("Post Benzamil RT", b ? null : R(well.PostBenzamilRt, 1), typeof(double));
                    tes.AddCell("Delta Fsk", b ? null : R(well.ForkolinDelta, 1), typeof(double));
                    tes.AddCell("Delta Potentiator", b ? null : R(well.PotentiatorDelta, 1), typeof(double));
                    tes.AddCell("Total Fsk+Pot", b ? null : R(well.ForkolinDelta + well.PotentiatorDelta, 1), typeof(double));
                    tes.AddCell("Delta Bumetanide", b ? null : R(well.BumetanideDelta, 1), typeof(double));
                    tes.AddCell("AUC Fsk", b ? null : R(well.ForskolinAuc, 1), typeof(double));
                    tes.AddCell("AUC Potentiator", b ? null : R(well.PotentiatorAuc, 1), typeof(double));
                    tes.AddCell("AUC Fsk+Pot", b ? null : R(well.ForskolinPotentiatorAuc, 1), typeof(double));
                    tes.AddCell("Entire AUC", b ? null : R(well.EntireAuc, 1), typeof(double));
                    tes.AddCell("Curve Reference", b ? null : well.CurveReference, typeof(string));
                    tes.AddCell("Baseline Shift", R(well.BaseLineShift, 3), typeof(double));

                    tes.IncrementRow();
                }
            }

            return tes;
        }
        
        private string R(double value, int decimals)
        {
            return Math.Round(value, decimals).ToString();
        }
        private double? R(double? value, int decimals)
        {
            if (value == null) return null;

            return Math.Round((double)value, decimals);
        }
        
        public void CopyTesToClipboard(TableEntryStore tes, bool headings)
        {
            try
            {
                string buf = headings ? TextTableInstancer.CreateSpotfireTable(tes) : TextTableInstancer.CreateTabDelimitedTable(tes);

                Clipboard.Clear();
                Clipboard.SetText(buf);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "BREATHE Data Cruncher");
            }
        }
    }
}
