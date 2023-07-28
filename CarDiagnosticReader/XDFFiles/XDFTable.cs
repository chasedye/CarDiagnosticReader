using System;
using System.Diagnostics;
using System.IO;
using System.Data;

namespace CarDiagnosticReader.XDFFiles
{
    public class XDFTable
    {
        #region Variables
        //attributes from XDF file
        public string uniqueID { get; set; }
        public string title { get; set; }
        public string descSize { get; set; }
        public string desc { get; set; }
        public string address { get; set; }
        public string zEq { get; set; }
        public string xOutType { get; set; }
        public string yOutType { get; set; }
        public string rangeLow { get; set; }
        public string rangeHigh { get; set; }
        public string rows { get; set; }
        public string cols { get; set; }
        public string xUnits { get; set; }
        public string yUnits { get; set; }
        public string zUnits { get; set; }
        public string xLabels { get; set; }
        public string xLabelType { get; set; }
        public string xEq { get; set; }
        public string yLabels { get; set; }
        public string yLabelType { get; set; }
        public string yEq { get; set; }
        public string xAxisMin { get; set; }
        public string xAxisMax { get; set; }
        public string yAxisMin { get; set; }
        public string yAxisMax { get; set; }
        //helper vars
        public float[,] tableData { get; set; }
        public bool edited { get; set; } //true means tableData has been edited and not saved yet. this gets changed back to false when changes are saved and written to file
        #endregion

        #region Constructors
        public XDFTable(string uniqueID, string title, string descSize, string desc, string address, string zEq, string xOutType, string yOutType, string rangeLow, string rangeHigh, string rows, string cols, string xUnits, string yUnits, string zUnits, string xLabels, string xLabelType, string xEq, string yLabels, string yLabelType, string yEq, string xAxisMin, string xAxisMax, string yAxisMin, string yAxisMax)
        {
            this.uniqueID = uniqueID;
            this.title = title;
            this.descSize = descSize;
            this.desc = desc;
            this.address = address;
            this.zEq = zEq;
            this.xOutType = xOutType;
            this.yOutType = yOutType;
            this.rangeLow = rangeLow;
            this.rangeHigh = rangeHigh;
            this.rows = rows;
            this.cols = cols;
            this.xUnits = xUnits;
            this.yUnits = yUnits;
            this.zUnits = zUnits;
            this.xLabels = xLabels;
            this.xLabelType = xLabelType;
            this.xEq = xEq;
            this.yLabels = yLabels;
            this.yLabelType = yLabelType;
            this.yEq = yEq;
            this.xAxisMin = xAxisMin;
            this.xAxisMax = xAxisMax;
            this.yAxisMin = yAxisMin;
            this.yAxisMax = yAxisMax;
            this.tableData = null;
        }

        public XDFTable()
        {
            this.uniqueID = "";
            this.title = "";
            this.descSize = "";
            this.desc = "";
            this.address = "";
            this.zEq = "";
            this.xOutType = "";
            this.yOutType = "";
            this.rangeLow = "";
            this.rangeHigh = "";
            this.rows = "";
            this.cols = "";
            this.xUnits = "";
            this.yUnits = "";
            this.zUnits = "";
            this.xLabels = "";
            this.xLabelType = "";
            this.xEq = "";
            this.yLabels = "";
            this.yLabelType = "";
            this.yEq = "";
            this.xAxisMin = "";
            this.xAxisMax = "";
            this.yAxisMin = "";
            this.yAxisMax = "";
            this.tableData = null;
            this.edited = false;
        }
        #endregion

        #region Methods
        //given the file path to a binary file, uses the rows, cols, and address to find the values in the binary file 
        //and assign tableData with that data 
        public void FetchFromBin(string filePath)
        {
            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            DataTable dt = new DataTable();
            int rowsB10 = Convert.ToInt32(rows, 16);
            int colsB10 = Convert.ToInt32(cols, 16);
            string eq = "";

            fileStream.Seek(Convert.ToInt32(address,16), SeekOrigin.Begin);

            //int multRowsCols = rowsB10 * colsB10;

            tableData = new float[rowsB10, colsB10];

            for(int i = 0; i < rowsB10; i++)
            {
                for (int j = 0; j < colsB10; j++)
                {
                    if (zEq.Contains(','))
                    {
                        eq = zEq.Split(',')[0];
                    }
                    else
                        eq = zEq;
                    eq = eq.Replace("X", binaryReader.ReadByte().ToString());
                    //cover our asses here
                    if (CheckDivideByZero(eq))
                    {
                        Debug.WriteLine($"{title}: divide by zero avoided, skipping...");
                        return;
                    }
                    tableData[i,j] = float.Parse(dt.Compute(eq, "").ToString());
                }
            }
            fileStream.Close();
            binaryReader.Close();
        }

        //given a string that is an equation, checks for if the equation will divide by zero
        //returns true if it will and false if not
        private bool CheckDivideByZero(string equation)
        {
            if (equation == null)
                return false;

            for (int i = 0; i < equation.Length; i++)
            {
                if (equation[i] == '/')
                {
                    if (equation[i + 1] == '0')
                        return true;
                }
            }

            return false;
        }
        #endregion
    }
}
