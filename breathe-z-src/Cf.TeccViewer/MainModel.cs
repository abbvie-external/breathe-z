using Cf.Daq.Common;
using Cf.TeccViewer.Annotations;
using SciChart.Data.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Cf.TeccViewer
{
    public class MainModel : INotifyPropertyChanged
    {
        private DateTime? lastUpdateTime;
        private string sourceFolder;
        private string sourceFile;
        private List<Measurement> measurements;
        private List<Measurement> currentMeasurements;
        private List<int> platforms;
        private int? platform;
        private bool autoRefresh;
        private bool needUiFresh;
        private readonly Dispatcher uiDispatcher;
        private bool allWells;
        private string selectWellsText;
        private bool needsCurrentRefresh;
        private bool showLatestAcquisition;
        private bool chartRVisible;
        private bool chartGVisible;
        private bool chartPd1Visible;
        private bool chartPd2Visible;
        private bool chartIEqVisible;
        private DoubleRange xAxis;
        private string format;
        private readonly TeccWellMapper teccWellMapper = new TeccWellMapper();

        private readonly object fileReadLock = new object();
        private string selectedWellChartMeasurment;

        public Action RefreshAction { get; set; }

        public bool NeedUiFresh
        {
            get { return needUiFresh; }
            set
            {
                if (value.Equals(needUiFresh)) return;
                needUiFresh = value;
                OnPropertyChanged("NeedUiFresh");
            }
        }

        public IList<string> Formats { get { return teccWellMapper.PlateFormats; } }

        public string Format
        {
            get { return format; }
            set
            {
                if (value == format) return;
                format = value;
                OnPropertyChanged("Format");
                UpdateCurrentMeasurements();

                NeedUiFresh = true;

                Properties.Settings.Default.PlateFormat = value;

                SaveProps();
            }
        }

        public int Rows
        {
            get { return teccWellMapper.GetRowCount(Format); }
        }

        public int Columns
        {
            get { return teccWellMapper.GetColCount(Format); }
        }

        public IList<TeccWellMap> WellMaps
        {
            get { return teccWellMapper.GetMaps(Format); }
        }

        public string SourceFolder
        {
            get
            {
                lock (fileReadLock)
                {
                    return sourceFolder;
                }
            }
            set
            {
                lock (fileReadLock)
                {
                    if (value == sourceFolder) return;
                    sourceFolder = value;
                    OnPropertyChanged("SourceFolder");

                    Properties.Settings.Default.FolderSource = SourceFolder;
                    SaveProps();
                }
            }
        }

        public string SourceFile
        {
            get
            {
                lock (fileReadLock)
                {
                    return sourceFile;
                }
            }
            set
            {
                lock (fileReadLock)
                {
                    if (sourceFile == value)
                    {
                        return;
                    }

                    sourceFile = value;
                    lastUpdateTime = null;
                    OnPropertyChanged("SourceFile");
                }
            }
        }

        public List<string> WellChartMeasurements { get { return new List<string> { "G", "R", "PD-1", "PD-2", "IEq" }; } }

        public string SelectedWellChartMeasurment
        {
            get { return selectedWellChartMeasurment; }
            set
            {
                selectedWellChartMeasurment = value;
                OnPropertyChanged();
                needsCurrentRefresh = true;
            }
        }

        public List<Measurement> Measurements
        {
            get { return measurements; }
            set
            {
                if (Equals(value, measurements)) return;
                measurements = value;
                OnPropertyChanged("Measurements");

                Platforms = measurements.Select(m => m.Position).Distinct().OrderBy(p => p).ToList();

                if (ShowLatestAcquisition && measurements.Any())
                {
                    Platform = measurements.Last().Position;
                }
                else
                {
                    if (Platforms.All(p => p != Platform) && Platforms.Any())
                    {
                        Platform = Platforms.First();
                    }
                }
            }
        }

        public List<int> Platforms
        {
            get { return platforms; }
            set
            {
                if (Equals(value, platforms)) return;
                platforms = value;
                OnPropertyChanged("Platforms");
            }
        }

        public int? Platform
        {
            get { return platform; }
            set
            {
                if (value == platform) return;
                platform = value;
                OnPropertyChanged("Platform");

                UpdateCurrentMeasurements();
            }
        }

        public List<Measurement> CurrentMeasurements
        {
            get { return currentMeasurements; }
            set
            {
                var matches = true;

                if (value == null || currentMeasurements == null)
                {
                    matches = false;
                }
                else if (value.Count != currentMeasurements.Count)
                {
                    matches = false;
                }
                else if (value.Count == currentMeasurements.Count)
                {
                    for (int i = 0; i < value.Count; i++)
                    {
                        var a = value[i];
                        var b = currentMeasurements[i];

                        if (a.Position != b.Position || a.FileName != b.FileName)
                        {
                            matches = false;
                            break;
                        }
                    }
                }

                if (!matches)
                {
                    currentMeasurements = value;
                    OnPropertyChanged("CurrentMeasurements");
                    NeedUiFresh = true;
                }

                OnPropertyChanged("HasCurrentMeasurements");
            }
        }

        public bool AutoRefresh
        {
            get { return autoRefresh; }
            set
            {
                if (value.Equals(autoRefresh)) return;
                autoRefresh = value;
                OnPropertyChanged("AutoRefresh");
                OnPropertyChanged("AutoRefreshInverse");

                Properties.Settings.Default.AutoRefresh = AutoRefresh;
                SaveProps();
            }
        }

        public bool AutoRefreshInverse { get { return !AutoRefresh; } }

        public bool ShowLatestAcquisition
        {
            get { return showLatestAcquisition; }
            set
            {
                if (value.Equals(showLatestAcquisition)) return;
                showLatestAcquisition = value;
                OnPropertyChanged("ShowLatestAcquisition");

                Properties.Settings.Default.ShowLatestAcquisition = ShowLatestAcquisition;
                SaveProps();

                if (value && Measurements != null && Measurements.Any())
                {
                    Platform = Measurements.Last().Position;
                }
            }
        }

        public bool FilterWells
        {
            get { return allWells; }
            set
            {
                if (value.Equals(allWells)) return;
                allWells = value;
                OnPropertyChanged("FilterWells");
                UpdateCurrentMeasurements();
            }
        }

        public List<int> SelectedWells
        {
            get
            {
                var wells = new List<int>();

                if (string.IsNullOrWhiteSpace(SelectWellsText))
                {
                    return wells;
                }

                var items = SelectWellsText.Split(new[] { ' ', ',' });

                foreach (var item in items)
                {
                    var pairs = item.Split('-');

                    if (pairs.Length == 1)
                    {
                        int dummy;

                        if (int.TryParse(item, out dummy))
                        {
                            wells.Add(dummy);
                        }
                    }
                    else if (pairs.Length == 2)
                    {
                        int dummy1;
                        int dummy2;

                        if (int.TryParse(pairs[0], out dummy1) && int.TryParse(pairs[1], out dummy2))
                        {
                            for (int i = dummy1; i <= dummy2; i++)
                            {
                                wells.Add(i);
                            }
                        }
                    }
                }

                return wells;
            }
        }

        public string SelectWellsText
        {
            get { return selectWellsText; }
            set
            {
                if (value == selectWellsText) return;
                selectWellsText = value;
                OnPropertyChanged("SelectWellsText");
            }
        }

        public void UpdateSelectedWells()
        {
            needsCurrentRefresh = true;
        }

        public bool RefreshFromFolder()
        {
            lock (fileReadLock)
            {
                if (Directory.Exists(SourceFolder))
                {
                    var file = Directory
                        .GetFiles(SourceFolder, "*.dat")
                        .Select(fn => new FileInfo(fn))
                        .OrderByDescending(f => f.LastWriteTime)
                        .FirstOrDefault();

                    if (file == null)
                    {
                        SourceFile = null;
                    }
                    else
                    {
                        if (file.FullName != SourceFile)
                        {
                            SourceFile = file.FullName;
                            lastUpdateTime = null;
                        }

                        return RefreshFromFile();
                    }
                }
            }

            return false;
        }

        public bool RefreshFromFile()
        {
            List<Measurement> newMeasurements;

            lock (fileReadLock)
            {
                if (!string.IsNullOrWhiteSpace(SourceFile) || File.Exists(SourceFile))
                {
                    var file = new FileInfo(SourceFile);

                    if (lastUpdateTime == file.LastWriteTimeUtc && !needsCurrentRefresh)
                    {
                        return false;
                    }

                    var loader = new TeccFileLoader();
                    newMeasurements = loader.LoadMeasurements(SourceFile);
                    lastUpdateTime = file.LastWriteTimeUtc;
                    needsCurrentRefresh = false;
                }
                else
                {
                    return false;
                }
            }

            uiDispatcher.Invoke(
                () =>
                {
                    Measurements = newMeasurements;
                    UpdateCurrentMeasurements();
                });

            return true;
        }

        private void UpdateCurrentMeasurements()
        {
            if (Measurements == null) return;

            var newMeasurements = Measurements
                .Where(m => m.Position == Platform)
                .OrderBy(m => m.Number)
                .ThenBy(m => m.Tecc)
                .ToList();

            if (FilterWells)
            {
                var filter = SelectedWells.ToHashSet();
                newMeasurements = newMeasurements.Where(m => filter.Contains(m.Tecc)).ToList();
            }

            var max = teccWellMapper.GetWellCount(Format);

            foreach (var measurement in newMeasurements)
            {
                if (measurement.Tecc > max)
                {
                    measurement.WellAddress = "";
                }
                else
                {
                    var teccMap = teccWellMapper.GetMap(Format, measurement.Tecc);
                    measurement.WellAddress = teccMap.WellAddress;
                }
            }

            CurrentMeasurements = newMeasurements;
        }

        public bool HasCurrentMeasurements
        {
            get { return CurrentMeasurements != null && CurrentMeasurements.Any(); }
        }

        public bool ChartRVisible
        {
            get { return chartRVisible; }
            set
            {
                if (value.Equals(chartRVisible)) return;
                chartRVisible = value;
                OnPropertyChanged("ChartRVisible");
                Properties.Settings.Default.ChartRVisible = ChartRVisible;
                Properties.Settings.Default.Save();
            }
        }

        public bool ChartGVisible
        {
            get { return chartGVisible; }
            set
            {
                if (value.Equals(chartGVisible)) return;
                chartGVisible = value;
                OnPropertyChanged("ChartGVisible");
                Properties.Settings.Default.ChartGVisible = ChartGVisible;
                Properties.Settings.Default.Save();
            }
        }

        public bool ChartPd1Visible
        {
            get { return chartPd1Visible; }
            set
            {
                if (value.Equals(chartPd1Visible)) return;
                chartPd1Visible = value;
                OnPropertyChanged("ChartPd1Visible");
                Properties.Settings.Default.ChartPd1Visible = ChartPd1Visible;
                Properties.Settings.Default.Save();
            }
        }

        public bool ChartPd2Visible
        {
            get { return chartPd2Visible; }
            set
            {
                if (value.Equals(chartPd2Visible)) return;
                chartPd2Visible = value;
                OnPropertyChanged("ChartPd2Visible");
                Properties.Settings.Default.ChartPd2Visible = ChartPd2Visible;
                Properties.Settings.Default.Save();
            }
        }

        public bool ChartIEqVisible
        {
            get { return chartIEqVisible; }
            set
            {
                if (value.Equals(chartIEqVisible)) return;
                chartIEqVisible = value;
                OnPropertyChanged("ChartIEqVisible");
                Properties.Settings.Default.ChartIEqVisible = ChartIEqVisible;
                Properties.Settings.Default.Save();
            }
        }

        public DoubleRange XAxis
        {
            get { return xAxis; }
            set
            {
                if (Equals(value, xAxis)) return;
                xAxis = value;
                OnPropertyChanged("XAxis");
            }
        }

        public MainModel()
        {
            var props = Properties.Settings.Default;

            SelectedWellChartMeasurment = WellChartMeasurements[0];

            SourceFolder = props.FolderSource;
            AutoRefresh = props.AutoRefresh;
            ShowLatestAcquisition = props.ShowLatestAcquisition;
            Format = props.PlateFormat;

            ChartRVisible = props.ChartRVisible;
            ChartPd1Visible = props.ChartPd1Visible;
            ChartPd2Visible = props.ChartPd2Visible;
            ChartIEqVisible = props.ChartIEqVisible;

            NeedUiFresh = true;

            uiDispatcher = Dispatcher.CurrentDispatcher;

            Task.Run(() =>
                     {
                         int counter = 0;

                         while (true)
                         {
                             try
                             {
                                 bool needsFresh = AutoRefresh && RefreshFromFile();

                                 if (RefreshAction != null && (NeedUiFresh || needsFresh))
                                 {
                                     OnPropertyChanged("CurrentMeasurements");
                                     uiDispatcher.Invoke(RefreshAction);
                                     NeedUiFresh = false;
                                 }

                                 counter = (counter + 1) % 10;
                             }
                             // ReSharper disable once EmptyGeneralCatchClause
                             catch
                             {
                                 NeedUiFresh = false;
                             }

                             Task.Delay(100).Wait();
                         }
                         // ReSharper disable once FunctionNeverReturns
                     });
        }

        private void SaveProps()
        {
            Properties.Settings.Default.Save();
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