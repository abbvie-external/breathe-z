using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using AutomationEngineering.CommonTools.ExtensionMethods;
using AutomationEngineering.CommonTools.Tables;
using AutomationEngineering.LoggingHelper;
using BreatheZ.Ui.Annotations;
using BreatheZ.Ui.Properties;
using Cf.Tecc.Controller;
using log4net;

namespace BreatheZ.Ui.Tecc
{
    public class TeccModel : INotifyPropertyChanged
    {
        private string dataPath;
        private string currentFileSanExtension;
        private int currentIndex;
        private DateTime startTime;
        private readonly Stopwatch timer = new Stopwatch();
        private readonly TeccController tecc = new TeccController();
        private Dictionary<int, Stopwatch> channelTimers;
        private Stopwatch lastMeasurement;
        private static readonly ILog log = LogCreator.ClassLogger();
        private double? lastMeasurmentInterval;
        private IList<MeasurementInfo> measurements;
        private Stopwatch runStart;
        private readonly object fileLock = new object();
        private bool teccSettingsDirty;
        private string transientComment;
        private string teccSettingsString;
        private List<object[]> altLines;
        private MeasurementInfo selectedMeasurement;
        private readonly Dictionary<int, int> runCount = new Dictionary<int, int>();
        private bool filterMeasurements;
        private bool takeResistanceOnOffsetPlate;

        private Dictionary<int, double> offsetByWell = new Dictionary<int, double>();

        public string TeccSettingsString
        {
            get { return teccSettingsString; }
            set
            {
                teccSettingsString = value;
                OnPropertyChanged();
            }
        }

        public string TransientComment
        {
            get { return transientComment; }
            set
            {
                transientComment = value;
                OnPropertyChanged();
            }
        }

        public bool TeccSettingsDirty
        {
            get { return teccSettingsDirty; }
            set
            {
                teccSettingsDirty = value;
                OnPropertyChanged();
            }
        }

        public bool IsInitialized { get; set; }
        public string DataPath
        {
            get { return dataPath; }
            set
            {
                if (value == dataPath) return;
                dataPath = value;
                OnPropertyChanged();
            }
        }

        public string CurrentFile
        {
            get { return string.IsNullOrWhiteSpace(CurrentFileSanExtension) ? null : CurrentFileSanExtension + ".dat"; }
        }
        public string CurrentFileAlt
        {
            get { return string.IsNullOrWhiteSpace(CurrentFileSanExtension) ? null : CurrentFileSanExtension + ".csv"; }
        }

        public string CurrentFileFullPath
        {
            get { return string.IsNullOrWhiteSpace(CurrentFileSanExtension) ? null : Path.Combine(DataPath, CurrentFile); }
        }

        public string CurrentFileFullPathAlt
        {
            get { return string.IsNullOrWhiteSpace(CurrentFileSanExtension) ? null : Path.Combine(DataPath, CurrentFileAlt); }
        }

        public IList<MeasurementInfo> Measurements
        {
            get { return measurements; }
            private set
            {
                if (Equals(value, measurements)) return;
                measurements = value;
                OnPropertyChanged();
            }
        }

        public MeasurementInfo SelectedMeasurement
        {
            get { return selectedMeasurement; }
            set
            {
                selectedMeasurement = value;
                OnPropertyChanged();
                OnPropertyChanged("IsMeasurementSelected");
                OnPropertyChanged("FilteredMeasurements");
            }
        }

        public IList<ChannelMeasurement> FilteredMeasurements
        {
            get
            {
                var sel = SelectedMeasurement;
                if (sel == null) return null;

                var mes = (FilterMeasurements)
                    ? sel.Measurements.Where(c => c.HasIssues || (c.RtRaw <= 5 && !c.VoltageOnly))
                    : sel.Measurements;

                return mes.OrderBy(m => m.TeccChannel).ToList();
            }
        }

        public bool FilterMeasurements
        {
            get { return filterMeasurements; }
            set
            {
                filterMeasurements = value;
                OnPropertyChanged();
                OnPropertyChanged("FilteredMeasurements");
            }
        }

