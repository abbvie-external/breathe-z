using SciChart.Charting;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.PointMarkers;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace Cf.TeccViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly Dictionary<string, Tuple<DoubleRange, DoubleRange>> ranges = new Dictionary<string, Tuple<DoubleRange, DoubleRange>>();

        private readonly MainModel model;
        private readonly Dictionary<string, SciChartSurface> wellCharts = new Dictionary<string, SciChartSurface>();

        private static readonly string[] colorValues =
                                                       {
                                                           "#CA1983","#107724","#FF4200","#6DC3E5","#6D3C02","#B982FE","#EE9E9F","#BCB211",
                                                           "#522947","#F929E0","#284D46","#3C639C","#DBA860","#FC6180","#85C162","#593D8A",
                                                           "#97A8E5","#F898F3","#D72D28","#712723","#820F5A","#4C6917","#F61315","#4FC2E1"
                                                       };

        private List<NumericAxis> xAxes;
        private List<NumericAxis> yAxes;
        private readonly Dictionary<int, FastLineRenderableSeries> cachedSeries = new Dictionary<int, FastLineRenderableSeries>();
        private readonly Dictionary<int, FastLineRenderableSeries> cachedSeriesOver = new Dictionary<int, FastLineRenderableSeries>();
        private readonly Dictionary<int, FastLineRenderableSeries> cachedSeriesUnder = new Dictionary<int, FastLineRenderableSeries>();

        public MainWindow()
        {
            InitializeComponent();
            model = new MainModel { RefreshAction = Refresh };
            DataContext = model;

            model.PropertyChanged += model_PropertyChanged;

            ThemeManager.SetTheme(sciIndividual, "BrightSpark");
            ThemeManager.SetTheme(chartR, "BrightSpark");
            ThemeManager.SetTheme(chartPd1, "BrightSpark");
            ThemeManager.SetTheme(chartPd2, "BrightSpark");
            ThemeManager.SetTheme(chartIEq, "BrightSpark");
        }

        private void model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedWellChartMeasurment")
            {
                YAxisMaxIndividual.Text = string.Empty;
                YAxisMinIndividual.Text = string.Empty;
            }
        }

        private void Refresh()
        {
            RefreshChart(chartR, c => c.R, 0, 10, Constants.MeasurementR);
            RefreshChart(chartG, c => c.G, 0, 20, Constants.MeasurementG);
            RefreshChart(chartPd1, c => c.Pd1, -15, 15, Constants.MeasurementPd1);
            RefreshChart(chartPd2, c => c.Pd2, -15, 15, Constants.MeasurementPd2);
            RefreshChart(chartIEq, c => c.I, -15, 15, Constants.MeasurementIEq);

            BuildWellCharts();

            PopulateWellCharts();
        }

        private void PopulateWellCharts()
        {
            foreach (var sciChartSurface in wellCharts)
            {
                sciChartSurface.Value.RenderableSeries.Clear();
            }

            if (model.CurrentMeasurements == null || !model.CurrentMeasurements.Any())
            {
                return;
            }

            switch (model.SelectedWellChartMeasurment)
            {
                case Constants.MeasurementG:
                    PopulateSingleWellCharts(model.CurrentMeasurements, d => d.G, Colors.Black);
                    break;

                case Constants.MeasurementR:
                    PopulateSingleWellCharts(model.CurrentMeasurements, d => d.R, Colors.Blue);
                    break;

                case Constants.MeasurementPd1:
                    PopulateSingleWellCharts(model.CurrentMeasurements, d => d.Pd1, Colors.Red);
                    break;

                case Constants.MeasurementPd2:
                    PopulateSingleWellCharts(model.CurrentMeasurements, d => d.Pd2, Colors.Green);
                    break;

                case Constants.MeasurementIEq:
                    PopulateSingleWellCharts(model.CurrentMeasurements, d => d.I, Colors.Purple);
                    break;
            }
        }

        private void PopulateSingleWellCharts(IEnumerable<Measurement> teccData, Func<Measurement, double?> selector, Color color)
        {
            var byTecc = teccData.GroupBy(m => new { m.WellAddress, m.Tecc }).ToList();

            using (sciIndividual.SuspendUpdates())
            {
                foreach (var td in byTecc)
                {
                    if (!cachedSeries.ContainsKey(td.Key.Tecc))
                    {
                        continue;
                    }

                    var rs = cachedSeries[td.Key.Tecc];
                    var rsOver = cachedSeriesOver[td.Key.Tecc];
                    var rsUnder = cachedSeriesUnder[td.Key.Tecc];

                    var doubleData = new XyDataSeries<double, double>();
                    var doubleDataOver = new XyDataSeries<double, double>();
                    var doubleDataUnder = new XyDataSeries<double, double>();

                    var data = td.OrderBy(t => t.Number).ToList();

                    var xValues = data.Select(d => (double)d.Number).ToArray();
                    var yValues = data.Select(d => (double)(selector(d))).ToArray();

                    DoubleRange yRange; ;

                    double minY;
                    double maxY;

                    if (double.TryParse(YAxisMinIndividual.Text, out minY) &&
                        double.TryParse(YAxisMaxIndividual.Text, out maxY))
                    {
                        yRange = new DoubleRange(minY, maxY);
                        rs.YAxis.VisibleRange = yRange;
                    }
                    else
                    {
                        yRange = ranges[model.SelectedWellChartMeasurment].Item2;
                        rs.YAxis.VisibleRange = yRange;
                    }

                    for (int i = 0; i < yValues.Length; i++)
                    {
                        if (yValues[i] > yRange.Max)
                        {
                            doubleDataOver.Append(xValues[i], yRange.Max);
                            yValues[i] = yRange.Max;
                        }
                        else if (yValues[i] < yRange.Min)
                        {
                            doubleDataUnder.Append(xValues[i], yRange.Min);
                            yValues[i] = yRange.Min;
                        }
                        else
                        {
                            doubleDataUnder.Append(xValues[i], double.NaN);
                            doubleDataOver.Append(xValues[i], double.NaN);
                        }
                    }

                    doubleData.Append(xValues, yValues);
                    rs.DataSeries = doubleData;

                    rsOver.DataSeries = doubleDataOver;
                    rsUnder.DataSeries = doubleDataUnder;
                }
            }
        }

        private void BuildWellCharts()
        {
            sciIndividual.RenderableSeries.Clear();

            xAxes = new List<NumericAxis>
                    {
                        X1,
                        X2,
                        X3,
                        X4,
                        X5,
                        X6,
                        X7,
                        X8,
                        X9,
                        X10,
                        X11,
                        X12
                    };

            yAxes = new List<NumericAxis>
                    {
                        Y1,
                        Y2,
                        Y3,
                        Y4,
                        Y5,
                        Y6,
                        Y7,
                        Y8
                    };

            int well = 1;

            sciIndividual.XAxes.Clear();
            sciIndividual.YAxes.Clear();

            foreach (var a in xAxes)
            {
                a.Visibility = Visibility.Collapsed;
            }

            foreach (var a in yAxes)
            {
                a.Visibility = Visibility.Collapsed;
            }

            if (model.Format.Contains("24"))
            {
                sciIndividual.LeftAxesPanelTemplate =
                    wellsDisplayControl.Resources["YAxesPanel24"] as ItemsPanelTemplate;
                sciIndividual.BottomAxesPanelTemplate =
                    wellsDisplayControl.Resources["XAxesPanel24"] as ItemsPanelTemplate;
            }
            else
            {
                sciIndividual.LeftAxesPanelTemplate =
                    wellsDisplayControl.Resources["YAxesPanel"] as ItemsPanelTemplate;
                sciIndividual.BottomAxesPanelTemplate =
                    wellsDisplayControl.Resources["XAxesPanel"] as ItemsPanelTemplate;
            }

            for (int x = 0; x < model.Columns; x++)
            {
                sciIndividual.XAxes.Add(xAxes[x]);
            }

            for (int y = 0; y < model.Rows; y++)
            {
                sciIndividual.YAxes.Add(yAxes[y]);
            }

            var pm = new EllipsePointMarker
            {
                Width = 3,
                Height = 3,
                Fill = Colors.Green
            };

            for (int x = 0; x < model.Columns; x++)
            {
                for (int y = 0; y < model.Rows; y++)
                {
                    var series = new FastLineRenderableSeries
                    {
                        //SeriesColor = Colors.Green,
                        Stroke = Colors.Green,
                        YAxis = yAxes[y],
                        YAxisId = yAxes[y].Id,
                        Name = string.Format("RS_{0}_{1}", x, y),
                        XAxis = xAxes[x],
                        XAxisId = xAxes[x].Id,
                        PointMarker = pm,
                        StrokeThickness = 1
                    };

                    series.YAxis.Visibility = Visibility.Visible;
                    series.XAxis.Visibility = Visibility.Visible;

                    sciIndividual.RenderableSeries.Add(series);
                    cachedSeries[well] = series;

                    var seriesOver = new FastLineRenderableSeries
                    {
                        //SeriesColor = Colors.Red,
                        Stroke = Colors.Red,
                        YAxis = yAxes[y],
                        YAxisId = yAxes[y].Id,
                        Name = string.Format("RSO_{0}_{1}", x, y),
                        XAxis = xAxes[x],
                        XAxisId = xAxes[x].Id,
                        StrokeThickness = 1
                    };

                    sciIndividual.RenderableSeries.Add(seriesOver);
                    cachedSeriesOver[well] = seriesOver;

                    var seriesUnder = new FastLineRenderableSeries
                    {
                        //SeriesColor = Colors.Blue,
                        Stroke = Colors.Blue,
                        YAxis = yAxes[y],
                        YAxisId = yAxes[y].Id,
                        Name = string.Format("RSU_{0}_{1}", x, y),
                        XAxis = xAxes[x],
                        XAxisId = xAxes[x].Id,
                        StrokeThickness = 1
                    };

                    sciIndividual.RenderableSeries.Add(seriesUnder);
                    cachedSeriesUnder[well] = seriesUnder;

                    well++;
                }
            }
        }

        private void RefreshChart(ISciChartSurface chart, Func<Measurement, double?> selector, double prefMin, double prefMax, string measurementName)
        {
            using (chart.SuspendUpdates())
            {
                chart.RenderableSeries.Clear();

                if (model.CurrentMeasurements == null || !model.CurrentMeasurements.Any()) return;

                int colorIndex = 0;

                double minX = double.MaxValue;
                double maxX = double.MinValue;

                double minY = double.MaxValue;
                double maxY = double.MinValue;

                var teccs = model.CurrentMeasurements.Where(m => selector(m) != null).GroupBy(m => string.Format("Well {1} ({0})", m.Tecc, m.WellAddress));

                var allYs = new List<double>();

                foreach (var tecc in teccs)
                {
                    var doubleData = new XyDataSeries<double, double>
                    {
                        SeriesName = tecc.Key,
                    };

                    // ReSharper disable once PossibleInvalidOperationException Filtered above
                    var dataXy = tecc.OrderBy(t => t.Number).ToList();
                    var dataY = dataXy.Select(c => (double)selector(c)).ToArray();
                    var dataX = dataXy.Select(c => (double)c.Number).ToArray();

                    minY = Math.Min(dataY.Min(), minY);
                    maxY = Math.Max(dataY.Max(), maxY);

                    minX = Math.Min(dataX.Min(), minX);
                    maxX = Math.Max(dataX.Max(), maxX);

                    allYs.AddRange(dataY);

                    doubleData.Append(dataX, dataY);

                    var color = (Color)ColorConverter.ConvertFromString(colorValues[colorIndex % colorValues.Length]);
                    colorIndex++;

                    IPointMarker marker = new EllipsePointMarker();

                    marker.Width = 9;
                    marker.Height = 9;
                    marker.Fill = color;
                    marker.StrokeThickness = 0;

                    // Create a RenderableSeries and ensure DataSeries is set
                    var renderSeries = new FastLineRenderableSeries
                    {
                        DataSeries = doubleData,
                        PointMarker = marker,
                        StrokeThickness = 1,
                        SelectedPointMarker = new EllipsePointMarker
                        {
                            Stroke = color,
                            Fill = Colors.Transparent,
                            StrokeThickness = 1,
                            Width = 11,
                            Height = 11,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    };

                    //Add the new RenderableSeries
                    chart.RenderableSeries.Add(renderSeries);
                }

                chart.ZoomExtents();

                var countToSkip = (int)(allYs.Count * 0.05);
                var middleCount = (int)(allYs.Count() * 0.9);

                var the90 = allYs.OrderBy(a => a).Skip(countToSkip).Take(middleCount);

                var middleTop = the90.Last();
                var middleBottom = the90.First();

                var middleRange = middleTop - middleBottom;
                var middleOffset = middleRange / 2;

                var yBottom = Math.Max(middleBottom - middleOffset, minY);
                var yTop = Math.Min(middleTop + middleOffset, maxY);

                var yRange = new DoubleRange(yBottom, yTop);
                var xRange = new DoubleRange(minX, maxX);

                ranges[measurementName] = new Tuple<DoubleRange, DoubleRange>(xRange, yRange);

                chart.YAxis.VisibleRange = yRange;
            }
        }

        private void SelectSourceFolder_OnClick(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog1 = new FolderBrowserDialog();

            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                model.SourceFolder = folderBrowserDialog1.SelectedPath;
            }
        }

        private void SelectSourceFile_OnClick(object sender, RoutedEventArgs e)
        {
            var browser = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "TECC DAT File|*.dat"
            };

            browser.ShowDialog(GetWindow(this));

            if (string.IsNullOrWhiteSpace(browser.FileName))
            {
                return;
            }

            model.SourceFile = browser.FileName;
        }

        private void ApplyWellFilter_Click(object sender, RoutedEventArgs e)
        {
            model.UpdateSelectedWells();
        }

        private void ClearZoomR_Click(object sender, RoutedEventArgs e)
        {
            ResetAxis(chartR, Constants.MeasurementR);
            ResetAxis(chartPd1, Constants.MeasurementPd1);
            ResetAxis(chartIEq, Constants.MeasurementIEq);
            ResetAxis(chartPd2, Constants.MeasurementIEq);
        }

        private void ResetAxis(ISciChartSurface chart, string measurementName)
        {
            model.XAxis = ranges[measurementName].Item1;
            chart.YAxis.VisibleRange = ranges[measurementName].Item2;
        }

        private void GetLatestFile_OnClick(object sender, RoutedEventArgs e)
        {
            model.RefreshFromFolder();
        }

        private void SeriesSelectionModifier_SelectionChanged(object sender, EventArgs e)
        {
        }

        private void ClearZoomTimeCourse_Click(object sender, RoutedEventArgs e)
        {
            YAxisMaxIndividual.Text = string.Empty;
            YAxisMinIndividual.Text = string.Empty;
            PopulateWellCharts();
        }

        private void ZoomToY(object sender, RoutedEventArgs e)
        {
            PopulateWellCharts();
        }
    }
}