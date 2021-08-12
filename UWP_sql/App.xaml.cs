/******************************************************************************
 * Main class. Represents application and all of it global settings and
 * methods to initialize it. Start class of UWP-application.
 * 
 * @file App.xaml.cs
 * @author 0riginalSin
 * @brief Contains class declaration with comments.
 *
 ******************************************************************************
 */
using System;
using System.Collections.Generic;
using System.IO;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Text.Json;
using System.Threading;

namespace UWP_sql
{
    /// <summary>
    /// Class contains constants, usinging by application.
    /// </summary>
    static class Constants
    {
        //C:\Users\A\AppData\Local\Packages\e462456f-a8e5-43f5-a312-e405714dbc24_j9v2x9sjwk42y\LocalState\
        //public const DateTime PfDelay = new DateTime();
        public const int GpioPin = 2;
        public const byte UsernameLength = 20;

        public const byte RowsToSend = 20;
        public const int TransactionDelay = 1000;
        public const int RowsLimitInDB = 120; //should be greater than RowsToSend or not hmm!!!

        public const int PfShutDelay = 100; //milliseconds
    }

    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        //public static StorageFile DbFile { get; set; }
        public static LocalDB DataBase { get; set; }

        //public static List<Row> tempLofRow = new List<Row>();
        public static ObservableCollection<Row> DBContentTemp = new ObservableCollection<Row>();
        //public static SqliteDataReader query;
        //public static Timer SendTimer;
        public static int StartRowToSend = 0;

        //public static bool IsTaskRunning;
        static public Task sendAndUpdate;
        static public CancellationToken sendAndUpdateToken;
        static public CancellationTokenSource sendAndUpdateCancelTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain()
        /// </summary>
        public App()
        {
            DataBase = new LocalDB();
            //IsTaskRunning = false;
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            //SendTimer = new Timer(Constants.TransactionDelay);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            sendAndUpdateCancelTokenSource.Cancel();
            await sendAndUpdate.AsAsyncAction();
            //int a = 1;
            //if (a==1)
            ////if(!App.IsTaskRunning)
            deferral.Complete();
        }

