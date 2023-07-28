namespace CarDiagnosticReader.XDFFiles
{
    public class XDFHeader
    {
        #region Variables
        //attributes from XDF file
        public string fileVers { get; set; }
        public string defTitle { get; set; }
        public string descSize { get; set; }
        public string desc { get; set; }
        public string author { get; set; }
        public string binSize { get; set; }
        public string baseOffset { get; set; }
        public string aDSAssoc { get; set; }
        public string aDSCheck { get; set; }
        public string genFlags { get; set; }
        public string modeFlags { get; set; }
        #endregion

        #region Constructors
        public XDFHeader(string fileVers, string defTitle, string descSize, string desc, string author, string binSize, string baseOffset, string aDSAssoc, string aDSCheck, string genFlags, string modeFlags)
        {
            this.fileVers = fileVers;
            this.defTitle = defTitle;
            this.descSize = descSize;
            this.desc = desc;
            this.author = author;
            this.binSize = binSize;
            this.baseOffset = baseOffset;
            this.aDSAssoc = aDSAssoc;
            this.aDSCheck = aDSCheck;
            this.genFlags = genFlags;
            this.modeFlags = modeFlags;
        }

        public XDFHeader()
        {
            this.fileVers = "";
            this.defTitle = "";
            this.descSize = "";
            this.desc = "";
            this.author = "";
            this.binSize = "";
            this.baseOffset = "";
            this.aDSAssoc = "";
            this.aDSCheck = "";
            this.genFlags = "";
            this.modeFlags = "";
        }
        #endregion
    }
}
