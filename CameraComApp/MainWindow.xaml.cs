using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO.Ports;
using System.IO;
using System.Collections.Specialized;

namespace CameraComApp
{
    public partial class MainWindow : Window
    {
        private readonly CameraService _cameraService;
        private readonly SerialPortService _serialPortService;
        private readonly SocketService _socketService;
        private readonly ConfigurationService _configService;
        private bool _isProcessingQr; 
        private bool _useSocket;
        private readonly ObservableCollection<LogEntry> _logEntries;
        // private const string LogFilePath = "log.txt";
      //  private string linkFolder = AppDomain.CurrentDomain.BaseDirectory;
        string linkLogFolder = AppDomain.CurrentDomain.BaseDirectory + "log";
        string namePC = Environment.MachineName;
        private const int MaxLogEntries = 10000;



        public MainWindow()
        {
            InitializeComponent();
            _cameraService = new CameraService();
            _serialPortService = new SerialPortService();
            _socketService = new SocketService();
            _configService = new ConfigurationService("Setup.ini");
            _logEntries = new ObservableCollection<LogEntry>();
            LogDataGrid.ItemsSource = _logEntries;
            InitializeControls();
            if (CameraComboBox.Items.Count > 0)
            {
                Start();
            }
            else
            {
                MessageBox.Show("Camera not found!", "Error", MessageBoxButton.OK , MessageBoxImage.Error);
            }
        }

        private void InitializeControls()
        {
            // Initialize camera list
            var cameras = _cameraService.GetAvailableCameras();
            CameraComboBox.ItemsSource = cameras;
            if (cameras.Count > 0)
            {
               
                var defaultCamera = _configService.GetSetting("UsbScan", "");
                int Camera;
                if (int.TryParse(defaultCamera, out Camera))
                {
                    CameraComboBox.SelectedIndex = Camera;
                }
                else
                {
                    StatusTextBlock.Text = "Invalid UsbScan = " + Camera;
                }
               
            }
               

            // Initialize COM ports
            ComPortComboBox.ItemsSource = SerialPort.GetPortNames();
            var defaultComPort = _configService.GetSetting("CammeraCom", "");
            if (!string.IsNullOrEmpty(defaultComPort) && ComPortComboBox.Items.Contains(defaultComPort))
           
                ComPortComboBox.SelectedItem = defaultComPort;

            // Initialize data
            var defaultData = _configService.GetSetting("type","COM");
            if (defaultData.Equals("socket"))
            {
                SocketRadioButton.IsChecked = true;
            }
          //  _useSocket = SocketRadioButton.IsChecked == true;
            // Initialize socket settings
            IpAddressTextBox.Text = _configService.GetSetting("ipAdress", "127.0.0.1");
            PortTextBox.Text = _configService.GetSetting("Port", "12345");

            // Subscribe to events
            _cameraService.FrameReceived += CameraService_FrameReceived;
        //    _cameraService.CameraStatusChanged += _cameraService_CameraStatusChanged;
            _serialPortService.CommandReceived += SerialPortService_CommandReceived;
            _socketService.CommandReceived += SocketService_CommandReceived;
            _cameraService.QrCodeDetected += CameraService_QrCodeDetected;
            _logEntries.CollectionChanged += LogEnTries_CollectionChanged;

            // Initialize log visibility
            LogGrid.Visibility = Visibility.Collapsed;
            ShowLogButton.IsChecked = false;
        }

        private void _cameraService_CameraStatusChanged(object sender, string status)
        {
            Dispatcher.Invoke(() =>
            {
                StatusTextBlock.Text = status;
                var logEntry = new LogEntry { Timestamp = DateTime.Now, Event = status };
                _logEntries.Add(logEntry);
                SaveLogToFile(logEntry);
            });
        }

