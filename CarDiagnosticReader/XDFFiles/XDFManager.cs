using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Policy;
using static SQLite.SQLite3;

namespace CarDiagnosticReader.XDFFiles
{
    public static class XDFManager
    {
        #region Variables
        public static XDFHeader headerVar = new XDFHeader();
        public static XDFChecksum checksumVar = new XDFChecksum(); 
        public static List<XDFConstant> constantVars = new List<XDFConstant>();
        public static List<XDFTable> tableVars = new List<XDFTable>();

        public static string xdfFileName = "";
        public static string xdfFilePath = "";
        public static string binFileName = "";
        public static string binFilePath = "";
        public static string binChecksum = "";
        #endregion

        #region Methods
        // this is a function for front end to call when they make an immediate change 
        // this will allow for writing back every table back to the binary file instead of writing to 
        // binary file every time the user makes a change they may not want
        //returns true or false based on whether the edit was successful or not
        public static bool EditTableData(string fileName, string tableName, int x, int y, float newVal)
        {
            XDFTable table = tableVars.Find(x => x.title == tableName);
            if (table == null) return false;

            table.tableData[y, x] = newVal;
            table.edited = true;
            return true;
        }

        //given a constants name to edit, and its new value
        //updates the constants value NOT the binary file
        //returns true or false based on whether the edit was successful or not
        public static bool EditConstantData(string fileName, string constantName, float newVal)
        {
            XDFConstant con = constantVars.Find(x => x.title == constantName);
            if (con == null) return false;

            con.value = newVal;
            con.edited = true;
            return true;
        }

        //given a binary file name to edit, and a table name to edit
        //edits the binary file based on what is in the tableData array which should be updated using EditTableData()
        //returns true or false based on whether the edit was successful or not
        public static bool SaveTableChanges(string fileName, string tableName)
        {
            // search for table
            XDFTable table = tableVars.Find(x => x.title == tableName);
            if (table == null)
            {
                Debug.WriteLine($"{tableName}: table not found");
                return false;
            }

            //open the file and find the correct spot from the address
            FileStream fileStream = new FileStream(binFilePath, FileMode.Open, FileAccess.ReadWrite);
            BinaryWriter binaryWriter = new(fileStream);

            // keep track of index
            int count = 0;

            // go through each cell in table data
            foreach (int value in table.tableData)
            {
                // get equation
                string[] eq;
                if (table.zEq.Contains(","))
                    eq = table.zEq.Split(",")[0].Split(" ");
                else
                    eq = table.zEq.Split(" ");

                float result = value;
                if (eq[1] == "*")
                {
                    result -= float.Parse(eq[4]);
                    //cover our asses here
                    if (eq[0][0] == '0')
                    {
                        Debug.WriteLine($"{tableName}: divide by zero avoided, skipping...");
                        continue;
                    }
                    result /= float.Parse(eq[0]);
                }
                else if (eq[1] == "/")
                {
                    result -= float.Parse(eq[4]);
                    //cover our asses here
                    if (result == 0)
                    {
                        Debug.WriteLine($"{tableName}: divide by zero avoided, skipping...");
                        continue;
                    }
                    result = float.Parse(eq[0]) / result;
                }

                // jump to address
                fileStream.Seek(Convert.ToInt32(table.address, 16) + count, SeekOrigin.Begin);

                byte bite = 0;
                int X = Convert.ToInt32(result);

                // no switch statement since im assuming it is only one byte long
                if (X > byte.MaxValue)
                {
                    Debug.WriteLine("Invalid value to save as");
                    return false;
                }
                else
                {
                    bite = (byte)X;
                }

                // write to file
                binaryWriter.Write(bite);
                count++;
            }
            binaryWriter.Close();
            fileStream.Close();
            table.edited = false;
            // close both readers
            Debug.WriteLine($"{tableName}: Changes saved and written to file");
            return true;
        }

