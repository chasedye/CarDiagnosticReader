using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using CarDiagnosticReader.XDFFiles;
using HelixToolkit.Wpf;
using OxyPlot;
using CarDiagnosticReader.Controls;
using CarDiagnosticReader.XMLFiles;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using System.Collections.Generic;
using OxyPlot.Legends;

namespace CarDiagnosticReader.ViewModels
{
    public class HeatMapViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<HeatMapCell> _heatMapData;

        public ObservableCollection<string> XLabels { get; set; }
        public ObservableCollection<string> YLabels { get; set; }
        public ObservableCollection<string> xAxisTitle { get; set; }
        public ObservableCollection<string> yAxisTitle { get; set; }

        private HelixViewport3D viewport3D;
        private bool is3DButtonEnabled;
        private bool is2DButtonEnabled;
        private string manager;

        public dynamic tableVariable { get; set; }

        public ObservableCollection<HeatMapCell> HeatMapData
        {
            get => _heatMapData;
            set
            {
                _heatMapData = value;
                OnPropertyChanged();
            }
        }

        private int _rowCount = 8;
        public int RowCount
        {
            get => _rowCount;
            set
            {
                _rowCount = value;
                OnPropertyChanged();
            }
        }

        private int _columnCount = 8;
        public int ColumnCount
        {
            get => _columnCount;
            set
            {
                _columnCount = value;
                OnPropertyChanged();
            }
        }

        public bool Is3DButtonEnabled
        {
            get { return is3DButtonEnabled; }
            set
            {
                if (is3DButtonEnabled != value)
                {
                    is3DButtonEnabled = value;
                    OnPropertyChanged(nameof(Is3DButtonEnabled));
                }
            }
        }
        public bool Is2DButtonEnabled
        {
            get { return is2DButtonEnabled; }
            set
            {
                if (is2DButtonEnabled != value)
                {
                    is2DButtonEnabled = value;
                    OnPropertyChanged(nameof(Is2DButtonEnabled));
                }
            }
        }

