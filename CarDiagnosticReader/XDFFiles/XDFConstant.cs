using System;
using System.Data;
using System.Diagnostics;
using System.IO;

namespace CarDiagnosticReader.XDFFiles
{
    public class XDFConstant
    {
        #region Variables
        //attributes from XDF file
        public string uniqueID { get; set; }
        public string title { get; set; }
        public string descSize { get; set; }
        public string desc { get; set; }
        public string units { get; set; }
        public string sizeInBits { get; set; }
        public string address { get; set; }
        public string equation { get; set; }
        //helper vars
        public float value { get; set; }
        public bool edited { get; set; } //true means value has been edited and not saved yet. this gets changed back to false when changes are saved and written to file
        #endregion

        #region Constructors
        public XDFConstant(string uniqueID, string title, string descSize, string desc, string units, string sizeInBits, string address, string equation)
        {
            this.uniqueID = uniqueID;
            this.title = title;
            this.descSize = descSize;
            this.desc = desc;
            this.units = units;
            this.sizeInBits = sizeInBits;
            this.address = address;
            this.equation = equation;
        }

        public XDFConstant()
        {
            this.uniqueID = "";
            this.title = "";
            this.descSize = "";
            this.desc = "";
            this.units = "";
            this.sizeInBits = "";
            this.address = "";
            this.equation = "";
            this.edited = false;
        }
        #endregion

        #region Methods
        //given the file path to a binary file, uses the sizeInBits and address to find the value in the binary file
        //and assign value with that data 
        public void FetchFromBin(string filePath)
        {
            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            DataTable dt = new DataTable();
            int numBytes;
            string eq = "";

            fileStream.Seek(Convert.ToInt32(address, 16), SeekOrigin.Begin);

            if(sizeInBits == "" || sizeInBits == null || sizeInBits == "0x1")
                numBytes = 1;
            else
                numBytes = Convert.ToInt32(sizeInBits, 16) / 8;

            if (equation.Contains(','))
                eq = equation.Split(',')[0];
            else
                eq = equation;
            
            byte[] data = binaryReader.ReadBytes(numBytes); 
            int hexValue = GetBase10FromHex(data);
            eq = eq.Replace("X", hexValue.ToString());
            //cover our asses here
            if (CheckDivideByZero(eq))
            {
                Debug.WriteLine($"{title}: divide by zero avoided, skipping...");
                return;
            }
            value = float.Parse(dt.Compute(eq, "").ToString());
            fileStream.Close();
            binaryReader.Close();
        }

        //given an array of bytes, returns the base 10 integer value of that array 
        //returned int will look something like 65535
        public int GetBase10FromHex(byte[] byteArray)
        {
            string newHexString ="";

            foreach(byte b in byteArray)
                newHexString = newHexString + (b.ToString("X"));

            return Int32.Parse(newHexString, System.Globalization.NumberStyles.HexNumber);
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