        //given a binary file name to edit, constant name to edit
        //edits the binary file with the value in the constant.value at the address the constant has in its definition file
        //returns true or false based on whether the edit was successful or not
        //TODO: implement fileName use when xdf manager has the capability to have multiple files open
        public static bool SaveConstantChanges(string fileName, string constName)
        {
            //search for the specified constant 
            XDFConstant con = constantVars.Find(x => x.title == constName);
            if (con == null)
            {
                Debug.WriteLine($"{constName}: constant not found");
                return false;
            }

            //get our equation
            string[] eq;
            float result = con.value;
            if (con.equation.Contains(","))
                eq = con.equation.Split(",")[0].Split(" ");
            else
                eq = con.equation.Split(" ");

            //evaluate the inverse of the "to" equation since there is no "from" equation to reference
            //basically the equation looks like this 57.22 = 3750000 / X + 12345 and we need to solve for X
            if (eq[1] == "*")
            {
                result -= float.Parse(eq[4]);
                //cover our asses here
                if (eq[0][0] == '0')
                {
                    Debug.WriteLine($"{constName}: divide by zero avoided, skipping...");
                    return false;
                }
                result /= float.Parse(eq[0]);
            }
            else if (eq[1] == "/")
            {
                result -= float.Parse(eq[4]);
                //cover our asses here
                if (result == 0)
                {
                    Debug.WriteLine($"{constName}: divide by zero avoided, skipping...");
                    return false;
                }
                result = float.Parse(eq[0]) / result;
            }
            //this is the int we need to convert to hex and store in the binary file
            int X = Convert.ToInt32(result); //the convert is to make it an int from a float
            int numBytes = 0;
            byte bite = 0;
            ushort ushorty = 0;
            bool le;

            //get the number of bytes this constant uses
            if (con.sizeInBits == "" || con.sizeInBits == null || con.sizeInBits == "0x1")
                numBytes = 1;
            else
                numBytes = Convert.ToInt32(con.sizeInBits, 16) / 8;

            switch (numBytes)
            {
                case 1:
                    if (X > byte.MaxValue)
                    {
                        Debug.WriteLine("Invalid value to save as");
                        return false;
                    }
                    else
                        bite = (byte)X;
                    break;
                case 2:
                    if (X > ushort.MaxValue)
                    {
                        Debug.WriteLine("Invalid value to save as");
                        return false;
                    }
                    else
                        ushorty = (ushort)X;
                    break;
                case 3:
                    Debug.WriteLine("Trying to edit 3 bytes: Not implemented yet");
                    return false;
            }

            //open the file and find the correct spot from the address
            FileStream fileStream = new FileStream(binFilePath, FileMode.Open, FileAccess.ReadWrite);
            BinaryWriter binaryWriter = new(fileStream);
            fileStream.Seek(Convert.ToInt32((con.address), 16), SeekOrigin.Begin);

            //write to the file accordingly
            switch (numBytes)
            {
                case 1:
                    binaryWriter.Write(bite);
                    break;
                case 2:
                    le = BitConverter.IsLittleEndian;
                    if (le)
                        binaryWriter.Write(System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(ushorty));
                    else
                        binaryWriter.Write(ushorty);
                    break;
                case 3:
                    Debug.WriteLine("Trying to edit 3 bytes: Not implemented yet");
                    return false;
                case 4:
                    le = BitConverter.IsLittleEndian;
                    if (le)
                        binaryWriter.Write(System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(X));
                    else
                        binaryWriter.Write(X);
                    break;
            }

            con.edited = false;
            binaryWriter.Close();
            fileStream.Close();
            Debug.WriteLine($"{constName}: Changes saved and written to file");
            return true;
        }

