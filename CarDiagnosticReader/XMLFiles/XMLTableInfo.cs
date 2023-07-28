using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarDiagnosticReader.XMLFiles
{
    public class XMLTableInfo
    {
        //general info
        public string title { get; set; }
        public string type { get; set; }
        public int dimension { get; set; }
        public string category { get; set; }
        public string swapxy { get; set; }
        public string level { get; set; }
        public string xORy2D { get; set; }
        public bool edited { get; set; }
        //x axis
        public string xName { get; set; }
        public int xLength { get; set; }
        public string xScaling { get; set; }
        public string xAddress { get; set; }
        public string xFormat { get; set; }
        public bool xFlip { get; set; }
        public bool xStatic { get; set; }
        public float[] xLabels { get; set; }
        public string[] xStringLabels { get; set; }
        //y axis
        public string yName { get; set; }
        public int yLength { get; set; }
        public string yScaling { get; set; }
        public string yAddress { get; set; }
        public string yFormat { get; set; }
        public bool yFlip { get; set; }
        public bool yStatic { get; set; }
        public float[] yLabels { get; set; }
        public string[] yStringLabels { get; set; }
        //actual data
        public string dataLabel { get; set; }
        public string dataScaling { get; set; }
        public string dataAddress { get; set; }
        public string dataFormat { get; set; }
        public float[,] tableData { get; set; }
        public string[,] stringData { get; set; }

        public XMLTableInfo(int Dimension)
        {
            switch(Dimension)
            {
                case 3:
                    dimension = 3;
                    type = "3D";
                    break;
                case 2:
                    dimension = 2;
                    type = "2D";
                    break;
                case 1:
                    dimension = 1;
                    type = "1D";
                    break;
            }
        }
        public XMLTableInfo(string Type)
        {
            switch (Type)
            {
                case "3D":
                    dimension = 3;
                    type = "3D";
                    break;
                case "2D":
                    dimension = 2;
                    type = "2D";
                    break;
                case "1D":
                    dimension = 1;
                    type = "1D";
                    break;
            }
        }
    }
}