        public bool IsMeasurementSelected
        {
            get { return SelectedMeasurement != null; }
        }

        public double? LastMeasurementInterval
        {
            get { return lastMeasurmentInterval; }
            set
            {
                lastMeasurmentInterval = value;
                OnPropertyChanged();
            }
        }

        public List<string> PlateFormats
        {
            get { return new List<string> { "96-Well", "24-Well" }; }
        }

        public DateTime StartTime
        {
            get { return startTime; }
            set
            {
                if (value.Equals(startTime)) return;
                startTime = value;
                OnPropertyChanged();
            }
        }

        public string CurrentFileSanExtension
        {
            get { return currentFileSanExtension; }
            set
            {
                if (value == currentFileSanExtension) return;
                currentFileSanExtension = value;
                OnPropertyChanged();
                OnPropertyChanged("CurrentFile");
                OnPropertyChanged("CurrentFileFullPath");
                OnPropertyChanged("CurrentFileFullPathAlt");
                OnPropertyChanged("CurrentFileAlt");
            }
        }

        public int CurrentIndex
        {
            get { return currentIndex; }
            set
            {
                if (value == currentIndex) return;
                currentIndex = value;
                OnPropertyChanged();
            }
        }

        public bool TakeResistanceOnOffsetPlate
        {
            get { return takeResistanceOnOffsetPlate; }
            set
            {
                takeResistanceOnOffsetPlate = value;
                OnPropertyChanged();
            }
        }

        public List<int> NumberOfRealPlatesOptions
        {
            get { return new List<int> { 1, 2, 3, 4, 5, 6 }; }
        }

        public byte[] MonitorChannels
        {
            get { return new byte[] { 1, 2, 3, 4 }; }
        }

        public void RenameFile(string fileName)
        {
            lock (fileLock)
            {
                var oldNameFull = CurrentFileFullPath;
                var oldNameFullAlt = CurrentFileFullPathAlt;

                var oldName = CurrentFileSanExtension;

                try
                {
                    CurrentFileSanExtension = fileName;

                    File.Move(oldNameFull, CurrentFileFullPath);
                    File.Move(oldNameFullAlt, CurrentFileFullPathAlt);

                }
                catch (Exception)
                {
                    CurrentFileSanExtension = oldName;
                    throw;
                }
            }
        }

        public void StartNewFile()
        {
            lock (fileLock)
            {
                for (int i = 0; i < 1000; i++)
                {
                    runCount[i] = 1;
                }

                altLines = new List<object[]>();

                Measurements = new List<MeasurementInfo>();

                StartTime = DateTime.Now;

                LastMeasurementInterval = null;
                offsetByWell = new Dictionary<int, double>();

                lastMeasurement = new Stopwatch();
                runStart = Stopwatch.StartNew();

                if (channelTimers != null)
                {
                    foreach (var channelTimer in channelTimers)
                    {
                        channelTimer.Value.Stop();
                    }
                }

                channelTimers = new Dictionary<int, Stopwatch>();

                for (int i = 0; i < 20; i++)
                {
                    channelTimers.Add(i, new Stopwatch());
                }

                var sansExtension = StartTime.ToString("yyyy-MM-dd HH_mm_ss");
                
                CurrentFileSanExtension = sansExtension;

                log.InfoFormat("Starting New TECC File '{0}'", CurrentFileFullPath);

                CurrentIndex = 1;
                timer.Restart();

                StampHeader();
            }
        }

        public void ConfigureTeccFull()
        {
            var settings = Settings.Default;

            tecc.DebugMode = settings.TeccDebug;

            if (!IsInitialized)
            {
                var hardware = tecc.Initialize(60000, settings.ExpectedBoardCount, settings.PlateFormat);

                if (hardware.DeviceCount != tecc.ExpectedBoardCount)
                {
                    log.Warn("Count of devices does not match: " + tecc.ExpectedBoardCount);

                    if (Settings.Default.ThrowOnInvalidDeviceCount)
                    {
                        throw new Exception("Number of devices incorrect: " + hardware.DeviceCount);
                    }
                }

                IsInitialized = true;
            }

            ConfigTeccLight();
        }