        private void LogEnTries_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && _logEntries.Count > 0)
            {
                LogDataGrid.ScrollIntoView(_logEntries[_logEntries.Count - 1]);
                while (_logEntries.Count > MaxLogEntries)
                {
                    _logEntries.RemoveAt(0);
                }
            }
        }

        private void CameraService_QrCodeDetected(object sender, string qrCode)
        {
            Dispatcher.Invoke(() =>
            {
                StatusTextBlock.Text = $"QR Code Detected: {qrCode}";
                if (_useSocket)
                {
                    _socketService.SendData(qrCode + "END");

                }

                else
                {
                    _serialPortService.SendData(qrCode + "END");
                }
                   
                var logEnTry = new LogEntry { Timestamp = DateTime.Now, Event = $"Sent QR: {qrCode}" };
                _logEntries.Add(logEnTry);
                SaveLogToFile(logEnTry);
              //  StatusTextBlock.Text = $"Send QR Code OK";
                _isProcessingQr = false;
            });
        }

        private void SerialPortService_CommandReceived(object sender, string command)
        {
            if (!_useSocket)
                ProcessCommand(command);
        }

        private void SocketService_CommandReceived(object sender, string command)
        {
            if (_useSocket)
                ProcessCommand(command);
        }

        private bool TryReconnectCamera()
        {
            var logEnTry1 = new LogEntry { Timestamp = DateTime.Now, Event = "Camera Error auto Reconnect" };
            _logEntries.Add(logEnTry1);
            SaveLogToFile(logEnTry1);
            try
            {

                _cameraService.StopCamera();
                _cameraService.StartCamera(CameraComboBox.SelectedIndex);
                if (_cameraService.IsCameraRunning)
                {
                    var reconnectLog = new LogEntry { Timestamp = DateTime.Now, Event = "Camera Reconnect ok " };
                    _logEntries.Add(reconnectLog);
                    SaveLogToFile(reconnectLog);
                    return true;
                }
            }
            catch (Exception ex)
            {
                var errorLog = new LogEntry { Timestamp = DateTime.Now, Event = "Camera Reconnect Error " };
                StatusTextBlock.Text = "Camera Reconnect Error";
                _logEntries.Add(errorLog);
                SaveLogToFile(errorLog);
                return false;
            }
            return false;
        }
        private void ProcessCommand(string command)
        {
            Dispatcher.Invoke(() =>
            {
                if (command == "START" && !_isProcessingQr)
                {
                    _isProcessingQr = true;
                    StatusTextBlock.Text = "Reading QR Code...";
                    var logEnTry = new LogEntry { Timestamp = DateTime.Now, Event = "Received START command" };
                    _logEntries.Add(logEnTry);
                    SaveLogToFile(logEnTry);
                    if (!_cameraService.IsCameraRunning && CameraComboBox.SelectedIndex >=0) // nếu camera bị ngắt, lỗi, kết nối lại 
                    {
                        if (!TryReconnectCamera()) return;

                    }

                    _cameraService.StartQrCodeReading(3000, () =>
                    {
                        string timeoutMessage = "TIMEOUT";
                        if (_useSocket)
                            _socketService.SendData(timeoutMessage);
                        else
                            _serialPortService.SendData(timeoutMessage);
                        Dispatcher.Invoke(() =>
                        {
                            StatusTextBlock.Text = "QR Code reading timeout";
                            var timeoutLogEntry = new LogEntry { Timestamp = DateTime.Now, Event = "Sent TIMEOUT" };
                            _logEntries.Add(timeoutLogEntry);
                            SaveLogToFile(timeoutLogEntry);
                            _isProcessingQr = false;
                        });
                    });
                }
            });
        }

        private void CameraService_FrameReceived(object sender, System.Drawing.Bitmap frame)
        {
            Dispatcher.Invoke(() =>
            {
                CameraImage.Source = ConvertBitmapToBitmapSource(frame);
            });
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            Start();
        }
        private void Start()
        {
            if (CameraComboBox.SelectedIndex >= 0)
            {
                _cameraService.StartCamera(CameraComboBox.SelectedIndex);
                _useSocket = SocketRadioButton.IsChecked == true;

                if (_useSocket)
                {
                    if (string.IsNullOrEmpty(PortTextBox.Text))
                    {
                        StatusTextBlock.Text = "Please enter valid Port";
                        return;
                    }
                    int port;
                    if (int.TryParse(PortTextBox.Text, out port))
                    {
                        _socketService.StartServer(port);
                    }
                    else
                    {
                        StatusTextBlock.Text = "Invalid port number";
                        return;
                    }
                }
                else
                {
                    if (ComPortComboBox.SelectedItem != null)
                    {
                        _serialPortService.OpenPort(ComPortComboBox.SelectedItem.ToString());
                    }
                }
                StatusTextBlock.Text = $"Camera and {(_useSocket ? "Socket server" : "COM port")} started";
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
           
            _cameraService.StopCamera();
            if (_useSocket)
                _socketService.StopServer();
            else
                _serialPortService.ClosePort();
            StatusTextBlock.Text = $"Camera and {(_useSocket ? "Socket server" : "COM port")} stopped";
        }

        private void CameraComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_cameraService.IsCameraRunning)
            {
                _cameraService.StopCamera();
                _cameraService.StartCamera(CameraComboBox.SelectedIndex);
            }
        }

        private BitmapSource ConvertBitmapToBitmapSource(System.Drawing.Bitmap bitmap)
        {
            using (var memoryStream = new System.IO.MemoryStream())
            {
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
                memoryStream.Position = 0;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _cameraService.StopCamera();
            _serialPortService.ClosePort();
            _socketService.StopServer();
            
            base.OnClosed(e);
        }
        private string date()
        {
            DateTime timenow = DateTime.Now;
            return timenow.ToString("dd-MM-yyyy");
        }
        private void SaveLogToFile(LogEntry logEntry)
        {

            if (!Directory.Exists(linkLogFolder))
            {
                Directory.CreateDirectory(linkLogFolder);
            }
            string ngaythang = date();
            string filePath = Path.Combine(linkLogFolder, $"{namePC}_{ngaythang}.txt");
            try {
                string logLine = $"{logEntry.Timestamp:HH:mm:ss:fff} - {logEntry.Event}\n";
            //    txtLog.AppendText($"{DateTime.Now:HH:mm:ss} - {text}{Environment.NewLine}");
                File.AppendAllText(filePath, logLine);
            } catch (Exception ex)
            {
                Console.WriteLine("loi save file "+ex.Message);
            }
        }

        private void ShowLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogGrid.Visibility = ShowLogButton.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }
    }
    
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
    }

    
}