        public HeatMapViewModel(PlotView plotview2D, HelixViewport3D viewport3D, object tableVariable, LoggerControl loggerControl, string manager)
        {
            if (tableVariable is XDFTable)
            {
                this.manager = "XDF";
                XDFTable xdfTableVariable = (XDFTable)tableVariable;
                loggerControl.AddLogMessage("Getting HeatMapviewModel of " + xdfTableVariable.title);

                this.tableVariable = (XDFTable)tableVariable;

                this.viewport3D = viewport3D;

                string[] tempXLabel = xdfTableVariable.xLabels.Split(',');
                string[] tempYLabel = xdfTableVariable.yLabels.Split(',');


                XLabels = new ObservableCollection<string> { };
                YLabels = new ObservableCollection<string> { };
                xAxisTitle = new ObservableCollection<string> { };
                yAxisTitle= new ObservableCollection<string> { };

                foreach (var label in tempXLabel)
                {
                    XLabels.Add(label);
                }
                foreach (var label in tempYLabel)
                {
                    YLabels.Add(label);
                }
                Debug.WriteLine("HEATMAP: " + xdfTableVariable.title);
                this.ColumnCount = xdfTableVariable.tableData.GetLength(1);
                this.RowCount = xdfTableVariable.tableData.GetLength(0);
                LoadSampleData(xdfTableVariable.tableData, xdfTableVariable.tableData.Cast<float>().Min(), xdfTableVariable.tableData.Cast<float>().Max());

                if (ColumnCount > 1 && RowCount > 1)
                {
                    // Access the HeatmapViewModel instance and update the IsButtonEnabled property
                    this.Is3DButtonEnabled = true;
                    this.is2DButtonEnabled = false;

                    createXDF3DGraph(xdfTableVariable.tableData, xdfTableVariable.tableData.Cast<float>().Min(), xdfTableVariable.tableData.Cast<float>().Max(), xdfTableVariable);
                    xAxisTitle.Add(xdfTableVariable.xUnits);
                    yAxisTitle.Add(xdfTableVariable.yUnits);
                }
                else
                {
                    this.Is2DButtonEnabled = true;
                    this.is3DButtonEnabled = false;

                    //2d graph implementation
                    createXDF2DGraph(plotview2D, xdfTableVariable);
                    xAxisTitle.Add(xdfTableVariable.xUnits);
                    yAxisTitle.Add(xdfTableVariable.yUnits);
                }
            }
            else if (tableVariable is XMLTableInfo)
            {
                this.manager = "XML";
                XMLTableInfo xmlTableVariable = (XMLTableInfo)tableVariable;
                loggerControl.AddLogMessage("Getting HeatMapviewModel of " + xmlTableVariable.title);

                this.tableVariable = (XMLTableInfo)tableVariable;
                this.viewport3D = viewport3D;

                this.Is3DButtonEnabled = false;

                XLabels = new ObservableCollection<string> { };
                YLabels = new ObservableCollection<string> { };
                xAxisTitle = new ObservableCollection<string> { };
                yAxisTitle = new ObservableCollection<string> { };

                if (xmlTableVariable.yStatic && xmlTableVariable.xStatic)
                {
                   
                    foreach (var label in xmlTableVariable.xStringLabels)
                    {
                        XLabels.Add(label);
                    }
                    foreach (var label in xmlTableVariable.yStringLabels)
                    {
                        YLabels.Add(label);
                    }
                }
                else if (xmlTableVariable.yStatic && !xmlTableVariable.xStatic)
                {
                    
                    foreach (var label in xmlTableVariable.yStringLabels)
                    {
                        YLabels.Add(label);
                    }
                }
                else if (!xmlTableVariable.yStatic && xmlTableVariable.xStatic)
                {
                  
                    foreach (var label in xmlTableVariable.xStringLabels)
                    {
                        XLabels.Add(label);
                    }
                }
                else
                {
                 
                    if (xmlTableVariable.xLabels != null)
                    {
                        foreach (var label in xmlTableVariable.xLabels)
                        {
                            XLabels.Add(label.ToString("F2"));
                        }
                    }
                    if (xmlTableVariable.yLabels != null)
                    {
                        foreach (var label in xmlTableVariable.yLabels)
                        {
                            YLabels.Add(label.ToString("F2"));
                        }
                    }
                }

                this.ColumnCount = xmlTableVariable.tableData.GetLength(1);
                this.RowCount = xmlTableVariable.tableData.GetLength(0);
                LoadSampleData(xmlTableVariable.tableData, xmlTableVariable.tableData.Cast<float>().Min(), xmlTableVariable.tableData.Cast<float>().Max());

                if (xmlTableVariable.dimension == 3)
                {
                    // Access the HeatmapViewModel instance and update the IsButtonEnabled property
                    this.Is3DButtonEnabled = true;
                    this.is2DButtonEnabled = false;

                    if (xmlTableVariable.xName != null)
                    {
                        xAxisTitle.Add(xmlTableVariable.xName);
                    }
                    if (xmlTableVariable.yName != null)
                    {
                        yAxisTitle.Add(xmlTableVariable.yName);
                    }

                    createXML3DGraph(xmlTableVariable.tableData, xmlTableVariable.tableData.Cast<float>().Min(), xmlTableVariable.tableData.Cast<float>().Max(), xmlTableVariable);
                }
                else if (xmlTableVariable.dimension == 2)
                {
                    this.Is2DButtonEnabled = true;
                    this.is3DButtonEnabled = false;

                    // 2D graph implementation
                    if (xmlTableVariable.xName != null)
                    {
                        xAxisTitle.Add(xmlTableVariable.xName);
                    }
                    if (xmlTableVariable.yName != null)
                    {
                        yAxisTitle.Add(xmlTableVariable.yName);
                    }

                    createXML2DGraph(plotview2D, xmlTableVariable);

                }
                else
                {
                    if (xmlTableVariable.xName != null)
                    {
                        xAxisTitle.Add(xmlTableVariable.xName);
                    }
                    if (xmlTableVariable.yName != null)
                    {
                        yAxisTitle.Add(xmlTableVariable.yName);
                    }

                    this.is2DButtonEnabled = false;
                    this.is3DButtonEnabled = false;
                }
            }
        }

