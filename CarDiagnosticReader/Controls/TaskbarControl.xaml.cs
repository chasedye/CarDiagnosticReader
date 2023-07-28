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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CarDiagnosticReader.Controls
{
    /// <summary>
    /// Interaction logic for TaskbarControl.xaml
    /// </summary>
    public partial class TaskbarControl : UserControl
    {
        public TaskbarControl()
        {
            InitializeComponent();
        }

        private void TaskbarButton_Click(object sender, RoutedEventArgs e)
        {
            // Find the SidebarControl instance
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                var sidebarControl = mainWindow.FindName("sidebarControl") as SidebarControl;

                if (sidebarControl != null)
                {
                    // Access the SidebarControl instance here
                    if (sender is Button button)
                    {
                        mainWindow.ShowContent(button.Tag.ToString());
                    }
                }
            }
            
        }


    }
}
