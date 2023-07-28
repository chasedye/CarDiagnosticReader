using CarDiagnosticReader.XDFFiles;
using CarDiagnosticReader.XMLFiles;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;

namespace CarDiagnosticReader.Controls
{
    /// <summary>
    /// Interaction logic for DataTableListcontrol.xaml
    /// </summary>
    public partial class DataTableListControl : UserControl
    {
        public List<CarFileInfo> fileInfo = new();
        private string tempBinName = "";
        private string tempBinPath = "";
        private string tempBinChecksum = "";
        LoggerControl loggerControl;

        public DataTableListControl(LoggerControl loggerControl)
        {
            this.loggerControl = loggerControl;
            InitializeComponent();

            //when the app starts up load in all info
            SyncFilesWithDB();
        }

        //click function for the top button that says "Open" 
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "All Files|*.*";
            Nullable<bool> dialogOK = fileDialog.ShowDialog();
            
            if (dialogOK == true)
            {
                FileInfo fileInfo = new FileInfo(fileDialog.FileName);

                if (fileInfo.Extension == ".xml")
                {
                    XMLManager.xmlCarFilePath = fileInfo.FullName;
                    XMLManager.xmlCarFileName = fileInfo.Name;
                    XMLManager.ReadAllFromXML();
                    loggerControl.AddLogMessage("XML Definition file received");

                    //check if they already uploaded a binary file
                    if (tempBinName != "" && tempBinPath != "" && tempBinChecksum != "")
                    {
                        XMLManager.binFilePath = tempBinPath;
                        XMLManager.binFileName = tempBinName;
                        XMLManager.binChecksum = tempBinChecksum;
                        XMLManager.FetchAllFromBin();
                        TextBlockList.ItemsSource = XMLManager.tablesToDisplay;

                        //reset the temps
                        tempBinName = "";
                        tempBinPath = "";
                        tempBinChecksum = "";
                    }
                }
                else if (fileInfo.Extension == ".xdf")
                {
                    XDFManager.xdfFilePath = fileInfo.FullName;
                    XDFManager.xdfFileName = fileInfo.Name;
                    XDFManager.ReadAllFromXDF();
                    loggerControl.AddLogMessage("XDF Definition file received");

                    //check if they already uploaded a binary file
                    if (tempBinName != "" && tempBinPath != "" && tempBinChecksum != "")
                    {
                        XDFManager.binFilePath = tempBinPath;
                        XDFManager.binFileName = tempBinName;
                        XDFManager.binChecksum = tempBinChecksum;
                        XDFManager.FetchAllFromBin();
                        TextBlockList.ItemsSource = XDFManager.tableVars;

                        //reset the temps
                        tempBinName = "";
                        tempBinPath = "";
                        tempBinChecksum = "";
                    }
                }
                else if (fileInfo.Extension == ".hex" || fileInfo.Extension == ".bin")
                {
                    loggerControl.AddLogMessage("Use the other button for binary or hex files");
                }
                else
                {
                    loggerControl.AddLogMessage("File format not recognized");
                }
            }
        }

        //click function for the bottom button that says "Binary File" 
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "All Files|*.*";
            Nullable<bool> dialogOK = fileDialog.ShowDialog();

