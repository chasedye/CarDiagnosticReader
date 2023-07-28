namespace CarDiagnosticReader
{
    public class CarFileInfo
    {
        public string name { get; set; } //primary key e.g. 1234evo.xml
        public string filepath { get; set; } //e.g. ..\..\..\CarFiles\XML\evo7base.xml
        public string type { get; set; } //e.g. XML base -OR- XML car -OR- XDF car
        public string datetime { get; set; } //e.g. 5/23/2023 12:41:28 AM
        public string checksum { get; set; } //e.g. d5d20bcba65c4a59cb39cbf220db710e
        public string binName { get; set; } //e.g. DSMStockE931.bin
        public string idAddress { get; set; } //e.g. f52
        public string idHex { get; set; } //e.g. 94170008
        public string notes { get; set; }

        public CarFileInfo()
        {
            name = "";
            filepath = "";
            type = "";
            datetime = "";
            checksum = "";
            binName = "";
            idAddress = "";
            idHex = "";
            notes = "";
        }
    }
}