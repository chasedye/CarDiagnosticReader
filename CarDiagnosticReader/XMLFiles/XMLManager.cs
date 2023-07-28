using CarDiagnosticReader.XDFFiles;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Xml.Serialization;

namespace CarDiagnosticReader.XMLFiles
{
    public static class XMLManager
    {
        #region Variables
        public static Rom baseRom = new Rom(); //stands for read only memory  
        public static Rom carRom = new Rom(); //These objects holds all data from the given XML file and associated base file after ReadAllFromXML() is called
        public static List<XMLTableInfo> tablesToDisplay = new(); //master object that stores all info for every table that is valid to display to the user

        public static string xmlCarFileName = "";
        public static string xmlCarFilePath = "";
        public static string xmlBaseFileName = "";
        public static string xmlBaseFilePath = "";
        public static string binFilePath = "";
        public static string binFileName = "";
        public static string binChecksum = "";
        #endregion

        #region Methods
        // this is a function for front end to call when they make an immediate change 
        // this will allow for writing back every table back to the binary file instead of writing to 
        // binary file every time the user makes a change they may not want
        // returns true or false based on whether the edit was successful or not
        public static bool EditTableData(string fileName, string tableName, int x, int y, float newVal)
        {
            XMLTableInfo table = tablesToDisplay.Find(x => x.title == tableName);
            if (table == null) return false;

            //search for the data scaling
            Scaling dataScaling = baseRom.Scalings.Find(x => x.Name == table.dataScaling);
            if (dataScaling == null)
            {
                Debug.WriteLine($"{table.dataScaling}: scaling not found");
                return false;
            }

            //make sure its in the range of valid values defined in the scaling from the base file
            if(newVal > float.Parse(dataScaling.Max) || newVal < float.Parse(dataScaling.Min))
            {
                Debug.WriteLine("Invalid value");
                return false;
            }

            table.tableData[y, x] = newVal;
            table.edited = true;
            return true;
        }

        //given a binary file name to edit, and a table name to edit
        //edits the binary file based on what is in the tableData array which should be updated using EditTableData()
        //returns true or false based on whether the edit was successful or not
        public static bool SaveTableChanges(string fileName, string tableName)
        {
            // search for table
            XMLTableInfo table = tablesToDisplay.Find(x => x.title == tableName);
            if (table == null)
            {
                Debug.WriteLine($"{tableName}: table not found");
                return false;
            }

            //search for the data scaling
            Scaling dataScaling = baseRom.Scalings.Find(x => x.Name == table.dataScaling);
            if(dataScaling == null)
            {
                Debug.WriteLine($"{table.dataScaling}: scaling not found");
                return false;
            }

            //open the file and find the correct spot from the address
            FileStream fileStream = new FileStream(binFilePath, FileMode.Open, FileAccess.ReadWrite);
            BinaryWriter binaryWriter = new(fileStream);
            DataTable dt = new DataTable();

            int run = 0, jump = 0, count = 0;
            bool swapxy = table.swapxy.Equals("true", StringComparison.OrdinalIgnoreCase);
            //if swapxy is true swap the num elements
            if (swapxy)
            {
                run = table.yLength;
                jump = table.xLength;
            }
            else
            {
                run = table.xLength;
                jump = table.yLength;
            }

            for (int i = 0; i < jump; i++)
            {
                for (int j = 0; j < run; j++)
                {
                    float value;
                    if(swapxy) value = table.tableData[j, i];
                    else value = table.tableData[i, j];
                    
                    //compute the equation
                    float result = 0;
                    string eq = dataScaling.Frexpr;
                    eq = eq.Replace("x", value.ToString());
                    //cover our asses here
                    if (CheckDivideByZero(eq))
                    {
                        Debug.WriteLine($"{tableName}: divide by zero avoided, skipping...");
                        continue;
                    }
                    result = float.Parse(dt.Compute(eq, "").ToString());
                    long X = Convert.ToInt64(result);

                    // jump to address
                    if(swapxy) 
                        fileStream.Seek(Convert.ToInt32(table.dataAddress, 16) + i * table.yLength + j, SeekOrigin.Begin);
                    else 
                        fileStream.Seek(Convert.ToInt32(table.dataAddress, 16) + j * table.xLength + i, SeekOrigin.Begin);
                    
                    /*if (swapxy) 
                        Debug.WriteLine(" index is " + i * table.xLength + j );
                    else 
                        Debug.WriteLine(" index is " + j * table.yLength + i);*/

                    //determine in what format to write back to the file and write accordingly
                    switch (dataScaling.Storagetype)
                    {
                        case "uint32": //uint
                            if (X > uint.MaxValue)
                            {
                                Debug.WriteLine("Invalid value to save as");
                                return false;
                            }
                            else
                            {
                                uint bite = (uint)X;
                                binaryWriter.Write(bite);
                                count += 4;
                            }
                            break;
                        case "int32": //int
                            if (X > int.MaxValue)
                            {
                                Debug.WriteLine("Invalid value to save as");
                                return false;
                            }
                            else
                            {
                                int bite = (int)X;
                                binaryWriter.Write(bite);
                                count += 4;
                            }
                            break;
                        case "uint16": //ushort
                            if (X > ushort.MaxValue)
                            {
                                Debug.WriteLine("Invalid value to save as");
                                return false;
                            }
                            else
                            {
                                ushort bite = (ushort)X;
                                binaryWriter.Write(bite);
                                count += 2;
                            }
                            break;
                        case "int16": //short
                            if (X > short.MaxValue)
                            {
                                Debug.WriteLine("Invalid value to save as");
                                return false;
                            }
                            else
                            {
                                short bite = (short)X;
                                binaryWriter.Write(bite);
                                count += 2;
                            }
                            break;
                        case "uint8": //byte
                            if (X > byte.MaxValue)
                            {
                                Debug.WriteLine("Invalid value to save as");
                                return false;
                            }
                            else
                            {
                                byte bite = (byte)X;
                                binaryWriter.Write(bite);
                                count++;
                            }
                            break;
                        case "int8": //sbyte
                            if (X > sbyte.MaxValue)
                            {
                                Debug.WriteLine("Invalid value to save as");
                                return false;
                            }
                            else
                            {
                                sbyte bite = (sbyte)X;
                                binaryWriter.Write(bite);
                                count++;
                            }
                            break;
                    }
                }
            }

            //update the db and clean up
            binaryWriter.Close();
            fileStream.Close();
            table.edited = false;
            dt.Dispose();
            Debug.WriteLine($"{tableName}: Changes saved and written to file");
            return true;
        }

        //master function to read all data from the XML files at xmlCarFilePath and xmlBaseFilePath
        public static void ReadAllFromXML()
        {
            carRom = new Rom("XML car");
            XmlSerializer serializer = new XmlSerializer(typeof(Rom));

            using (FileStream fs = new FileStream(xmlCarFilePath, FileMode.Open))
                carRom = (Rom) serializer.Deserialize(fs);

            //if this definition file includes a base file
            if(carRom != null && carRom.Include != null)
            {
                xmlBaseFileName = carRom.Include + ".xml";
                //find the file path for the base file from our database
                xmlBaseFilePath = SQLiteDataAccess.SearchByName(xmlBaseFileName).filepath;

                baseRom = new Rom("XML base");
                using (FileStream fs = new FileStream(xmlBaseFilePath, FileMode.Open))
                    baseRom = (Rom)serializer.Deserialize(fs);
            }
        }

