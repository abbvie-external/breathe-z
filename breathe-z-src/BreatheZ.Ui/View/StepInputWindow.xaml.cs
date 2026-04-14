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

namespace BreatheZ.Ui.View
{
    /// <summary>
    /// Interaction logic for StepInputWindow.xaml
    /// </summary>
    public partial class StepInputWindow : Window
    {
        public bool IsCancelled { get; private set; }

        public StepInputWindow()
        {
            InitializeComponent();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            IsCancelled = true;
            Close();
        }

        private void Done(object sender, RoutedEventArgs e)
        {
            IsCancelled = false;
            Close();
        }
    }
}