            if (dialogOK == true)
            {
                FileInfo fileInfo = new FileInfo(fileDialog.FileName);

                if (fileInfo.Extension == ".hex" || fileInfo.Extension == ".bin")
                {
                    loggerControl.AddLogMessage("Binary file received");

                    //determine if a definition file has been uploaded for this binary file yet
                    if (XDFManager.xdfFileName != "" && XDFManager.xdfFilePath != "")
                    {
                        XDFManager.binFilePath = fileInfo.FullName;
                        XDFManager.binFileName = fileInfo.Name;
                        XDFManager.binChecksum = GenerateChecksum(fileInfo.FullName);
                        XDFManager.FetchAllFromBin();
                        TextBlockList.ItemsSource = XDFManager.tableVars;
                        //xdfManager.PrintAllValues(); //for debugging 
                    }
                    else if(XMLManager.xmlCarFileName != "" && XMLManager.xmlCarFilePath != "")
                    {
                        XMLManager.binFilePath = fileInfo.FullName;
                        XMLManager.binFileName = fileInfo.Name;
                        XMLManager.binChecksum = GenerateChecksum(fileInfo.FullName);
                        XMLManager.FetchAllFromBin();
                        TextBlockList.ItemsSource = XMLManager.tablesToDisplay;
                        // xmlManager.PrintAllVars(); //for debugging
                    }
                    else
                    {
                        //they have not uploaded a definition file, lets find one that works with the given binary file
                        string name = fileInfo.Name;
                        string check = GenerateChecksum(fileInfo.FullName);
                        //BIG TODO:    ---------------also chase pls read---------------
                        //now we need to try idAddresses on this bin file and see if the 4 bits at that address equal the idHex
                        //we can use the bin file's name as a helper for this but otherwise brute force, try every entry in the db until one works
                        CarFileInfo? definition = FindDefinitionFile(name, fileInfo.FullName);

                        if(definition == null)
                        {
                            tempBinName = name;
                            tempBinPath = fileInfo.FullName;
                            tempBinChecksum = check;
                            loggerControl.AddLogMessage("Definition file not found automatically, please upload your own");
                        }
                        else if (definition.type == "XML car")
                        {
                            loggerControl.AddLogMessage($"XML Definition file {definition.name} found");
                            //xmlManager = new XMLManager();
                            XMLManager.xmlCarFilePath = definition.filepath;
                            XMLManager.xmlCarFileName = definition.name;
                            XMLManager.ReadAllFromXML();

                            XMLManager.binFilePath = fileInfo.FullName;
                            XMLManager.binFileName = name;
                            XMLManager.binChecksum = check;
                            XMLManager.FetchAllFromBin();
                            TextBlockList.ItemsSource = XMLManager.tablesToDisplay;
                            //xmlManager.PrintAllVars(); //for debugging 
                            //xmlManager.PrintAllTableData();
                        }
                        else if (definition.type == "XDF car") //pretty sure this is unreachable but i'll leave it there just in case
                        {
                            //xdfManager = new XDFManager();
                            XDFManager.xdfFilePath = definition.filepath;
                            XDFManager.xdfFileName = definition.name;
                            XDFManager.ReadAllFromXDF();

                            XDFManager.binFilePath = fileInfo.FullName;
                            XDFManager.binFileName = name;
                            XDFManager.binChecksum = check;
                            XDFManager.FetchAllFromBin();
                            TextBlockList.ItemsSource = XDFManager.tableVars;
                            //xdfManager.PrintAllValues(); //for debugging
                        }
                        else
                        {
                            tempBinName = name;
                            tempBinPath = fileInfo.FullName;
                            tempBinChecksum = check;
                            loggerControl.AddLogMessage("Definition file not found automatically, please upload your own");
                        }
                    }
                }
                else if (fileInfo.Extension == ".xdf" || fileInfo.Extension == ".xml")
                {
                    loggerControl.AddLogMessage("Use the other button for definition files");
                }
                else
                {
                    loggerControl.AddLogMessage("File format not recognized");
                }

                TextBlockList.Visibility = Visibility.Visible;
            }
        }

        //given the binary file's name, and full file path
        //finds a definition file that works for the given binary file
        //returns null if none are found
        private CarFileInfo? FindDefinitionFile(string name, string filePath)
        {
            //use the fileInfo list(line 21) instead of doing a bunch of sql queries
            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);

            string[] files = Directory.GetFiles(@"CarFiles", "*.*", SearchOption.AllDirectories);
            List<CarFileInfo> carFiles = new List<CarFileInfo>();

            //get all our of files names in a list
            foreach (string f in files)
            {
                carFiles.Add(SQLiteDataAccess.SearchByName(Path.GetFileName(f)));
            }

            //go through each file and make sure the db is update w its info
            foreach (CarFileInfo defFile in carFiles)
            {
                // quick check to be safe
                if(defFile != null && defFile.idHex != "")
                {
                    byte[] tempHex = { 0, 0 };
                    fileStream.Seek(Convert.ToInt32(defFile.idAddress, 16), SeekOrigin.Begin);
                    // length of hexid in bytes
                    int length = defFile.idHex.Length / 2;
                    tempHex = binaryReader.ReadBytes(length);      
                    
                    try
                    {
                        // this gives us the hex version of the id
                        string temp = BitConverter.ToString(tempHex).Replace("-", " ");
                        // return if true and close files
                        if (String.Compare(defFile.idHex, temp.Replace(" ", "")) == 0)
                        {
                            binaryReader.Close();
                            fileStream.Close();
                            return defFile;
                        }
                    } catch (Exception e)
                    {
                        // error happened just keep checking
                        Debug.WriteLine("Nothing yet");
                    }
                }
            }
            binaryReader.Close();
            fileStream.Close();
            return null;
        }

        //given a string of the path to a file, generates an MD5 Checksum hash value for the given file and returns it as a string
        public string GenerateChecksum(string filePath)
        {
            if (filePath == null)
            {
                Debug.WriteLine("File path given is NULL");
                return "-1";
            }
            string result = "-1";
            var md5 = MD5.Create();

            try
            {
                var stream = File.OpenRead(filePath);
                var hash = md5.ComputeHash(stream);
                stream.Close();
                result = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch(FileNotFoundException e)
            {
                Debug.WriteLine(e.Message);
            }
            catch(DirectoryNotFoundException e)
            {
                Debug.WriteLine(e.Message);
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            return result;
        }

        //given two file paths, computes the MD5 checksum for both and returns true or false based on if they are the same or not
        public Boolean CompareChecksums(string filePath1, string filePath2)
        {
            if(filePath1 == null)
            {
                Debug.WriteLine("File path 1 is NULL");
                return false;
            }
            else if(filePath2 == null)
            {
                Debug.WriteLine("File path 2 is NULL");
                return false;
            }
            string hash1 = GenerateChecksum(filePath1);
            string hash2 = GenerateChecksum(filePath2);

            if(hash1 == hash2)
            {
                Debug.WriteLine("Files are IDENTICAL");
                return true;
            }

            Debug.WriteLine("Files are DIFFERENT");
            return false;
        }

        //goes through all files in the CarFiles folder and makes sure the data in the db is the up to date
        public void SyncFilesWithDB()
        {
            string[] files = Directory.GetFiles(@"CarFiles", "*.*", SearchOption.AllDirectories);
            List<string> filenames = new List<string>();

            //get all our of files names in a list
            foreach(string f in files)
            {
                filenames.Add(Path.GetFileName(f));
            }

            //go through each file and make sure the db is update w its info
            foreach(string s in filenames)
            {
                CarFileInfo result = SQLiteDataAccess.SearchByName(s);
                CarFileInfo newInfo = new CarFileInfo();
                string checksum = "";

                //get info about the local file, starting with checksum
                string[] split = s.Split('.');
                if (split[1] == "xml")
                {
                    checksum = GenerateChecksum(@"CarFiles\XML\" + s);
                }
                else if (split[1] == "xdf")
                {
                    checksum = GenerateChecksum(@"CarFiles\XDF\" + s);
                }

                //get the rest of the info needed except datetime
                newInfo.name = s;
                if (split[1] == "xml")
                {
                    newInfo.filepath = @"CarFiles\XML\" + s;
                    if (s.Contains("base"))
                        newInfo.type = "XML base";
                    else
                        newInfo.type = "XML car";
                }
                else if (split[1] == "xdf")
                {
                    newInfo.filepath = @"CarFiles\XDF\" + s;
                    newInfo.type = "XDF car";
                }
                newInfo.checksum = checksum;
                //these 4 are set to nothing cuz these should be inputted manually. At least for now...
                newInfo.binName = "";
                newInfo.idAddress = "";
                newInfo.idHex = "";
                newInfo.notes = "";

                if(result == null)
                {
                    //file is not in the database or it is at least under the wrong name
                    //search for the file by its checksum
                    CarFileInfo dbChecksum = SQLiteDataAccess.SearchByChecksum(checksum);
                    
                    if(dbChecksum == null)
                    {
                        //this file doesn't exist in the db yet, lets create an entry in the db
                        loggerControl.AddLogMessage("Creating new entry in db...");
                        newInfo.datetime = DateTime.Now.ToString();
                        SQLiteDataAccess.SaveCarFileInfo(newInfo);
                    }
                    else if (dbChecksum.checksum == checksum)
                    {
                        //the file we were originally looking for is in the db just under a different name than the local file, lets edit its entry
                        loggerControl.AddLogMessage("Updating entry by checksum");
                        newInfo.datetime = DateTime.Now.ToString();
                        SQLiteDataAccess.EditSyncInfoByChecksum(newInfo.checksum, newInfo);
                    }
                }
                else
                {
                    //this file is in the db confirmed by a matching name, lets make sure all data is up to date
                    if(result.filepath == newInfo.filepath && result.type == newInfo.type && result.checksum == newInfo.checksum)
                    {
                        //file info is up to date in db, don't need to do anything
                        loggerControl.AddLogMessage("Info is up to date");
                        continue;
                    }
                    else
                    {
                        //file info is not up to date, lets edit the entry in the db
                        loggerControl.AddLogMessage("Updating entry by name");
                        newInfo.datetime = DateTime.Now.ToString();
                        SQLiteDataAccess.EditSyncInfoByName(newInfo.name, newInfo);
                    }
                }
            }
            loggerControl.AddLogMessage("All file records up to date!");
            fileInfo = SQLiteDataAccess.LoadAllCarFileInfo();
        }

        private void TextBlockList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get the index of the selected item
            int selectedIndex = TextBlockList.SelectedIndex;

            if(XDFManager.xdfFileName != "" && XDFManager.xdfFilePath != "" && XDFManager.binFileName != "" && XDFManager.binFilePath != "")
            {
                // Create a new window
                Window newWindow = new Window
                {
                    Title = XDFManager.tableVars[selectedIndex].title,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ResizeMode = ResizeMode.NoResize
                };

                // Get the instance of MyCustomControl from XAML resources
                HeatMapControl controlInstance = new HeatMapControl(XDFManager.tableVars[selectedIndex], loggerControl, "XDF");

                // Set the content of the new window to the custom control
                newWindow.Content = controlInstance;

                // Show the new window
                newWindow.Show();
            }
            else if(XMLManager.xmlCarFileName != "" && XMLManager.xmlCarFilePath != "" && XMLManager.binFileName != "" && XMLManager.binFilePath != "")
            {
                // Create a new window
                Window newWindow = new Window
                {
                    Title = XMLManager.tablesToDisplay[selectedIndex].title,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ResizeMode = ResizeMode.NoResize
                };

                // Get the instance of MyCustomControl from XAML resources
                HeatMapControl controlInstance = new HeatMapControl(XMLManager.tablesToDisplay[selectedIndex], loggerControl, "XML");

                // Set the content of the new window to the custom control
                newWindow.Content = controlInstance;

                // Show the new window
                newWindow.Show();
            }

        }
    }
}
