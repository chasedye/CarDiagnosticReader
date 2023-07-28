using Dapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CarDiagnosticReader
{
    public class SQLiteDataAccess
    {
        //forms a connection w our local database and returns a list of all CarFileInfo objects 
        public static List<CarFileInfo> LoadAllCarFileInfo()
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectionString()))
            {
                try
                {
                    var output = conn.Query<CarFileInfo>("SELECT * FROM CarFileInfo", new DynamicParameters());
                    return output.ToList();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    return null;
                }
            }
        }

        //takes a string as input and searches the CarFileInfo db for an entry with a matching name
        //returns a single object because name is UNIQUE in the db
        public static CarFileInfo SearchByName(string s)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectionString()))
            {
                try
                {
                    var output = conn.QuerySingleOrDefault<CarFileInfo>($"SELECT * FROM CarFileInfo WHERE name=\'{s}\'", new DynamicParameters());
                    return output;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    return null;
                }
            }
        }

        //takes a string as input and searches the CarFileInfo db for an entry with a matching checksum
        //returns a single object because checksum is UNIQUE in the db
        public static CarFileInfo SearchByChecksum(string s)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectionString()))
            {
                try
                {
                    var output = conn.QuerySingleOrDefault<CarFileInfo>($"SELECT * FROM CarFileInfo WHERE checksum=\'{s}\'", new DynamicParameters());
                    return output;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    return null;
                }
            }
        }

        //takes a string as input and searches the CarFileInfo db for entries with a matching type returns a list of CarFileInfo objects
        public static List<CarFileInfo> SearchByType(string s)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectionString()))
            {
                try
                {
                    var output = conn.Query<CarFileInfo>($"SELECT * FROM CarFileInfo WHERE type=\'{s}\'", new DynamicParameters());
                    return output.ToList();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    return null;
                }
            }
        }

        //takes a string as input and searches the CarFileInfo db for entries with a matching date returns a list of CarFileInfo objects
        public static List<CarFileInfo> SearchByDate(string s)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectionString()))
            {
                try
                {
                    var output = conn.Query<CarFileInfo>($"SELECT * FROM CarFileInfo WHERE datetime=\'{s}\'", new DynamicParameters());
                    return output.ToList();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    return null;
                }
            }
        }

        //takes a string as input and searches the CarFileInfo db for entries with a matching binName returns a list of CarFileInfo objects
        public static List<CarFileInfo> SearchByBinName(string s)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectionString()))
            {
                try
                {
                    var output = conn.Query<CarFileInfo>($"SELECT * FROM CarFileInfo WHERE binName=\'{s}\'", new DynamicParameters());
                    return output.ToList();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    return null;
                }
            }
        }

        //takes a string as input and searches the CarFileInfo db for entries with a matching idAddress returns a list of CarFileInfo objects
        public static List<CarFileInfo> SearchByIdAddress(string s)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectionString()))
            {
                try
                {
                    var output = conn.Query<CarFileInfo>($"SELECT * FROM CarFileInfo WHERE idAddress=\'{s}\'", new DynamicParameters());
                    return output.ToList();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    return null;
                }
            }
        }

        //takes a string as input and searches the CarFileInfo db for entries with a matching idHex returns a list of CarFileInfo objects
        public static List<CarFileInfo> SearchByIdHex(string s)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectionString()))
            {
                try
                {
                    var output = conn.Query<CarFileInfo>($"SELECT * FROM CarFileInfo WHERE idHex=\'{s}\'", new DynamicParameters());
                    return output.ToList();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    return null;
                }
            }
        }

        //given a CarFileInfo object, inserts it into the database 
        public static void SaveCarFileInfo(CarFileInfo fileInfo)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectionString()))
            {
                try
                {
                    conn.Execute("INSERT INTO CarFileInfo (name, filepath, type, datetime, checksum, binName, idAddress, idHex, notes) VALUES (@name, @filepath, @type, @datetime, @checksum, @binName, @idAddress, @idHex, @notes)", fileInfo);
                }
                catch(Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }

        //given a filename as a string, deletes the entry in the db that has that filename
        public static void DeleteEntryByName(string s)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectionString()))
            {
                try
                {
                    conn.Execute($"DELETE FROM CarFileInfo WHERE name=\'{s}\'", new DynamicParameters());
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }

        //given a string for the name of the file and a CarFileInfo object with the updated information, 
        //edits the entry for that file in the db
        public static void EditEntryByName(string s, CarFileInfo newInfo)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectionString()))
            {
                try
                {
                    conn.Execute($"UPDATE CarFileInfo SET name=\'{newInfo.name}\', filepath=\'{newInfo.filepath}\', type=\'{newInfo.type}\', datetime=\'{newInfo.datetime}\', checksum=\'{newInfo.checksum}\', binName=\'{newInfo.binName}\', idAddress=\'{newInfo.idAddress}\', idHex=\'{newInfo.idHex}\', notes=\'{newInfo.notes}\' WHERE name=\'{s}\'", new DynamicParameters());
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }

        //given a string for the checksum of the file and a CarFileInfo object with the updated information, 
        //edits the entry for that file in the db
        public static void EditEntryByChecksum(string s, CarFileInfo newInfo)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectionString()))
            {
                try
                {
                    conn.Execute($"UPDATE CarFileInfo SET name=\'{newInfo.name}\', filepath=\'{newInfo.filepath}\', type=\'{newInfo.type}\', datetime=\'{newInfo.datetime}\', checksum=\'{newInfo.checksum}\', binName=\'{newInfo.binName}\', idAddress=\'{newInfo.idAddress}\', idHex=\'{newInfo.idHex}\', notes=\'{newInfo.notes}\' WHERE checksum=\'{s}\'", new DynamicParameters());
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }

        //given a string for the name of the file and a CarFileInfo object with the updated information, 
        //edits the entry for that file in the db
        public static void EditSyncInfoByName(string s, CarFileInfo newInfo)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectionString()))
            {
                try
                {
                    conn.Execute($"UPDATE CarFileInfo SET name=\'{newInfo.name}\', filepath=\'{newInfo.filepath}\', type=\'{newInfo.type}\', datetime=\'{newInfo.datetime}\', checksum=\'{newInfo.checksum}\' WHERE name=\'{s}\'", new DynamicParameters());
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }

        //given a string for the checksum of the file and a CarFileInfo object with the updated information, 
        //edits the entry for that file in the db
        public static void EditSyncInfoByChecksum(string s, CarFileInfo newInfo)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectionString()))
            {
                try
                {
                    conn.Execute($"UPDATE CarFileInfo SET name=\'{newInfo.name}\', filepath=\'{newInfo.filepath}\', type=\'{newInfo.type}\', datetime=\'{newInfo.datetime}\', checksum=\'{newInfo.checksum}\' WHERE checksum=\'{s}\'", new DynamicParameters());
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }

        //given a string for the name of the definition file, a string of the current time, and the updated checksum
        //edits the entry for that file in the db
        /*public static void EditAssociateChecksumByName(string s, string time, string checksum)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectionString()))
            {
                try
                {
                    conn.Execute($"UPDATE CarFileInfo SET datetime=\'{time}\', associateChecksum=\'{checksum}\' WHERE name=\'{s}\'", new DynamicParameters());
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }*/

        //given a string for the id of a connection string, returns the specified connection string
        //parameter for id is OPTIONAL and if you don't specify anything id will equal "Default" 
        private static string LoadConnectionString(string id = "Default")
        {
            return ConfigurationManager.ConnectionStrings[id].ConnectionString;
        }
    }
}