        //USE ABOVE FUNCTIONS
        //given a binary file name to edit, constant name to edit, and the new value it should be
        //edits the binary file at the address the constant has in its definition file
        //returns true or false based on whether the edit was successful or not
        //TODO: implement fileName use when xdf manager has the capability to have multiple files open
        public static bool EditBinConstant(string fileName, string constName, float newVal)
        {
            //search for the specified constant 
            XDFConstant con = constantVars.Find(x => x.title == constName);
            if(con == null)
            {
                Debug.WriteLine($"{constName}: constant not found");
                return false;
            }

            //get our equation
            string[] eq;
            float result = newVal;
            if (con.equation.Contains(","))
                eq = con.equation.Split(",")[0].Split(" ");
            else
                eq = con.equation.Split(" ");

            //evaluate the inverse of the "to" equation since there is no "from" equation to reference
            //basically the equation looks like this 57.22 = 3750000 / X + 12345 and we need to solve for X
            if (eq[1] == "*")
            {
                result -= float.Parse(eq[4]);
                result /= float.Parse(eq[0]);
            }
            else if (eq[1] == "/")
            {
                result -= float.Parse(eq[4]);
                result = float.Parse(eq[0]) / result;
            }
            //this is the int we need to convert to hex and store in the binary file
            int X = Convert.ToInt32(result);
            int numBytes = 0;
            byte bite = 0;
            ushort ushorty = 0;
            bool le;

            //get the number of bytes this constant uses
            if (con.sizeInBits == "" || con.sizeInBits == null || con.sizeInBits == "0x1")
                numBytes = 1;
            else
                numBytes = Convert.ToInt32(con.sizeInBits, 16) / 8;

            switch(numBytes)
            {
                case 1:
                    if (X > byte.MaxValue)
                    {
                        Debug.WriteLine("Invalid value to save as");
                        return false;
                    }
                    else
                        bite = (byte)X;
                    break;
                case 2:
                    if (X > ushort.MaxValue)
                    {
                        Debug.WriteLine("Invalid value to save as");
                        return false;
                    }
                    else
                        ushorty = (ushort)X;
                    break;
                case 3:
                    Debug.WriteLine("Trying to edit 3 bytes: Not implemented yet");
                    return false;
            }

            //open the file and find the correct spot from the address
            FileStream fileStream = new FileStream(binFilePath, FileMode.Open, FileAccess.ReadWrite);
            BinaryWriter binaryWriter = new(fileStream);
            fileStream.Seek(Convert.ToInt32((con.address), 16), SeekOrigin.Begin);

            //write to the file accordingly
            switch (numBytes)
            {
                case 1:
                    binaryWriter.Write(bite);
                    break;
                case 2:
                    le = BitConverter.IsLittleEndian;
                    if(le)
                        binaryWriter.Write(System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(ushorty));
                    else
                        binaryWriter.Write(ushorty);
                    break;
                case 3:
                    Debug.WriteLine("Trying to edit 3 bytes: Not implemented yet");
                    return false;
                case 4:
                    le = BitConverter.IsLittleEndian;
                    if (le)
                        binaryWriter.Write(System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(X));
                    else
                        binaryWriter.Write(X);
                    break;
            }

            binaryWriter.Close();
            fileStream.Close();
            return true;
        }