        private void LoadSampleData(float[,] matrix, float relativeMin, float relativeMax)
        {
            // Create an ObservableCollection of HeatMapCell
            HeatMapData = new ObservableCollection<HeatMapCell>();

            double cellWidth = 600.0 / ColumnCount;
            double cellHeight = 600.0 / RowCount;

            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    float relativeValue = (matrix[i, j] - relativeMin) / (relativeMax - relativeMin);
                    byte red = (byte)(relativeValue * 255);
                    byte green = (byte)((1 - relativeValue) * 255);
                    Color cellColor = Color.FromRgb(red, green, 0);

                    var cell = new HeatMapCell { Value = matrix[i, j], Width = cellWidth, Height = cellHeight, CellColor = cellColor, ColCount = ColumnCount, RowCount = RowCount, RowIndex = i, ColIndex = j };
                    cell.ValueChanged += (s, e) => UpdateCell(cell, relativeMin, relativeMax); // Subscribing to ValueChanged event

                    HeatMapData.Add(cell);
                }
            }
        }

        private void createXDF3DGraph(float[,] matrix, float relativeMin, float relativeMax, XDFTable tableVariable)
        {
            int rows = matrix.GetLength(0);
            int columns = matrix.GetLength(1);

            string[] XLabels = tableVariable.xLabels.Split(',');
            string[] YLabels = tableVariable.yLabels.Split(',');
            string[] ZLabels = CreateZLabels(matrix);

            PrintMatrix(matrix);

            System.Windows.Media.Media3D.ModelVisual3D labelVisual = new System.Windows.Media.Media3D.ModelVisual3D();

            float zOffset = relativeMin;

            // Calculate the scaling factor for the z-axis
            double zStep = (relativeMax - relativeMin) / (ZLabels.Length - 1);
            double newMax = ZLabels.Length;

            // Create data points and add them to the mesh builder
            for (int row = 0; row < rows - 1; row++)
            {
                for (int col = 0; col < columns - 1; col++)
                {
                    // Create a mesh builder for this cell
                    HelixToolkit.Wpf.MeshBuilder meshBuilder = new HelixToolkit.Wpf.MeshBuilder();

                    double z = matrix[row, col]; // Z coordinate
                    double relativeValue = (z - relativeMin) / (relativeMax - relativeMin);
                    byte red = (byte)(relativeValue * 255);
                    byte green = (byte)((1 - relativeValue) * 255);
                    Color cellColor = Color.FromRgb(red, green, 0);

                    double z2 = matrix[row + 1, col]; // Z2 coordinate
                    double relativeValue2 = (z2 - relativeMin) / (relativeMax - relativeMin);
                    double z3 = matrix[row + 1, col + 1]; // Z3 coordinate
                    double relativeValue3 = (z3 - relativeMin) / (relativeMax - relativeMin);
                    double z4 = matrix[row, col + 1]; // Z4 coordinate
                    double relativeValue4 = (z4 - relativeMin) / (relativeMax - relativeMin);

                    double newZ = relativeValue * newMax;
                    double newZ1 = relativeValue2 * newMax;
                    double newZ2 = relativeValue3 * newMax;
                    double newZ3 = relativeValue4 * newMax;

                    // Add a quad to the mesh builder
                    meshBuilder.AddQuad(
                            new Point3D(row, col, newZ + zOffset),
                            new Point3D(row + 1, col, newZ1 + zOffset),
                            new Point3D(row + 1, col + 1, newZ2 + zOffset),
                            new Point3D(row, col + +1, newZ3 + zOffset)
                    );

                    // Create a material with the cell color
                    var material = new System.Windows.Media.Media3D.DiffuseMaterial(new SolidColorBrush(cellColor));

                    // Create a geometry model with the mesh and material
                    System.Windows.Media.Media3D.GeometryModel3D geometryModel = new System.Windows.Media.Media3D.GeometryModel3D(meshBuilder.ToMesh(), material)
                    {
                        BackMaterial = material
                    };

                    // Create a model visual and add it to the viewport
                    System.Windows.Media.Media3D.ModelVisual3D modelVisual = new System.Windows.Media.Media3D.ModelVisual3D { Content = geometryModel };

                    viewport3D.Children.Add(modelVisual);
                }
            }

            if (Properties.Settings.Default.DataLabels)
            {

            // Create text labels for the axes
            for (int i = 0; i < columns; i++)
            {
                var xAxisLabel = new BillboardTextVisual3D
                {
                    Text = XLabels[i],
                    Position = new Point3D(-.5, i, -1 + zOffset),
                    Height = 10, // Adjust the size of the labels as desired
                    Foreground = Brushes.Black,
                };

                labelVisual.Children.Add(xAxisLabel);
            }

            var xTitleLabel = new BillboardTextVisual3D
            {
                Text = tableVariable.xUnits,
                Position = new Point3D(-2, columns / 2, -2 + zOffset), // Set the position of the label
                FontSize = 10,

            };
            labelVisual.Children.Add(xTitleLabel);

            for (int i = 0; i < rows; i++)
            {
                var yAxisLabel = new BillboardTextVisual3D
                {
                    Text = YLabels[i],
                    Position = new Point3D(i, -.5, -1 + zOffset),
                    Height = 10, // Adjust the size of the labels as desired
                    Foreground = Brushes.Black,

                };

                labelVisual.Children.Add(yAxisLabel);
            }

            var yTitleLabel = new BillboardTextVisual3D
            {
                Text = tableVariable.yUnits,
                Position = new Point3D(rows / 2, -2, -2 + zOffset), // Set the position of the label
                FontSize = 10,

            };
            labelVisual.Children.Add(yTitleLabel);

            double zNum = 0;
            for (int i = 0; i < ZLabels.Length; i++)
            {
                double z = i * zStep + zOffset;
                double relativeValue = (z - relativeMin) / (relativeMax - relativeMin);

                double newZ = relativeValue * newMax;
                zNum = newZ;

                double tmp = double.Parse(ZLabels[i]);

                var zAxisLabel = new BillboardTextVisual3D
                {
                    Text = tmp.ToString("0.0"),
                    Position = new Point3D(-.5, -0.5, newZ + zOffset),
                    Height = 10, // Adjust the size of the labels as desired
                    Foreground = Brushes.Black
                };

                labelVisual.Children.Add(zAxisLabel);
            }

            var zTitleLabel = new BillboardTextVisual3D
            {
                Text = tableVariable.zUnits,
                Position = new Point3D(-2, -2, (zNum + zOffset) / 2), // Set the position of the label
                FontSize = 10,

            };
            labelVisual.Children.Add(zTitleLabel);

            var titleLabel = new BillboardTextVisual3D
            {
                Text = tableVariable.title,
                Position = new Point3D(-5, -5, (zNum + zOffset + 5)), // Set the position of the label
                FontSize = 14,

            };
            labelVisual.Children.Add(titleLabel);

            }
            // Create a mesh builder
            HelixToolkit.Wpf.MeshBuilder meshBuilderLabels = new HelixToolkit.Wpf.MeshBuilder(false, true); // false for creating normals, true for creating texture coordinates.

            // Create a mesh geometry
            System.Windows.Media.Media3D.MeshGeometry3D meshGeometry = new System.Windows.Media.Media3D.MeshGeometry3D();

            // Add vertices to the mesh geometry
            meshGeometry.Positions = new Point3DCollection(meshBuilderLabels.Positions);

            // Add triangles to the mesh geometry
            foreach (var triangleIndices in meshBuilderLabels.TriangleIndices)
            {
                meshGeometry.TriangleIndices.Add(triangleIndices);
            }

            // Create a WriteableBitmap for the color scale
            WriteableBitmap bitmap = new WriteableBitmap(256, 1, 96, 96, PixelFormats.Rgb24, null);
            byte[] pixels = new byte[256 * 3];
            for (int i = 0; i < 256; i++)
            {
                pixels[i * 3] = (byte)i; // Red
                pixels[i * 3 + 1] = (byte)(255 - i); // Green
                pixels[i * 3 + 2] = 0; // Blue
            }
            bitmap.WritePixels(new Int32Rect(0, 0, 256, 1), pixels, 256 * 3, 0);

            // Create an ImageBrush from the WriteableBitmap
            ImageBrush brush = new ImageBrush(bitmap);

            // Create a material from the ImageBrush
            System.Windows.Media.Media3D.Material materialLabel = new System.Windows.Media.Media3D.DiffuseMaterial(brush);

            // Use the material for your GeometryModel3D
            System.Windows.Media.Media3D.GeometryModel3D dataModel = new System.Windows.Media.Media3D.GeometryModel3D(meshGeometry, materialLabel)
            {
                BackMaterial = materialLabel,
            };

            // Create a model visual for the data model
            ModelVisual3D dataVisual = new ModelVisual3D { Content = dataModel };

            // Add the data visual to the viewport
            viewport3D.Children.Add(dataVisual);

            viewport3D.Children.Add(labelVisual);
            // Create an ambient light
            var ambientLight = new AmbientLight(Colors.White);

            if (Properties.Settings.Default.Gridlines)
            {
                // Create gridlines
                ModelVisual3D gridlinesVisual = CreateGridlines(matrix, rows, columns, Colors.Gray, zOffset);

                // Add the gridlines visual to the viewport
                viewport3D.Children.Add(gridlinesVisual);
            }

            ModelVisual3D modelVisualLabels = new ModelVisual3D
            {
                Content = new Model3DGroup
                {
                    Children = { ambientLight, dataModel }
                }
            };

            viewport3D.Children.Add(modelVisualLabels);
        }

        private void createXML3DGraph(float[,] matrix, float relativeMin, float relativeMax, XMLTableInfo tableVariable)
        {
            int rows = matrix.GetLength(0);
            int columns = matrix.GetLength(1);

            if (tableVariable.yStatic || tableVariable.xStatic)
            {
                foreach (var label in tableVariable.xStringLabels)
                {
                    XLabels.Add(label);
                }
                foreach (var label in tableVariable.yStringLabels)
                {
                    YLabels.Add(label);
                }
            }
            else 
            {
                foreach (var label in tableVariable.xLabels)
                {
                    XLabels.Add(label.ToString("F2"));
                }
                foreach (var label in tableVariable.xLabels)
                {
                    YLabels.Add(label.ToString("F2"));
                }
            }

            string[] ZLabels = CreateZLabels(matrix);

            PrintMatrix(matrix);

            System.Windows.Media.Media3D.ModelVisual3D labelVisual = new System.Windows.Media.Media3D.ModelVisual3D();

            float zOffset = relativeMin;

            // Calculate the scaling factor for the z-axis
            double zStep = (relativeMax - relativeMin) / (ZLabels.Length - 1);
            double newMax = ZLabels.Length;

            // Create data points and add them to the mesh builder
            for (int row = 0; row < rows - 1; row++)
            {
                for (int col = 0; col < columns - 1; col++)
                {
                    // Create a mesh builder for this cell
                    HelixToolkit.Wpf.MeshBuilder meshBuilder = new HelixToolkit.Wpf.MeshBuilder();

                    double z = matrix[row, col]; // Z coordinate
                    double relativeValue = (z - relativeMin) / (relativeMax - relativeMin);
                    byte red = (byte)(relativeValue * 255);
                    byte green = (byte)((1 - relativeValue) * 255);
                    Color cellColor = Color.FromRgb(red, green, 0);

                    double z2 = matrix[row + 1, col]; // Z2 coordinate
                    double relativeValue2 = (z2 - relativeMin) / (relativeMax - relativeMin);
                    double z3 = matrix[row + 1, col + 1]; // Z3 coordinate
                    double relativeValue3 = (z3 - relativeMin) / (relativeMax - relativeMin);
                    double z4 = matrix[row, col + 1]; // Z4 coordinate
                    double relativeValue4 = (z4 - relativeMin) / (relativeMax - relativeMin);

                    double newZ = relativeValue * newMax;
                    double newZ1 = relativeValue2 * newMax;
                    double newZ2 = relativeValue3 * newMax;
                    double newZ3 = relativeValue4 * newMax;

                    // Add a quad to the mesh builder
                    meshBuilder.AddQuad(
                        new Point3D(row, col, newZ + zOffset),
                        new Point3D(row + 1, col, newZ1 + zOffset),
                        new Point3D(row + 1, col + 1, newZ2 + zOffset),
                        new Point3D(row, col + +1, newZ3 + zOffset)
                    );

                    // Create a material with the cell color
                    var material = new System.Windows.Media.Media3D.DiffuseMaterial(new SolidColorBrush(cellColor));

                    // Create a geometry model with the mesh and material
                    System.Windows.Media.Media3D.GeometryModel3D geometryModel = new System.Windows.Media.Media3D.GeometryModel3D(meshBuilder.ToMesh(), material)
                    {
                        BackMaterial = material
                    };

                    // Create a model visual and add it to the viewport
                    System.Windows.Media.Media3D.ModelVisual3D modelVisual = new System.Windows.Media.Media3D.ModelVisual3D { Content = geometryModel };

                    viewport3D.Children.Add(modelVisual);
                }
            }

            if (Properties.Settings.Default.DataLabels)
            {


                // Create text labels for the axes
                for (int i = 0; i < columns; i++)
            {
                var xAxisLabel = new BillboardTextVisual3D
                {
                    Text = XLabels[i],
                    Position = new Point3D(-.5, i, -1 + zOffset),
                    Height = 10, // Adjust the size of the labels as desired
                    Foreground = Brushes.Black,

                };

                labelVisual.Children.Add(xAxisLabel);
            }

            var xTitleLabel = new BillboardTextVisual3D
            {
                Text = tableVariable.xName,
                Position = new Point3D(-2, columns / 2, -2 + zOffset), // Set the position of the label
                FontSize = 10,

            };
            labelVisual.Children.Add(xTitleLabel);

            for (int i = 0; i < rows; i++)
            {
                var yAxisLabel = new BillboardTextVisual3D
                {
                    Text = YLabels[i],
                    Position = new Point3D(i, -.5, -1 + zOffset),
                    Height = 10, // Adjust the size of the labels as desired
                    Foreground = Brushes.Black,

                };

                labelVisual.Children.Add(yAxisLabel);
            }

            var yTitleLabel = new BillboardTextVisual3D
            {
                Text = tableVariable.yName,
                Position = new Point3D(rows / 2, -2, -2 + zOffset), // Set the position of the label
                FontSize = 10,

            };
            labelVisual.Children.Add(yTitleLabel);


            double zNum = 0;
            for (int i = 0; i < ZLabels.Length; i++)
            {
                double z = i * zStep + zOffset;
                double relativeValue = (z - relativeMin) / (relativeMax - relativeMin);

                double newZ = relativeValue * newMax;
                zNum = newZ;

                double tmp = double.Parse(ZLabels[i]);

                var zAxisLabel = new BillboardTextVisual3D
                {
                    Text = tmp.ToString("0.0"),
                    Position = new Point3D(-.5, -0.5, newZ + zOffset),
                    Height = 10, // Adjust the size of the labels as desired
                    Foreground = Brushes.Black
                };

                labelVisual.Children.Add(zAxisLabel);
            }

            var zTitleLabel = new BillboardTextVisual3D
            {
                Text = tableVariable.dataLabel,
                Position = new Point3D(-2, -2, (zNum + zOffset) / 2), // Set the position of the label
                FontSize = 10,

            };
            labelVisual.Children.Add(zTitleLabel);

            var titleLabel = new BillboardTextVisual3D
            {
                Text = tableVariable.title,
                Position = new Point3D(-5, -5, (zNum + zOffset + 5)), // Set the position of the label
                FontSize = 14,

            };
            labelVisual.Children.Add(titleLabel);

            }
            // Create a mesh builder
            HelixToolkit.Wpf.MeshBuilder meshBuilderLabels = new HelixToolkit.Wpf.MeshBuilder(false, true); // false for creating normals, true for creating texture coordinates.

            // Create a mesh geometry
            System.Windows.Media.Media3D.MeshGeometry3D meshGeometry = new System.Windows.Media.Media3D.MeshGeometry3D();

            // Add vertices to the mesh geometry
            meshGeometry.Positions = new Point3DCollection(meshBuilderLabels.Positions);

            // Add triangles to the mesh geometry
            foreach (var triangleIndices in meshBuilderLabels.TriangleIndices)
            {
                meshGeometry.TriangleIndices.Add(triangleIndices);
            }

            // Create a WriteableBitmap for the color scale
            WriteableBitmap bitmap = new WriteableBitmap(256, 1, 96, 96, PixelFormats.Rgb24, null);
            byte[] pixels = new byte[256 * 3];
            for (int i = 0; i < 256; i++)
            {
                pixels[i * 3] = (byte)i; // Red
                pixels[i * 3 + 1] = (byte)(255 - i); // Green
                pixels[i * 3 + 2] = 0; // Blue
            }
            bitmap.WritePixels(new Int32Rect(0, 0, 256, 1), pixels, 256 * 3, 0);

            // Create an ImageBrush from the WriteableBitmap
            ImageBrush brush = new ImageBrush(bitmap);

            // Create a material from the ImageBrush
            System.Windows.Media.Media3D.Material materialLabel = new System.Windows.Media.Media3D.DiffuseMaterial(brush);

            // Use the material for your GeometryModel3D
            System.Windows.Media.Media3D.GeometryModel3D dataModel = new System.Windows.Media.Media3D.GeometryModel3D(meshGeometry, materialLabel)
            {
                BackMaterial = materialLabel,
            };

            // Create a model visual for the data model
            ModelVisual3D dataVisual = new ModelVisual3D { Content = dataModel };

            // Add the data visual to the viewport
            viewport3D.Children.Add(dataVisual);

            viewport3D.Children.Add(labelVisual);
            // Create an ambient light
            var ambientLight = new AmbientLight(Colors.White);

            if (Properties.Settings.Default.Gridlines)
            {

            // Create gridlines
            ModelVisual3D gridlinesVisual = CreateGridlines(matrix, rows, columns, Colors.Gray, zOffset);

            // Add the gridlines visual to the viewport
            viewport3D.Children.Add(gridlinesVisual);
            }

            ModelVisual3D modelVisualLabels = new ModelVisual3D
            {
                Content = new Model3DGroup
                {
                    Children = { ambientLight, dataModel }
                }
            };

            viewport3D.Children.Add(modelVisualLabels);
        }

        private void createXML2DGraph(PlotView plotView2D, XMLTableInfo xmlTableVariable)
        {

            int rows = xmlTableVariable.tableData.GetLength(0);
            int columns = xmlTableVariable.tableData.GetLength(1);

            // Create the plot model
            var plotModel = new PlotModel
            {
                Title = xmlTableVariable.title
            };

            Debug.WriteLine("2D GRAPH MATRIX");
            PrintMatrix(xmlTableVariable.tableData);

            // Create the X and Y axes
            var xAxis = new LinearAxis
            {
                Title = xmlTableVariable.yName,
                Position = AxisPosition.Bottom // Place X axis at the bottom of the plot area
            };

            var yAxis = new LinearAxis
            {
                Title = xmlTableVariable.dataLabel,
                Position = AxisPosition.Left // Place Y axis at the left side of the plot area
            };

            // Add the axes to the plot model
            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);

            LineSeries lineSeries = new LineSeries
            {
                MarkerType = MarkerType.Circle
            };
            for (int row = 0; row < rows; row++)
            {

                for (int col = 0; col < columns; col++)
                {
                    double x = 0;
                    double y = 0;

                    if (xmlTableVariable.yLabels != null)
                    {
                        x = xmlTableVariable.yLabels[row];
                        y = xmlTableVariable.tableData[row, col];

                    }
                    else if (xmlTableVariable.xLabels != null)
                    {
                        x = xmlTableVariable.xLabels[col];
                        y = xmlTableVariable.tableData[row, col];
                    }

                    // Add data points to the line series
                    lineSeries.Points.Add(new DataPoint(x, y));
                }
            }




            // Add the line series to the plot model
            plotModel.Series.Add(lineSeries);

            // Add the plot model to the plotview
            plotView2D.Model = plotModel;

        }

        private void createXDF2DGraph(PlotView plotview2D, XDFTable xdfTableVariable)
        {

            int rows = xdfTableVariable.tableData.GetLength(0);
            int columns = xdfTableVariable.tableData.GetLength(1);

            // Create the plot model
            var plotModel = new PlotModel
            {
                Title = xdfTableVariable.title
            };

            Debug.WriteLine("2D GRAPH MATRIX");
            PrintMatrix(xdfTableVariable.tableData);

            // Create the X and Y axes
            var xAxis = new LinearAxis
            {
                Title = xdfTableVariable.yUnits,
                Position = AxisPosition.Bottom // Place X axis at the bottom of the plot area
            };

            var yAxis = new LinearAxis
            {
                Title = xdfTableVariable.xUnits,
                Position = AxisPosition.Left // Place Y axis at the left side of the plot area
            };

            // Add the axes to the plot model
            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);

            LineSeries lineSeries = new LineSeries
            {
                MarkerType = MarkerType.Circle
            };
            
            string[] XLabels = xdfTableVariable.xLabels.Split(',');
            string[] YLabels = xdfTableVariable.yLabels.Split(',');

            for (int row = 0; row < rows; row++)
            {

                for (int col = 0; col < columns; col++)
                {
                    double x = 0;
                    double y = 0;

                    if (xdfTableVariable.yLabels.Length > 1)
                    {

                        x = Double.Parse(YLabels[row]);
                        y = xdfTableVariable.tableData[row, col];
                    }

                    // Add data points to the line series
                    lineSeries.Points.Add(new DataPoint(x, y));
                }
            }


            // Add the line series to the plot model
            plotModel.Series.Add(lineSeries);

            // Add the plot model to the plotview
            plotview2D.Model = plotModel;

        }


        private ModelVisual3D CreateGridlines(float[,] matrix, int rows, int columns, Color color, float zOffset)
        {
            var gridlineBuilder = new HelixToolkit.Wpf.MeshBuilder();

            // Define the gridline thickness
            double gridlineThickness = 0.03;

          
            for (int row = 0; row <= rows; row++)
            {
                
                gridlineBuilder.AddCylinder(
                    new Point3D(row - 0.5, -0.5, zOffset - 0.5),
                    new Point3D(row - 0.5, -0.5, zOffset - 0.5 - gridlineThickness),
                    gridlineThickness,
                    10
                );

                // Add the line to the axis
                gridlineBuilder.AddCylinder(
                    new Point3D(row - 0.5, -0.5, zOffset - 0.5),
                    new Point3D(row - 0.5, columns - 0.5, zOffset - 0.5),
                    gridlineThickness,
                    10
                );
            }

            
            for (int col = 0; col <= columns; col++)
            {
                
            gridlineBuilder.AddCylinder(
                    new Point3D(-0.5, col - 0.5, zOffset - 0.5),
                    new Point3D(-0.5, col - 0.5, zOffset - 0.5 - gridlineThickness),
                gridlineThickness,
                10
            );

                // Add the line to the axis
            gridlineBuilder.AddCylinder(
                    new Point3D(-0.5, col - 0.5, zOffset - 0.5),
                    new Point3D(rows - 0.5, col - 0.5, zOffset - 0.5),
                gridlineThickness,
                10
            );
            }

            // Add the z-axis line
            gridlineBuilder.AddCylinder(
                new Point3D(-0.5, -0.5, zOffset - .5),
                new Point3D(-0.5, -0.5, zOffset + columns - .5),
                gridlineThickness,
                10
            );

            // Create a mesh geometry for gridlines
            var gridlineGeometry = new MeshGeometry3D
            {
                Positions = new Point3DCollection(gridlineBuilder.Positions),
                TriangleIndices = new Int32Collection(gridlineBuilder.TriangleIndices)
            };

            // Create a model for gridlines
            var gridlineModel = new GeometryModel3D(gridlineGeometry, new DiffuseMaterial(new SolidColorBrush(color)));

            // Create a model visual for the gridline model
            var gridlineVisual = new ModelVisual3D { Content = gridlineModel };

            return gridlineVisual;
        }

        private string[] CreateZLabels(float[,] matrix)
        {
            int rowCount = matrix.GetLength(0);
            int columnCount = matrix.GetLength(1);

            float minValue = float.MaxValue;
            float maxValue = float.MinValue;

            int zLabelCount = (rowCount + columnCount) / 2;

            // Find the minimum and maximum values in the matrix
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < columnCount; j++)
                {
                    float value = matrix[i, j];
                    minValue = Math.Min(minValue, value);
                    maxValue = Math.Max(maxValue, value);
                }
            }

            // Calculate the interval between labels
            float interval = (maxValue - minValue) / (zLabelCount - 1);

            string[] zLabels = new string[zLabelCount];

            // Generate the labels using the interval
            for (int i = 0; i < zLabelCount; i++)
            {
                float value = minValue + i * interval;
                zLabels[i] = value.ToString();

            }

            return zLabels;
        }

        private void PrintMatrix(float[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int columns = matrix.GetLength(1);

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    Debug.Write(matrix[row, col] + "\t");
                }
                Debug.WriteLine("");
            }
        }

        private void UpdateCell(HeatMapCell cell, float relativeMin, float relativeMax)
        {
            float relativeValue = (cell.Value - relativeMin) / (relativeMax - relativeMin);
            byte red = (byte)(relativeValue * 255);
            byte green = (byte)((1 - relativeValue) * 255);
            cell.CellColor = Color.FromRgb(red, green, 0);

            // TODO Update Cell with XMLManager/XDFManager 
            this.tableVariable.tableData[cell.RowIndex, cell.ColIndex] = cell.Value;
            Debug.WriteLine("updated", cell.RowIndex, cell.ColIndex);

            if (this.tableVariable is XDFTable)
            {
                XDFManager.EditTableData(XDFManager.binFileName, this.tableVariable.title, cell.ColIndex, cell.RowIndex, cell.Value);
            }
            else
            {
                XMLManager.EditTableData(XMLManager.binFileName, this.tableVariable.title, cell.ColIndex, cell.RowIndex, cell.Value);
            }

        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class HeatMapCell : INotifyPropertyChanged
    {
        private float _value;
        public double Width { get; set; }
        public double Height { get; set; }
        private Color _cellColor;

        public int RowCount { get; set; }
        public int ColCount { get; set; }

        public int RowIndex { get; set; }
        public int ColIndex { get; set; }

        public float Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged();
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public Color CellColor
        {
            get => _cellColor;
            set
            {
                if (_cellColor != value)
                {
                    _cellColor = value;
                    OnPropertyChanged();
                }
            }
        }

        // Other properties...

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler ValueChanged; // New event for Value changes

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
