using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using FileWatcherLibrary;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using System.Configuration;
using System.ServiceProcess;
using System.Drawing.Imaging;
using Microsoft.Win32;
using Path = System.IO.Path;
using System.Diagnostics;
using System.Windows.Threading;

namespace ApplicationDesktop
{
    public partial class MainWindow : Window
    {
        //do usunięcia                                                                                                                                                //do usunięcia
        private static readonly string serviceUrl = ConfigurationManager.AppSettings["ServiceUrl"];
        private static readonly string serverDirectory = ConfigurationManager.AppSettings["ServerDirectory"]; 
        private static readonly string serverFile = ConfigurationManager.AppSettings["ServerFile"];
        private static readonly string serverStatus = ConfigurationManager.AppSettings["ServerStatus"];

        readonly ServiceController service = new ServiceController(ConfigurationManager.AppSettings["ServiceName"]);
        private Boolean statusService;
        private Boolean statusWatched;

        private FileWatcherManager _fileWatcherManager = null;
        

        private string fileName = null;
        private string filePath = null;
        private string directoryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);//domyślna wartość ustawiona na folder moje dokumenty;

        private DispatcherTimer timer;//do automatycznego odświeżania


        public MainWindow()
        {
            InitializeComponent();
            InicializeSettings(); //podstawowe dynamiczne ustawienia elementów
            this.Closing += MainWindow_Closing;
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(_fileWatcherManager != null)
            {
                if (radioDirectory.IsChecked == true)
                {
                    _fileWatcherManager.StopFileWatch();
                    _fileWatcherManager = null;
                }
                else if (radioFile.IsChecked == true)
                {
                    _fileWatcherManager.StopFileContentWatch();
                    _fileWatcherManager = null;
                }
            }
          
        }

        //podstawowe dynamiczne ustawienia elementów
        private void InicializeSettings()
        {
            try
            {
                if (service.Status == ServiceControllerStatus.Running)
                {
                    statusService = false;
                    SendToServiceStatus();
                    buttonStartService.IsEnabled = false; buttonStopService.IsEnabled = true;
                    statusService = true;
                }
                else
                {
                    buttonStartService.IsEnabled = true;    buttonStopService.IsEnabled = false;
                    labelStatusService.Content = "Stopped"; labelStatusService.Foreground = Brushes.DarkRed;
                    statusService = false;
                }
            }
            catch
            {
                buttonStartService.Visibility = Visibility.Collapsed;
                buttonStopService.Visibility = Visibility.Collapsed;
                borderService.Visibility = Visibility.Collapsed;
                labelService.Visibility = Visibility.Collapsed;
                statusService = false;
                labelStatusService.Content = "needs to be reinstall!";
                labelStatusService.Foreground = Brushes.Red;
            }
            //ustawienie usługi
            
            radioDirectory.IsChecked = true;
            //ustawienia czasu odświeżania
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(double.Parse(ConfigurationManager.AppSettings["TimerTick"])); // Ustaw interwał na 15 sekund
            timer.Tick += Timer_Tick;

            if (!EventLog.SourceExists(ConfigurationManager.AppSettings["logName"])) checkBoxHistory.IsEnabled = false;
        }

