/******************************************************************************
 * Main class. Represents application and all of it global settings and
 * methods to initialize it. Start class of UWP-application.
 * 
 * @file App.xaml.cs
 * @author 0riginalSin
 * @brief Contains App class declaration with comments.
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
        public static LocalDB DataBase { get; set; }

        public static ObservableCollection<LocalDB.Row> DBContentTemp = new ObservableCollection<LocalDB.Row>();
        public static int StartRowToSend = 0;

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
            this.InitializeComponent();
            this.Suspending += OnSuspending;
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
            deferral.Complete();
        }

        /// <summary>
        /// Read from Json file and initialize variables.
        /// </summary>
        public static void fromJsonInit()
        {
            if (File.Exists( Path.Combine(ApplicationData.Current.LocalFolder.Path, "ini.json") ))
            {
                string fileName = Path.Combine(ApplicationData.Current.LocalFolder.Path, "ini.json");
                string jsonString = File.ReadAllText(fileName);
                List<int> tempList = JsonSerializer.Deserialize<List<int>>(jsonString);
                StartRowToSend = tempList[0];
            }
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
}
