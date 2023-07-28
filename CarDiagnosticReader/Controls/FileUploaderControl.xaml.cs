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

namespace CarDiagnosticReader.Controls
{
    public partial class FileUploaderControl : UserControl
    {
        public FileUploaderControl()
        {
            InitializeComponent();
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "Supported Files (*.bin;*.xdf;*.xml)|*.bin;*.xdf;*.xml|Binary Files (*.bin)|*.bin|XDF Files (*.xdf)|*.xdf|XML Files (*.xml)|*.xml"
            };

            bool? result = openFileDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                // Process the selected files
                foreach (string file in openFileDialog.FileNames)
                {
                    // Your file processing logic here
                }
            }
        }
    }
}
