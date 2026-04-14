using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BreatheCalc.FourDispense.Data;
using CsvHelper;

namespace BreatheCalc.FourDispense.IO
{
    class BreatheDataReader
    {
        private readonly Dictionary<string, int> columnIndices = new Dictionary<string, int>();

        public Experiment LoadData(string filePath, int offsetPlate, int rCorrection)
        {
            var plates = new Dictionary<int, Plate>();

            var addressRegEx = new Regex(@"([A-Z]+)(\d+)");

            var ext = Path.GetExtension(filePath);
            var delimiter = ext.EndsWith("CSV", StringComparison.InvariantCultureIgnoreCase) ? "," : "\t";

            var justFileName = Path.GetFileName(filePath);
            var justFileNameNoExt = Path.GetFileNameWithoutExtension(filePath);
            var justFileFolder = Path.GetDirectoryName(filePath);


            var txt = AutomationEngineering.CommonTools.Files.FileHelper.LoadTextFromLockedFile(filePath);

            using (TextReader stream = new StringReader(txt))
            using (var reader = new CsvReader(stream))
            {
                reader.Configuration.Delimiter = delimiter;
                reader.Read();

                var headers = reader.FieldHeaders;

                RecordPosition(headers, "Platform");
                RecordPosition(headers, "TECC");
                RecordPosition(headers, "Time");
                RecordPosition(headers, "Rt");
                RecordPosition(headers, "PD-2");
                RecordPosition(headers, "Well Address");
                RecordPosition(headers, "Step Name");

                do
                {
                    var row = reader.CurrentRecord;

                    Plate currPlate;

                    int dummy;
                    if (row.Length == 0 || !int.TryParse(row[0], out dummy)) continue;

                    var platform = (int) GetValueDouble(row, "Platform");

                    if (!plates.TryGetValue(platform, out currPlate))
                    {
                        currPlate = new Plate { Platform = platform, IsOffset = (platform == offsetPlate), FileName = justFileName, FileFolder = justFileFolder};
                        plates[platform] = currPlate;
                    }

                    var stage = GetValueString(row, "Step Name");
                    var tecc = GetValueInt(row, "TECC");
                    var time = GetValueInt(row, "Time");
                    var rt = GetValueDouble(row, "Rt");
                    var pd2 = GetValueDouble(row, "PD-2");
                    var address = GetValueString(row, "Well Address");

                    var addressMatch = addressRegEx.Match(address).Groups;
                    var rowLetter = addressMatch[1].ToString();
                    var colNumber = addressMatch[2].ToString();

                    var well = currPlate.Wells.FirstOrDefault(w => w.Tecc == tecc);

                    if (well == null)
                    {
                        var curveRef = string.Format("{0}-{1}-{2}", justFileNameNoExt, currPlate.Platform, address);
                   
                        well = new Well
                               {
                                   CurveReference = curveRef,
                                   Tecc = tecc,
                                   WellAddress = address,
                                   Row = rowLetter,
                                   Col = colNumber
                               };

                        currPlate.Wells.Add(well);
                    }

                    var measurement = new Measurement
                                      {
                                          V = pd2,
                                          R = rt,
                                          RCorrected = rt - rCorrection,
                                          TimeMinutes = Math.Round(time/60.0, 3),
                                          Index = well.Measurements.Count + 1,
                                          Stage = stage
                                      };

                    well.Measurements.Add(measurement);
                } while (reader.Read());
            }

            return new Experiment {Plates = plates.Select(k => k.Value).ToList()};
        }

        void RecordPosition(IList<string> columns, string name)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                if (string.Equals(columns[i], name, StringComparison.InvariantCultureIgnoreCase))
                {
                    columnIndices[name] = i;
                    return;
                }
            }

            throw new IndexOutOfRangeException("Could not find colum for '" + name + "'");
        }

        private double GetValueDouble(IList<string> columns, string name)
        {
            var index = GetIndex(columns, name);

            double dummy;

            if (double.TryParse(columns[index], out dummy))
            {
                return dummy;
            }

            throw new InvalidCastException("Could not convert " + name + " column to a decimal for " + columns[index]);
        }

        private int GetValueInt(IList<string> columns, string name)
        {
            var index = GetIndex(columns, name);

            int dummy;

            if (int.TryParse(columns[index], out dummy))
            {
                return dummy;
            }

            throw new InvalidCastException("Could not convert " + name + " column to a int for " + columns[index]);
        }

        // ReSharper disable once UnusedParameter.Local
        private int GetIndex(IList<string> columns, string name)
        {
            var index = columnIndices[name];

            if (index > columns.Count - 1)
            {
                throw new IndexOutOfRangeException("Could not find index for " + name);
            }

            return index;
        }

        private string GetValueString(IList<string> columns, string name)
        {
            var index = GetIndex(columns, name);
            return columns[index];
        }
    }
}
