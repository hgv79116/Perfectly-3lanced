using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace VideoCaptureWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private readonly VideoCapture capture;
        private readonly BackgroundWorker bkgWorker;
        SerialPort _serialPort = new SerialPort("COM5", 9600, Parity.None, 8, StopBits.One);
        private string _data = "Data Display";
        public string Data
        {
            get { return _data; }
            set
            {
                _data = value;
                OnPropertyChanged();
            }
        }
        private bool _connStatus = false;

        public bool ConnStatus
        {
            get { return _connStatus; }
            set
            {
                _connStatus = value;
                OnPropertyChanged();
            }
        }
        #region UI_ObservableObject
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            ConnStatus = false;

            capture = new VideoCapture();

            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += Worker_DoWork;

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            capture.Open(0, VideoCaptureAPIs.ANY);
            if (!capture.IsOpened())
            {
                Close();
                return;
            }

            bkgWorker.RunWorkerAsync();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            bkgWorker.CancelAsync();

            capture.Dispose();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            while (!worker.CancellationPending)
            {
                using (var frameMat = capture.RetrieveMat())
                {
                    // Must create and use WriteableBitmap in the same thread(UI Thread).
                    Dispatcher.Invoke(() =>
                    {
                        FrameImage.Source = frameMat.ToWriteableBitmap();
                    });
                }

                Thread.Sleep(30);
            }
        }
        #region ClickEvents
        private void CloseSerialClick(object sender, RoutedEventArgs e)
        {
            _serialPort.WriteLine("open");

        }
        private void OpenSerialClick(object sender, RoutedEventArgs e)
        {
            if (ConnStatus) return;
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
            try
            {
                _serialPort.Open();
                ConnStatus = true;
            }
            catch
            {
                ConnStatus = false;
                MessageBox.Show("Please Reconnect");
            }
        }
        private void ChangeData(object sender, RoutedEventArgs e)
        {
            _serialPort.WriteLine("close");
        }
        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data = _serialPort.ReadLine();
            Debug.Print(data);
            Data = data;
        }
        #endregion
    }
}
