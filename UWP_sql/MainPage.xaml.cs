/******************************************************************************
 * This file contain class, represents main page, GUI handlers and all
 * functional used with this page. MainPage instance using with App class.
 * 
 * @file App.xaml.cs
 * @author 0riginalSin
 * @brief Contains MainPage class declaration with comments.
 *
 ******************************************************************************
 */
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.Devices.Gpio;
using Microsoft.Toolkit.Uwp.UI.Controls;

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Timers;
using System.Threading;
using MySql.Data.MySqlClient;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409
namespace UWP_sql
{
    /// <summary>
    /// Start page and the only one. Contatins all GUI to initialize info about using devices like IMM and PF and
    /// start logging process. Fields specifies information about current session and GUI representation.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //App.DataBase.GetData();

        public ObservableCollection<Row> Prop { get; set; } = App.DBContentTemp; //Customer.GetSampleCustomerList(1);
        //ItemsSource="{x:Bind prop}"
        private static GpioPin PfButtonPin; // pin where PF button connected
        private static GpioController GpioControl; // controller for GPIO
        static bool PfShutFlag = false; // state of PF true=shuted
        //private static CultureInfo RuCulture = new CultureInfo("ru-RU"); // culture settings we use
        static bool ReadingPfShutFlag = false; // flag of logging PF shuting
        static bool ReadingPfFlag = false; // flag of reading PF bar code
        static string PfBarCodeTemp = ""; // temp var for read PF bar code
        static bool ReadingImmFlag = false; // temp var for read IMM bar code
        static string ImmBarCodeTemp = ""; // temp var for read IMM bar code

        static string UserName = "Имя пользователя"; // username
        public static string ImmBarCode; // code of using IMM
        public static string PfBarCode; // code of using PF
        //static private uint currentPfNumOfSymb = 0; // counter for symbols of PF bar code read
        //static private uint currentImmNumOfSymb = 0; // counter for symbols of IMM bar code read
        static bool MayInvokeNoPressFormDialog = true; // flag of dialog to avoid multipile creation
        // converter for date representation in DateGrid
        private DateTimeToStringConverter DateTimeConverter = new DateTimeToStringConverter();

        private DateTime lastTimePfShut; //time point using to check for bounce button
        //private Task sendAndUpdate;
        //private CancellationTokenSource sendAndUpdateCancelTokenSource = new CancellationTokenSource();
        //private CancellationToken sendAndUpdateToken;
        //long milliseconds_;
        //long milliseconds__;
        //long timeError = 0;

        public MainPage()
        {
            App.DataBase.GetData();
            this.InitializeComponent();
            Row rowToScrollTo;
            if ((App.DBContentTemp.Count() - 1) >= 0)
                rowToScrollTo = App.DBContentTemp[App.DBContentTemp.Count() - 1];
            else
                rowToScrollTo = null;
            DBShowDataGrid.ScrollIntoView(rowToScrollTo, null);

            //get hardware settings and show it with GUI
            App.DataBase.GetHardwareSettings(out PfBarCode, out ImmBarCode);
            CurrentImmBarCodeTBlock.Text = "Используемый ТПА: " + ImmBarCode.ToString();
            CurrentPfBarCodeTBlock.Text = "Используемая ПФ: " + PfBarCode.ToString();
            //check for gpio on using platform and initialize it
            GpioControl = GpioController.GetDefault();
            if (GpioControl != null)
            {
                //int a = GpioControl.PinCount;
                PfButtonPin = GpioControl.OpenPin(Constants.GpioPin);
                //GpioPin pin5 = GpioControl.OpenPin(5);
                //bool b = pin5.IsDriveModeSupported(GpioPinDriveMode.InputPullUp);
                PfButtonPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
                PfButtonPin.ValueChanged += PfButtonPin_ValueChanged;
            }
            //Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            //Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;
            Window.Current.CoreWindow.CharacterReceived += CoreWindow_CharReceived;
            //App.DataBase.SendEnd += SendEnd_Handler;

            //App.SendTimer.Elapsed += Send;
            //App.SendTimer.Start();
            lastTimePfShut = DateTime.MinValue;
            //milliseconds_ = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            //milliseconds__ = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            //Thread myThread = new Thread(new ThreadStart(SendThreadFunc));
            //myThread.Start();
            App.fromJsonInit();
            this.InitializeComponent();
            //initialize Token for correct kill of secondary thread when application closes
            //SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += this.OnCloseRequest;
            App.sendAndUpdateToken = App.sendAndUpdateCancelTokenSource.Token;
            App.sendAndUpdate = Task.Run(SendThreadFunc);
        }

