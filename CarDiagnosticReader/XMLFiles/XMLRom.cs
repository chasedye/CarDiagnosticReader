using System.Collections.Generic;
using System.Printing;
using System.Windows.Documents;
using System.Xml.Serialization;

namespace CarDiagnosticReader.XMLFiles
{
    [XmlRoot(ElementName = "rom")]
    public class Rom
    {
        // ROMID
        [XmlElement(ElementName = "romid")]
        public Romid Romid { get; set; }

        // Notes
        [XmlElement(ElementName = "notes")]
        public string Notes { get; set; }

        // Include
        [XmlElement(ElementName = "include")]
        public string Include { get; set; }

        // Scalings
        [XmlElement(ElementName = "scaling")]
        public List<Scaling> Scalings { get; set; }

        // Tables
        [XmlElement(ElementName = "table")]
        public List<Table> Tables { get; set; }

        public string Type { get; set; }

        #region Constructors
        public Rom(Romid romid, string notes, string include, List<Scaling> scaling, List<Table> table, string type)
        {
            Romid = romid;
            Notes = notes;
            Include = include;
            Scalings = scaling;
            Tables = table;
            Type = type;
        }

        public Rom(string type)
        {
            Romid = new Romid();
            Notes = "";
            Include = "";
            Scalings = new List<Scaling>();
            Tables = new List<Table>();
            Type = type;
        }

        public Rom()
        {
            Romid = new Romid();
            Notes = "";
            Include = "";
            Scalings = new List<Scaling>();
            Tables = new List<Table>();
            Type = "";
        }
        #endregion
    }

    [XmlRoot(ElementName = "romid")]
    public class Romid
    {
        [XmlElement(ElementName = "xmlid")]
        public string Xmlid { get; set; }
        [XmlElement(ElementName = "internalidaddress")]
        public string Internalidaddress { get; set; }
        [XmlElement(ElementName = "internalidhex")]
        public string Internalidhex { get; set; }
        [XmlElement(ElementName = "caseid")]
        public string Caseid { get; set; }
        [XmlElement(ElementName = "make")]
        public string Make { get; set; }
        [XmlElement(ElementName = "market")]
        public string Market { get; set; }
        [XmlElement(ElementName = "model")]
        public string Model { get; set; }
        [XmlElement(ElementName = "submodel")]
        public string Submodel { get; set; }
        [XmlElement(ElementName = "transmission")]
        public string Transmission { get; set; }
        [XmlElement(ElementName = "year")]
        public string Year { get; set; }
        [XmlElement(ElementName = "flashmethod")]
        public string Flashmethod { get; set; }
        [XmlElement(ElementName = "memmodel")]
        public string Memmodel { get; set; }
    }

    [XmlRoot(ElementName = "scaling")]
    public class Scaling
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
        [XmlAttribute(AttributeName = "units")]
        public string Units { get; set; }
        [XmlAttribute(AttributeName = "toexpr")]
        public string Toexpr { get; set; }
        [XmlAttribute(AttributeName = "frexpr")]
        public string Frexpr { get; set; }
        [XmlAttribute(AttributeName = "format")]
        public string Format { get; set; }
        [XmlAttribute(AttributeName = "min")]
        public string Min { get; set; }
        [XmlAttribute(AttributeName = "max")]
        public string Max { get; set; }
        [XmlAttribute(AttributeName = "inc")]
        public string Inc { get; set; }
        [XmlAttribute(AttributeName = "storagetype")]
        public string Storagetype { get; set; }
        [XmlAttribute(AttributeName = "endian")]
        public string Endian { get; set; }
        [XmlElement(ElementName = "data")]
        public List<Data> Data { get; set; }
        [XmlAttribute(AttributeName = "storagebits")]
        public string Storagebits { get; set; }
    }

    [XmlRoot(ElementName = "data")]
    public class Data
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "table")]
    public class Table
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }
        [XmlAttribute(AttributeName = "elements")]
        public string Elements { get; set; }
        [XmlAttribute(AttributeName = "scaling")]
        public string Scaling { get; set; }
        [XmlElement(ElementName = "table")]
        public List<Table> NestedTables { get; set; }
        [XmlAttribute(AttributeName = "category")]
        public string Category { get; set; }
        [XmlAttribute(AttributeName = "swapxy")]
        public string Swapxy { get; set; }
        [XmlAttribute(AttributeName = "level")]
        public string Level { get; set; }
        [XmlAttribute(AttributeName = "flipy")]
        public string Flipy { get; set; }
        [XmlAttribute(AttributeName = "flipx")]
        public string Flipx { get; set; }
        [XmlAttribute(AttributeName = "address")]
        public string Address { get; set; }
        [XmlElement(ElementName = "data")]
        public List<string> DataText { get; set; }
        //helper vars
        [XmlIgnore]
        public float[,] tableData { get; set; }//THIS IS GONNA GET DELETED, PLEASE REMOVE ANY REFERENCES
    }
}
