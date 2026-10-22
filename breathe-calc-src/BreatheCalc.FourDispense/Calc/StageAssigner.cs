using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutomationEngineering.CommonTools.ExtensionMethods;
using BreatheCalc.FourDispense.Data;

namespace BreatheCalc.FourDispense.Calc
{
    class StageAssigner
    {
        public string AssignStages(Experiment experiment)
        {
            var plates = experiment.AssayPlates;

            string returnedMessage = string.Empty;

            var wells = plates.SelectMany(p => p.Wells).ToList();

            var stageDict = new Dictionary<int, string>();

            var exampleMeasurements = wells.First().Measurements;

            string currentStage = Stages.StageBaseline;

            var forsks = new[] { "Stimulator", Stages.StageForskolin, "ForskolinCorrector", "Forksolin" };

            //if there is no forskolin but there is a Potentiator, then it is a combined 3 dispense protocol
            var doPotSubstitution = exampleMeasurements.Exists(me => me.Stage == "Potentiator") &&
                                     !exampleMeasurements.Exists(me => me.Stage.IsAnyOf(forsks));

            experiment.Protocol = doPotSubstitution ? Protocols.ThreeDispenseCombinedForkPot : Protocols.FourDispenseSepForkPot;

            Func<string, string, bool> comparer = (s1, s2) => string.Equals(s1.Trim(),s2.Trim(), StringComparison.InvariantCultureIgnoreCase);

            foreach (var m in exampleMeasurements)
            {
                if (!m.Stage.IsAnyOf("Manual Measurement", "MeasurementCourse"))
                {
                    if (m.Stage.IsAnyOf(comparer, Stages.StageBenzamil,"Amiloride"))
                    {
                        currentStage = Stages.StageBenzamil;
                    }
                    else if (m.Stage.IsAnyOf(comparer, forsks))
                    {
                        currentStage = Stages.StageForskolin;
                    }
                    else if (m.Stage.IsAnyOf(comparer, "Potentiator") && doPotSubstitution)
                    {
                        currentStage = Stages.StageForskolin;
                    }
                    else if (m.Stage.IsAnyOf(comparer, "Potentiator"))
                    {
                        currentStage = Stages.StagePotentiator;
                    }
                    else if (m.Stage.IsAnyOf(comparer, "Bumetanide", "Inhibitor"))
                    {
                        currentStage = Stages.StageBumetanide;
                    }
                    else
                    {
                        throw new Exception("Could not find stage for " + m.Stage);
                    }
                }

                stageDict[m.Index] = currentStage;
            }

            var measurements = wells.SelectMany(w => w.Measurements);
            measurements.ActOnEach(m=>m.Stage = stageDict[m.Index]);

            foreach (var well in wells)
            {
                string cur = "Baseline";
                
                for (int i = 0; i < well.Measurements.Count;i++)
                {
                    if (well.Measurements[i].Stage != cur)
                    {
                        cur = well.Measurements[i].Stage;
                        well.Measurements[i].IsInitial = true;
                    }
                }
            }
            
            return returnedMessage;
        }
    }
}
