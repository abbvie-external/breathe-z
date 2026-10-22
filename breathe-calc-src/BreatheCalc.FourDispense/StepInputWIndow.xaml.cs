using System;
using System.Collections.Generic;
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
using AutomationEngineering.CommonTools.Gui;
using BreatheCalc.FourDispense.Properties;

namespace BreatheCalc.FourDispense
{
    /// <summary>
    /// Interaction logic for StepInputWIndow.xaml
    /// </summary>
    public partial class StepInputWindow : Window
    {
        public bool IsCancelled { get; private set; }
        public int MaxPointCount { get; set; }

        public int UsedPoints
        {
            get
            {
                var pts = Settings.Default.Forksolin + Settings.Default.Baseline + Settings.Default.Bumetinide +
                          Settings.Default.Potentiator + Settings.Default.Benzamil;

                return pts;
            }
        }

        public StepInputWindow()
        {
            InitializeComponent();
        }

        public StepInputWindow(int maxPointCount)
        {
            InitializeComponent();
            MaxPointCount = maxPointCount;
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            IsCancelled = true;
            Close();
        }

        private void Done(object sender, RoutedEventArgs e)
        {
            if (UsedPoints > MaxPointCount)
            {
                MessageBox.Show(this, $"Specified {UsedPoints} points when this file only\ncontains data for {MaxPointCount}","BREATHE Data Cruncher");
                return;
            }

            IsCancelled = false;
            Close();
        }

        private void Points_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (maxPts != null)
            {
                UpdateCount();
            }
        }

        private void UpdateCount()
        {
            maxPts.Content = $"You have specified {UsedPoints} out of {MaxPointCount} points in data file";
            maxPts.Foreground = (UsedPoints > MaxPointCount) ? Brushes.Red : Brushes.Green;
        }

        private void StepInputWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateCount();
        }
    }
}
