using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BreatheZ.Ui.ViewModel;

namespace BreatheZ.Ui.View
{
    /// <summary>
    /// Interaction logic for TextInput.xaml
    /// </summary>
    public partial class TextInputView : Window
    {

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private TextInputModel model;

        public TextInputModel Model
        {
            get { return model; }
            set
            {
                model = value;
                DataContext = model;
            }
        }

        public TextInputView()
        {
            SourceInitialized += Window_SourceInitialized;
            InitializeComponent();

            Dispatcher.BeginInvoke(new Action(
                () =>
                {
                    if (text.Text != null)
                    {
                        text.CaretIndex = text.Text.Length;
                    }

                }), null);
        }

        public static TextInputModel Show(Window owner, string prompt, string oldText)
        {
            var model = new TextInputModel
            {
                Caption = prompt,
                Text = oldText,
            };

            var window = new TextInputView
            {
                Owner = owner,
                Model = model,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            window.ShowDialog();

            return model;
        }

        private void Done(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            model.IsCancelled = true;
            Close();
        }


        void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }

    }
}