        public void ConfigTeccLight()
        {
            var settings = Settings.Default;

            tecc.Configure(
                TeccFilterState.MtuLow,
                TeccFilterState.MtuLow,
                settings.OscThreshold,
                settings.InitialRAP,
                settings.Area,
                settings.RtxSettings.Settings,
                (byte)(settings.MonitorChannel - 1)
                );

            TeccSettingsDirty = false;
        }

        private void StampHeader()
        {
            lock (fileLock)
            {
                FileCheck();

                log.Info("Configuring file header");

                using (var file = File.CreateText(CurrentFileFullPath))
                {
                    file.WriteLine("Filetype TEC-RTVT");

                    var infoLine = string.Format("{0:MM-dd-yyyy} ,  {0:HH:mm:ss} ,  {1}", StartTime, CurrentFileSanExtension);
                    file.WriteLine(infoLine);

                    file.WriteLine("    Number  Platform      TECC	    Time         Rt	      PD-1        PD-2        Ieq");
                    file.WriteLine(string.Empty);
                }

                StampHeaderAlt();
            }
        }

        private void StampHeaderAlt()
        {
            using (var file = File.CreateText(CurrentFileFullPathAlt))
            {
                var headersFull = new List<Tuple<string, string>> 
                                  {
                                      new Tuple<string, string>("Reading Index","INTEGER"),
                                      new Tuple<string, string>("Platform","INTEGER"),
                                      new Tuple<string, string>("Platform Reading Index","INTEGER"),
                                      new Tuple<string, string>("TECC","INTEGER"),
                                      new Tuple<string, string>("Time","INTEGER"),
                                      new Tuple<string, string>("Rt","INTEGER"),
                                      new Tuple<string, string>("PD-1","REAL"),
                                      new Tuple<string, string>("PD-2","REAL"),
                                      new Tuple<string, string>("Ieq","REAL"),
                                      new Tuple<string, string>("Step Name","STRING"),
                                      new Tuple<string, string>("Time Since Last Read Same Plate","REAL"),
                                      new Tuple<string, string>("Time Since Last Read","REAL"),
                                      new Tuple<string, string>("Well Address","STRING"),
                                      new Tuple<string, string>("Column","INTEGER"),
                                      new Tuple<string, string>("Row","STRING"),
                                      new Tuple<string, string>("TECC Board","INTEGER"),
                                      new Tuple<string, string>("TECC Index","INTEGER"),
                                      new Tuple<string, string>("R (Raw)","INTEGER"),
                                      new Tuple<string, string>("Osc","STRING"),
                                      new Tuple<string, string>("V Overflow on VT1","STRING"),
                                      new Tuple<string, string>("V Overflow on VT2","STRING"),
                                      new Tuple<string, string>("I Overflow on RT","STRING"),
                                      new Tuple<string, string>("V Overflow on RT","STRING"),
                                      new Tuple<string, string>("Current RTX Rt","INTEGER"),
                                      new Tuple<string, string>("Next RTX Rt","INTEGER"),
                                      new Tuple<string, string>("Current RTX Index","INTEGER"),
                                      new Tuple<string, string>("Next RTX Index","INTEGER"),
                                      new Tuple<string, string>("RTX Index Change","BOOLEAN"),
                                      new Tuple<string, string>("Overflow","BOOLEAN"),
                                      new Tuple<string, string>("Has Issues","BOOLEAN"),
                                      new Tuple<string, string>("File Stamp","DATETIME"),
                                      new Tuple<string, string>("Timestamp","DATETIME"),
                                      new Tuple<string, string>("Transient Comment","STRING"),
                                      new Tuple<string, string>("TECC Settings","STRING"),
                                  };

                var headers1 = headersFull.Select(h => CsvEscaper.Escape(h.Item1)).ToArray();
                var headers2 = headersFull.Select(h => CsvEscaper.Escape(h.Item2)).ToArray();

                file.WriteLine(headers1.ToDelimitedList(","));
                file.WriteLine(headers2.ToDelimitedList(","));
            }
        }