        private void SendToServiceDirectorySTOP()
        {
            string inputText = "STOP";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var content = new StringContent(inputText, Encoding.UTF8, "text/plain");
                    HttpResponseMessage response = client.PostAsync(serverDirectory, content).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        buttonWatch.IsEnabled = true;
                        buttonStop.IsEnabled = false;
                        labelStatusService.Content = "Running";
                        labelStatusService.Foreground = Brushes.Gray;
                    }
                    else
                    {
                        MessageBox.Show("Failed to send text. STOP Directory", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        SendToServiceStatus();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SendToServiceDirectoryWatch()
        {
            string inputText = directoryPath;

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var content = new StringContent(inputText, Encoding.UTF8, "text/plain");
                    HttpResponseMessage response = client.PostAsync(serverDirectory, content).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        buttonWatch.IsEnabled = false;
                        buttonStop.IsEnabled = true;
                        labelStatusService.Content = "Watched";
                        labelStatusService.Foreground = Brushes.Green;
                        checkBoxHistory.IsEnabled = true;
                    }
                    else
                    {
                        MessageBox.Show("Failed to send text to watch Directory", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        SendToServiceStatus();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SendToServiceFileSTOP()
        {
            FileDataModel fileData = new FileDataModel { FileName = "STOP", FilePath = "STOP" };
            string json = JsonConvert.SerializeObject(fileData, Formatting.Indented);

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = client.PostAsync(serverFile, content).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        buttonWatch.IsEnabled = true;
                        buttonStop.IsEnabled = false;
                        labelStatusService.Content = "Running";
                        labelStatusService.Foreground = Brushes.Gray;
                    }
                    else
                    {   
                        MessageBox.Show("Failed to send text to Stop File", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        SendToServiceStatus();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SendToServiceFileWatch()
        {
            FileDataModel fileData = new FileDataModel { FileName = fileName, FilePath = filePath };
            string json = JsonConvert.SerializeObject(fileData, Formatting.Indented);

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = client.PostAsync(serverFile, content).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        buttonWatch.IsEnabled = false;
                        buttonStop.IsEnabled = true;
                        labelStatusService.Content = "Watched";
                        labelStatusService.Foreground = Brushes.Green;
                        checkBoxHistory.IsEnabled = true;
                    }
                    else
                    {
                        MessageBox.Show("Failed to send text to watch File", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        SendToServiceStatus();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void SendToServiceStatus()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(serverStatus);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = response.Content.ReadAsStringAsync().Result;
                        string trimmedResponseContent = responseContent.Substring(1, responseContent.Length - 2);//ponieważ przysyła się w jakimś "'`.
                        if (trimmedResponseContent.Equals("STOP"))
                        {
                            //nic nie śledzimy 
                            statusWatched = false;
                            labelStatusService.Content = "Running";
                            labelStatusService.Foreground = Brushes.Gray;
                            buttonWatch.IsEnabled = true;
                            buttonStop.IsEnabled = false;
                        }
                        else
                        {
                            
                            //rozróżniamy czy śledzimy folder(D$), czy plik(F$)
                            if (trimmedResponseContent[0] == 'D')
                            {
                                statusWatched = true;
                                labelStatusService.Content = "Watched";
                                labelStatusService.Foreground = Brushes.GreenYellow;
                                labelDirectory.Text = trimmedResponseContent.Substring(2);
                                directoryPath = trimmedResponseContent.Substring(2);
                                radioDirectory.IsChecked = true;
                                radioFile.IsChecked = false;
                                buttonWatch.IsEnabled = false;
                                buttonStop.IsEnabled = true;
                                ButtonWatch_Click(null, null);
                                //czy Watch directory się samo zmieni po zmianie radioutton                                                             //czy Watch directory się samo zmieni po zmianie radioutton
                            }
                            else if (trimmedResponseContent[0] == 'F')
                            {
                                statusWatched = true;
                                labelStatusService.Content = "Watched";
                                labelStatusService.Foreground = Brushes.GreenYellow;
                                labelFile.Text = trimmedResponseContent.Substring(2);
                                fileName = Path.GetFileName(trimmedResponseContent.Substring(2));
                                filePath = Path.GetDirectoryName(trimmedResponseContent.Substring(2));
                                labelFile.Foreground = Brushes.Black;
                                radioDirectory.IsChecked = false;
                                radioFile.IsChecked = true;
                                radioFile.IsEnabled = true;
                                buttonWatch.IsEnabled = false;
                                buttonStop.IsEnabled = true;
                                ButtonWatch_Click(null, null);
                                //czy Watch directory się samo zmieni po zmianie radioutton                                                                //czy Watch directory się samo zmieni po zmianie radioutton
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ButtonStartService_Click(object sender, RoutedEventArgs e)
        {
            SwitchService(true);
            EnabledButtons();
        }

        private void ButtonStopService_Click(object sender, RoutedEventArgs e)
        {
            SwitchService(false);
            if(_fileWatcherManager != null) ButtonStop_Click(null, null);
            else
            {
                buttonWatch.IsEnabled = true;
                buttonStop.IsEnabled = false;
            }
            EnabledButtons();
        }

        public void EnabledButtons()
        {
            if (service.Status == ServiceControllerStatus.Running) { buttonStartService.IsEnabled = false; buttonStopService.IsEnabled = true; }
            else { buttonStartService.IsEnabled = true; buttonStopService.IsEnabled = false; }
        }
        public void SwitchService(Boolean switchService)
        {
            if (switchService)
            {
                try
                {
                    if (service.Status == ServiceControllerStatus.Stopped) service.Start();
                }
                catch (InvalidOperationException)
                {
                    statusService = false;
                    MessageBox.Show("Could not start the service.", "Error",  MessageBoxButton.OK, MessageBoxImage.Error);
                }
                //SendToServiceStatus();                                                                                                    //chyba nie potrzebne
                labelStatusService.Content = "Running";
                labelStatusService.Foreground = Brushes.Gray;
                statusService = true;
                service.WaitForStatus(ServiceControllerStatus.Running);
            }
            else
            {
                try
                {
                    if (radioFile.IsChecked == true && _fileWatcherManager != null) SendToServiceFileSTOP();
                    if (radioDirectory.IsChecked == true && _fileWatcherManager != null) SendToServiceDirectorySTOP();
                    statusWatched = false;
                    if (service.Status == ServiceControllerStatus.Running) service.Stop();
                }
                catch (InvalidOperationException)
                {
                    statusService = true;
                    MessageBox.Show("Could not stop the service.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                labelStatusService.Content = "Stopped";
                labelStatusService.Foreground = Brushes.DarkRed;
                statusService= false;
                service.WaitForStatus(ServiceControllerStatus.Stopped);
            }
        }

        private void ButtonChangeFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a File",
                Filter = "Text Files|*.txt",
                InitialDirectory = @"C:\Users\Username\Documents",
                CheckFileExists = false,
                CheckPathExists = true
            };


            if (dialog.ShowDialog() == true)
            {
                string selectedFilePath = dialog.FileName;
                string selectedFileName = Path.GetFileName(selectedFilePath);

                filePath = Path.GetDirectoryName(selectedFilePath);
                fileName = selectedFileName;

                if (fileName != null)
                {
                    labelFile.Text = filePath + "\\" + fileName;
                    labelFile.Foreground = Brushes.Black;

                    if(radioFile.IsChecked == true)
                    {
                        if (_fileWatcherManager != null)//jeśli wcześniej niczego nie obserowaliśmy to nie włączamy obserowania
                        {
                            radioFile.IsEnabled = false;
                            ButtonStop_Click(null, null);
                            textBoxFile.Text = string.Empty;
                            ButtonWatch_Click(null, null);
                        }
                        else textBoxFile.Text = string.Empty;
                    }
                    if(buttonWatch.IsEnabled != false) radioFile.IsEnabled = true;       
                }
                else
                {
                    radioFile.IsEnabled = false;
                    labelFile.Text = "none";
                    labelFile.Foreground = Brushes.Red;
                }
            }
        }

        private void ButtonChangeDirectory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                InitialDirectory = @"C:\Users\Username\Documents",  // ustawia wartość początkową
                Title = "Select a Directory", // zamieniamy "Save As" na "Select a Directory"
                Filter = "Directory|*.this.directory", // wyłączamy możliwość wyświetlenia plików
                FileName = "select" // ustawiamy nazwę "pliku" na "select.this.directory"
            };
            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FileName;
                // usuwamy sztuczną nazwę pliku
                path = path.Replace("\\select.this.directory", "");
                path = path.Replace(".this.directory", "");
                //jeśli użytkownik zminił nazwę "pliku" zakładamy, że to miał byc folder i go tworzymy
                if (!System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.CreateDirectory(path);
                }
                if(directoryPath != path) UpdateSelectedDirectoryPath(path);
            }
        }
        private void UpdateSelectedDirectoryPath(string newPath)
        {
            MessageBox.Show($"Windows log not exist.{directoryPath}, {newPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            if (newPath != null)
            {
                labelDirectory.Text = newPath;
                labelDirectory.Foreground = Brushes.Black;
                directoryPath = newPath;
                if (radioDirectory.IsChecked == true)
                {
                    if (_fileWatcherManager != null)//jeśli wcześniej niczego nie obserowaliśmy to nie włączamy obserowania
                    {
                        ButtonStop_Click(null, null);
                        textBoxDirectory.Text = string.Empty;
                        ButtonWatch_Click(null, null);
                    }
                    else textBoxDirectory.Text = string.Empty;
                }
            }
            else
            {
                labelDirectory.Text = "none";
                labelDirectory.Foreground = Brushes.Red;
            }

        }

        private void RadioFile_Checked(object sender, RoutedEventArgs e)
        {
            if(checkBoxHistory.IsChecked == false)
            {
                scrollBoxDirectory.Visibility = Visibility.Collapsed;
                scrollBoxHistory.Visibility = Visibility.Collapsed;
                scrollBoxFile.Visibility = Visibility.Visible;
            }
        }

        private void RadioDirectory_Checked(object sender, RoutedEventArgs e)
        {
            if(checkBoxHistory.IsChecked == false)
            {
                scrollBoxFile.Visibility = Visibility.Collapsed;
                scrollBoxHistory.Visibility = Visibility.Collapsed;
                scrollBoxDirectory.Visibility = Visibility.Visible;
            }
        }

        private void CheckBoxHistory_Checked(object sender, RoutedEventArgs e)
        {
                scrollBoxFile.Visibility = Visibility.Collapsed;
                scrollBoxDirectory.Visibility = Visibility.Collapsed;
                scrollBoxHistory.Visibility = Visibility.Visible;
                buttonCleanHistory.IsEnabled = true;
                buttonClear.IsEnabled = false;
                Debug.WriteLine("On");
                timer.Start();
                listViewEvent.Items.Clear();
                SetListBox();
        }
        private void CheckBoxHistory_UnChecked(object sender, RoutedEventArgs e)
        {
            buttonClear.IsEnabled = true;         
            buttonCleanHistory.IsEnabled = false;
            timer.Stop();

            if (radioDirectory.IsChecked == true)
            {
                RadioDirectory_Checked(null, null);
            }
            else if (radioFile.IsChecked == true)
            {
                RadioFile_Checked(null, null);
            }
        }
        //stworzyc
        public void SetListBox()
        {
            string logName = ConfigurationManager.AppSettings["logName"];
           

            if (!EventLog.SourceExists(logName))
            {
                MessageBox.Show("Windows log not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            EventLog Logs = new EventLog();

            Logs.Log = logName;

            foreach (EventLogEntry entry in Logs.Entries)
            {
                this.listViewEvent.Items.Add(new MyItem { Source = entry.Source, Message = entry.Message, TimeWritten = entry.TimeWritten.ToString() });
            }
        }

        public class MyItem
        {
            public string Source { get; set; }
            public string Message { get; set; }
            public string TimeWritten { get; set; }
        }

        private void ButtonCleanHistory_Click(object sender, RoutedEventArgs e)
        {
            listViewEvent.Items.Clear();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            listViewEvent.Items.Clear();
            SetListBox();
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            if (radioDirectory.IsChecked == true)
            {
                textBoxDirectory.Text = string.Empty;
            }
            else if (radioFile.IsChecked == true)
            {
                textBoxFile.Text = string.Empty;
            }
        }

        private void ButtonWatch_Click(object sender, RoutedEventArgs e)
        {
            if (statusService && !statusWatched)//usługa włączona przy starcie "śledznia" - zapisuje historię
            {
                if (radioDirectory.IsChecked == true)
                {
                    SendToServiceDirectoryWatch();
                }
                else if (radioFile.IsChecked == true)
                {
                    SendToServiceFileWatch();
                }
            }
            else
            {
                buttonStartService.IsEnabled = false;
                buttonWatch.IsEnabled = false;
                buttonStop.IsEnabled = true;
            }
            radioFile.IsEnabled = false;
            radioDirectory.IsEnabled = false;
            //usługa włączona/wyłączona była przy starcie
                if (radioDirectory.IsChecked == true)
                {
                    textBoxDirectory.Text += Environment.NewLine + "---Start watch directory---" + Environment.NewLine + Environment.NewLine;
                    _fileWatcherManager = new FileWatcherManager(directoryPath);
                    _fileWatcherManager.FileChangeEvent += DirectoryChangePrint;
                    _fileWatcherManager.FileDeletedEvent += DirectoryCurrentDelted;
                }
                else if (radioFile.IsChecked == true)
                {
                    textBoxFile.Text += Environment.NewLine + "--Start watch file--" + Environment.NewLine;
                    _fileWatcherManager= new FileWatcherManager(filePath);
                    _fileWatcherManager.WatchFileContentChanges(filePath+"\\"+fileName);
                    _fileWatcherManager.FileContentChangedEvent += FileChangePrint;
                    _fileWatcherManager.FileDeletedEvent += FileCurrentDeleted;
                    _fileWatcherManager.FileChangeActiveEvent += FileChangeActiveEventF;
                }
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            if (statusService)//usługa włączona przy starcie "śledznia" - zapisuje historię
            {
                 if (radioDirectory.IsChecked == true)
                {
                    SendToServiceDirectorySTOP();
                }
                else if (radioFile.IsChecked == true)
                {
                    SendToServiceFileSTOP();
                }
            }
            else
            {
                buttonStartService.IsEnabled = true;
                buttonWatch.IsEnabled = true;
                buttonStop.IsEnabled = false;
            }
 
            radioDirectory.IsEnabled = true;
            if (filePath != null) radioFile.IsEnabled = true;
            if (radioDirectory.IsChecked == true && _fileWatcherManager != null)
                {
                    textBoxDirectory.Text += Environment.NewLine + "---Stop watch directory---" + Environment.NewLine + Environment.NewLine;
                    _fileWatcherManager.StopFileWatch();
                    _fileWatcherManager = null;
                }
                else if (radioFile.IsChecked == true && _fileWatcherManager != null)
                {
                    textBoxFile.Text += Environment.NewLine + "--Stop watch file--" + Environment.NewLine;   
                    _fileWatcherManager.StopFileContentWatch();
                    _fileWatcherManager = null;
                    //_fileWatcherManager = new FileWatcherManager(filePath);           nie wiem po co przy stop
                }
        }

        //wyświetla śledzenie folderów
        private void DirectoryChangePrint(object sender, string e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                textBoxDirectory.Text += e + Environment.NewLine + Environment.NewLine;
            });
        }

        //do plików
        private void FileChangePrint(object sender, string e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if(!string.IsNullOrEmpty(e)) textBoxFile.Text += e + Environment.NewLine + Environment.NewLine;
               
            });
        }
        private void FileCurrentDeleted(object sender, string fullPath)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                labelFile.Text = "none";
                labelFile.Foreground = Brushes.Red;
                filePath = null;
                fileName = null;


                if (radioFile.IsChecked == true)
                {
                    ButtonStop_Click(null, null);
                }
                radioFile.IsEnabled = false;
                radioFile.IsChecked = false;
            });
        }
        private void DirectoryCurrentDelted(object sender, string fullPath)
        {

            Application.Current.Dispatcher.Invoke(() =>
            {
                labelDirectory.Text = "none";
                labelDirectory.Foreground = Brushes.Red;
                directoryPath = null;


                if (radioDirectory.IsChecked == true)
                {
                    ButtonStop_Click(null, null);
                }
                radioDirectory.IsEnabled = false;
                radioDirectory.IsChecked = false;
            });
        }
        private void FileChangeActiveEventF(object sender, string fullPath) {
            Application.Current.Dispatcher.Invoke(() =>
            {
                labelFile.Text = fullPath;
                labelFile.Foreground = Brushes.Black;

                if(radioFile.IsChecked == true)
                {
                    _fileWatcherManager.ChangePath(Path.GetDirectoryName(fullPath));
                }
            });
        }
    }
}
