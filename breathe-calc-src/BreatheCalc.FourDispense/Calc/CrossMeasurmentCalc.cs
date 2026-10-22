using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutomationEngineering.CommonTools.ExtensionMethods;
using BreatheCalc.FourDispense.Data;

namespace BreatheCalc.FourDispense.Calc
{
    class CrossMeasurmentCalc
    {
        public void CalcDeltas(Experiment experiment)
        {
            var plates = experiment.AssayPlates;

            var wells = plates.SelectMany(p => p.Wells);

            foreach (var well in wells)
            {
                var firstForskolin = well.Measurements.Where(m => m.Stage == Stages.StageForskolin).Take(4);
                var lastForskolin = well.Measurements.Last(m => m.Stage == Stages.StageForskolin);
                var lastBaseline = well.Measurements.Last(m => m.Stage == Stages.StageBaseline);
                var lastBenzamil = well.Measurements.LastOrDefault(m => m.Stage == Stages.StageBenzamil);

                //this is in case of there not being a benzamil
                if (lastBenzamil == null)
                {
                    lastBenzamil = well.Measurements.Single(m => m.Index == firstForskolin.First().Index - 1);
                }

                var beforeForskolin = well.Measurements.Single(m => m.Index == firstForskolin.First().Index - 1);
                var maxForskolinMeasurement = firstForskolin.Max(m => m.ValueForAuc);
                var maxForskolin = maxForskolinMeasurement - beforeForskolin.ValueForAuc;
                
                var pots = well.Measurements.Where(m => m.Stage == Stages.StagePotentiator).ToList();

                var lastPotentiator = pots.LastOrDefault();

                if (lastPotentiator != null)
                {
                    var maxPotentiator = pots.Take(5).Max(m => m.ValueForAuc) - lastForskolin.ValueForAuc;
                    well.PotentiatorDelta = maxPotentiator;
                }
                else
                {
                    well.PotentiatorDelta = 0;
                    lastPotentiator = well.Measurements.Last(m => m.Stage == Stages.StageForskolin);
                }
                
                var lastBumetinide = well.Measurements.LastOrDefault(m => m.Stage == Stages.StageBumetanide);
                
                well.BaselineRt = lastBaseline.RCorrected;
                well.Baseline = lastBenzamil.ValueForAuc;
                well.PostBenzamilRt = lastBenzamil.RCorrected;
                well.BenzamilDelta = lastBaseline.ValueForAuc - lastBenzamil.ValueForAuc;
                well.ForkolinDelta = maxForskolin;
                                
                well.BumetanideDelta = lastPotentiator.ValueBaselineCorrected - lastBumetinide?.ValueBaselineCorrected??0;
            }
        }

        public void CalcBaseLine(Experiment experiment, int bumatinidePts, bool correctOnMin)
        {
            var plates = experiment.AssayPlates;

            var wells = plates.SelectMany(p => p.Wells);

            foreach (var well in wells)
            {
                var lastBenzamil = well.Measurements.LastOrDefault(m => m.Stage == Stages.StageBenzamil);

                if (correctOnMin)
                {
                    var forBl = well.Measurements.Where(m => m.Stage == Stages.StageBumetanide).ToList();
                    
                    var min = (forBl.Any()) ? forBl.Min(m => m.ValueForAuc) : 0;

                    well.BaseLineShift = lastBenzamil?.ValueForAuc - min;

                    foreach (var measurement in well.Measurements)
                    {
                        measurement.ValueBaselineCorrected = measurement.ValueForAuc - min;
                        measurement.CBaseline = min;
                    }
                }
                else
                {
                    //this is in case of there not being a benzamil
                    if (lastBenzamil == null)
                    {
                        var firstForskolin = well.Measurements.First(m => m.Stage == Stages.StageForskolin);
                        lastBenzamil = well.Measurements.Single(m => m.Index == firstForskolin.Index - 1);
                    }

                    var lastBumatinidePts = well.Measurements.Where(m => m.Stage == Stages.StageBumetanide).ToList();
                    lastBumatinidePts.Reverse();
                    lastBumatinidePts = lastBumatinidePts.Take(bumatinidePts).ToList();

                    well.BaseLineShift = lastBenzamil?.ValueForAuc - lastBumatinidePts.Median(p=>p.ValueForAuc);

                    var xySet = new XYDataSet { new PointD(lastBenzamil.TimeMinutes, lastBenzamil.ValueForAuc) };

                    foreach (var lastBumatinidePt in lastBumatinidePts)
                    {
                        xySet.Add(new PointD(lastBumatinidePt.TimeMinutes, lastBumatinidePt.ValueForAuc));
                    }

                    var slope = xySet.Slope;
                    var b = xySet.YIntercept;

                    foreach (var measurement in well.Measurements)
                    {
                        if (measurement.Index < lastBenzamil.Index) continue;

                        var interp = measurement.TimeMinutes * slope + b;

                        measurement.CBaseline = Math.Round(interp, 3);
                        measurement.SlopeBaseline = slope;

                        measurement.ValueBaselineCorrected = measurement.ValueForAuc - measurement.CBaseline;
                    }
                }
            }
        }

