using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using AutomationEngineering.CommonTools.Files;

namespace Cf.TeccViewer
{
    class TeccFileLoader
    {
        public List<Measurement> LoadMeasurements(string fileName)
        {
            var text = FileHelper.LoadTextFromLockedFile(fileName);
            var lines = text.Split('\n');

            var measurements = new List<Measurement>(lines.Count() - 4);

            for (int i = 4; i < lines.Length; i++)
            {
                var line = lines[i];
                var cols = line.Split(new [] { ',' ,'\t'});

                float dummy;

                if (cols.Length < 7 || !float.TryParse(cols[0], out dummy)) continue;

                var r = float.Parse(cols[4]);
                var g = r > 0 ? (float)1000.0/r : 0;

                var measurement = new Measurement
                                  {
                                      Position = int.Parse(cols[1]),
                                      Number = int.Parse(cols[0]),
                                      Tecc = int.Parse(cols[2]),
                                      R = r,
                                      G=g,
                                      Pd1 = float.Parse(cols[5]),
                                      Pd2 = float.Parse(cols[6]),
                                      I = float.Parse(cols[7]),
                                      FileName = fileName
                                  };

                measurements.Add(measurement);
            }

            return measurements;
        }
    }
}