        //USE ABOVE FUNCTIONS
        //given a binary file name to edit, table name to edit, x and y indexs in the tableData array, and the new value it should be
        //edits the binary file at only the point of the edited value
        //returns true or false based on whether the edit was successful or not
        public static bool EditBinTableIndex(string fileName, string tableName, int x, int y, float newVal)
        {
            // search for table
            XDFTable table = tableVars.Find(x => x.title == tableName);
            if(table == null)
            {
                Debug.WriteLine($"{tableName}: table not found");
                return false;
            }
            //make sure we don't go out of bounds
            if(x >= Convert.ToInt32(table.cols, 16))
            {
                Debug.WriteLine("x index is out of bounds, cannot edit");
                return false;
            }
            if(y >= Convert.ToInt32(table.rows, 16))
            {
                Debug.WriteLine("y index is out of bounds, cannot edit");
                return false;
            }

            /** most likely edited cells, not axis values, so we only need to access
            /*  the first scalar for the table
            */
            // get equation
            string[] eq;
            float result = newVal;
            // ***im assuming zequation is for the datavalues, if not change - Chase*** //

            /** Example ZEquation: 1.000000 * X + -10.000000,TH|0|0|0|0|
             * when splitting it by comma, 
             * [0] will be "1.000000 * X + -10.000000"
             * and [1] will be "TH|0|0|0|0|"
             */
            if (table.zEq.Contains(","))
                eq = table.zEq.Split(",")[0].Split(" ");
            else
                eq = table.zEq.Split(" ");

            // quick check to see whats in equation string
            //foreach (string eqStr in eq)
            //{
                //Debug.WriteLine($"{eqStr}");
            //}

            //solve for X in the equation
            if (eq[1] == "*")
            {
                result -= float.Parse(eq[4]);
                result /= float.Parse(eq[0]);
            }
            else if (eq[1] == "/")
            {
                result -= float.Parse(eq[4]);
                result = float.Parse(eq[0]) / result;
            }
            
            //get the number of bytes this constant uses
            //this is the int we need to convert to hex and store in the binary file
            int X = Convert.ToInt32(result);
            byte bite = 0;

            // no switch statement since im assuming it is only one byte long, but maybe its 2 bytes?
            //edit: should be only 1 byte unless other definition files tell us otherwise 
            if(X > byte.MaxValue)
            {
                Debug.WriteLine("Invalid value to save as");
                return false;
            }
            else
            {
                bite = (byte)X;
            }

            //open the file and find the correct spot from the address
            FileStream fileStream = new FileStream(binFilePath, FileMode.Open, FileAccess.ReadWrite);
            BinaryWriter binaryWriter = new(fileStream);
            
            // need to find out how far is edited value
            int columns = Convert.ToInt32(table.cols, 16);
            /*
             * I did this by imagine we have a 4x5 grid and x is the 13th element, so itd look like:
             *      |   0   1   2   3   4
             *   0  |   z   z   z   z   z
             *   1  |   z   z   z   z   z
             *   2  |   z   z   X   z   z
             *   3  |   z   z   z   z   z
             *   
             *   im assuming the x and y are: [y, x] in the table matrix
             *   note: x is the 13th element, there are 4 rows and 5 columns
             *   -if we take our y (row number) and multiply it by the elements IN a row 
             *   (which would be the amount of columns) then we would need to add 
             *   our remaining index to find where X is, which is our column index (x) 
             *   - but part of me wants to add 1 since the column index starts at 0,
             *   however we're jumping, in the bin file, right to the first element in the heatmap 
             *   so i think it cancels out
             *   - edit: dont need to do this last bullet point^
             */
            int jump = y * columns + x;
            fileStream.Seek(Convert.ToInt32(table.address, 16) + jump, SeekOrigin.Begin);
            //write to the file accordingly
            binaryWriter.Write(bite);

            binaryWriter.Close();
            fileStream.Close();
            return true;
        }
        
        //master function to read all data from XDF file at xdfFilePath
        public static void ReadAllFromXDF()
        {
            ReadHeaderVar();
            ReadChecksumVar();
            ReadConstantVars();
            ReadTableVars();
        }

        //master function to populate all constantVars and tableVars with their appropriate values from the binary file at binFilePath
        public static void FetchAllFromBin()
        {
            //Debug.WriteLine("DATA XDFMAN: " + constantVars[0]);

            foreach (XDFConstant con in constantVars)
            {
                con.FetchFromBin(binFilePath);
            }
            foreach (XDFTable tab in tableVars)
            {
                tab.FetchFromBin(binFilePath);
            }
        }

