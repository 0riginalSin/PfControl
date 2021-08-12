/******************************************************************************
 * This file contains class, what implements methods to interact with local 
 * SQLite data base.
 * 
 * @file LocalDBclass.cs
 * @author 0riginalSin
 * @brief Contains LocalDB class declaration with comments.
 *
 ******************************************************************************
 */
using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Windows.Storage;

namespace UWP_sql
{
    /// <summary>
    /// Local SQLite data base functional
    /// </summary>
    public class LocalDB
    {
        /// <summary>
        /// Row of local SQLite data base
        /// </summary>
        public class Row
        {
            public long Primary_Key { get; set; }
            public string PF_Bar_Code { get; set; }
            public string IMM_Bar_Code { get; set; }
            public string Event_type { get; set; }
            public string User_Name { get; set; }
            public DateTime Date_Time { get; set; }

            public Row(long primaryKey, string pfBarCode, string immBarCode, string eventType, string userName, DateTime dateTime)
            {
                this.Primary_Key = primaryKey;
                this.PF_Bar_Code = pfBarCode;
                this.IMM_Bar_Code = immBarCode;
                this.Event_type = eventType;
                this.User_Name = userName;
                this.Date_Time = dateTime;
            }
        }

        private StorageFile DbFile { get; set; }
        public LocalDB()
        {
            this.CheckNCreateDB();
        }

        /// <summary>
        /// Init data base in local app folder, init ImmBarCode and PfBarCode
        /// </summary>
        private async void CheckNCreateDB()
        {
            this.DbFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("sqliteSample.db", CreationCollisionOption.OpenIfExists);
            using (SqliteConnection db = new SqliteConnection($"Filename={this.DbFile.Path}"))
            {
                db.Open();
                SqliteCommand createTable = new SqliteCommand(
                    "CREATE TABLE IF NOT " +
                    "EXISTS MyTable (Primary_Key INTEGER PRIMARY KEY, " +
                    "PF_Bar_Code NVARCHAR(2048) NULL, " +
                    "Imm_Bar_Code NVARCHAR(2048) NULL, " +
                    "Event NVARCHAR(2048) NULL, " +
                    "User_Name NVARCHAR(20) NULL, " +
                    "Date_Time DATETIME NULL)", db);

                createTable.ExecuteReader();
            }
        }

        /// <summary>
        /// Get last hardware settings from DB
        /// </summary>
        /// <param name="pfBarCode">Returnable press form bar code</param>
        /// <param name="immBarCode">Returnable injection molding machine bar code</param>
        public void GetHardwareSettings(out string pfBarCode, out string immBarCode)
        {
            using (SqliteConnection db = new SqliteConnection($"Filename={this.DbFile.Path}"))
            {
                db.Open();
                SqliteCommand getLastRow = new SqliteCommand(
                    "SELECT * FROM MyTable ORDER BY Primary_Key DESC LIMIT 1", db);
                SqliteDataReader lastRow = getLastRow.ExecuteReader();
                pfBarCode = "";
                immBarCode = "";
                while (lastRow.Read())
                {
                    pfBarCode = lastRow.GetString(1);
                    immBarCode = lastRow.GetString(2);
                }
            }
        }

        /// <summary>
        /// Adds row to data base
        /// </summary>
        /// <param name="barCode">Bar_Code clolumn value</param>
        /// <param name="userName">User_Name clolumn value</param>
        /// <param name="dateTime">Date_Time clolumn value</param>
        public void AddData(string pfBarCode, string immBarCode, string eventType, string userName, DateTime dateTime)
        {
            long tempPrimaryKey;
            using (SqliteConnection db = new SqliteConnection($"Filename={this.DbFile.Path}"))
            {
                db.Open();

                SqliteCommand insertCommand = new SqliteCommand(
                    "INSERT INTO MyTable (PF_Bar_Code, Imm_Bar_Code, Event, User_Name, Date_Time) " +
                    "VALUES (@first, @second, @third, @fourth, @fifth)",
                    db);
                insertCommand.Parameters.AddWithValue("@first", pfBarCode);
                insertCommand.Parameters.AddWithValue("@second", immBarCode);
                insertCommand.Parameters.AddWithValue("@third", eventType);
                insertCommand.Parameters.AddWithValue("@fourth", userName);
                insertCommand.Parameters.AddWithValue("@fifth", dateTime);

                insertCommand.ExecuteReader();
                if ((App.DBContentTemp.Count() - 1) >= 0)
                    tempPrimaryKey = App.DBContentTemp[App.DBContentTemp.Count() - 1].Primary_Key + 1;
                else
                    tempPrimaryKey = 0;
                App.DBContentTemp.Add(new Row(tempPrimaryKey, pfBarCode, immBarCode, eventType, userName, dateTime));
            }
        }
        /// <summary>
        /// Delete a batch of rows from local DB
        /// </summary>
        public bool DeleteBatchData(int start, int finish)
        {
            using (SqliteConnection db = new SqliteConnection($"Filename={this.DbFile.Path}"))
            {
                try
                {
                    db.Open();

                    SqliteCommand deleteCommand = new SqliteCommand(
                        "DELETE FROM `MyTable` WHERE `Primary_Key` >= " + start.ToString() +
                        " AND `Primary_Key` <= " + finish.ToString()+ ";",
                        db);

                    deleteCommand.ExecuteReader();
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Gets data from local data base
        /// </summary>
        /// <returns>Full data table like list of row</returns>
        public void GetData()
        {
            using (SqliteConnection db = new SqliteConnection($"Filename={this.DbFile.Path}"))
            {
                db.Open();
                SqliteCommand selectCommand = new SqliteCommand("SELECT * FROM MyTable", db);

                SqliteDataReader query = selectCommand.ExecuteReader();
                App.DBContentTemp.Clear();
                while (query.Read())
                {
                    App.DBContentTemp.Add(new Row(query.GetInt64(0), query.GetString(1), query.GetString(2), query.GetString(3),
                        query.GetString(4), query.GetDateTime(5)));
                }
            }
        }
        
        /// <summary>
        /// Delete all data from db
        /// </summary>
        public void ClearData()
        {
            using (SqliteConnection db = new SqliteConnection($"Filename={this.DbFile.Path}"))
            {
                db.Open();
                SqliteCommand insertCommand = new SqliteCommand("DELETE FROM MyTable", db);
                insertCommand.ExecuteReader();
                App.DBContentTemp.Clear();
            }
        }
    }
}
