using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BreatheCalc.FourDispense.Data;

namespace BreatheCalc.FourDispense.IO
{
    class TeccDataLoaderLog
    {
        private Dictionary<int, string> rowWiseMapper;
        private Dictionary<int, string> colWiseMapper;
        
        public Experiment LoadData(string filePath, int rCorrection,  bool rowwise, List<StageCount> stageCounts)
        {
            var addressRegEx = new Regex(@"([A-Z]+)(\d+)");
            const char delimiter = ',';

            var justFileName = Path.GetFileName(filePath);
            var justFileNameNoExt = Path.GetFileNameWithoutExtension(filePath);
            var justFileFolder = Path.GetDirectoryName(filePath);

            var lines = File.ReadAllLines(filePath);

            var offSetPlate = new Plate { Platform = 0, IsOffset = true, FileName = justFileName, FileFolder = justFileFolder };

            var stageMap = new Dictionary<int, string>();

            if (stageCounts != null)
            {

                for (int i = 0; i < 1000; i++)
                {
                    stageMap[i] = "MeasurementCourse";
                }

                var currentIndex = 0;
                foreach (var stageCount in stageCounts)
                {
                    if (stageCount.Count == 0) continue;

                    stageMap[currentIndex] = stageCount.Stage;
                    currentIndex += stageCount.Count;
                }
            }
            
            //read offsets
            for (int l = 0; l < lines.Length; l++)
            {
                if (!lines[l].StartsWith("Mean values of Offset", StringComparison.InvariantCultureIgnoreCase)) continue;
                
                for (int i = 0; i < 24; i++)
                {
                    var realLine = l + i + 2;

                    var columns = lines[realLine].Split(delimiter);

                    var tecc = int.Parse(columns[0].Split('-')[0]);
                    var address = (rowwise) ? rowWiseMapper[tecc] : colWiseMapper[tecc];
                    var pd2 = double.Parse(columns[3]);

                    var time = int.Parse(lines[l - 3].Split(delimiter)[1]);
                    
                    var addressMatch = addressRegEx.Match(address).Groups;
                    var rowLetter = addressMatch[1].ToString();
                    var colNumber = addressMatch[2].ToString();

                    var well = offSetPlate.Wells.FirstOrDefault(w => w.WellAddress == address);

                    if (well == null)
                    {
                        var curveRef = string.Format("{0}-{1}-{2}", justFileNameNoExt, offSetPlate.Platform, address);

                        well = new Well
                        {
                            CurveReference = curveRef,
                            Tecc = tecc,
                            WellAddress = address,
                            Row = rowLetter,
                            Col = colNumber
                        };

                        offSetPlate.Wells.Add(well);
                    }

                    var measurement = new Measurement
                    {
                        V = -pd2,
                        TimeMinutes = Math.Round(time / 60.0, 3),
                        Index = well.Measurements.Count + 1
                    };

                    well.Measurements.Add(measurement);
                }
            }

            var cellPlate = new Plate { Platform = 1, IsOffset = false, FileName = justFileName, FileFolder = justFileFolder };

            //read cell plates
            for (int l = 0; l < lines.Length; l++)
            {
                if (!lines[l].StartsWith("Mean values of PDRT ", StringComparison.InvariantCultureIgnoreCase)) continue;

                for (int i = 0; i < 24; i++)
                {
                    var realLine = l + i + 2;

                    var columns = lines[realLine].Split(delimiter);

                    var tecc = int.Parse(columns[0].Split('-')[0]);
                    var address = (rowwise) ? rowWiseMapper[tecc] : colWiseMapper[tecc];

                    var time = int.Parse(lines[l - 3].Split(delimiter)[1]);
                    
                    var r = double.Parse(columns[2]);
                    var pd2 = double.Parse(columns[1]);

                    var addressMatch = addressRegEx.Match(address).Groups;
                    var rowLetter = addressMatch[1].ToString();
                    var colNumber = addressMatch[2].ToString();

                    var well = cellPlate.Wells.FirstOrDefault(w => w.WellAddress == address);

                    if (well == null)
                    {
                        var curveRef = string.Format("{0}-{1}-{2}", justFileNameNoExt, offSetPlate.Platform, address);

                        well = new Well
                        {
                            CurveReference = curveRef,
                            Tecc = tecc,
                            WellAddress = address,
                            Row = rowLetter,
                            Col = colNumber
                        };

                        cellPlate.Wells.Add(well);
                    }

                    string stageName;

                    if (!stageMap.TryGetValue(well.Measurements.Count, out stageName))
                    {
                        stageName = string.Empty;
                    }

                    var measurement = new Measurement
                    {
                        V = -pd2,
                        TimeMinutes = Math.Round(time / 60.0, 3),
                        Index = well.Measurements.Count + 1,
                        Stage = stageName,
                        R = r,
                        RCorrected = r - rCorrection,
                    };

                    well.Measurements.Add(measurement);
                }
            }
            
            var plates = new List<Plate> {offSetPlate, cellPlate};

            foreach (var plate in plates)
            {
                plate.Wells = plate.Wells.OrderBy(w => w.Col).ThenBy(w => w.Row).ToList();

                for (int i = 0; i < plate.Wells.Count; i++)
                {
                    plate.Wells[i].Tecc = i + 1;
                }
            }

            return new Experiment {Plates = plates};
        }
        public TeccDataLoaderLog()
        {
            rowWiseMapper = new Dictionary<int, string>();

            int i = 1;

            for (int c = 0; c < 6; c++)
            {
                for (int r = 0; r < 4; r++)
                {
                    var row = (char) (65 + r);
                    var col = c + 1;

                    rowWiseMapper[i] = string.Format("{0}{1}", row,col);

                    i++;
                }
            }

            colWiseMapper = new Dictionary<int, string>();

            i = 1;

            for (int r = 0; r < 4; r++)
            {
                    for (int c = 0; c < 6; c++)
                    {
                    var row = (char)(65 + r);
                    var col = c + 1;

                    colWiseMapper[i] = string.Format("{0}{1}", row, col);

                    i++;
                }
            }
        }

    }
}