        //searches through the XDF file specified through xdf_FilePath and retrieves the data given in its HEADER tag
        //this data is stored in this objects headerVar
        public static void ReadHeaderVar()
        {
            StreamReader reader = new StreamReader(xdfFilePath);
            string currentLine = reader.ReadLine();
            string[] tempArr;

            while (currentLine != null)
            {
                if (currentLine == "%%HEADER%%")
                {
                    while (currentLine != "%%END%%")
                    {
                        currentLine = reader.ReadLine();

                        if (currentLine != null) //lil safety
                            tempArr = currentLine.Split('=');
                        else
                            break;

                        for (int i = 0; i < tempArr.Length; i++)
                        {
                            string sub = tempArr[i];
                            i++;
                            switch (sub.Trim())
                            {
                                case "001000 FileVers":
                                    headerVar.fileVers = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "001005 DefTitle":
                                    headerVar.defTitle = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "001007 DescSize":
                                    headerVar.descSize = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "001006 Desc":
                                    headerVar.desc = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "001010 Author":
                                    headerVar.author = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "001030 BinSize":
                                    headerVar.binSize = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "001035 BaseOffset":
                                    headerVar.baseOffset = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "001200 ADSAssoc":
                                    headerVar.aDSAssoc = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "001225 ADSCheck":
                                    headerVar.aDSCheck = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "001300 GenFlags":
                                    headerVar.genFlags = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "001325 ModeFlags":
                                    headerVar.modeFlags = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                }
                currentLine = reader.ReadLine();
            }
            reader.Close();
        }

        //searches through the XDF file specified through xdf_FilePath and retrieves the data given in its CHECKSUM tag
        //this data is stored in this objects checksumVar
        public static void ReadChecksumVar()
        {
            StreamReader reader = new StreamReader(xdfFilePath);
            string currentLine = reader.ReadLine();
            string[] tempArr;

            while (currentLine != null)
            {
                if (currentLine == "%%CHECKSUM%%")
                {
                    while (currentLine != "%%END%%")
                    {
                        currentLine = reader.ReadLine();

                        if (currentLine != null) //lil safety
                            tempArr = currentLine.Split('=');
                        else
                            break;

                        for (int i = 0; i < tempArr.Length; i++)
                        {
                            string sub = tempArr[i];
                            i++;
                            switch (sub.Trim())
                            {
                                case "000002 UniqueID":
                                    checksumVar.uniqueID = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "010005 Title":
                                    checksumVar.title = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "010010 DataStart":
                                    checksumVar.dataStart = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "010015 DataEnd":
                                    checksumVar.dataEnd = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "010020 SizeBits":
                                    checksumVar.sizeBits = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "010025 StoreAddr":
                                    checksumVar.storeAddr = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "010030 CalcMethod":
                                    checksumVar.calcMethod = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "010050 Flags":
                                    checksumVar.flags = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                }
                
                currentLine = reader.ReadLine();
            }
            reader.Close();
        }

        //searches through the XDF file specified through xdf_FilePath and retrieves the data given in all CONSTANT tags 
        //this data is stored in this objects constantVars
        public static void ReadConstantVars()
        {
            StreamReader reader = new StreamReader(xdfFilePath);
            string currentLine = reader.ReadLine();
            string[] tempArr;

            while (currentLine != null)
            {
                if (currentLine == "%%CONSTANT%%")
                {
                    XDFConstant tempConstantVariable = new XDFConstant();
                    while (currentLine != "%%END%%")
                    {
                        currentLine = reader.ReadLine();

                        if (currentLine != null) //lil safety
                            tempArr = currentLine.Split('=');
                        else
                            break;

                        for (int i = 0; i < tempArr.Length; i++)
                        {
                            string sub = tempArr[i];
                            i++;

                            switch (sub.Trim())
                            {
                                case "000002 UniqueID":
                                    tempConstantVariable.uniqueID = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "020005 Title":
                                    tempConstantVariable.title = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "020011 DescSize":
                                    tempConstantVariable.descSize = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "020010 Desc":
                                    tempConstantVariable.desc = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "020020 Units":
                                    tempConstantVariable.units = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "020050 SizeInBits":
                                    tempConstantVariable.sizeInBits = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "020100 Address":
                                    tempConstantVariable.address = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "020200 Equation":
                                    tempConstantVariable.equation = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                default:
                                    break;
                            }
                        }
                    }

                    // check to make sure constant is valid
                    if (CheckNecessaryVals_Const(tempConstantVariable))
                        constantVars.Add(tempConstantVariable);
                }
                currentLine = reader.ReadLine();
            }
            reader.Close();
        }

        //given an XDFConstant object, goes through and checks if it has the necessary values to display to the user
        //returns true or false based on if it is valid
        private static bool CheckNecessaryVals_Const(XDFConstant temp)
        {
            if (temp.equation == null || temp.address == null || temp.title == null || temp.title == "" || temp.address == "" || temp.equation == "")
            {
                // if we have the name use it to clarify which constant
                if(temp.title != null)
                {
                    Debug.WriteLine("Error with Constant: " + temp.title + "; missing necessary data.");
                    return false;
                }
                // if we have unique id instead use it to clarify
                if (temp.uniqueID != null)
                {
                    Debug.WriteLine("Error with Constant (id):" + temp.uniqueID + "; missing necessary data.");
                    return false;
                }
                // we cannot clarify which constant it is
                else
                {
                    Debug.WriteLine("Error with Constant: missing necessary data.");
                    return false;
                }
            }
            return true;
        }

        //searches through the XDF file specified through xdf_FilePath and retrieves the data given in all TABLE tags 
        //this data is stored in this objects tableVars
        public static void ReadTableVars()
        {
            StreamReader reader = new StreamReader(xdfFilePath);
            string currentLine = reader.ReadLine();
            string[] tempArr;

            while (currentLine != null)
            {
                if (currentLine == "%%TABLE%%")
                {
                    XDFTable tempTableVariable = new XDFTable();
                    while (currentLine != "%%END%%")
                    {
                        currentLine = reader.ReadLine();

                        if (currentLine != null) //lil safety
                            tempArr = currentLine.Split('=');
                        else
                            break;

                        for (int i = 0; i < tempArr.Length; i++)
                        {
                            string sub = tempArr[i];
                            i++;

                            switch (sub.Trim())
                            {
                                case "000002 UniqueID":
                                    tempTableVariable.uniqueID = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040005 Title":
                                    tempTableVariable.title = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040011 DescSize":
                                    tempTableVariable.descSize = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040010 Desc":
                                    tempTableVariable.desc = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040100 Address":
                                    tempTableVariable.address = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040200 ZEq":
                                    tempTableVariable.zEq = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040203 XOutType":
                                    tempTableVariable.xOutType = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040304 YOutType":
                                    tempTableVariable.yOutType = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040230 RangeLow":
                                    tempTableVariable.rangeLow = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040240 RangeHigh":
                                    tempTableVariable.rangeHigh = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040300 Rows":
                                    tempTableVariable.rows = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040305 Cols":
                                    tempTableVariable.cols = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040320 XUnits":
                                    tempTableVariable.xUnits = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040325 YUnits":
                                    tempTableVariable.yUnits = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040330 ZUnits":
                                    tempTableVariable.zUnits = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040350 XLabels":
                                    tempTableVariable.xLabels = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040352 XLabelType":
                                    tempTableVariable.xLabelType = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040354 XEq":
                                    tempTableVariable.xEq = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040360 YLabels":
                                    tempTableVariable.yLabels = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040362 YLabelType":
                                    tempTableVariable.yLabelType = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040364 YEq":
                                    tempTableVariable.yEq = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040660 XAxisMin":
                                    tempTableVariable.xAxisMin = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040670 XAxisMax":
                                    tempTableVariable.xAxisMax = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040760 YAxisMin":
                                    tempTableVariable.yAxisMin = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                case "040770 YAxisMax":
                                    tempTableVariable.yAxisMax = RemoveQuoatationMarks(tempArr[i]);
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                    // if the table has all the necessary elements then add it to the list of tables
                    if(CheckNecessaryVals_Table(tempTableVariable)) 
                        tableVars.Add(tempTableVariable);
                }
                currentLine = reader.ReadLine();
            }
            reader.Close();
        }

        //given an XDFTable object, goes through and checks if it has the necessary values to display to the user
        //returns true or false based on if it is valid
        private static bool CheckNecessaryVals_Table(XDFTable table) 
        {
            // quick check
            if(table == null) return false;

            // check identifiers of table
            if(table.title == null || table.title == "")
            {
                if(table.uniqueID == null || table.uniqueID == "")
                {
                    Debug.WriteLine("Error: Table title and unique ID are null");
                    return false;
                }
                else
                {
                    Debug.WriteLine("Error: Table (id) " + table.uniqueID + " is null");
                    return false;
                }
            }

            // check if table has rows and columns
            if(table.rows == null || table.cols == null || table.rows == "" || table.cols == "")
            {
                Debug.WriteLine("Error: Table " + table.title + " is null.");
                return false;
            }
            // if rows == 1 no y labels
            if (table.rows == "0x1")
            {
                if(table.xLabels == null || table.xLabels == "")
                {
                    Debug.WriteLine("Error: Table " + table.title + ", No XLabels for 1 by X amount of columns");
                    return false;
                }
            }
            // if cols == 1 then no x labels
            if (table.cols == "0x1")
            {
                if (table.yLabels == null || table.yLabels == "")
                {
                    Debug.WriteLine("Error: Table " + table.title + ", No YLabels for X by 1 amount of rows");
                    return false;
                }
            }
            // if neither are 1 check for both labels
            if(table.rows != "0x1" &&  table.cols != "0x1")
            {
                if (table.yLabels == null || table.yLabels == "")
                {
                    Debug.WriteLine("Error: Table " + table.title + ", No Ylabels for 3D graph");
                    return false;
                }
                if (table.xLabels == null || table.xLabels == "")
                {
                    Debug.WriteLine("Error: Table " + table.title + ", No Xlabels for 3D graph");
                    return false;
                }
            }
            
            // passed all the checks!
            return true;
        }

        //given a string removes all quotation marks from the string and returns the resulting string 
        public static string RemoveQuoatationMarks(string str)
        {
            string newString = str.Replace("\"", "");
            return newString;
        }

        //given a Table, goes through and verifies it is valid to be displayed to the user
        //returns true or false based on if it is valid
        public static bool VerifyXDFTable(XDFTable table)
        {
            // quick check
            if (table == null) return false;

            if (table.title == null || table.title == "")
            {
                Debug.WriteLine("Error: No title");
                return false;
            }

            // see if 1D, 2D, 3D
            if (table.rows == null || table.cols == null || table.rows == "" || table.cols == "")
            {
                Debug.WriteLine("Error: Rows or Columns are null");
                return false;
            }
            // if rows == 1 no y labels, if cols== 1 then no x labels
            else if(table.cols == "0x1")
            {
                if (table.xLabels == null || table.xLabels == "")
                {
                    Debug.WriteLine("Error: No XLables for 2D Graph");
                    return false;
                }
            }
            else if(table.rows == "0x1")
            {
                if (table.yLabels == null || table.yLabels == "")
                {
                    Debug.WriteLine("Error: No YLables for 2D Graph");
                    return false;
                }
            }
            // If neither are 1 then it is 3D
            else
            {
                if (table.xLabels == null || table.xLabels == "")
                {
                    Debug.WriteLine("Error: No XLables for 3D Graph");
                    return false;
                }
                if (table.yLabels == null || table.yLabels == "")
                {
                    Debug.WriteLine("Error: No YLables for 3D Graph");
                    return false;
                }
            }
                
            /** 
             * Tables should return:
             * 
             *  Title
             *              XLabels
             *             |--------------------|     
             *      YLables|                    |
             *             |                    |
             *             |____________________|
             *             
             * so after this function, depending on the type of graph,
             * all labels needed should be present, all that's lef that 
             * has not been checked is the data 
             */
            return true;
        }

        //prints every field from every variable in the XDFManager class. Mainly for debugging 
        public static void PrintAllVars()
        {
            Debug.WriteLine($"xdfFileName: {xdfFileName} | xdfFilePath: {xdfFilePath}\nbinFileName: {binFileName} | binFilePath: {binFilePath}\n");
            Debug.WriteLine($"HEADER\n FileVers: {headerVar.fileVers}\n DefTitle: {headerVar.defTitle}\n DescSize: {headerVar.descSize}\n Desc: {headerVar.desc}\n Author: {headerVar.author}\n BinSize: {headerVar.binSize}\n BaseOffset: {headerVar.baseOffset}\n ADSAssoc: {headerVar.aDSAssoc}\n ADSCheck: {headerVar.aDSCheck}\n GenFlags: {headerVar.genFlags}\n ModeFlags: {headerVar.modeFlags}\n");
            Debug.WriteLine($"CHECKSUM\n UniqueID: {checksumVar.uniqueID}\n Title: {checksumVar.title}\n DataStart: {checksumVar.dataStart}\n DataEnd: {checksumVar.dataEnd}\n SizeBits: {checksumVar.sizeBits}\n StoreAddr: {checksumVar.storeAddr}\n CalcMethod: {checksumVar.calcMethod}\n Flags: {checksumVar.flags}\n");
            Debug.WriteLine("CONSTANTS");
            foreach(XDFConstant con in constantVars)
            {
                Debug.WriteLine($" {con.title}\n  UniqueID: {con.uniqueID}\n  DescSize: {con.descSize}\n  Desc: {con.desc}\n  Units: {con.units}\n  SizeInBits: {con.sizeInBits}\n  Address: {con.address}\n  Equation: {con.equation}\n  Actual Value: {con.value}");
            }
            Debug.WriteLine("\nTABLES");
            foreach (XDFTable tab in tableVars)
            {
                Debug.WriteLine($" {tab.title}\n  UniqueID: {tab.uniqueID}\n  DescSize: {tab.descSize}\n  Desc: {tab.desc}\n  Address: {tab.address}\n  ZEq: {tab.zEq}\n  XOutType: {tab.xOutType}\n  YOutType: {tab.yOutType}\n  RangeLow: {tab.rangeLow}\n  RangeHigh: {tab.rangeHigh}\n  Rows: {tab.rows}\n  Cols: {tab.cols}\n  XUnits: {tab.xUnits}\n  YUnits: {tab.yUnits}\n  ZUnits: {tab.zUnits}\n  XLabels: {tab.xLabels}\n  XLabelType: {tab.xLabelType}\n  XEq: {tab.xEq}\n  YLabels: {tab.yLabels}\n  YLabelType: {tab.yLabelType}\n  YEq: {tab.yEq}\n  XAxisMin: {tab.xAxisMin}\n  XAxisMax: {tab.xAxisMax}\n  YAxisMin: {tab.yAxisMin}\n  YAxisMax: {tab.yAxisMax}");
                if(tab.tableData != null)
                {
                    for (int i = 0; i < tab.tableData.GetLength(0); i++)
                    {
                        for (int j = 0; j < tab.tableData.GetLength(1); j++)
                        {
                            Debug.Write($"{tab.tableData[i, j]} ");
                        }
                        Debug.WriteLine("");
                    }
                }
            }
        }

        //prints the values of every constant and table in the XDF manager
        public static void PrintAllValues()
        {
            foreach (XDFConstant con in constantVars)
            {
                Debug.WriteLine($"------{con.title}------\nValue: {con.value}\n");
            }

            foreach (XDFTable tab in tableVars)
            {
                Debug.WriteLine($"------{tab.title}------");
                if (tab.tableData != null)
                {
                    for (int i = 0; i < tab.tableData.GetLength(0); i++)
                    {
                        for (int j = 0; j < tab.tableData.GetLength(1); j++)
                        {
                            Debug.Write($"{tab.tableData[i, j]} ");
                        }
                        Debug.WriteLine("");
                    }
                }
                Debug.WriteLine("");
            }
        }
        #endregion
    }
}
