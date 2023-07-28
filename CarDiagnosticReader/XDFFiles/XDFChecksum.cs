namespace CarDiagnosticReader.XDFFiles
{
    public class XDFChecksum
    {
        #region Variables
        //attributes from XDF file
        public string uniqueID { get; set; }
        public string title { get; set; }
        public string dataStart { get; set; }
        public string dataEnd { get; set; }
        public string sizeBits { get; set; }
        public string storeAddr { get; set; }
        public string calcMethod { get; set; }
        public string flags { get; set; }
        #endregion

        #region Constructors
        public XDFChecksum(string uniqueID, string title, string dataStart, string dataEnd, string sizeBits, string storeAddr, string calcMethod, string flags)
        {
            this.uniqueID = uniqueID;
            this.title = title;
            this.dataStart = dataStart;
            this.dataEnd = dataEnd;
            this.sizeBits = sizeBits;
            this.storeAddr = storeAddr;
            this.calcMethod = calcMethod;
            this.flags = flags;
        }

        public XDFChecksum()
        {
            this.uniqueID = "";
            this.title = "";
            this.dataStart = "";
            this.dataEnd = "";
            this.sizeBits = "";
            this.storeAddr = "";
            this.calcMethod = "";
            this.flags = "";
        }
        #endregion
    }
}
