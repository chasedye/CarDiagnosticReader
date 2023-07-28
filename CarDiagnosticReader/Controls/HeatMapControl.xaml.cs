using CarDiagnosticReader.ViewModels;
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
using System.Xml.Schema;
using CarDiagnosticReader.XDFFiles;
using CarDiagnosticReader.XMLFiles;

namespace CarDiagnosticReader.Controls
{
    /// <summary>
    /// Interaction logic for HeatMapControl.xaml
    /// </summary>
    /// 

    public partial class HeatMapControl : UserControl
    {
        private int _rowCount = 8;
        public int RowCount
        {
            get => _rowCount;
            set
            {
                _rowCount = value;
                
            }
        }

        private int _columnCount = 8;
        public int ColumnCount
        {
            get => _columnCount;
            set
            {
                _columnCount = value;
                
            }
        }

        private string manager;
        private dynamic tableVariable;
        public HeatMapControl(object tableVariable, LoggerControl loggerControl, string manager)
        {
            this.manager = manager;
            this.tableVariable = tableVariable;
            InitializeComponent();
            HeatMapViewModel viewModel = new HeatMapViewModel(plotview2d, viewport3D, tableVariable, loggerControl, manager);
            DataContext= viewModel;
        }

        private void ToggleButton_Click3D(object sender, RoutedEventArgs e)
        {
            if (viewport3D.Visibility == Visibility.Visible)
                viewport3D.Visibility = Visibility.Collapsed;
            else
                viewport3D.Visibility = Visibility.Visible;
            }
        private void ToggleButton_Click2D(object sender, RoutedEventArgs e)
        {
            if (plotview2d.Visibility == Visibility.Visible)
                plotview2d.Visibility = Visibility.Collapsed;
            else
                plotview2d.Visibility = Visibility.Visible;
        }

        private void SaveTable(object sender, RoutedEventArgs e)
        {
            if(manager == "XDF")
            {
                XDFManager.SaveTableChanges(XDFManager.binFileName, this.tableVariable.title);
            }
            else if(manager == "XML")
            {
                XMLManager.SaveTableChanges(XMLManager.binFileName, this.tableVariable.title);
            }
        }

    }

   
}

    