        public void CalcForskolinComponents(Experiment experiment, int forskolinPtsForExtrapolation)
        {
            var plates = experiment.AssayPlates;

            var wells = plates.SelectMany(p => p.Wells);

            foreach (var well in wells)
            {
                var allForsk = well.Measurements.Where(m => m.Stage == Stages.StageForskolin).ToList();

                var toTake = Math.Min(allForsk.Count -1,forskolinPtsForExtrapolation);

                allForsk.Reverse();

                var forskPtsRegression = allForsk
                    .Take(toTake)
                    .Reverse()
                    .ToList();

                var xySet = new XYDataSet();

                foreach (var pt in forskPtsRegression)
                {
                    if (pt.ValueBaselineCorrected == null)
                    {
                        throw new InvalidOperationException("IBaselineCorrected is null for " + well.WellAddress + " " + pt.TimeMinutes);    
                    }

                    xySet.Add(new PointD(pt.TimeMinutes, (double)pt.ValueBaselineCorrected));
                }

                var slope = xySet.Slope;
                var b = xySet.YIntercept;

                well.ForskolinSlope = b;
                
                foreach (var measurement in well.Measurements.Where(m=>m.Stage == Stages.StagePotentiator))
                {
                    var interp = measurement.TimeMinutes * slope + b;

                    measurement.CPotentiator = Math.Max(0,Math.Round(interp, 3));

                    if (measurement.ValueBaselineCorrected == null || measurement.CPotentiator == null)
                    {
                        throw new InvalidOperationException("IBaselineCorrected or CForskolin is null?");
                    }

                    measurement.ValuePotentiator = measurement.ValueBaselineCorrected - measurement.CPotentiator;
                }

                var forskPtsForCurrent = well.Measurements.Where(m => m.Stage.IsAnyOf(Stages.StageForskolin)).ToList();

                foreach (var measurement in forskPtsForCurrent)
                {
                    measurement.ValueForsklin = measurement.ValueBaselineCorrected;
                }
            }
        }

        public void CalcForskolinAuc(Experiment experiment, bool useRfumsAuc = true)
        {
            var plates = experiment.AssayPlates;

            var wells = plates.SelectMany(p => p.Wells);

            foreach (var well in wells)
            {
                var start = well.Measurements.FindIndex(m => m.Stage == Stages.StageForskolin);
                var stop = well.Measurements.FindLastIndex(m => m.Stage == Stages.StageForskolin);

                if (start == -1 || stop == -1)
                {
                    return;
                }

                double auc;

                if (useRfumsAuc)
                {
                    auc = CalcRfumsAuc(start, stop, well.Measurements, m => m.ValueForsklin ?? m.ValueBaselineCorrected ?? 0);
                }
                else
                {
                    var pts = new List<PointD>();

                    for (int i = start; i <= stop; i++)
                    {
                        var meas = well.Measurements[i];

                        pts.Add(new PointD(meas.TimeMinutes, meas.ValueForsklin ?? 0));
                    }

                    auc = CalcAuc(pts);
                }

                well.ForskolinAuc = auc;
            }
        }

        public void CalcPotentiatorAuc(Experiment experiment)
        {
            var plates = experiment.AssayPlates;

            var wells = plates.SelectMany(p => p.Wells);

            foreach (var well in wells)
            {
                var start = well.Measurements.FindIndex(m => m.Stage == Stages.StagePotentiator);
                var stop = well.Measurements.FindLastIndex(m => m.Stage == Stages.StagePotentiator);
                var hasForskolin = well.Measurements.Any(m => m.Stage == Stages.StageForskolin);

                if (start == -1 || stop == -1 || !hasForskolin)
                {
                    return;
                }

                var pts = new List<PointD>();

                for (int i = start; i <= stop; i++)
                {
                    var meas = well.Measurements[i];
                    pts.Add(new PointD(meas.TimeMinutes, meas.ValuePotentiator ?? 0));
                }

                var auc = CalcAuc(pts);
                well.PotentiatorAuc = auc;
            }
        }
        public void CalcForskolinPotentiatorAuc(Experiment experiment)
        {
            var plates = experiment.AssayPlates;

            var wells = plates.SelectMany(p => p.Wells);

            foreach (var well in wells)
            {
                well.ForskolinPotentiatorAuc = well.ForskolinAuc + well.PotentiatorAuc;
            }
        }

        public void CalcEntireAuc(Experiment experiment)
        {
            var plates = experiment.AssayPlates;

            var wells = plates.SelectMany(p => p.Wells);

            foreach (var well in wells)
            {
                var start = well.Measurements.FindIndex(m => m.Stage == Stages.StageForskolin);
                var stop = well.Measurements.FindLastIndex(m => m.Stage == Stages.StagePotentiator);

                if (start == -1 || stop == -1)
                {
                    return;
                }

                var pts = new List<PointD>();

                for (int i = start; i <= stop; i++)
                {
                    var meas = well.Measurements[i];

                    pts.Add(new PointD(meas.TimeMinutes, meas.ValueBaselineCorrected ?? 0));
                }

                var auc = CalcAuc(pts);
                well.EntireAuc = auc;
            }
        }

        private double CalcAuc(IList<PointD> pts, bool killNegatives = false)
        {
            double sum = 0;
            
            for (int i = 1; i < pts.Count; i++)
            {
                var average = (pts[i].Y + pts[i - 1].Y)/2.0;
                average = (killNegatives) ? Math.Max(0, average) : average;
                var deltaTime = (pts[i].X - pts[i - 1].X);

                sum += (average*deltaTime);
            }

            return sum;
        }

        private double CalcRfumsAuc(int start, int stop, IList<Measurement> measurements, Func<Measurement, double> selector)
        {
            var pre = measurements[start - 1];
            var post = measurements[stop + 1];

            var time = post.TimeMinutes - pre.TimeMinutes;

            double sides = selector(pre) + selector(measurements[stop]);
            double sum = 0;

            for (int i = start; i < stop; i++)
            {
                sum += 2 * selector(measurements[i]);
            }

            sum += sides;

            var avg = sum / (2 * (stop - start + 2));

            var auc = avg * time;

            return auc;
        }
    }
}