        //public async void OnCloseRequest()//object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        //{
        //    App.sendAndUpdateCancelTokenSource.Cancel();
        //    await App.sendAndUpdate.AsAsyncAction();
        //    //while (App.IsTaskRunning) { }
        //    //Thread.Sleep(1);
        //}
        /// <summary>
        /// Secondary thread, that syncronize local SQLite table with MySQL-server
        /// </summary>
        private async void SendThreadFunc()
        {
            //no reason for permanently sync. this would free core for a time from Constants
            Thread.Sleep(Constants.TransactionDelay);

            //no reason for using protected algorithms in this application, so connection info stores in clear state
            const string connStr = "server=sgt-server.bwf.ru;user=tester;database=test;port=3306;password=tester";
            MySqlConnection conn = new MySqlConnection(connStr);
            long startTime;
            string result;
            //App.IsTaskRunning = true;
            while (true)
            {
                startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                if (App.sendAndUpdateToken.IsCancellationRequested)
                {
                    //App.IsTaskRunning = false;
                    //Thread.Sleep(Constants.TransactionDelay);
                    return;
                }
                //if there is unsyncronized rows - grab it and send to sql-server with Constants.RowsToSend batch limit
                if (App.StartRowToSend < App.DBContentTemp.Count)
                {
                    try
                    {
                        string sqlStr = "INSERT INTO T_ftry (Primary_Key, PF_Bar_Code, Imm_Bar_Code, Event, User_Name, Date_Time) VALUES";
                        int sizeOfbatch = 0;
                        //for (int i = App.StartRowToSend; i < App.StartRowToSend + Constants.RowsToSend; i++)
                        while (true)
                        {
                            sqlStr += "(\"" +
                                App.DBContentTemp[App.StartRowToSend + sizeOfbatch].Primary_Key.ToString() + "\", \"" +
                                App.DBContentTemp[App.StartRowToSend + sizeOfbatch].PF_Bar_Code.ToString() + "\", \"" +
                                App.DBContentTemp[App.StartRowToSend + sizeOfbatch].IMM_Bar_Code.ToString() + "\", \"" +
                                App.DBContentTemp[App.StartRowToSend + sizeOfbatch].Event_type.ToString() + "\", \"" +
                                App.DBContentTemp[App.StartRowToSend + sizeOfbatch].User_Name.ToString() + "\", \"" +
                                App.DBContentTemp[App.StartRowToSend + sizeOfbatch].Date_Time.ToString("yyyy-MM-dd HH:mm:ss");
                            sizeOfbatch += 1;
                            if (App.StartRowToSend + sizeOfbatch >= App.DBContentTemp.Count || sizeOfbatch >= Constants.RowsToSend)
                            {
                                sqlStr += "\");";
                                break;
                            }
                            else
                                sqlStr += "\"),";
                        }
                        if (App.sendAndUpdateToken.IsCancellationRequested)
                        {
                            //App.IsTaskRunning = false;
                            //Thread.Sleep(Constants.TransactionDelay);
                            return;
                        }
                        conn.Open();
                        MySqlCommand cmd = new MySqlCommand(sqlStr, conn);
                        cmd.ExecuteNonQuery();
                        result = " Синхронизация Успешна";
                        //Затраченное время: " + (DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTime).ToString(); //for debuging
                        App.StartRowToSend += sizeOfbatch;
                        App.startRowSave();
                    }
                    catch (Exception ex)
                    {
                        result = "Ошибка синхронизации с сервером";
                        //+ ex.ToString(); //(DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTime).ToString() + ex.ToString(); //for debuging
                    }
                    finally
                    {
                        conn.Close();
                    }
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        TBlockSendLog.Text = result;
                    });
                }
                //if quantity of rows > then we could store(specifies with Constants.RowsLimitInDB var)
                //delete rows from GUI up to allowed rows quantity
                if (App.DBContentTemp.Count > Constants.RowsLimitInDB)
                {
                    int rowsToDelete = App.DBContentTemp.Count - Constants.RowsLimitInDB;
                    if ( App.DataBase.DeleteBatchData((int)App.DBContentTemp[0].Primary_Key,
                        (int)App.DBContentTemp[0].Primary_Key + rowsToDelete - 1) )
                    {
                        for (int i = 0; i < rowsToDelete; i++)
                        {
                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                App.DBContentTemp.RemoveAt(0);
                                DBShowDataGrid.ScrollIntoView(App.DBContentTemp[App.DBContentTemp.Count() - 1], null);
                            });
                        }
                        App.StartRowToSend -= rowsToDelete;
                        if (App.StartRowToSend < 0)
                            App.StartRowToSend = 0;
                        App.startRowSave();
                    }
                }
                if (App.sendAndUpdateToken.IsCancellationRequested)
                {
                    //App.IsTaskRunning = false;
                    //Thread.Sleep(Constants.TransactionDelay);
                    return;
                }
                Thread.Sleep(Constants.TransactionDelay);
            }
        }
        //private void SendEnd_Handler(string errMessage)
        //{
        //    //TBlockSendLog.Text = errMessage;
        //    //SendButton.IsEnabled = true;
        //}
        /// <summary>
        /// Handler, should be invoke when pin state changed. Using for logging of PF shut event.
        /// </summary>
        /// <param name="sender">From what pin did event come.</param>
        /// <param name="args">Special args for this event type.</param>
        private void PfButtonPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            DateTime pfShutTime = DateTime.Now;
            TimeSpan currentDelay = pfShutTime - lastTimePfShut;
            if ( ReadingPfShutFlag && (currentDelay.TotalMilliseconds > Constants.PfShutDelay) )
            {
                if (args.Edge == GpioPinEdge.FallingEdge)
                {
                    _ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        if (!PfShutFlag)
                        {
                            TBlockPfButtonPin.Text = "Положение пресс - формы: Сомкнута";
                            PfShutFlag = true;
                        }
                    });
                }
                if (args.Edge == GpioPinEdge.RisingEdge)
                {
                    _ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        if (PfShutFlag)
                        {
                            PfShutFlag = false;
                            TBlockPfButtonPin.Text = "Положение пресс - формы: Разомкнута";
                            App.DataBase.AddData(PfBarCode, ImmBarCode, "Смыкание пресс-формы", UserName, DateTime.Now);
                            Row rowToScrollTo;
                            if ((App.DBContentTemp.Count() - 1) >= 0)
                                rowToScrollTo = App.DBContentTemp[App.DBContentTemp.Count() - 1];
                            else
                                rowToScrollTo = null;
                            DBShowDataGrid.ScrollIntoView(rowToScrollTo, null);
                            MayInvokeNoPressFormDialog = true;
                        }
                    });
                }
            }
            lastTimePfShut = pfShutTime;
        }
        /// <summary>
        /// Handler for key input event. Using for device configuration initialize with bath-code scanner
        /// because it could send batch-code like USB HID device. This is the way bath-code scanner connect with
        /// using platform in this application.
        /// </summary>
        /// <param name="sender">Do not use.</param>
        /// <param name="e">Using for key code get.</param>
        private void CoreWindow_CharReceived(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.CharacterReceivedEventArgs e)
        {
            //var a = e.KeyCode;
            //string b = Convert.ToChar(e.KeyCode).ToString();
            if (ReadingPfFlag)
            {
                if (e.KeyCode == 13) // Enter
                {
                    if (PfBarCode != PfBarCodeTemp)
                    {
                        TBlockLog.Text = "Лог сканера штрихкодов: " + PfBarCodeTemp;
                        TBlockLog.Text += " Добавлено";
                        App.DataBase.AddData(PfBarCodeTemp, ImmBarCode, "Замена пресс-формы", UserName, DateTime.Now);
                        Row rowToScrollTo;
                        if ((App.DBContentTemp.Count() - 1) >= 0)
                            rowToScrollTo = App.DBContentTemp[App.DBContentTemp.Count() - 1];
                        else
                            rowToScrollTo = null;
                        DBShowDataGrid.ScrollIntoView(rowToScrollTo, null);
                        PfBarCode = PfBarCodeTemp;
                        CurrentPfBarCodeTBlock.Text = "Используемая ПФ: " + PfBarCode.ToString();
                    }
                    PfBarCodeTemp = "";
                }
                else
                    PfBarCodeTemp += Convert.ToChar(e.KeyCode).ToString();
            }
            else if (ReadingImmFlag)
            {
                if (e.KeyCode == 13) // Enter
                {
                    if (ImmBarCode != ImmBarCodeTemp)
                    {
                        TBlockLog.Text = "Лог сканера штрихкодов: " + ImmBarCodeTemp;
                        TBlockLog.Text += " Добавлено";
                        App.DataBase.AddData(PfBarCode, ImmBarCodeTemp, "Замена термопластавтомата", UserName, DateTime.Now);
                        Row rowToScrollTo;
                        if ((App.DBContentTemp.Count() - 1) >= 0)
                            rowToScrollTo = App.DBContentTemp[App.DBContentTemp.Count() - 1];
                        else
                            rowToScrollTo = null;
                        DBShowDataGrid.ScrollIntoView(rowToScrollTo, null);
                        ImmBarCode = ImmBarCodeTemp;
                        CurrentImmBarCodeTBlock.Text = "Используемый ТПА: " + ImmBarCode.ToString();
                    }
                    ImmBarCodeTemp = "";
                }
                else
                    ImmBarCodeTemp += Convert.ToChar(e.KeyCode).ToString();
            }

        }
        //private void corewindow_keyup(windows.ui.core.corewindow sender, windows.ui.core.keyeventargs e)
        //{
        //    if (readingpfflag || readingimmflag && e.virtualkey == windows.system.virtualkey.shift)
        //        shiftpressed = false;
        //}
        //private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs e)
        //{
        //    if (ReadingPfFlag)
        //    {
        //        //TBlockLog.Text = currentPfNumOfSymb.ToString() + ": " + e.VirtualKey.ToString();
        //        //char a = Convert.ToChar(e.VirtualKey);
        //        //TBlockLog.Text += Char.ToLower(a);
        //        if (e.VirtualKey == Windows.System.VirtualKey.Shift)
        //        {
        //            shiftPressed = true;
        //        }
        //        else if (e.VirtualKey == Windows.System.VirtualKey.Enter)
        //        {
        //            //string str = sender.GetCurrentKeyEventDeviceId();
        //            if (PfBarCode != PfBarCodeTemp)
        //            {
        //                TBlockLog.Text = PfBarCodeTemp;
        //                TBlockLog.Text += " Added";
        //                //if (App.ImmBarCode != null) BarCodeTemp = App.ImmBarCode;
        //                //else BarCodeTemp = "";
        //                App.DataBase.AddData(PfBarCodeTemp, ImmBarCode, "Замена пресс-формы", UserName, DateTime.Now);
        //                PfBarCode = PfBarCodeTemp;
        //                CurrentPfBarCodeTBlock.Text = "Используемая ПФ: " + PfBarCode.ToString();
        //            }
        //            PfBarCodeTemp = "";
        //            shiftPressed = false;
        //        }
        //        else
        //        {
        //            if (shiftPressed)
        //                PfBarCodeTemp += Char.ToUpper(Convert.ToChar(e.VirtualKey)).ToString();
        //            else
        //                PfBarCodeTemp += Char.ToLower(Convert.ToChar(e.VirtualKey)).ToString();
        //        }
        //        //if (currentPfNumOfSymb == 3)
        //        //{
        //        //    currentPfNumOfSymb = 0;
        //        //    PfBarCodeTemp += e.VirtualKey.ToString();
        //        //    //string str = sender.GetCurrentKeyEventDeviceId();
        //        //    if (App.PfBarCode != PfBarCodeTemp)
        //        //    {
        //        //        //TBlockLog.Text += " Added";
        //        //        if (App.ImmBarCode != null) BarCodeTemp = App.ImmBarCode;
        //        //        else BarCodeTemp = "";
        //        //        App.DataBase.AddData(PfBarCodeTemp, BarCodeTemp, "Замена пресс-формы", UserName, DateTime.Now);
        //        //        App.PfBarCode = PfBarCodeTemp;
        //        //    }
        //        //}
        //        //else
        //        //{
        //        //    if (currentPfNumOfSymb == 0)
        //        //        PfBarCodeTemp = e.VirtualKey.ToString();
        //        //    else
        //        //        PfBarCodeTemp += e.VirtualKey.ToString();
        //        //    currentPfNumOfSymb++;
        //        //}
        //    }
        //    else if (ReadingImmFlag)
        //    {
        //        if (e.VirtualKey == Windows.System.VirtualKey.Shift)
        //        {
        //            shiftPressed = true;
        //        }
        //        else if (e.VirtualKey == Windows.System.VirtualKey.Enter)
        //        {
        //            //string str = sender.GetCurrentKeyEventDeviceId();
        //            if (ImmBarCode != ImmBarCodeTemp)
        //            {
        //                TBlockLog.Text = ImmBarCodeTemp;
        //                TBlockLog.Text += " Added";
        //                //if (App.ImmBarCode != null) BarCodeTemp = App.ImmBarCode;
        //                //else BarCodeTemp = "";
        //                App.DataBase.AddData(PfBarCode, ImmBarCodeTemp, "Замена пресс-формы", UserName, DateTime.Now);
        //                ImmBarCode = ImmBarCodeTemp;
        //                CurrentImmBarCodeTBlock.Text = "Используемый ТПА: " + ImmBarCode.ToString();
        //            }
        //            ImmBarCodeTemp = "";
        //            shiftPressed = false;
        //        }
        //        else
        //        {
        //            if (shiftPressed)
        //                ImmBarCodeTemp += Char.ToUpper(Convert.ToChar(e.VirtualKey)).ToString();
        //            else
        //                ImmBarCodeTemp += Char.ToLower(Convert.ToChar(e.VirtualKey)).ToString();
        //        }

        //        //    TBlockLog.Text = currentImmNumOfSymb.ToString() + ": " + e.VirtualKey.ToString();
        //        //    if (currentImmNumOfSymb == 3)
        //        //    {
        //        //        currentImmNumOfSymb = 0;
        //        //        ImmBarCodeTemp += e.VirtualKey.ToString();
        //        //        //string str = sender.GetCurrentKeyEventDeviceId();
        //        //        if (App.ImmBarCode != ImmBarCodeTemp)
        //        //        {
        //        //            TBlockLog.Text += " Added";
        //        //            if (App.PfBarCode != null) BarCodeTemp = App.PfBarCode;
        //        //            else BarCodeTemp = "";
        //        //            App.DataBase.AddData(BarCodeTemp, ImmBarCodeTemp, "Замена термопластавтомата", UserName, DateTime.Now);
        //        //            App.ImmBarCode = ImmBarCodeTemp;
        //        //        }
        //        //    }
        //        //    else
        //        //    {
        //        //        if (currentImmNumOfSymb == 0)
        //        //            ImmBarCodeTemp = e.VirtualKey.ToString();
        //        //        else
        //        //            ImmBarCodeTemp += e.VirtualKey.ToString();
        //        //        currentImmNumOfSymb++;
        //        //    }
        //    }
        //    else
        //        TBlockLog.Text = e.VirtualKey.ToString();
        //}
        /// <summary>
        /// Displays dialog window with information about error and path to prevent it.
        /// </summary>
        private async void DisplayNoPressFormDialog()
        {
            ContentDialog noPressForm = new ContentDialog()
            {
                Title = "Ошибка инициализации",
                Content = "Отсканируйте штрихкоды пресс-формы и термопластавтомата",
                CloseButtonText = "OK"
            };

            noPressForm.Closed += NoPressFormDialogClosed;
            await noPressForm.ShowAsync();
        }
        /// <summary>
        /// Handler. Should be use when dialog window closing.
        /// </summary>
        private void NoPressFormDialogClosed(ContentDialog sender, ContentDialogClosedEventArgs e)
        {
            MayInvokeNoPressFormDialog = true;
        }
        /// <summary>
        /// Handler. Should be invloke when start logging button is clicked. Set flag, what allow
        /// PfButtonPin_ValueChanged handler to work.
        /// </summary>
        private void StartReadingB_Click(object sender, RoutedEventArgs e)
        {
            if (ReadingPfShutFlag)
            {
                StartLoggingBTB.Text = "Начать логир.";
                ReadingPfShutFlag = false;
                ImmInitButton.IsEnabled = true;
                PfInitButton.IsEnabled = true;
            }
            else
            {
                if (PfBarCode != "" && ImmBarCode != "")
                {
                    StartLoggingBTB.Text = "Завершить логир.";
                    ReadingPfShutFlag = true;
                    ImmInitButton.IsEnabled = false;
                    PfInitButton.IsEnabled = false;
                }
                else if (MayInvokeNoPressFormDialog)
                {
                    DisplayNoPressFormDialog();
                    MayInvokeNoPressFormDialog = false;
                }
                //StartLoggingBTB.Text = "Завершить логир.";
                //ReadingPfShutFlag = true;
                //ImmInitButton.IsEnabled = false;
                //PfInitButton.IsEnabled = false;
            }
        }
        /// <summary>
        /// Handler. Should be invoke when IMM initialize button is clicked. Setup flag to allow
        /// read batch-code of IMM from code scanner.
        /// </summary>
        private void ImmInitB_Click(object sender, RoutedEventArgs e)
        {
            if (ReadingImmFlag)
            {
                ImmInitButton.Content = "Скан. ТПА";
                ReadingImmFlag = false;
                StartLoggingB.IsEnabled = true;
                PfInitButton.IsEnabled = true;
            }
            else
            {
                ImmInitButton.Content = "Скан. окончено";
                ReadingImmFlag = true;
                StartLoggingB.IsEnabled = false;
                PfInitButton.IsEnabled = false;
                ImmBarCodeTemp = "";
            }
        }
        /// <summary>
        /// Handler. Should be invoke when PF initialize button is clicked. Setup flag to allow
        /// read batch-code of PF from code scanner.
        /// </summary>
        private void PfInitB_Click(object sender, RoutedEventArgs e)
        {
            if (ReadingPfFlag)
            {
                PfInitButton.Content = "Скан. ПФ";
                ReadingPfFlag = false;
                StartLoggingB.IsEnabled = true;
                ImmInitButton.IsEnabled = true;
            }
            else
            {
                PfInitButton.Content = "Скан. окончено";
                ReadingPfFlag = true;
                StartLoggingB.IsEnabled = false;
                ImmInitButton.IsEnabled = false;
                PfBarCodeTemp = "";
            }
        }
        /// <summary>
        /// Clears local db.
        /// </summary>
        private void ClearB_Click(object sender, RoutedEventArgs e)
        {
            //App.DataBase.ClearData();
            ////DBShowDataGrid.ItemsSource = App.DataBase.GetData();
            
            //PfBarCode = "";
            //CurrentImmBarCodeTBlock.Text = "Используемый ТПА: ";
            //ImmBarCode = "";
            //CurrentPfBarCodeTBlock.Text = "Используемая ПФ: ";
        }
        /// <summary>
        /// Refreashes local db.
        /// </summary>
        private void RefreshB_Click(object sender, RoutedEventArgs e)
        {
            App.DataBase.GetData();
            DBShowDataGrid.ItemsSource = App.DBContentTemp;
        }
        /// <summary>
        /// Hanler. Should be invoke when text box for username input get new char.
        /// Check if the quantity of char in string is in the limit.
        /// </summary>
        private void UserNameTBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (UserNameTBox.Text.Length <= Constants.UsernameLength)
            {
                UserName = UserNameTBox.Text;
            }
            else
            {
                UserNameTBox.Text = UserNameTBox.Text.Substring(0, Constants.UsernameLength);
            }
        }
        /// <summary>
        /// Handler. Should be inboke when send button is clicked. Sinchronize local bd with server one.
        /// </summary>
        private async void Send(object sender, ElapsedEventArgs e)
        {
            ////con += 1;
            ////SendButton.IsEnabled = false;
            //long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            ////var result = await Task.Run(() => App.DataBase.SendData());

            //await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            //{
            //    TBlockSendLog.Text = /*(milliseconds2 - milliseconds__).ToString() + ' ' +*/
            //    (milliseconds - milliseconds_).ToString() + ' ' + timeError.ToString();
            //});
            //if (Math.Abs(milliseconds - milliseconds_ - 1000) > timeError)
            //    timeError = Math.Abs(milliseconds - milliseconds_ - 1000);
            //milliseconds_ = milliseconds;


            //var result = await Task.Run(() => App.DataBase.SendData());
            //await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            //{
            //    TBlockSendLog.Text = result;
            //});

        }
        private async void SendB_Click(object sender, RoutedEventArgs e)
        {
            //SendButton.IsEnabled = false;
            //TBlockSendLog.Text = "";
            //try
            //{
            //    //await System.Threading.Tasks.Task.Delay(3000);
            //    var result = await Task.Run(() => App.DataBase.SendData());//.ConfigureAwait(continueOnCapturedContext:true);
            //    TBlockSendLog.Text = result;
            //}
            //catch (Exception ex)
            //{
            //    System.Diagnostics.Debug.WriteLine(ex.Message);
            //}
            //SendButton.IsEnabled = true;
        }
        /// <summary>
        /// Handler. Should be invoked when new column added to DataGrid.
        /// Represent the column in ru language and ru DateTime view style.
        /// </summary>
        /// <param name="e">Column information.</param>
        private void DBShowDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            //if (e.Column.Header.ToString() == "Primary_Key")
            //    e.Column.Header = "№";
            //if (e.Column.Header.ToString() == "PF_Bar_Code")
            //    e.Column.Header = "Код ПФ";
            //if (e.Column.Header.ToString() == "IMM_Bar_Code")
            //    e.Column.Header = "Код ТПА";
            //if (e.Column.Header.ToString() == "Event_type")
            //    e.Column.Header = "Событие";
            //if (e.Column.Header.ToString() == "User_Name")
            //    e.Column.Header = "Имя пользователя";
            //if (e.Column.Header.ToString() == "Date_Time")
            //    e.Column.Header = "Дата";

            switch (e.Column.Header.ToString())
            {
                case "Primary_Key":
                    e.Column.Header = "№";
                    break;
                case "PF_Bar_Code":
                    e.Column.Header = "Код ПФ";
                    break;
                case "IMM_Bar_Code":
                    e.Column.Header = "Код ТПА";
                    break;
                case "Event_type":
                    e.Column.Header = "Событие";
                    break;
                case "User_Name":
                    e.Column.Header = "Имя пользователя";
                    break;
                case "Date_Time":
                    e.Column.Header = "Дата";
                    //e.Column.Format = "dd-MM-yyyy";
                    var col = e.Column as DataGridBoundColumn;
                    col.Binding.Converter = DateTimeConverter;
                    //DateTimeConverter con = new DateTimeConverter();
                    //(dgtc.Binding as Binding).Converter = con;
                    break;
            }
        }
    }
    /// <summary>
    /// Converter, specified to represent DateTime in string using ru format type.
    /// </summary>
    public class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            DateTime sourceTime = (DateTime)value;
            //return sourceTime.ToString("yyyy-MM-dd HH:mm:ss");
            return sourceTime.ToString("dd.MM.yyyy HH:mm:ss");
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            DateTime resultTime = DateTime.Parse(value.ToString());
            return resultTime;
        }
    }
    //protected override void Windows.UI.Xaml.Controls.Control.OnKeyDown(KeyRoutedEventArgs e) { };
}