        public void ResetComment(MeasurementInfo measurement, string comment)
        {
            measurement.TransientComment = comment;

            foreach (var channelMeasurement in measurement.Measurements)
            {
                altLines[channelMeasurement.OutputLine][32] = comment;
            }

            RewriteAltFile();
        }

        public void RewriteAltFile()
        {
            lock (fileLock)
            {
                if (!File.Exists(CurrentFileFullPathAlt))
                {
                    return;
                }

                StampHeaderAlt();

                using (var appenderNew = File.AppendText(CurrentFileFullPathAlt))
                {
                    foreach (var line in altLines)
                    {
                        var outLine = string.Join("\t", line);
                        appenderNew.WriteLine(outLine);
                    }
                }
            }
        }

        public MeasurementInfo WriteResults(int platform, string stepName, int? debugTimeS, bool voltageOnly)
        {
            Exception lastException = null;

            for (int i = 0; i < 3; i++)
            {
                if (i > 0)
                {
                    IsInitialized = false;
                    ConfigureTeccFull();
                }

                try
                {
                    if (TeccSettingsDirty)
                    {
                        log.Info("TECC Settings Dirty, doing Re-Configuration in Read");
                        ConfigTeccLight();
                        Task.Delay(200).Wait();
                    }

                    var validRtxSettings = tecc.ValidRtxSettings;

                    log.InfoFormat("Write Results for platform {0}, step {1}", platform, stepName);

                    var measurementStartTime = DateTime.Now;

                    double? elapsedOnChannel = null;

                    var channelTimer = channelTimers[platform];

                    if (channelTimer.IsRunning)
                    {
                        elapsedOnChannel = Math.Round(channelTimer.Elapsed.TotalSeconds, 1);
                    }

                    LastMeasurementInterval = (lastMeasurement.IsRunning) ? Math.Round(lastMeasurement.Elapsed.TotalSeconds, 1) : (double?)null;
                    lastMeasurement.Restart();

                    log.InfoFormat("Taking results for platform {0}, step {1}, elapsed since last {2}", platform, stepName, elapsedOnChannel);

                    channelTimer.Restart();
                    var readTimer = Stopwatch.StartNew();

                    List<ChannelValue> values;

                    if (voltageOnly)
                    {
                        values = tecc.VtRecordAndRead(60000, debugTimeS, Settings.Default.ShortElectrodes, runCount[platform]);
                    }
                    else
                    {
                        values = tecc.RtRecordAndRead(60000, debugTimeS, Settings.Default.ShortElectrodes, runCount[platform]);
                    }

                    var runElapsed = Math.Round(runStart.Elapsed.TotalSeconds);

                    if (values == null)
                    {
                        continue;
                    }

                    var measurementInfo = new MeasurementInfo
                    {
                        Position = platform,
                        MeasurementStep = stepName,
                        MeasurementStart = measurementStartTime,
                        ElapsedSinceLastTeccMeasurementS = LastMeasurementInterval,
                        ElapsedSinceLastTeccMeasurementSamePlateS = elapsedOnChannel,
                        MeasurementIndex = Measurements.Count + 1,
                        TeccMeasurementTimeS = Math.Round(readTimer.Elapsed.TotalSeconds, 1),
                        TransientComment = TransientComment ?? string.Empty
                    };

                    var channelsWithProblems = new List<ChannelValue>();
                    var rtxTrans = new HashSet<string>();

                    foreach (var channelValue in values)
                    {
                        if (channelValue.StatusOscillation ||
                            channelValue.StatusVoltageOverflowOnV1 ||
                            channelValue.StatusVoltageOverflowOnV2 ||
                            channelValue.StatusCurrentOverflowOnRt ||
                            channelValue.StatusVoltageOverflowOnRt)
                        {
                            channelsWithProblems.Add(channelValue);
                        }

                        if (channelValue.CurrentRtxIndex != channelValue.NextRtxIndex)
                        {
                            var rtx = validRtxSettings.FirstOrDefault(rt => rt.RtxIndex == channelValue.CurrentRtxIndex);
                            var rtxR = (rtx == null) ? (float?)null : rtx.ThresholdResistance;

                            var rtxNext = validRtxSettings.FirstOrDefault(rt => rt.RtxIndex == channelValue.NextRtxIndex);
                            var rtxRNext = (rtxNext == null) ? (float?)null : rtxNext.ThresholdResistance;

                            rtxTrans.Add(string.Format("{0}->{1}", rtxR, rtxRNext));
                        }
                    }

                    channelsWithProblems = channelsWithProblems.OrderBy(c => c.Channel).ToList();

                    if (channelsWithProblems.Count <= 8)
                    {
                        measurementInfo.ErrorText = channelsWithProblems.ToDelimitedList(c => c.Channel, ", ");
                    }
                    else
                    {
                        measurementInfo.ErrorText = channelsWithProblems.Take(8).ToDelimitedList(c => c.Channel, ", ") + "...";
                    }

                    if (rtxTrans.Any())
                    {
                        measurementInfo.RtxInfo = rtxTrans.OrderBy(r => r).ToDelimitedList(", ");
                    }

                    var rValues = new List<double>();
                    var rValuesRaw = new List<double>();
                    var pdValues = new List<double>();
                    var iEqValues = new List<double>();

                    lock (fileLock)
                    {
                        if (!File.Exists(CurrentFileFullPathAlt))
                        {
                            throw new FileNotFoundException("Could not find data file to write in results: " + CurrentFileFullPath);
                        }

                        using (var appenderOld = File.AppendText(CurrentFileFullPath))
                        using (var appenderNew = File.AppendText(CurrentFileFullPathAlt))
                        {
                            foreach (var channelValue in values)
                            {
                                var r = Math.Round(channelValue.Resistance, 0);
                                var rRaw = Math.Round(channelValue.ResistanceRaw, 0);
                                var pd1 = Math.Round(channelValue.VoltagePreMv, 1);
                                var pd2 = Math.Round(channelValue.VoltagePostMv, 1);

                                if (platform == 0)
                                {
                                    offsetByWell[channelValue.WellNumber] = pd1;
                                }

                                double offset;

                                offsetByWell.TryGetValue(channelValue.WellNumber, out offset);

                                var pd1Corrected = pd1 - offset;

                                var iEq = Math.Round((r < 0.001 && r > -0.001) ? 0 : 1000 * pd1Corrected / r, 1);

                                rValues.Add(r);
                                rValuesRaw.Add(rRaw);
                                pdValues.Add(pd1);
                                iEqValues.Add(iEq);

                                {
                                    var lines = new object[]
                                            {
                                        CurrentIndex.ToString().PadLeft(10),
                                        platform.ToString().PadLeft(5),
                                        channelValue.Channel,
                                        runElapsed.ToString().PadLeft(11),
                                        r.ToString().PadLeft(11),
                                        pd1.ToString().PadLeft(11),
                                        pd2.ToString().PadLeft(11),
                                        iEq.ToString().PadLeft(11)
                                            };

                                    var outLine = string.Join(",", lines);

                                    appenderOld.WriteLine(outLine);
                                }

                                var rtx = validRtxSettings.FirstOrDefault(rt => rt.RtxIndex == channelValue.CurrentRtxIndex);
                                var rtxR = (rtx == null) ? (float?)null : rtx.ThresholdResistance;

                                var rtxNext = validRtxSettings.FirstOrDefault(rt => rt.RtxIndex == channelValue.NextRtxIndex);
                                var rtxRNext = (rtxNext == null) ? (float?)null : rtxNext.ThresholdResistance;

                                {
                                    var line = new object[]
                                            {
                                        CurrentIndex.ToString(),
                                        platform.ToString(),
                                        runCount[platform],
                                        channelValue.Channel,
                                        runElapsed.ToString(),
                                        r.ToString(),
                                        pd1.ToString(),
                                        pd2.ToString(),
                                        iEq.ToString(),
                                        stepName,
                                        (int?) elapsedOnChannel,
                                        LastMeasurementInterval.ToString(),
                                        channelValue.WellAddress,
                                        channelValue.WellColumn,
                                        channelValue.WellRow,
                                        channelValue.TeccBoard,
                                        channelValue.BoardChannel,
                                        rRaw,
                                        channelValue.StatusOscillation ? "OSC" : "",
                                        channelValue.StatusVoltageOverflowOnV1 ? "OF" : "",
                                        channelValue.StatusVoltageOverflowOnV2 ? "OF" : "",
                                        channelValue.StatusCurrentOverflowOnRt ? "OF" : "",
                                        channelValue.StatusVoltageOverflowOnRt ? "OF" : "",
                                        rtxR,
                                        rtxRNext,
                                        channelValue.CurrentRtxIndex,
                                        channelValue.NextRtxIndex,
                                        channelValue.RtxChange,
                                        channelValue.Overflow,
                                        channelValue.HasIssues,
                                        StartTime.ToLongDateString(),
                                        DateTime.Now,
                                        TransientComment,
                                        tecc.TeccConfigString
                                            };

                                    var outLine = string.Join(",", line.Select(CsvEscaper.Escape).ToArray());

                                    appenderNew.WriteLine(outLine);

                                    altLines.Add(line);
                                }

                                measurementInfo.Measurements.Add(
                                    new ChannelMeasurement
                                    {
                                        WellAddress = channelValue.WellAddress,
                                        TeccBoard = channelValue.TeccBoard,
                                        BoardChannel = channelValue.BoardChannel,
                                        Rt = Math.Round(r, 0),
                                        Pd1 = Math.Round(pd1, 1),
                                        Pd2 = Math.Round(pd2, 1),
                                        IEq = Math.Round(iEq, 1),
                                        TeccChannel = channelValue.Channel,
                                        RtRaw = rRaw,
                                        StatusCurrentOverflowOnRt = channelValue.StatusCurrentOverflowOnRt,
                                        StatusOscillation = channelValue.StatusOscillation,
                                        StatusVoltageOverflowOnV1 = channelValue.StatusVoltageOverflowOnV1,
                                        StatusVoltageOverflowOnV2 = channelValue.StatusVoltageOverflowOnV2,
                                        StatusVoltageOverflowOnRt = channelValue.StatusVoltageOverflowOnRt,
                                        CurrentRtxR = rtxR,
                                        NextRtxR = rtxRNext,
                                        BoardWell = channelValue.BoardWell,
                                        RtxChange = channelValue.RtxChange,
                                        HasIssues = channelValue.HasIssues,
                                        OutputLine = altLines.Count - 1,
                                        VoltageOnly = voltageOnly
                                    });
                            }

                            appenderNew.WriteLine();
                            appenderOld.WriteLine();
                        }

                        if (rValues.Any())
                        {
                            measurementInfo.RangeR = string.Format("{0} to {1}", rValues.Min(), rValues.Max());
                            measurementInfo.RangeRRaw = string.Format("{0} to {1}", rValuesRaw.Min(), rValuesRaw.Max());
                            measurementInfo.RangePd = string.Format("{0} to {1}", pdValues.Min(), pdValues.Max());
                            measurementInfo.RangeIEq = string.Format("{0} to {1}", iEqValues.Min(), iEqValues.Max());
                        }

                        Measurements = Measurements.Union(new[] { measurementInfo }).OrderByDescending(m => m.MeasurementIndex).ToList();
                    }

                    TransientComment = null;

                    log.Info("Successful write to TECC output file");

                    runCount[platform] = runCount[platform] + 1;
                    CurrentIndex++;

                    return measurementInfo;
                }
                catch
                {
                    lastException = new Exception("Could not communicate with TECC!");
                }
            }

            lastException = lastException??new Exception("Unknown failure in reading from TECC or writing data.");
            throw lastException;
        }

        public string GetCurrentFileContents()
        {
            lock (fileLock)
            {
                if (!File.Exists(CurrentFileFullPathAlt))
                {
                    return null;
                }

                return File.ReadAllText(CurrentFileFullPathAlt);
            }
        }

        private void FileCheck()
        {
            if (!Directory.Exists(DataPath))
            {
                throw new DirectoryNotFoundException("Could not find DataPath " + DataPath);
            }

            if (string.IsNullOrWhiteSpace(CurrentFile))
            {
                throw new Exception("Cannot open a null file");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
