using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AutomationEngineering.CommonTools.ExtensionMethods;
using BreatheCalc.FourDispense.Data;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.PointMarkers;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Data.Model;

namespace BreatheCalc.FourDispense
{
    /// <summary>
    /// Interaction logic for PlateQc.xaml
    /// </summary>
    public partial class PlateQc
    {
        private readonly Experiment experiment;
        
        private Dictionary<int, RadioButton> radioMap;
        private List<NumericAxis> xAxes;
        private List<NumericAxis> yAxes;

        private readonly Dictionary<int, FastLineRenderableSeries> cachedSeries = new Dictionary<int, FastLineRenderableSeries>();
        private readonly Dictionary<int, XyScatterRenderableSeries> cachedSeriesDots = new Dictionary<int, XyScatterRenderableSeries>();
        
        private Plate selectedPlate;

        private int wellCount;
        private int cols;
        private int rows;

        private readonly bool initialized;

        public PlateQc()
        {
            InitializeComponent();
        }

        internal PlateQc(Experiment experiment)
        {
            if (experiment == null)
            {
                return;
            }

            this.experiment = experiment;

            InitializeComponent();

            BindToOptionButtons();

            var first = experiment.Plates.First().Platform;

            var rb = radioMap.Single(r => r.Key == first);
            rb.Value.IsChecked = true;

            UpdateSelectedPlate();
            SetupWellFormat();
            BuildWellCharts();
            PopulateSingleWellCharts();

            sciIndividual.SelectedRenderableSeries.CollectionChanged += SelectedRenderableSeries_CollectionChanged;

            initialized = true;
            AutoRangeAll();
        }

        void SelectedRenderableSeries_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var items = new List<FastLineRenderableSeries>();