        /// <summary>
        /// Read from Json file and initialize variables.
        /// </summary>
        public static void fromJsonInit()
        {
            if (File.Exists( Path.Combine(ApplicationData.Current.LocalFolder.Path, "ini.json") ))
            {
                //File.Create( Path.Combine(ApplicationData.Current.LocalFolder.Path, "ini.txt") );

                string fileName = Path.Combine(ApplicationData.Current.LocalFolder.Path, "ini.json");
                string jsonString = File.ReadAllText(fileName);
                List<int> tempList = JsonSerializer.Deserialize<List<int>>(jsonString);
                StartRowToSend = tempList[0];
            }
            //List<int> weatherForecast = new List<int>();
            //weatherForecast.Add(1);
            //weatherForecast.Add(2);
            //string fileName = Path.Combine(ApplicationData.Current.LocalFolder.Path, "WeatherForecast.json");
            //string jsonString = JsonSerializer.Serialize(weatherForecast);
            //File.WriteAllText(fileName, jsonString);
            //ApplicationData.Current.LocalFolder.CreateFileQuery("ini.txt");
        }
        /// <summary>
        /// Save startRowToSend to Json file.
        /// </summary>
        public static void startRowSave()
        {
            List<int> tempList = new List<int>();
            tempList.Add(StartRowToSend);
            string fileName = Path.Combine(ApplicationData.Current.LocalFolder.Path, "ini.json");
            string jsonString = JsonSerializer.Serialize(tempList);
            File.WriteAllText(fileName, jsonString);
        }
    }
    ///// <summary>
    ///// Row of local data base
    ///// </summary>
    //public class Row
    //{
    //    public long Primary_Key { get; set; }
    //    public string PF_Bar_Code { get; set; }
    //    public string IMM_Bar_Code { get; set; }
    //    public string Event_type { get; set; }
    //    public string User_Name { get; set; }
    //    public DateTime Date_Time { get; set; }

    //    public Row(long primaryKey, string pfBarCode, string immBarCode, string eventType, string userName, DateTime dateTime)
    //    {
    //        this.Primary_Key = primaryKey;
    //        this.PF_Bar_Code = pfBarCode;
    //        this.IMM_Bar_Code = immBarCode;
    //        this.Event_type = eventType;
    //        this.User_Name = userName;
    //        this.Date_Time = dateTime;
    //    }
    //}
    ///// <summary>
    ///// Local DB functional
    ///// </summary>
    //public class LocalDB
    //{
    //    private StorageFile DbFile { get; set; }
    //    public event SendEndHandler SendEnd;
    //    public delegate void SendEndHandler(string errMessage);
    //    public LocalDB()
    //    {
    //        this.CheckNCreateDB();
    //    }
    //    /// <summary>
    //    /// Init data base in local app folder, init ImmBarCode and PfBarCode
    //    /// </summary>
    //    private async void CheckNCreateDB()
    //    {
    //        this.DbFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("sqliteSample.db", CreationCollisionOption.OpenIfExists);
    //        using (SqliteConnection db = new SqliteConnection($"Filename={this.DbFile.Path}"))
    //        {
    //            db.Open();
    //            //string s = this.DbFile.Path;
    //            SqliteCommand createTable = new SqliteCommand(
    //                "CREATE TABLE IF NOT " +
    //                "EXISTS MyTable (Primary_Key INTEGER PRIMARY KEY, " +
    //                "PF_Bar_Code NVARCHAR(2048) NULL, " +
    //                "Imm_Bar_Code NVARCHAR(2048) NULL, " +
    //                "Event NVARCHAR(2048) NULL, " +
    //                "User_Name NVARCHAR(20) NULL, " +
    //                "Date_Time DATETIME NULL)", db);

    //            createTable.ExecuteReader();
    //        }
    //    }
    //    /// <summary>
    //    /// Get last hardware settings from DB
    //    /// </summary>
    //    /// <param name="pfBarCode">Returnable press form bar code</param>
    //    /// <param name="immBarCode">Returnable injection molding machine bar code</param>
    //    public void GetHardwareSettings(out string pfBarCode, out string immBarCode)
    //    {
    //        using (SqliteConnection db = new SqliteConnection($"Filename={this.DbFile.Path}"))
    //        {
    //            db.Open();
    //            SqliteCommand getLastRow = new SqliteCommand(
    //                "SELECT * FROM MyTable ORDER BY Primary_Key DESC LIMIT 1", db);
    //            SqliteDataReader lastRow = getLastRow.ExecuteReader();
    //            pfBarCode = "";
    //            immBarCode = "";
    //            while (lastRow.Read())
    //            {
    //                pfBarCode = lastRow.GetString(1);
    //                immBarCode = lastRow.GetString(2);
    //            }
    //        }
    //    }
    //    /// <summary>
    //    /// Adds row to data base
    //    /// </summary>
    //    /// <param name="barCode">Bar_Code clolumn value</param>
    //    /// <param name="userName">User_Name clolumn value</param>
    //    /// <param name="dateTime">Date_Time clolumn value</param>
    //    public void AddData(string pfBarCode, string immBarCode, string eventType, string userName, DateTime dateTime)
    //    {
    //        long tempPrimaryKey;
    //        using (SqliteConnection db = new SqliteConnection($"Filename={this.DbFile.Path}"))
    //        {
    //            db.Open();

    //            SqliteCommand insertCommand = new SqliteCommand(
    //                "INSERT INTO MyTable (PF_Bar_Code, Imm_Bar_Code, Event, User_Name, Date_Time) " +
    //                "VALUES (@first, @second, @third, @fourth, @fifth)",
    //                db);
    //            // Using parameterized query to prevent SQL injection attacks
    //            insertCommand.Parameters.AddWithValue("@first", pfBarCode);
    //            insertCommand.Parameters.AddWithValue("@second", immBarCode);
    //            insertCommand.Parameters.AddWithValue("@third", eventType);
    //            insertCommand.Parameters.AddWithValue("@fourth", userName);
    //            insertCommand.Parameters.AddWithValue("@fifth", dateTime);

    //            insertCommand.ExecuteReader();
    //            if ((App.DBContentTemp.Count() - 1) >= 0)
    //                tempPrimaryKey = App.DBContentTemp[App.DBContentTemp.Count() - 1].Primary_Key + 1;
    //            else
    //                tempPrimaryKey = 0;
    //            App.DBContentTemp.Add(new Row(tempPrimaryKey, pfBarCode, immBarCode, eventType, userName, dateTime));
    //        }
    //    }
    //    /// <summary>
    //    /// Delete a batch of rows from local DB
    //    /// </summary>
    //    public bool DeleteBatchData(int start, int finish)
    //    {
    //        using (SqliteConnection db = new SqliteConnection($"Filename={this.DbFile.Path}"))
    //        {
    //            try
    //            {
    //                db.Open();

    //                SqliteCommand deleteCommand = new SqliteCommand(
    //                    "DELETE FROM `MyTable` WHERE `Primary_Key` >= "+ start.ToString() +" AND `Primary_Key` <= " + finish.ToString()
    //                    /*Constants.RowsLomitInDB.ToString()*/ + ";",
    //                    db);

    //                deleteCommand.ExecuteReader();

    //            }
    //            catch (Exception)
    //            {
    //                return false;
    //            }
    //            return true;
    //        }
    //    }
    //    /// <summary>
    //    /// Gets data from local data base
    //    /// </summary>
    //    /// <returns>Full data table like list of row</returns>
    //    public void GetData()
    //    {
    //        //List<Row>  entries = new List<Row>();

    //        using (SqliteConnection db = new SqliteConnection($"Filename={this.DbFile.Path}"))
    //        {
    //            db.Open();
    //            SqliteCommand selectCommand = new SqliteCommand("SELECT * FROM MyTable", db);

    //            SqliteDataReader query = selectCommand.ExecuteReader();
    //            //App.query = selectCommand.ExecuteReader();
    //            //System.Collections.IEnumerator ie = query.GetEnumerator();
    //            //List < System.Data.Common.DataRecordInternal >
    //            //foreach (var item in query)
    //            //{
    //            //    var a = item;
    //            //    int i = 0;
    //            //    i += 1;
    //            //}
    //            App.DBContentTemp.Clear();
    //            while (query.Read())
    //            {
    //                App.DBContentTemp.Add(new Row(query.GetInt64(0), query.GetString(1), query.GetString(2), query.GetString(3),
    //                    query.GetString(4), query.GetDateTime(5)));
    //            }
    //        }
    //        //App.DBContentTemp = new ObservableCollection<Row>(entries);

    //        //return App.query;
    //    }
    //    public async Task<string> SendData()
    //    {
    //        //MainPage.con += 1;
    //        //return MainPage.con.ToString();
    //        //return await Task.Run(() => {

    //        long startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

    //        //CultureInfo RuCulture = new CultureInfo("ru-Ru");
    //        //mysql -u tester -ptester -h sgt-server.bwf.ru -P3306 test
    //        string connStr = "server=sgt-server.bwf.ru;user=tester;database=test;port=3306;password=tester";
    //        MySqlConnection conn = new MySqlConnection(connStr);



    //        //await Task.Delay(5000);
    //        string result;
    //        try
    //        {
    //            //await Task.Run(async () => await conn.OpenAsync());
    //            /*await */
    //            await conn.OpenAsync();//.Wait();
    //                                   //" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "
    //                                   //string sql = "INSERT INTO T_ftry (PF_Bar_Code, Imm_Bar_Code, Event, User_Name, Date_Time) " +
    //                                   //    "VALUES ('PF_Bar_Code','Imm_Bar_Code', 'Event', 'User_Name', '" + App.DBContentTemp[App.DBContentTemp.Count() - 1].Date_Time.ToString("yyyy-MM-dd HH:mm:ss") + "')";
    //                                   //string sql2 = "INSERT INTO T_ftry (PF_Bar_Code, Imm_Bar_Code, Event, User_Name, Date_Time) " +
    //                                   //    "VALUES ('PF_Bar_Code','Imm_Bar_Code', 'Event', 'User_Name', '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')";
    //            string sqlStr = "INSERT INTO T_ftry (PF_Bar_Code, Imm_Bar_Code, Event, User_Name, Date_Time) " +
    //                "VALUES (@PF_Bar_Code, @Imm_Bar_Code, @Event, @User_Name, @Date_Time)";

    //            MySqlCommand cmd = new MySqlCommand(sqlStr, conn);
    //            //int lastElement;
    //            //if ((App.DBContentTemp.Count() - 1) >= 0)
    //            //    lastElement = App.DBContentTemp.Count() - 1;
    //            //else
    //            //    lastElement = 0;

    //            //await Task.Delay(3000);
    //            //return "test";

    //            //foreach (Row r in App.DBContentTemp) //идем по всем
    //            //{
    //            //    cmd.Parameters.Clear();
    //            //    cmd.Parameters.AddWithValue("@PF_Bar_Code", r.PF_Bar_Code);
    //            //    cmd.Parameters.AddWithValue("@Imm_Bar_Code", r.IMM_Bar_Code);
    //            //    cmd.Parameters.AddWithValue("@Event", r.Event_type);
    //            //    cmd.Parameters.AddWithValue("@User_Name", r.User_Name);
    //            //    cmd.Parameters.AddWithValue("@Date_Time", r.Date_Time);
    //            for (int i = App.StartRowToSend; i < App.StartRowToSend + Constants.RowsToSend; i++)
    //            {
    //                if (App.StartRowToSend >= App.DBContentTemp.Count) {
    //                    result = (DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTime).ToString() + "Nothing to send";
    //                    return result;
    //                }
    //                cmd.Parameters.Clear();
    //                cmd.Parameters.AddWithValue("@PF_Bar_Code", App.DBContentTemp[i].PF_Bar_Code);
    //                cmd.Parameters.AddWithValue("@Imm_Bar_Code", App.DBContentTemp[i].IMM_Bar_Code);
    //                cmd.Parameters.AddWithValue("@Event", App.DBContentTemp[i].Event_type);
    //                cmd.Parameters.AddWithValue("@User_Name", App.DBContentTemp[i].User_Name);
    //                cmd.Parameters.AddWithValue("@Date_Time", App.DBContentTemp[i].Date_Time);


    //                //cmd.Parameters["@PF_Bar_Code"].Value =  r.PF_Bar_Code;
    //                //cmd.Parameters["@Imm_Bar_Code"].Value =  r.IMM_Bar_Code;
    //                //cmd.Parameters["@Event"].Value = r.Event_type;
    //                //cmd.Parameters["@User_Name"].Value = r.User_Name;
    //                //cmd.Parameters["@Date_Time"].Value = r.Date_Time;
    //                //cmd.Parameters.AddWithValue("@date", Apxp.DBContentTemp[App.DBContentTemp.Count() - 1].Date_Time.ToString("yyyy-MM-dd HH:mm:ss"));
    //                //var cmd = conn.CreateCommand();
    //                //cmd.CommandText = "INSERT INTO T_ftry (PF_Bar_Code, Imm_Bar_Code, Event, User_Name, Date_Time) " +
    //                //    "VALUES ('PF_Bar_Code','Imm_Bar_Code', 'Event', 'User_Name', '@date')";
    //                //cmd.Parameters.Add("@date", DateTime.Now);
    //                //conn.Open();
    //                /*await */
    //                await cmd.ExecuteNonQueryAsync();//.Wait();
    //            }
    //            result = (DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTime).ToString() + "Done";
    //            //result = "Done";
    //            App.StartRowToSend += 1;
    //        }
    //        catch (Exception ex)
    //        {
    //            /*await */
    //            //result = ex.ToString();
    //            result = (DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTime).ToString() + ex.ToString();
    //            //SendEnd?.Invoke( ex.ToString() );
    //            //SendEnd?.Invoke("N " + (DateTime.Now - time).ToString());

    //            //return ex.ToString();
    //        }
    //        finally
    //        {
    //            /*await */
    //            await conn.CloseAsync();//.Wait();
    //                                    //SendEnd?.Invoke("Done");
    //        }
    //        return result;

    //        //}).ConfigureAwait(continueOnCapturedContext: false);
    //    }

    //    /// <summary>
    //    /// Send local data base to the server
    //    /// </summary>
    //    //public string Send()
    //    //{
    //    //    //CultureInfo RuCulture = new CultureInfo("ru-Ru");
    //    //    //mysql -u tester -ptester -h sgt-server.bwf.ru -P3306 test
    //    //    string connStr = "server=sgt-server.bwf.ru;user=tester;database=test;port=3306;password=tester";
    //    //    MySqlConnection conn = new MySqlConnection(connStr);
    //    //    try
    //    //    {
    //    //        conn.Open();
    //    //        //" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "
    //    //        //string sql = "INSERT INTO T_ftry (PF_Bar_Code, Imm_Bar_Code, Event, User_Name, Date_Time) " +
    //    //        //    "VALUES ('PF_Bar_Code','Imm_Bar_Code', 'Event', 'User_Name', '" + App.DBContentTemp[App.DBContentTemp.Count() - 1].Date_Time.ToString("yyyy-MM-dd HH:mm:ss") + "')";
    //    //        //string sql2 = "INSERT INTO T_ftry (PF_Bar_Code, Imm_Bar_Code, Event, User_Name, Date_Time) " +
    //    //        //    "VALUES ('PF_Bar_Code','Imm_Bar_Code', 'Event', 'User_Name', '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')";
    //    //        string sqlStr = "INSERT INTO T_ftry (PF_Bar_Code, Imm_Bar_Code, Event, User_Name, Date_Time) " +
    //    //            "VALUES (@PF_Bar_Code, @Imm_Bar_Code, @Event, @User_Name, @Date_Time)";

    //    //        MySqlCommand cmd = new MySqlCommand(sqlStr, conn);
    //    //        int lastElement;
    //    //        if ((App.DBContentTemp.Count() - 1) >= 0)
    //    //            lastElement = App.DBContentTemp.Count() - 1;
    //    //        else
    //    //            lastElement = 0;
    //    //        foreach (Row r in App.DBContentTemp)
    //    //        {
    //    //            cmd.Parameters.Clear();
    //    //            cmd.Parameters.AddWithValue("@PF_Bar_Code", r.PF_Bar_Code);
    //    //            cmd.Parameters.AddWithValue("@Imm_Bar_Code", r.IMM_Bar_Code);
    //    //            cmd.Parameters.AddWithValue("@Event", r.Event_type);
    //    //            cmd.Parameters.AddWithValue("@User_Name", r.User_Name);
    //    //            cmd.Parameters.AddWithValue("@Date_Time", r.Date_Time);

    //    //            //cmd.Parameters["@PF_Bar_Code"].Value =  r.PF_Bar_Code;
    //    //            //cmd.Parameters["@Imm_Bar_Code"].Value =  r.IMM_Bar_Code;
    //    //            //cmd.Parameters["@Event"].Value = r.Event_type;
    //    //            //cmd.Parameters["@User_Name"].Value = r.User_Name;
    //    //            //cmd.Parameters["@Date_Time"].Value = r.Date_Time;
    //    //            //cmd.Parameters.AddWithValue("@date", Apxp.DBContentTemp[App.DBContentTemp.Count() - 1].Date_Time.ToString("yyyy-MM-dd HH:mm:ss"));
    //    //            //var cmd = conn.CreateCommand();
    //    //            //cmd.CommandText = "INSERT INTO T_ftry (PF_Bar_Code, Imm_Bar_Code, Event, User_Name, Date_Time) " +
    //    //            //    "VALUES ('PF_Bar_Code','Imm_Bar_Code', 'Event', 'User_Name', '@date')";
    //    //            //cmd.Parameters.Add("@date", DateTime.Now);
    //    //            //conn.Open();
    //    //            cmd.ExecuteNonQuery();
    //    //        }
    //    //    }
    //    //    catch (Exception ex)
    //    //    {
    //    //        conn.Close();
    //    //        return ex.ToString();
    //    //    }

    //    //    conn.Close();
    //    //    return "Done";
    //    //}
    //    //async public Task<string> Send()
    //    //{
    //    //    CultureInfo RuCulture = new CultureInfo("ru-Ru");
    //    //    //mysql -u tester -ptester -h sgt-server.bwf.ru -P3306 test
    //    //    string connStr = "server=sgt-server.bwf.ru;user=tester;database=test;port=3306;password=tester";
    //    //    MySqlConnection conn = new MySqlConnection(connStr);
    //    //    try
    //    //    {
    //    //        await conn.OpenAsync();
    //    //        //" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "
    //    //        string sql = "INSERT INTO T_ftry (PF_Bar_Code, Imm_Bar_Code, Event, User_Name, Date_Time) " +
    //    //            "VALUES ('PF_Bar_Code','Imm_Bar_Code', 'Event', 'User_Name', '" + App.DBContentTemp[App.DBContentTemp.Count() - 1].Date_Time.ToString("yyyy-MM-dd HH:mm:ss") + "')";
    //    //        string sql2 = "INSERT INTO T_ftry (PF_Bar_Code, Imm_Bar_Code, Event, User_Name, Date_Time) " +
    //    //            "VALUES ('PF_Bar_Code','Imm_Bar_Code', 'Event', 'User_Name', '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')";
    //    //        string sqlStr = "INSERT INTO T_ftry (PF_Bar_Code, Imm_Bar_Code, Event, User_Name, Date_Time) " +
    //    //            "VALUES (@PF_Bar_Code, @Imm_Bar_Code, @Event, @User_Name, @Date_Time)";

    //    //        MySqlCommand cmd = new MySqlCommand(sqlStr, conn);
    //    //        int lastElement;
    //    //        if ((App.DBContentTemp.Count() - 1) >= 0)
    //    //            lastElement = App.DBContentTemp.Count() - 1;
    //    //        else
    //    //            lastElement = 0;

    //    //        cmd.Parameters.AddWithValue("@PF_Bar_Code", App.DBContentTemp[lastElement].PF_Bar_Code);
    //    //        cmd.Parameters.AddWithValue("@Imm_Bar_Code", App.DBContentTemp[lastElement].IMM_Bar_Code);
    //    //        cmd.Parameters.AddWithValue("@Event", App.DBContentTemp[lastElement].Event_type);
    //    //        cmd.Parameters.AddWithValue("@User_Name", App.DBContentTemp[lastElement].User_Name);
    //    //        cmd.Parameters.AddWithValue("@Date_Time", App.DBContentTemp[lastElement].Date_Time);

    //    //        //cmd.Parameters.AddWithValue("@date", Apxp.DBContentTemp[App.DBContentTemp.Count() - 1].Date_Time.ToString("yyyy-MM-dd HH:mm:ss"));
    //    //        //var cmd = conn.CreateCommand();
    //    //        //cmd.CommandText = "INSERT INTO T_ftry (PF_Bar_Code, Imm_Bar_Code, Event, User_Name, Date_Time) " +
    //    //        //    "VALUES ('PF_Bar_Code','Imm_Bar_Code', 'Event', 'User_Name', '@date')";
    //    //        //cmd.Parameters.Add("@date", DateTime.Now);
    //    //        //conn.Open();
    //    //        await cmd.ExecuteNonQueryAsync();
    //    //    }
    //    //    catch (Exception ex)
    //    //    {
    //    //        conn.Close();
    //    //        return ex.ToString();
    //    //    }

    //    //    conn.Close();
    //    //    return "Done";
    //    //}

    //    /// <summary>
    //    /// Clears local data base
    //    /// </summary>
    //    public void ClearData()
    //    {
    //        using (SqliteConnection db = new SqliteConnection($"Filename={this.DbFile.Path}"))
    //        {
    //            db.Open();
    //            SqliteCommand insertCommand = new SqliteCommand("DELETE FROM MyTable", db);
    //            insertCommand.ExecuteReader();
    //            App.DBContentTemp.Clear();
    //        }
    //    }
    //}
}