        //master function to populate all tables in carRom with their appropriate values from the binary file at binFilePath
        public static void FetchAllFromBin()
        {
            //TODO: check out IC Relay Control Vehicle Speed and Lean Spool RPM. High RPM IPW is a lost cause
            FileStream fileStream = new FileStream(binFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            DataTable dt = new DataTable();
            XMLTableInfo? newTable = null;
            foreach (Table tab in carRom.Tables)
            {
                newTable = VerifyAndPopulateTable(tab);
                if (newTable == null)
                    continue;

                //see how many nested tables there are
                if(newTable.dimension == 3)
                {
                    //this is a 3D table [n, n]
                    #region 3D table
                    // get scalings
                    Scaling baseDataScaling = baseRom.Scalings.Find(x => x.Name == newTable.dataScaling);
                    Scaling baseXScaling = baseRom.Scalings.Find(x => x.Name == newTable.xScaling);
                    Scaling baseYScaling = baseRom.Scalings.Find(x => x.Name == newTable.yScaling);

                    if(baseDataScaling == null || (baseXScaling == null && !newTable.xStatic) || (baseYScaling == null && !newTable.yStatic))
                    {
                        Debug.WriteLine($"{tab.Name}: A scaling was not found, skipping...");
                        continue;
                    }

                    newTable.dataLabel = baseDataScaling.Units;
                    newTable.dataFormat = baseDataScaling.Format;
                    if (!newTable.xStatic)
                    {
                        newTable.xName += $" ({baseXScaling.Units})";
                        newTable.xFormat = baseXScaling.Format;
                    }
                    if (!newTable.yStatic)
                    {
                        newTable.yName += $" ({baseYScaling.Units})";
                        newTable.yFormat = baseYScaling.Format;
                    }

                    //now were ready to look through the binary file with info from our baseScalings, tab, and newTable
                    string dataEquation = baseDataScaling.Toexpr;
                    string xEquation = "";
                    string yEquation = "";
                    if (!newTable.xStatic)
                        xEquation = baseXScaling.Toexpr;
                    if (!newTable.yStatic)
                        yEquation = baseYScaling.Toexpr;
                    byte b1 = 0;
                    byte[] b2 = { 0, 0 };

                    //get array sizes set correctly
                    if (!newTable.xStatic)
                        newTable.xLabels = new float[newTable.xLength];
                    if (!newTable.yStatic)
                        newTable.yLabels = new float[newTable.yLength];
                    newTable.tableData = new float[newTable.yLength, newTable.xLength];

                    //lets start off with the X axis if its not static
                    if (!newTable.xStatic)
                    {
                        //find the address
                        fileStream.Seek(Convert.ToInt32(newTable.xAddress, 16), SeekOrigin.Begin);

                        for (int i = 0; i < newTable.xLength; i++)
                        {
                            //read the correct amount of bytes and apply endianess
                            if (baseXScaling.Storagetype == "uint32" || baseXScaling.Storagetype == "int32")
                            {
                                b2 = binaryReader.ReadBytes(4);
                                if (baseXScaling.Endian == "big")
                                    Array.Reverse(b2);
                            }
                            else if (baseXScaling.Storagetype == "uint16" || baseXScaling.Storagetype == "int16")
                            {
                                b2 = binaryReader.ReadBytes(2);
                                if (baseXScaling.Endian == "big")
                                    Array.Reverse(b2);
                            }
                            else
                            {
                                b1 = binaryReader.ReadByte();
                            }

                            //set up to expression based on storage type
                            switch (baseXScaling.Storagetype.Trim())
                            {
                                case "uint32": //uint
                                    xEquation = xEquation.Replace("x", BitConverter.ToUInt32(b2, 0).ToString());
                                    break;
                                case "int32": //int
                                    xEquation = xEquation.Replace("x", BitConverter.ToInt32(b2, 0).ToString());
                                    break;
                                case "uint16": //ushort
                                    xEquation = xEquation.Replace("x", BitConverter.ToUInt16(b2, 0).ToString());
                                    break;
                                case "int16": //short
                                    xEquation = xEquation.Replace("x", BitConverter.ToInt16(b2, 0).ToString());
                                    break;
                                case "uint8": //byte
                                    xEquation = xEquation.Replace("x", b1.ToString());
                                    break;
                                case "int8": //sbyte
                                    xEquation = xEquation.Replace("x", ((sbyte)b1).ToString());
                                    break;
                            }

                            //cover our asses here
                            if (CheckDivideByZero(xEquation))
                            {
                                Debug.WriteLine($"{tab.Name}: divide by zero avoided, skipping...");
                                continue;
                            }

                            newTable.xLabels[i] = float.Parse(dt.Compute(xEquation, "").ToString());
                            //reset the equation back to having an x
                            xEquation = baseXScaling.Toexpr;
                        }
                    }

                    //do the same for the Y axis if its not static
                    if (!newTable.yStatic)
                    {
                        //find the address
                        fileStream.Seek(Convert.ToInt32(newTable.yAddress, 16), SeekOrigin.Begin);

                        for (int i = 0; i < newTable.yLength; i++)
                        {
                            //read the correct amount of bytes and apply endianess
                            if (baseYScaling.Storagetype == "uint32" || baseYScaling.Storagetype == "int32")
                            {
                                b2 = binaryReader.ReadBytes(4);
                                if (baseYScaling.Endian == "big")
                                    Array.Reverse(b2);
                            }
                            else if (baseYScaling.Storagetype == "uint16" || baseYScaling.Storagetype == "int16")
                            {
                                b2 = binaryReader.ReadBytes(2);
                                if (baseYScaling.Endian == "big")
                                    Array.Reverse(b2);
                            }
                            else
                            {
                                b1 = binaryReader.ReadByte();
                            }

                            //set up to expression based on storage type
                            switch (baseYScaling.Storagetype.Trim())
                            {
                                case "uint32": //uint
                                    yEquation = yEquation.Replace("x", BitConverter.ToUInt32(b2, 0).ToString());
                                    break;
                                case "int32": //int
                                    yEquation = yEquation.Replace("x", BitConverter.ToInt32(b2, 0).ToString());
                                    break;
                                case "uint16": //ushort
                                    yEquation = yEquation.Replace("x", BitConverter.ToUInt16(b2, 0).ToString());
                                    break;
                                case "int16": //short
                                    yEquation = yEquation.Replace("x", BitConverter.ToInt16(b2, 0).ToString());
                                    break;
                                case "uint8": //byte
                                    yEquation = yEquation.Replace("x", b1.ToString());
                                    break;
                                case "int8": //sbyte
                                    yEquation = yEquation.Replace("x", ((sbyte)b1).ToString());
                                    break;
                            }

                            //cover our asses here
                            if (CheckDivideByZero(yEquation))
                            {
                                Debug.WriteLine($"{tab.Name}: divide by zero avoided, skipping...");
                                continue;
                            }

                            newTable.yLabels[i] = float.Parse(dt.Compute(yEquation, "").ToString());
                            //reset the equation back to having an x
                            yEquation = baseYScaling.Toexpr;
                        }
                    }

                    //do the same one last time for the actual data
                    //find the address
                    fileStream.Seek(Convert.ToInt32(newTable.dataAddress, 16), SeekOrigin.Begin);

                    int run = 0, jump = 0;
                    //if swapxy is true swap the num elements
                    if (newTable.swapxy.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        run = newTable.yLength;
                        jump = newTable.xLength;
                    }
                    else
                    {
                        run = newTable.xLength;
                        jump = newTable.yLength;
                    }

                    for (int i = 0; i < jump; i++)
                    {
                        for(int j = 0; j < run; j++)
                        {
                            //read the correct amount of bytes and apply endianess
                            if (baseDataScaling.Storagetype == "uint32" || baseDataScaling.Storagetype == "int32")
                            {
                                b2 = binaryReader.ReadBytes(4);
                                if (baseDataScaling.Endian == "big")
                                    Array.Reverse(b2);
                            }
                            else if (baseDataScaling.Storagetype == "uint16" || baseDataScaling.Storagetype == "int16")
                            {
                                b2 = binaryReader.ReadBytes(2);
                                if (baseDataScaling.Endian == "big")
                                    Array.Reverse(b2);
                            }
                            else
                            {
                                b1 = binaryReader.ReadByte();
                            }

                            //set up to expression based on storage type
                            switch (baseDataScaling.Storagetype.Trim())
                            {
                                case "uint32": //uint
                                    dataEquation = dataEquation.Replace("x", BitConverter.ToUInt32(b2, 0).ToString());
                                    break;
                                case "int32": //int
                                    dataEquation = dataEquation.Replace("x", BitConverter.ToInt32(b2, 0).ToString());
                                    break;
                                case "uint16": //ushort
                                    dataEquation = dataEquation.Replace("x", BitConverter.ToUInt16(b2, 0).ToString());
                                    break;
                                case "int16": //short
                                    dataEquation = dataEquation.Replace("x", BitConverter.ToInt16(b2, 0).ToString());
                                    break;
                                case "uint8": //byte
                                    dataEquation = dataEquation.Replace("x", b1.ToString());
                                    break;
                                case "int8": //sbyte
                                    dataEquation = dataEquation.Replace("x", ((sbyte)b1).ToString());
                                    break;
                            }

                            //cover our asses here
                            if (CheckDivideByZero(dataEquation))
                            {
                                Debug.WriteLine($"{tab.Name}: divide by zero avoided, skipping...");
                                continue;
                            }

                            if (newTable.swapxy.Equals("true", StringComparison.OrdinalIgnoreCase))
                                newTable.tableData[j, i] = float.Parse(dt.Compute(dataEquation, "").ToString());
                            else
                                newTable.tableData[i, j] = float.Parse(dt.Compute(dataEquation, "").ToString());
                            //reset the equation back to having an x
                            dataEquation = baseDataScaling.Toexpr;
                        }
                    }
                    #endregion
                }
                else if(newTable.dimension == 2)
                {
                    //this is a 2D table either [n, 1] or [1, n]
                    #region 2D Table
                    // get scalings
                    Scaling baseOuterScaling = baseRom.Scalings.Find(x => x.Name == newTable.dataScaling);
                    Scaling baseInnerScaling = null;

                    if(newTable.xORy2D.Contains("X", StringComparison.OrdinalIgnoreCase) && !newTable.xStatic)
                        baseInnerScaling = baseRom.Scalings.Find(x => x.Name == newTable.xScaling);
                    else if(newTable.xORy2D.Contains("Y", StringComparison.OrdinalIgnoreCase) && !newTable.yStatic)
                        baseInnerScaling = baseRom.Scalings.Find(x => x.Name == newTable.yScaling);

                    if (baseOuterScaling == null || (baseInnerScaling == null && (!newTable.xStatic && !newTable.yStatic)))
                    {
                        Debug.WriteLine($"{tab.Name}: A scaling was not found, skipping...");
                        continue;
                    }

                    if(newTable.dataScaling == "blobbits")
                    {
                        Debug.WriteLine($"{tab.Name}: blobbits not implemented yet...");
                        continue;
                    }

                    newTable.dataLabel = baseOuterScaling.Units;
                    newTable.dataFormat = baseOuterScaling.Format;

                    //now were ready to look through the binary file with info from our baseTable, baseScalings, and tab
                    string outerEquation = baseOuterScaling.Toexpr;
                    string innerEquation = "";
                    int elements = 0;
                    byte b1 = 0;
                    byte[] b2 = { 0, 0 };

                    // check if we have a second scaler
                    if (baseInnerScaling != null)
                        innerEquation = baseInnerScaling.Toexpr;

                    // get array set
                    if (newTable.xORy2D.Contains("Y", StringComparison.OrdinalIgnoreCase))
                    {
                        newTable.tableData = new float[newTable.yLength, 1];
                        elements = newTable.yLength;
                        if (!newTable.yStatic)
                        {
                            newTable.yName += $" ({baseInnerScaling.Units})";
                            newTable.yLabels = new float[newTable.yLength];
                            newTable.yFormat = baseInnerScaling.Format;
                        }
                    }
                    else if (newTable.xORy2D.Contains("X", StringComparison.OrdinalIgnoreCase))
                    {
                        newTable.tableData = new float[1, newTable.xLength];
                        elements = newTable.xLength;
                        if (!newTable.xStatic)
                        {
                            newTable.xName += $" ({baseInnerScaling.Units})";
                            newTable.xLabels = new float[newTable.xLength];
                            newTable.xFormat = baseInnerScaling.Format;
                        }
                    }

                    //find the address for the data
                    fileStream.Seek(Convert.ToInt32(newTable.dataAddress, 16), SeekOrigin.Begin);

                    //should be for the actual data 
                    for (int i = 0; i < elements; i++)
                    {
                        //read the correct amount of bytes and apply endianess
                        if (baseOuterScaling.Storagetype == "uint32" || baseOuterScaling.Storagetype == "int32")
                        {
                            b2 = binaryReader.ReadBytes(4);
                            if (baseOuterScaling.Endian == "big")
                                Array.Reverse(b2);
                        }
                        else if (baseOuterScaling.Storagetype == "uint16" || baseOuterScaling.Storagetype == "int16")
                        {
                            b2 = binaryReader.ReadBytes(2);
                            if (baseOuterScaling.Endian == "big")
                                Array.Reverse(b2);
                        }
                        else
                        {
                            b1 = binaryReader.ReadByte();
                        }

                        //set up to expression based on storage type
                        switch (baseOuterScaling.Storagetype.Trim())
                        {
                            case "uint32": //uint
                                outerEquation = outerEquation.Replace("x", BitConverter.ToUInt32(b2, 0).ToString());
                                break;
                            case "int32": //int
                                outerEquation = outerEquation.Replace("x", BitConverter.ToInt32(b2, 0).ToString());
                                break;
                            case "uint16": //ushort
                                outerEquation = outerEquation.Replace("x", BitConverter.ToUInt16(b2, 0).ToString());
                                break;
                            case "int16": //short
                                outerEquation = outerEquation.Replace("x", BitConverter.ToInt16(b2, 0).ToString());
                                break;
                            case "uint8": //byte
                                outerEquation = outerEquation.Replace("x", b1.ToString());
                                break;
                            case "int8": //sbyte
                                outerEquation = outerEquation.Replace("x", ((sbyte)b1).ToString());
                                break;
                        }

                        //cover our asses here
                        if (CheckDivideByZero(outerEquation))
                        {
                            Debug.WriteLine($"{tab.Name}: divide by zero avoided, skipping...");
                            continue;
                        }

                        //assign the values correctly based on how our tableData array was formatted
                        if (newTable.xORy2D.Contains("Y", StringComparison.OrdinalIgnoreCase))
                        {
                            if (newTable.yFlip)
                                newTable.tableData[elements - i - 1, 0] = float.Parse(dt.Compute(outerEquation, "").ToString());
                            else
                                newTable.tableData[i, 0] = float.Parse(dt.Compute(outerEquation, "").ToString());
                        }
                        else if (newTable.xORy2D.Contains("X", StringComparison.OrdinalIgnoreCase))
                        {
                            if (newTable.xFlip)
                                newTable.tableData[0, elements - i - 1] = float.Parse(dt.Compute(outerEquation, "").ToString());
                            else
                                newTable.tableData[0, i] = float.Parse(dt.Compute(outerEquation, "").ToString());
                        }

                        //reset the equation to have an x in it again
                        outerEquation = baseOuterScaling.Toexpr;
                    }

                    //SAME THING NOW FOR THE NESTED TABLE. This should be the X or Y axis values
                    if (baseInnerScaling == null && newTable.xStatic || newTable.yStatic)
                    {
                        tablesToDisplay.Add(newTable);
                        continue;
                    }
                    if (newTable.xORy2D.Contains("Y", StringComparison.OrdinalIgnoreCase))
                        fileStream.Seek(Convert.ToInt32(newTable.yAddress, 16), SeekOrigin.Begin);
                    else if (newTable.xORy2D.Contains("X", StringComparison.OrdinalIgnoreCase))
                        fileStream.Seek(Convert.ToInt32(newTable.xAddress, 16), SeekOrigin.Begin);

                    for (int k = 0; k < elements; k++)
                    {
                        //read the correct amount of bytes and apply endianess
                        if (baseInnerScaling.Storagetype == "uint32" || baseInnerScaling.Storagetype == "int32")
                        {
                            b2 = binaryReader.ReadBytes(4);
                            if (baseInnerScaling.Endian == "big")
                                Array.Reverse(b2);
                        }
                        else if (baseInnerScaling.Storagetype == "uint16" || baseInnerScaling.Storagetype == "int16")
                        {
                            b2 = binaryReader.ReadBytes(2);
                            if (baseInnerScaling.Endian == "big")
                                Array.Reverse(b2);
                        }
                        else
                        {
                            b1 = binaryReader.ReadByte();
                        }

                        //set up to expression based on storage type
                        switch (baseInnerScaling.Storagetype.Trim())
                        {
                            case "uint32": //uint
                                innerEquation = innerEquation.Replace("x", BitConverter.ToUInt32(b2, 0).ToString());
                                break;
                            case "int32": //int
                                innerEquation = innerEquation.Replace("x", BitConverter.ToInt32(b2, 0).ToString());
                                break;
                            case "uint16": //ushort
                                innerEquation = innerEquation.Replace("x", BitConverter.ToUInt16(b2, 0).ToString());
                                break;
                            case "int16": //short
                                innerEquation = innerEquation.Replace("x", BitConverter.ToInt16(b2, 0).ToString());
                                break;
                            case "uint8": //byte
                                innerEquation = innerEquation.Replace("x", b1.ToString());
                                break;
                            case "int8": //sbyte
                                innerEquation = innerEquation.Replace("x", ((sbyte)b1).ToString());
                                break;
                        }

                        //cover our asses here
                        if (CheckDivideByZero(innerEquation))
                        {
                            Debug.WriteLine($"{tab.Name}: divide by zero avoided, skipping...");
                            continue;
                        }

                        //assign the values correctly based on how our tableData array was formatted
                        if (newTable.xORy2D.Contains("Y", StringComparison.OrdinalIgnoreCase))
                            newTable.yLabels[k] = float.Parse(dt.Compute(innerEquation, "").ToString());
                        else if (newTable.xORy2D.Contains("X", StringComparison.OrdinalIgnoreCase))
                            newTable.xLabels[k] = float.Parse(dt.Compute(innerEquation, "").ToString());

                        //reset the equation back to having an x
                        innerEquation = baseInnerScaling.Toexpr;
                    }
                    
                    #endregion
                }
                else if (newTable.dimension == 1)
                {
                    //there are no nested tables this is a 1D table [1, 1]
                    #region 1D Table
                    Scaling baseScaling = baseRom.Scalings.Find(x => x.Name == newTable.dataScaling);

                    if (baseScaling == null)
                    {
                        Debug.WriteLine($"{tab.Name}: scaling not found, skipping...");
                        continue;
                    }

                    newTable.dataLabel = baseScaling.Units;
                    newTable.dataFormat = baseScaling.Format;

                    //now were ready to look through the binary file with info from our baseTable, baseScaling, and tab
                    string equation = baseScaling.Toexpr;
                    byte b1 = 0;
                    byte[] b2 = {0, 0};
                    newTable.tableData = new float[1, 1];

                    //find the address
                    fileStream.Seek(Convert.ToInt32(newTable.dataAddress, 16), SeekOrigin.Begin);

                    //TODO: if units = hex in the scaling from the base file: display as hex

                    //read the correct amount of bytes and apply endianess
                    if (baseScaling.Storagetype == "uint32" || baseScaling.Storagetype == "int32")
                    {
                        b2 = binaryReader.ReadBytes(4);
                        if (baseScaling.Endian == "big")
                            Array.Reverse(b2);
                    }
                    else if (baseScaling.Storagetype == "uint16" || baseScaling.Storagetype == "int16")
                    {
                        b2 = binaryReader.ReadBytes(2);
                        if(baseScaling.Endian == "big")
                            Array.Reverse(b2);
                    }
                    else
                    {
                        b1 = binaryReader.ReadByte();
                    }

                    //set up to expression based on storage type
                    switch(baseScaling.Storagetype.Trim())
                    {
                        case "uint32": //uint
                            equation = equation.Replace("x", BitConverter.ToUInt32(b2, 0).ToString());
                            break;
                        case "int32": //int
                            equation = equation.Replace("x", BitConverter.ToInt32(b2, 0).ToString());
                            break;
                        case "uint16": //ushort
                            equation = equation.Replace("x", BitConverter.ToUInt16(b2, 0).ToString());
                            break;
                        case "int16": //short
                            equation = equation.Replace("x", BitConverter.ToInt16(b2, 0).ToString());
                            break;
                        case "uint8": //byte
                            equation = equation.Replace("x", b1.ToString());
                            break;
                        case "int8": //sbyte
                            equation = equation.Replace("x", ((sbyte)b1).ToString());
                            break;
                    }

                    //cover our asses here
                    if(CheckDivideByZero(equation))
                    {
                        Debug.WriteLine($"{tab.Name}: divide by zero avoided, skipping...");
                        continue;
                    }

                    //evaluate the equation and store it in the new table
                    newTable.tableData[0, 0] = float.Parse(dt.Compute(equation, "").ToString());
                    #endregion
                }
                tablesToDisplay.Add(newTable);
            }
            fileStream.Close();
            binaryReader.Close();
            dt.Dispose();
            //PrintAllTableData();
        }

        //given a string that is an equation, checks for if the equation will divide by zero
        //returns true if it will and false if not
        private static bool CheckDivideByZero(string equation)
        {
            if (equation == null)
                return false;

            for(int i = 0; i < equation.Length; i++)
            {
                if (equation[i] == '/')
                {
                    if (equation[i + 1] == '0')
                        return true;
                }
            }

            return false;
        }

        //given a Table, goes through and verifies it is valid to be displayed to the user
        //this will populate an XMLTableInfo object which is then returned if the table is valid
        //otherwise it will return null and probably tell you why in debug output
        public static XMLTableInfo? VerifyAndPopulateTable(Table tab)
        {
            XMLTableInfo newInfo;
            Table baseTable = null;

            //assign dimension and type correctly
            if (tab.NestedTables.Count == 2)
            {
                if (tab.Name == null || tab.NestedTables[0].Name == null || tab.NestedTables[1].Name == null)
                {
                    Debug.WriteLine("A name is missing from a table in the car file, skipping...");
                    return null;
                }
                newInfo = new(3);
            }
            else if (tab.NestedTables.Count == 1)
            {
                if (tab.Name == null || tab.NestedTables[0].Name == null)
                {
                    Debug.WriteLine("A name is missing from a table in the car file, skipping...");
                    return null;
                }
                newInfo = new(2);
            }
            else if (tab.NestedTables.Count == 0)
            {
                if (tab.Name == null)
                {
                    Debug.WriteLine("A name is missing from a table in the car file, skipping...");
                    return null;
                }
                newInfo = new(1);
            }
            else
                return null;

            //find the table in baseRom who's name matches this table's name
            baseTable = baseRom.Tables.Find(x => Regex.Replace(x.Name, @"\s+", String.Empty).Contains(Regex.Replace(tab.Name, @"\s+", String.Empty), StringComparison.OrdinalIgnoreCase));

            //if we cant find it the first time, lower our expectations...
            if (baseTable == null)
            {
                string temp = tab.Name; //replace extra characters with nothing 
                baseTable = baseRom.Tables.Find(x => x.Name.Contains(Regex.Replace(temp, @"[^\w\d\s]", "")));
                if (baseTable == null) //still not found
                {
                    Debug.WriteLine($"{tab.Name}: was not found in the base file, skipping...");
                    return null;
                }
            }

            //if we got this far baseTable is not null
            newInfo.title = tab.Name;

            //category
            if (tab.Category == null && baseTable.Category == null)
            {
                newInfo.category = "Uncategorized";
            }
            else if (tab.Category != null && baseTable.Category != null)
            {
                if (!Regex.Replace(tab.Category, @"\s+", String.Empty).Contains(Regex.Replace(baseTable.Category, @"\s+", String.Empty), StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"{tab.Name}: category discrepancy between car and base file, car file takes precedence");
                }
                newInfo.category = tab.Category;
            }
            else if (tab.Category != null && baseTable.Category == null)
            {
                newInfo.category = tab.Category;
            }
            else if (tab.Category == null && baseTable.Category != null)
            {
                newInfo.category = baseTable.Category;
            }
            //level e.g. 1(developer), 2(advanced), 3(intermediate), or 4(beginner) 
            if (tab.Level == null && baseTable.Level == null)
            {
                newInfo.level = "4";
            }
            else if (tab.Level != null && baseTable.Level != null)
            {
                if (!Regex.Replace(tab.Level, @"\s+", String.Empty).Contains(Regex.Replace(baseTable.Level, @"\s+", String.Empty), StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"{tab.Name}: level discrepancy between car and base file, car file takes precedence");
                }
                newInfo.level = tab.Level;
            }
            else if (tab.Level != null && baseTable.Level == null)
            {
                newInfo.level = tab.Level;
            }
            else if (tab.Level == null && baseTable.Level != null)
            {
                newInfo.level = baseTable.Level;
            }
            //dataAddress
            if (tab.Address == null && baseTable.Address == null)
            {
                Debug.WriteLine($"{tab.Name}: address missing for data, skipping...");
                return null;
            }
            else if (tab.Address != null && baseTable.Address != null)
            {
                if (!Regex.Replace(tab.Address, @"\s+", String.Empty).Contains(Regex.Replace(baseTable.Address, @"\s+", String.Empty), StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"{tab.Name}: dataAddress discrepancy between car and base file, car file takes precedence");
                }
                newInfo.dataAddress = tab.Address;
            }
            else if (tab.Address != null && baseTable.Address == null)
            {
                newInfo.dataAddress = tab.Address;
            }
            else if (tab.Address == null && baseTable.Address != null)
            {
                newInfo.dataAddress = baseTable.Address;
            }
            //dataScaling
            if (tab.Scaling == null && baseTable.Scaling == null)
            {
                Debug.WriteLine($"{tab.Name}: scaling missing for data, skipping...");
                return null;
            }
            else if (tab.Scaling != null && baseTable.Scaling != null)
            {
                if (!Regex.Replace(tab.Scaling, @"\s+", String.Empty).Contains(Regex.Replace(baseTable.Scaling, @"\s+", String.Empty), StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"{tab.Name}: dataScaling discrepancy between car and base file, car file takes precedence");
                }
                newInfo.dataScaling = tab.Scaling;
            }
            else if (tab.Scaling != null && baseTable.Scaling == null)
            {
                newInfo.dataScaling = tab.Scaling;
            }
            else if (tab.Scaling == null && baseTable.Scaling != null)
            {
                newInfo.dataScaling = baseTable.Scaling;
            }

            if (newInfo.dimension == 3) //3D
            {
                //swapxy
                if (tab.Swapxy == null && baseTable.Swapxy == null)
                {
                    newInfo.swapxy = "false";
                }
                else if (tab.Swapxy != null && baseTable.Swapxy != null)
                {
                    if (!Regex.Replace(tab.Swapxy, @"\s+", String.Empty).Contains(Regex.Replace(baseTable.Swapxy, @"\s+", String.Empty), StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"{tab.Name}: swapxy discrepancy between car and base file, car file takes precedence");
                    }
                    newInfo.swapxy = tab.Swapxy;
                }
                else if (tab.Swapxy != null && baseTable.Swapxy == null)
                {
                    newInfo.swapxy = tab.Swapxy;
                }
                else if (tab.Swapxy == null && baseTable.Swapxy != null)
                {
                    newInfo.swapxy = baseTable.Swapxy;
                }
                //check type
                string temp1 = "", temp2 = "";
                if (tab.NestedTables[0].Type == null && baseTable.NestedTables[0].Type == null)
                {
                    Debug.WriteLine($"{tab.Name}: type missing for an axis, skipping...");
                    return null;
                }
                else if (tab.NestedTables[0].Type != null && baseTable.NestedTables[0].Type != null)
                {
                    if (!Regex.Replace(tab.NestedTables[0].Type, @"\s+", String.Empty).Contains(Regex.Replace(baseTable.NestedTables[0].Type, @"\s+", String.Empty), StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"{tab.Name}: axis type discrepancy between car and base file, car file takes precedence");
                    }
                    temp1 = tab.NestedTables[0].Type;
                }
                else if (tab.NestedTables[0].Type != null && baseTable.NestedTables[0].Type == null)
                {
                    temp1 = tab.NestedTables[0].Type;
                }
                else if (tab.NestedTables[0].Type == null && baseTable.NestedTables[0].Type != null)
                {
                    temp1 = baseTable.NestedTables[0].Type;
                }

                if (tab.NestedTables[1].Type == null && baseTable.NestedTables[1].Type == null)
                {
                    Debug.WriteLine($"{tab.Name}: type missing for an axis, skipping...");
                    return null;
                }
                else if (tab.NestedTables[1].Type != null && baseTable.NestedTables[1].Type != null)
                {
                    if (!Regex.Replace(tab.NestedTables[1].Type, @"\s+", String.Empty).Contains(Regex.Replace(baseTable.NestedTables[1].Type, @"\s+", String.Empty), StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"{tab.Name}: axis type discrepancy between car and base file, car file takes precedence");
                    }
                    temp2 = tab.NestedTables[1].Type;
                }
                else if (tab.NestedTables[1].Type != null && baseTable.NestedTables[1].Type == null)
                {
                    temp2 = tab.NestedTables[1].Type;
                }
                else if (tab.NestedTables[1].Type == null && baseTable.NestedTables[1].Type != null)
                {
                    temp2 = baseTable.NestedTables[1].Type;
                }
                //names
                if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                    newInfo.xName = tab.NestedTables[0].Name;
                else
                    newInfo.yName = tab.NestedTables[0].Name;

                if (temp2.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                    newInfo.xName = tab.NestedTables[1].Name;
                else
                    newInfo.yName = tab.NestedTables[1].Name;
                if (!temp1.Contains("Static", StringComparison.OrdinalIgnoreCase))
                {
                    //Address1
                    if (tab.NestedTables[0].Address == null && baseTable.NestedTables[0].Address == null)
                    {
                        Debug.WriteLine($"{tab.Name}: address missing for an axis, skipping...");
                        return null;
                    }
                    else if (tab.NestedTables[0].Address != null && baseTable.NestedTables[0].Address != null)
                    {
                        if (!Regex.Replace(tab.NestedTables[0].Address, @"\s+", String.Empty).Contains(Regex.Replace(baseTable.NestedTables[0].Address, @"\s+", String.Empty), StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.WriteLine($"{tab.Name}: axis address discrepancy between car and base file, car file takes precedence");
                        }

                        if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                            newInfo.xAddress = tab.NestedTables[0].Address;
                        else
                            newInfo.yAddress = tab.NestedTables[0].Address;
                    }
                    else if (tab.NestedTables[0].Address != null && baseTable.NestedTables[0].Address == null)
                    {
                        if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                            newInfo.xAddress = tab.NestedTables[0].Address;
                        else
                            newInfo.yAddress = tab.NestedTables[0].Address;
                    }
                    else if (tab.NestedTables[0].Address == null && baseTable.NestedTables[0].Address != null)
                    {
                        if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                            newInfo.xAddress = baseTable.NestedTables[0].Address;
                        else
                            newInfo.yAddress = baseTable.NestedTables[0].Address;
                    }
                    //Scaling1
                    if (tab.NestedTables[0].Scaling == null && baseTable.NestedTables[0].Scaling == null)
                    {
                        Debug.WriteLine($"{tab.Name}: scaling missing for an axis, skipping...");
                        return null;
                    }
                    else if (tab.NestedTables[0].Scaling != null && baseTable.NestedTables[0].Scaling != null)
                    {
                        if (!Regex.Replace(tab.NestedTables[0].Scaling, @"\s+", String.Empty).Contains(Regex.Replace(baseTable.NestedTables[0].Scaling, @"\s+", String.Empty), StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.WriteLine($"{tab.Name}: axis scaling discrepancy between car and base file, car file takes precedence");
                        }

                        if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                            newInfo.xScaling = tab.NestedTables[0].Scaling;
                        else
                            newInfo.yScaling = tab.NestedTables[0].Scaling;
                    }
                    else if (tab.NestedTables[0].Scaling != null && baseTable.NestedTables[0].Scaling == null)
                    {
                        if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                            newInfo.xScaling = tab.NestedTables[0].Scaling;
                        else
                            newInfo.yScaling = tab.NestedTables[0].Scaling;
                    }
                    else if (tab.NestedTables[0].Scaling == null && baseTable.NestedTables[0].Scaling != null)
                    {
                        if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                            newInfo.xScaling = baseTable.NestedTables[0].Scaling;
                        else
                            newInfo.yScaling = baseTable.NestedTables[0].Scaling;
                    }
                }
                //Length1
                if (tab.NestedTables[0].Elements == null && baseTable.NestedTables[0].Elements == null)
                {
                    Debug.WriteLine($"{tab.Name}: elements missing for an axis, skipping...");
                    return null;
                }
                else if (tab.NestedTables[0].Elements != null && baseTable.NestedTables[0].Elements != null)
                {
                    if (!Regex.Replace(tab.NestedTables[0].Elements, @"\s+", String.Empty).Contains(Regex.Replace(baseTable.NestedTables[0].Elements, @"\s+", String.Empty), StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"{tab.Name}: axis elements discrepancy between car and base file, car file takes precedence");
                    }

                    if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                        newInfo.xLength = int.Parse(tab.NestedTables[0].Elements);
                    else
                        newInfo.yLength = int.Parse(tab.NestedTables[0].Elements);
                }
                else if (tab.NestedTables[0].Elements != null && baseTable.NestedTables[0].Elements == null)
                {
                    if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                        newInfo.xLength = int.Parse(tab.NestedTables[0].Elements);
                    else
                        newInfo.yLength = int.Parse(tab.NestedTables[0].Elements);
                }
                else if (tab.NestedTables[0].Elements == null && baseTable.NestedTables[0].Elements != null)
                {
                    if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                        newInfo.xLength = int.Parse(baseTable.NestedTables[0].Elements);
                    else
                        newInfo.yLength = int.Parse(baseTable.NestedTables[0].Elements);
                }
                if (!temp1.Contains("Static", StringComparison.OrdinalIgnoreCase))
                {
                    //Address2
                    if (tab.NestedTables[1].Address == null && baseTable.NestedTables[1].Address == null)
                    {
                        Debug.WriteLine($"{tab.Name}: address missing for an axis, skipping...");
                        return null;
                    }
                    else if (tab.NestedTables[1].Address != null && baseTable.NestedTables[1].Address != null)
                    {
                        if (!Regex.Replace(tab.NestedTables[1].Address, @"\s+", String.Empty).Contains(Regex.Replace(baseTable.NestedTables[1].Address, @"\s+", String.Empty), StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.WriteLine($"{tab.Name}: axis address discrepancy between car and base file, car file takes precedence");
                        }

                        if (temp2.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                            newInfo.xAddress = tab.NestedTables[1].Address;
                        else
                            newInfo.yAddress = tab.NestedTables[1].Address;
                    }
                    else if (tab.NestedTables[1].Address != null && baseTable.NestedTables[1].Address == null)
                    {
                        if (temp2.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                            newInfo.xAddress = tab.NestedTables[1].Address;
                        else
                            newInfo.yAddress = tab.NestedTables[1].Address;
                    }
                    else if (tab.NestedTables[1].Address == null && baseTable.NestedTables[1].Address != null)
                    {
                        if (temp2.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                            newInfo.xAddress = baseTable.NestedTables[1].Address;
                        else
                            newInfo.yAddress = baseTable.NestedTables[1].Address;
                    }
                    //Scaling2
                    if (tab.NestedTables[1].Scaling == null && baseTable.NestedTables[1].Scaling == null)
                    {
                        Debug.WriteLine($"{tab.Name}: scaling missing for an axis, skipping...");
                        return null;
                    }
                    else if (tab.NestedTables[1].Scaling != null && baseTable.NestedTables[1].Scaling != null)
                    {
                        if (!Regex.Replace(tab.NestedTables[1].Scaling, @"\s+", String.Empty).Contains(Regex.Replace(baseTable.NestedTables[1].Scaling, @"\s+", String.Empty), StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.WriteLine($"{tab.Name}: axis scaling discrepancy between car and base file, car file takes precedence");
                        }

                        if (temp2.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                            newInfo.xScaling = tab.NestedTables[1].Scaling;
                        else
                            newInfo.yScaling = tab.NestedTables[1].Scaling;
                    }
                    else if (tab.NestedTables[1].Scaling != null && baseTable.NestedTables[1].Scaling == null)
                    {
                        if (temp2.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                            newInfo.xScaling = tab.NestedTables[1].Scaling;
                        else
                            newInfo.yScaling = tab.NestedTables[1].Scaling;
                    }
                    else if (tab.NestedTables[1].Scaling == null && baseTable.NestedTables[1].Scaling != null)
                    {
                        if (temp2.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                            newInfo.xScaling = baseTable.NestedTables[1].Scaling;
                        else
                            newInfo.yScaling = baseTable.NestedTables[1].Scaling;
                    }
                }
                //Length2
                if (tab.NestedTables[1].Elements == null && baseTable.NestedTables[1].Elements == null)
                {
                    Debug.WriteLine($"{tab.Name}: elements missing for an axis, skipping...");
                    return null;
                }
                else if (tab.NestedTables[1].Elements != null && baseTable.NestedTables[1].Elements != null)
                {
                    if (!Regex.Replace(tab.NestedTables[1].Elements, @"\s+", String.Empty).Contains(Regex.Replace(baseTable.NestedTables[1].Elements, @"\s+", String.Empty), StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"{tab.Name}: axis elements discrepancy between car and base file, car file takes precedence");
                    }

                    if (temp2.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                        newInfo.xLength = int.Parse(tab.NestedTables[1].Elements);
                    else
                        newInfo.yLength = int.Parse(tab.NestedTables[1].Elements);
                }
                else if (tab.NestedTables[1].Elements != null && baseTable.NestedTables[1].Elements == null)
                {
                    if (temp2.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                        newInfo.xLength = int.Parse(tab.NestedTables[1].Elements);
                    else
                        newInfo.yLength = int.Parse(tab.NestedTables[1].Elements);
                }
                else if (tab.NestedTables[1].Elements == null && baseTable.NestedTables[1].Elements != null)
                {
                    if (temp2.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                        newInfo.xLength = int.Parse(baseTable.NestedTables[1].Elements);
                    else
                        newInfo.yLength = int.Parse(baseTable.NestedTables[1].Elements);
                }
                //check for static X or Y axes
                if (temp1.Contains("Static X Axis", StringComparison.OrdinalIgnoreCase))
                {
                    newInfo.xStatic = true;
                    newInfo.xStringLabels = new string[newInfo.xLength];
                    if (tab.NestedTables[0].DataText != null && tab.NestedTables[0].DataText.Count > 0)
                    {
                        for (int i = 0; i < newInfo.xLength && i < tab.NestedTables[0].DataText.Count; i++)
                            newInfo.xStringLabels[i] = tab.NestedTables[0].DataText[i];
                    }
                    else if (baseTable.NestedTables[0].DataText != null && baseTable.NestedTables[0].DataText.Count > 0)
                    {
                        for (int i = 0; i < newInfo.xLength && i < baseTable.NestedTables[0].DataText.Count; i++)
                            newInfo.xStringLabels[i] = baseTable.NestedTables[0].DataText[i];
                    }
                    else
                    {
                        Debug.WriteLine($"{tab.Name}: missing static axis values, skipping...");
                        return null;
                    }
                }
                else if (temp1.Contains("Static Y Axis", StringComparison.OrdinalIgnoreCase))
                {
                    newInfo.yStatic = true;
                    newInfo.yStringLabels = new string[newInfo.yLength];
                    if (tab.NestedTables[0].DataText != null && tab.NestedTables[0].DataText.Count > 0)
                    {
                        for (int i = 0; i < newInfo.yLength && i < tab.NestedTables[0].DataText.Count; i++)
                            newInfo.yStringLabels[i] = tab.NestedTables[0].DataText[i];
                    }
                    else if (baseTable.NestedTables[0].DataText != null && baseTable.NestedTables[0].DataText.Count > 0)
                    {
                        for (int i = 0; i < newInfo.yLength && i < baseTable.NestedTables[0].DataText.Count; i++)
                            newInfo.yStringLabels[i] = baseTable.NestedTables[0].DataText[i];
                    }
                    else
                    {
                        Debug.WriteLine($"{tab.Name}: missing static axis values, skipping...");
                        return null;
                    }
                }
                if (temp2.Contains("Static X Axis", StringComparison.OrdinalIgnoreCase))
                {
                    newInfo.xStatic = true;
                    newInfo.xStringLabels = new string[newInfo.xLength];
                    if (tab.NestedTables[1].DataText != null && tab.NestedTables[1].DataText.Count > 0)
                    {
                        for (int i = 0; i < newInfo.xLength && i < tab.NestedTables[1].DataText.Count; i++)
                            newInfo.xStringLabels[i] = tab.NestedTables[1].DataText[i];
                    }
                    else if (baseTable.NestedTables[1].DataText != null && baseTable.NestedTables[1].DataText.Count > 0)
                    {
                        for (int i = 0; i < newInfo.xLength && i < baseTable.NestedTables[1].DataText.Count; i++)
                            newInfo.xStringLabels[i] = baseTable.NestedTables[1].DataText[i];
                    }
                    else
                    {
                        Debug.WriteLine($"{tab.Name}: missing static axis values, skipping...");
                        return null;
                    }
                }
                else if (temp2.Contains("Static Y Axis", StringComparison.OrdinalIgnoreCase))
                {
                    newInfo.yStatic = true;
                    newInfo.yStringLabels = new string[newInfo.yLength];
                    if (tab.NestedTables[1].DataText != null && tab.NestedTables[1].DataText.Count > 0)
                    {
                        for (int i = 0; i < newInfo.yLength && i < tab.NestedTables[1].DataText.Count; i++)
                            newInfo.yStringLabels[i] = tab.NestedTables[1].DataText[i];
                    }
                    else if (baseTable.NestedTables[1].DataText != null && baseTable.NestedTables[1].DataText.Count > 0)
                    {
                        for (int i = 0; i < newInfo.yLength && i < baseTable.NestedTables[1].DataText.Count; i++)
                            newInfo.yStringLabels[i] = baseTable.NestedTables[1].DataText[i];
                    }
                    else
                    {
                        Debug.WriteLine($"{tab.Name}: missing static axis values, skipping...");
                        return null;
                    }
                }
            }
            else if (newInfo.dimension == 2) //2D
            {
                //check type
                string temp1 = "";
                if (tab.NestedTables[0].Type == null && baseTable.NestedTables[0].Type == null)
                {
                    Debug.WriteLine($"{tab.Name}: type missing for an axis, skipping...");
                    return null;
                }
                else if (tab.NestedTables[0].Type != null && baseTable.NestedTables[0].Type != null)
                {
                    if (!Regex.Replace(tab.NestedTables[0].Type, @"\s+", String.Empty).Contains(Regex.Replace(baseTable.NestedTables[0].Type, @"\s+", String.Empty), StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"{tab.Name}: axis type discrepancy between car and base file, car file takes precedence");
                    }
                    temp1 = tab.NestedTables[0].Type;
                }
                else if (tab.NestedTables[0].Type != null && baseTable.NestedTables[0].Type == null)
                {
                    temp1 = tab.NestedTables[0].Type;
                }
                else if (tab.NestedTables[0].Type == null && baseTable.NestedTables[0].Type != null)
                {
                    temp1 = baseTable.NestedTables[0].Type;
                }
                //names
                if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                    newInfo.xName = tab.NestedTables[0].Name;
                else
                    newInfo.yName = tab.NestedTables[0].Name;
                if (!temp1.Contains("Static", StringComparison.OrdinalIgnoreCase))
                {
                    //Address
                    if (tab.NestedTables[0].Address == null && baseTable.NestedTables[0].Address == null)
                    {
                        Debug.WriteLine($"{tab.Name}: address missing for an axis, skipping...");
                        return null;
                    }
                    else if (tab.NestedTables[0].Address != null && baseTable.NestedTables[0].Address != null)
                    {
                        if (!Regex.Replace(tab.NestedTables[0].Address, @"\s+", String.Empty).Contains(Regex.Replace(baseTable.NestedTables[0].Address, @"\s+", String.Empty), StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.WriteLine($"{tab.Name}: axis address discrepancy between car and base file, car file takes precedence");
                        }

                        if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                            newInfo.xAddress = tab.NestedTables[0].Address;
                        else
                            newInfo.yAddress = tab.NestedTables[0].Address;
                    }
                    else if (tab.NestedTables[0].Address != null && baseTable.NestedTables[0].Address == null)
                    {
                        if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                            newInfo.xAddress = tab.NestedTables[0].Address;
                        else
                            newInfo.yAddress = tab.NestedTables[0].Address;
                    }
                    else if (tab.NestedTables[0].Address == null && baseTable.NestedTables[0].Address != null)
                    {
                        if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                            newInfo.xAddress = baseTable.NestedTables[0].Address;
                        else
                            newInfo.yAddress = baseTable.NestedTables[0].Address;
                    }
                    //Scaling
                    if (tab.NestedTables[0].Scaling == null && baseTable.NestedTables[0].Scaling == null)
                    {
                        Debug.WriteLine($"{tab.Name}: scaling missing for an axis, skipping...");
                        return null;
                    }
                    else if (tab.NestedTables[0].Scaling != null && baseTable.NestedTables[0].Scaling != null)
                    {
                        if (!Regex.Replace(tab.NestedTables[0].Scaling, @"\s+", String.Empty).Contains(Regex.Replace(baseTable.NestedTables[0].Scaling, @"\s+", String.Empty), StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.WriteLine($"{tab.Name}: axis scaling discrepancy between car and base file, car file takes precedence");
                        }

                        if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                            newInfo.xScaling = tab.NestedTables[0].Scaling;
                        else
                            newInfo.yScaling = tab.NestedTables[0].Scaling;
                    }
                    else if (tab.NestedTables[0].Scaling != null && baseTable.NestedTables[0].Scaling == null)
                    {
                        if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                            newInfo.xScaling = tab.NestedTables[0].Scaling;
                        else
                            newInfo.yScaling = tab.NestedTables[0].Scaling;
                    }
                    else if (tab.NestedTables[0].Scaling == null && baseTable.NestedTables[0].Scaling != null)
                    {
                        if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                            newInfo.xScaling = baseTable.NestedTables[0].Scaling;
                        else
                            newInfo.yScaling = baseTable.NestedTables[0].Scaling;
                    }
                }
                //Length
                if (tab.NestedTables[0].Elements == null && baseTable.NestedTables[0].Elements == null)
                {
                    Debug.WriteLine($"{tab.Name}: elements missing for an axis, skipping...");
                    return null;
                }
                else if (tab.NestedTables[0].Elements != null && baseTable.NestedTables[0].Elements != null)
                {
                    if (!Regex.Replace(tab.NestedTables[0].Elements, @"\s+", String.Empty).Contains(Regex.Replace(baseTable.NestedTables[0].Elements, @"\s+", String.Empty), StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"{tab.Name}: axis elements discrepancy between car and base file, car file takes precedence");
                    }

                    if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                        newInfo.xLength = int.Parse(tab.NestedTables[0].Elements);
                    else
                        newInfo.yLength = int.Parse(tab.NestedTables[0].Elements);
                }
                else if (tab.NestedTables[0].Elements != null && baseTable.NestedTables[0].Elements == null)
                {
                    if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                        newInfo.xLength = int.Parse(tab.NestedTables[0].Elements);
                    else
                        newInfo.yLength = int.Parse(tab.NestedTables[0].Elements);
                }
                else if (tab.NestedTables[0].Elements == null && baseTable.NestedTables[0].Elements != null)
                {
                    if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                        newInfo.xLength = int.Parse(baseTable.NestedTables[0].Elements);
                    else
                        newInfo.yLength = int.Parse(baseTable.NestedTables[0].Elements);
                }
                //set static axes correctly
                if (temp1.Contains("X Axis", StringComparison.OrdinalIgnoreCase))
                {
                    newInfo.xORy2D = "X";
                    if (temp1.Contains("Static", StringComparison.OrdinalIgnoreCase))
                    {
                        newInfo.xStatic = true;
                        newInfo.xStringLabels = new string[newInfo.xLength];
                        if (tab.NestedTables[0].DataText != null && tab.NestedTables[0].DataText.Count > 0)
                        {
                            for (int i = 0; i < newInfo.xLength && i < tab.NestedTables[0].DataText.Count; i++)
                                newInfo.xStringLabels[i] = tab.NestedTables[0].DataText[i];
                        }
                        else if (baseTable.NestedTables[0].DataText != null && baseTable.NestedTables[0].DataText.Count > 0)
                        {
                            for (int i = 0; i < newInfo.xLength && i < baseTable.NestedTables[0].DataText.Count; i++)
                                newInfo.xStringLabels[i] = baseTable.NestedTables[0].DataText[i];
                        }
                        else
                        {
                            Debug.WriteLine($"{tab.Name}: missing static axis values, skipping...");
                            return null;
                        }
                    }
                    else
                        newInfo.xStatic = false;
                }
                else
                {
                    newInfo.xORy2D = "Y";
                    if (temp1.Contains("Static", StringComparison.OrdinalIgnoreCase))
                    {
                        newInfo.yStatic = true;
                        newInfo.yStringLabels = new string[newInfo.yLength];
                        if (tab.NestedTables[0].DataText != null && tab.NestedTables[0].DataText.Count > 0)
                        {
                            for (int i = 0; i < newInfo.yLength && i < tab.NestedTables[0].DataText.Count; i++)
                                newInfo.yStringLabels[i] = tab.NestedTables[0].DataText[i];
                        }
                        else if (baseTable.NestedTables[0].DataText != null && baseTable.NestedTables[0].DataText.Count > 0)
                        {
                            for (int i = 0; i < newInfo.yLength && i < baseTable.NestedTables[0].DataText.Count; i++)
                                newInfo.yStringLabels[i] = baseTable.NestedTables[0].DataText[i];
                        }
                        else
                        {
                            Debug.WriteLine($"{tab.Name}: missing static axis values, skipping...");
                            return null;
                        }
                    }
                    else
                        newInfo.yStatic = false;
                }
                //flipy
                if ((tab.Flipy != null && tab.Flipy.Contains("true", StringComparison.OrdinalIgnoreCase)) || (tab.NestedTables[0].Flipy != null && tab.NestedTables[0].Flipy.Contains("true", StringComparison.OrdinalIgnoreCase)) || (baseTable.Flipy != null && baseTable.Flipy.Contains("true", StringComparison.OrdinalIgnoreCase)) || (baseTable.NestedTables[0].Flipy != null && baseTable.NestedTables[0].Flipy.Contains("true", StringComparison.OrdinalIgnoreCase)))
                    newInfo.yFlip = true;
                else
                    newInfo.yFlip = false;
                //flipx
                if ((tab.Flipx != null && tab.Flipx.Contains("true", StringComparison.OrdinalIgnoreCase)) || (tab.NestedTables[0].Flipx != null && tab.NestedTables[0].Flipx.Contains("true", StringComparison.OrdinalIgnoreCase)) || (baseTable.Flipx != null && baseTable.Flipx.Contains("true", StringComparison.OrdinalIgnoreCase)) || (baseTable.NestedTables[0].Flipx != null && baseTable.NestedTables[0].Flipx.Contains("true", StringComparison.OrdinalIgnoreCase)))
                    newInfo.xFlip = true;
                else
                    newInfo.xFlip = false;
            }
            else if (newInfo.dimension == 1) //1D
            {
                //dont think we need to do anything actually lol
                return newInfo;
            }
            else
                return null; //invalid

            return newInfo;
        }

        //prints every field from every variable in the XMLManager class. Mainly for debugging 
        public static void PrintAllVars()
        {
            Debug.WriteLine($"xmlCarFileName: {xmlCarFileName} | xmlCarFilePath: {xmlCarFilePath}\nxmlBaseFileName: {xmlBaseFileName} | xmlBaseFilePath: {xmlBaseFilePath}\nbinFileName: {binFileName} | binFilePath: {binFilePath}\n");
            Debug.WriteLine($"ROMID\n Xmlid: {carRom.Romid.Xmlid}\n Internalidaddress: {carRom.Romid.Internalidaddress}\n Internalidhex: {carRom.Romid.Internalidhex}\n Caseid: {carRom.Romid.Caseid}\n Make: {carRom.Romid.Make}\n Market: {carRom.Romid.Market}\n Model: {carRom.Romid.Model}\n Submodel: {carRom.Romid.Submodel}\n Transmission: {carRom.Romid.Transmission}\n Year: {carRom.Romid.Year}\n Flashmethod: {carRom.Romid.Flashmethod}\n Memmodel: {carRom.Romid.Memmodel}\n");
            Debug.WriteLine($"NOTES: {carRom.Notes}\n");
            Debug.WriteLine($"INCLUDE: {carRom.Include}\n");
            Debug.WriteLine("SCALINGS");
            
            foreach(Scaling scal in carRom.Scalings)
            {
                Debug.WriteLine($" {scal.Name}\n  Units: {scal.Units}\n  ToExpr: {scal.Toexpr}\n  FromExpr: {scal.Frexpr}\n  Format: {scal.Format}\n  Min: {scal.Min}\n  Max: {scal.Max}\n  Increment: {scal.Inc}\n  StorageType: {scal.Storagetype}\n  Endian: {scal.Endian}\n  StorageBits: {scal.Storagebits}");
                if(scal.Data.Count != 0) Debug.WriteLine("  DATA");
                foreach(Data d in scal.Data)
                {
                    Debug.WriteLine($"   Name: {d.Name}, Value: {d.Value}");
                }
            }
            Debug.WriteLine("\nTABLES");
            foreach(Table tab in carRom.Tables)
            {
                Debug.WriteLine($" {tab.Name}\n  Type: {tab.Type}\n  Elements: {tab.Elements}\n  Scaling: {tab.Scaling}\n  Category: {tab.Category}\n  SwapXY: {tab.Swapxy}\n  Level: {tab.Level}\n  Flipy: {tab.Flipy}\n  Address: {tab.Address}");
                if(tab.NestedTables.Count != 0) Debug.WriteLine("  INNER TABLES");
                foreach(Table t in tab.NestedTables)
                {
                    Debug.WriteLine($"\t  {t.Name}\n\t   Type: {t.Type}\n\t   Elements: {t.Elements}\n\t   Scaling: {t.Scaling}\n\t   Category: {t.Category}\n\t   SwapXY: {t.Swapxy}\n\t   Level: {t.Level}\n\t   Flipy: {t.Flipy}\n\t   Address: {t.Address}");
                    if (t.DataText.Count != 0) Debug.Write("\t   DATA\n\t    ");
                    foreach(string s in t.DataText)
                    {
                        Debug.Write($"{s} ");
                    }
                    Debug.Write("\n");
                }
            }
        }

        //prints all tableData from all of the XMLTableInfo objects in tablesToDisplay 
        public static void PrintAllTableData()
        {
            foreach (XMLTableInfo tab in tablesToDisplay)
            {
                Debug.Write($"------{tab.title}------\n  ");
                for (int i = 0; i < tab.tableData.GetLength(0) + 1; i++)
                {
                    for (int j = 0; j < tab.tableData.GetLength(1); j++)
                    {
                        if (i == 0)
                        {
                            if (tab.xStatic)
                                Debug.Write(tab.xStringLabels[j] + " ");
                            else if(tab.xLabels != null)
                                Debug.Write(tab.xLabels[j] + " ");
                            continue;
                        }
                        else if (i > 0 && j == 0)
                        {
                            if (tab.yStatic)
                                Debug.Write(tab.yStringLabels[i - 1] + " ");
                            else if(tab.yLabels != null)
                                Debug.Write(tab.yLabels[i - 1] + " ");
                        }
                        Debug.Write(tab.tableData[i - 1, j].ToString() + " ");
                    }
                    Debug.WriteLine("");
                }
                Debug.WriteLine("");
            }
        }
        #endregion
    } // end of class
}