            if (e.NewItems != null)
            {
                items.AddRange(e.NewItems.OfType<FastLineRenderableSeries>());
            }
           
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var i in items)
                {
                    var well = i.Tag as Well;

                    if (well == null) continue;

                    well.IsValid = !well.IsValid;
                    i.Stroke = MapToPlotColor(well.IsValid);
                }
            }
        }

        private void SetupWellFormat()
        {
            wellCount = experiment.Plates.First().Wells.Count;

            if (wellCount == 24)
            {
                rows = 4;
                cols = 6;
            }
            else
            {
                rows = 8;
                cols = 12;
            }
        }

        private void BindToOptionButtons()
        {
            radioMap = new Dictionary<int, RadioButton>
                       {
                           {0, rb0},
                           {1, rb1},
                           {2, rb2},
                           {3, rb3},
                           {4, rb4},
                           {5, rb5},
                           {6, rb6},
                       };

            foreach (var radioButton in radioMap)
            {
                radioButton.Value.Visibility = (experiment.Plates.Any(p => p.Platform == radioButton.Key))
                    ? Visibility.Visible
                    : Visibility.Collapsed;
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

            if (wellCount == 24)
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

            for (int x = 0; x < cols; x++)
            {
                sciIndividual.XAxes.Add(xAxes[x]);
            }

            for (int y = 0; y < rows; y++)
            {
                sciIndividual.YAxes.Add(yAxes[y]);
            }

            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    var series = new FastLineRenderableSeries
                    {
                        YAxis = yAxes[y],
                        YAxisId = yAxes[y].Id,
                        Name = string.Format("RS_{0}_{1}", x, y),
                        XAxis = xAxes[x],
                        XAxisId = xAxes[x].Id,
                        StrokeThickness = 1,
                        PointMarker = new EllipsePointMarker { Stroke = Colors.LawnGreen, Width = 5, Height = 5, Fill = Colors.LawnGreen }
                    };

                    series.YAxis.Visibility = Visibility.Visible;
                    series.XAxis.Visibility = Visibility.Visible;

                    sciIndividual.RenderableSeries.Add(series);
                    cachedSeries[well] = series;

                    var seriesDots = new XyScatterRenderableSeries
                                     {
                                         YAxis = yAxes[y],
                                         YAxisId = yAxes[y].Id,
                                         Name = string.Format("RSD_{0}_{1}", x, y),
                                         XAxis = xAxes[x],
                                         XAxisId = xAxes[x].Id,
                                         StrokeThickness = 1,
                                         PointMarker =
                                             new EllipsePointMarker
                                             {
                                                 Stroke = Colors.Blue,
                                                 Fill = Colors.Transparent,
                                                 StrokeThickness = 1,
                                                 Width = 11,
                                                 Height = 11,
                                                 HorizontalAlignment = HorizontalAlignment.Center,
                                                 VerticalAlignment = VerticalAlignment.Center
                                             }
                                     };

                    seriesDots.YAxis.Visibility = Visibility.Visible;
                    seriesDots.XAxis.Visibility = Visibility.Visible;

                    sciIndividual.RenderableSeries.Add(seriesDots);
                    cachedSeriesDots[well] = seriesDots;
                    
                    well++;
                }
            }
        }

        private void UpdateSelectedPlate()
        {
            foreach (var i in radioMap)
            {
                if (i.Value.IsChecked == true)
                {
                    selectedPlate = experiment.Plates.SingleOrDefault(p => p.Platform == i.Key);
                }
            }
        }

        private void PopulateSingleWellCharts()
        {
            var yMin = Properties.Settings.Default.RangeMin;
            var yMax = Properties.Settings.Default.RangeMax;

            if (yMax <= yMin) return;

            var yRange = new DoubleRange(Properties.Settings.Default.RangeMin, Properties.Settings.Default.RangeMax);

            using (sciIndividual.SuspendUpdates())
            {
                foreach (var well in selectedPlate.Wells)
                {
                    if (!cachedSeries.ContainsKey(well.Tecc))
                    {
                        continue;
                    }

                    var rs = cachedSeries[well.Tecc];

                    var doubleDataDots = new XyDataSeries<double, double>();
                    var doubleData = new XyDataSeries<double, double>();
                    var doubleDataOver = new XyDataSeries<double, double>();
                    var doubleDataUnder = new XyDataSeries<double, double>();

                    var data = well.Measurements.OrderBy(t => t.TimeMinutes).ToList();

                    var yValues =  (selectedPlate.Platform == 0) ? data.Select(d => d.V).ToArray() : data.Select(d => d.ValueForAuc).ToArray();
                    var xValues = data.Select(d => d.TimeMinutes).ToArray();
                    var isInitial = data.Select(d => d.IsInitial).ToArray();

                    for (int i = 0; i < yValues.Length; i++)
                    {
                        if (yValues[i] > yRange.Max)
                        {
                            doubleDataOver.Append(xValues[i], yRange.Max);
                            yValues[i] = double.NaN;
                        }
                        else if (yValues[i] < yRange.Min)
                        {
                            doubleDataUnder.Append(xValues[i], yRange.Min);
                            yValues[i] = double.NaN;
                        }
                        else
                        {
                            doubleDataUnder.Append(xValues[i], double.NaN);
                            doubleDataOver.Append(xValues[i], double.NaN);
                        }

                        if (isInitial[i])
                        {
                            doubleDataDots.Append(xValues[i], yValues[i]);
                        }
                    }

                    doubleData.Append(xValues, yValues);
                    rs.DataSeries = doubleData;
                    rs.Tag = well;
                 
                    rs.Stroke = MapToPlotColor(well.IsValid);
                    
                    rs.YAxis.VisibleRange = yRange;

                    var rsDots = cachedSeriesDots[well.Tecc];
                    rsDots.DataSeries = doubleDataDots;
                    rsDots.Tag = well;
                }
            }
        }

        private void AutoRangeAll()
        {
            double minY = double.MaxValue;
            double maxY = double.MinValue;

            foreach (var well in selectedPlate.Wells)
            {
                // ReSharper disable once PossibleInvalidOperationException Filtered above
                var dataXy = well.Measurements.OrderBy(t => t.Index).ToList();
                var dataY = dataXy.Select(c => selectedPlate.Platform == 0 ? c.V : c.ValueForAuc).ToArray();

                minY = Math.Min(dataY.Min(), minY);
                maxY = Math.Max(dataY.Max(), maxY);
            }

            Properties.Settings.Default.RangeMin = Math.Floor(minY);
            Properties.Settings.Default.RangeMax = Math.Ceiling(maxY);
        }
        
        private Color MapToPlotColor(bool isValid)
        {
            return (isValid) ? Colors.LawnGreen : Colors.Red;
        }

        private void EventSetter_OnHandler(object sender, RoutedEventArgs e)
        {
            UpdateSelectedPlate();

            if (AutoScaleOnPlateChange.IsChecked == true)
            {
                AutoRangeAll();
            }

            SetupWellFormat();
            BuildWellCharts();
            PopulateSingleWellCharts();
        }

        private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!initialized) return;

            BuildWellCharts();
            PopulateSingleWellCharts();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AutoScaleAll_Click(object sender, RoutedEventArgs e)
        {
            AutoRangeAll();
        }

        private void XAxisAccept_Click(object sender, RoutedEventArgs e)
        {
            MarkX(sender, true);
        }

        private void XAxisReject_Click(object sender, RoutedEventArgs e)
        {
            MarkX(sender, false);
        }

        private void MarkX(object sender, bool state)
        {
            var col = int.Parse((sender as Button).DataContext as string);

            foreach (var well in selectedPlate.Wells)
            {
                var wellCol = int.Parse(well.Col);

                if (wellCol == col)
                {
                    well.IsValid = state;
                }
            }

            PopulateSingleWellCharts();
        }

        private void MarkY(object sender, bool state)
        {
            var row = (sender as Button).DataContext;

            foreach (var well in selectedPlate.Wells)
            {
                if (well.Row.Equals(row))
                {
                    well.IsValid = state;
                }
            }

            PopulateSingleWellCharts();
        }

        private void YAxisAccept_Click(object sender, RoutedEventArgs e)
        {
            MarkY(sender,true);
        }
        
        private void YAxisReject_Click(object sender, RoutedEventArgs e)
        {
            MarkY(sender, false);
        }
    }
}

