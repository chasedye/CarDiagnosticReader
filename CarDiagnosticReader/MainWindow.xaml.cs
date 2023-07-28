using CarDiagnosticReader.Controls;
using CarDiagnosticReader.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace CarDiagnosticReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ShowContent("Default");
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BackendControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void SidebarControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void sidebarControl_Loaded_1(object sender, RoutedEventArgs e)
        {

        }

        public void ShowContent(string tabName)
        {
            switch (tabName)
            {
                case "DataTableList":
                    sidebarControl.SidebarContent.Content = new DataTableListControl(loggerControl);
                    break;
                case "Settings":
                    MainContentArea.Content = new ViewControl();
                    break;
                case "Help":
                    MainContentArea.Content = new HelpControl();
                    break;
                default:
                    sidebarControl.SidebarContent.Content = new DataTableListControl(loggerControl);
                    MainContentArea.Content = new HelpControl();
                    break;
            }
        }
    }
}
