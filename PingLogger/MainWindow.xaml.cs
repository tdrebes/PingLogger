using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SQLite;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Reflection;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using System.ComponentModel;
using System.Data;
using System.Windows.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;

namespace PingLogger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string data_source = "PingLogger.sqlite";
        public ChartValues<double> Values { get; set; }
        public static DatabaseConnector dbConnector;
        public static MainWindow mainWindow;
        public static LineSeries graph;
        public static bool autoRefresh;
        public static DataGrid pingDataGrid;
        public DispatcherTimer dispatcherTimer;
        private NotifyIcon nIcon = null;
        public bool isVisible = true;
        public bool pauseGraphOnMinimize;

        public MainWindow()
        {
            InitializeComponent();
            FixMenuOrientation();
            pauseGraphOnMinimize = PauseOnMinimize_Checkbox.IsChecked.GetValueOrDefault();

            //Tray Icon:
            nIcon = new NotifyIcon();


            string strping = "PL";
            using (Icon icon = CreateTextIcon(strping))
            {
                nIcon.Icon = icon;

                ContextMenuStrip cMenuStrip = new ContextMenuStrip();
                cMenuStrip.Items.Add("Show/Hide", null, NotifyIconMenuShowHide_Click);
                cMenuStrip.Items.Add(new ToolStripSeparator());
                cMenuStrip.Items.Add("Exit", null, NotifyIconMenuExit_Click);
                nIcon.ContextMenuStrip = cMenuStrip;
                nIcon.DoubleClick += new EventHandler(NotifyIconMenuShowHide_Click);

                nIcon.Visible = true;
            }

            mainWindow = this;
            this.DataContext = this;
            pingDataGrid = this.DataGrid1;
            autoRefresh = AutoRefresh_Checkbox.IsChecked.Value;

            //Refresh Timer:
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);


            if (autoRefresh) SetTimer();

            //System.Windows.Data.Binding binding = new System.Windows.Data.Binding("yLabel");
            //binding.Source = new[] { "10", "25", "50", "75", "100", "150", "200" };
            PingModule.onlyLogTimeouts = true;

            dbConnector = new DatabaseConnector();
            dbConnector.CreatePingLogTable();

            //Start Ping Task:
            try
            {
                Task pingTask = new Task(PingModule.logPingTask);
                pingTask.Start();
                StatusText1.Text = "Pinging: " + PingModule.ip;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to start ping task.");
                Debug.WriteLine(e);
            }


            Values = new ChartValues<double> { 1, 2, 3, 4, 5 };
            DataContext = this;
            graph = new LineSeries
            {
                Values = new ChartValues<ObservableValue>
                {
                    new ObservableValue(0),
                    //new ObservableValue(0)
                },
                LabelPoint = point => point.X + "K ," + point.Y,
                PointGeometry = null
                //PointGeometrySize = 0
            };

            Chart1.Series = new SeriesCollection(){ graph };

            KeyBinding SaveFileKeyBinding = new KeyBinding(
                ApplicationCommands.Save, Key.S, ModifierKeys.Control);
            this.InputBindings.Add(SaveFileKeyBinding);


            //string cs = "Data Source=:memory:";
            if (!File.Exists(data_source))
            {
                CreateDb();
            }
            string cs = "Data Source = " + data_source;


            string stm = "SELECT SQLITE_VERSION()";

            using var con = new SQLiteConnection(cs);
            con.Open();

            dbConnector.CreatePingLogTable();
            dbConnector.CreateTimoutLogTable();


            using var cmd = new SQLiteCommand(stm, con);
            string version = cmd.ExecuteScalar().ToString();

            Debug.WriteLine($"SQLite version: {version}");
        }


        public void CreateDb()
        {
            SQLiteConnection.CreateFile(data_source);
        }

        public void FixMenuOrientation()
        {
            var menuDropAlignmentField = typeof(SystemParameters).GetField("_menuDropAlignment", BindingFlags.NonPublic | BindingFlags.Static);
            Action setAlignmentValue = () => {
                if (SystemParameters.MenuDropAlignment && menuDropAlignmentField != null) menuDropAlignmentField.SetValue(null, false);
            };
            setAlignmentValue();
            SystemParameters.StaticPropertyChanged += (sender, e) => { setAlignmentValue(); };
        }

        private void Ping_Click(object sender, RoutedEventArgs e)
        {
            long ping = PingModule.GetPing("1.1.1.1");
            dbConnector.InsertPing(ping);
        }

        private void SaveFile_run(object sender, ExecutedRoutedEventArgs e)
        {
            Debug.WriteLine("saving...");
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "Text file (*.txt)|*.txt";
            if (saveFileDialog.ShowDialog() == true)
            {
                string path = saveFileDialog.FileName;
                dbConnector.exportToXml(path);
            }
        }

        private void SaveFile_active(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CreateDatabase_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(dbConnector.CreateDatabase());
        }

        public void UpdateGrid_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();
            //dbConnector.UpdateDataGrid(DataGrid1, "Select * FROM ping_log;");
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void LogTimeouts_Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            PingModule.onlyLogTimeouts = true;
        }

        private void LogTimeouts_Checkbox_Unchecked(object sender, RoutedEventArgs e)
        {
            PingModule.onlyLogTimeouts = false;
        }

        private void AutoRefresh_Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            autoRefresh = true;
            SetTimer();
        }

        private void AutoRefresh_Checkbox_Unchecked(object sender, RoutedEventArgs e)
        {
            autoRefresh = false;
            StopTimer();
        }

        //Refresh grid data on timer tick
        protected void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Debug.WriteLine("Refreshing Data...");
            RefreshData();
        }

        public void RefreshData()
        {
            DataSet ds = dbConnector.getPingDataSet();
            DataGrid1.ItemsSource = ds.Tables[0].DefaultView;

            //Sort desc:
            var column = DataGrid1.Columns[0];
            DataGrid1.Items.SortDescriptions.Clear();
            DataGrid1.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, ListSortDirection.Descending));

            foreach (var col in DataGrid1.Columns)
            {
                col.SortDirection = null;
            }
            column.SortDirection = ListSortDirection.Descending;
            //DataGrid1.Items.Refresh();
        }

        private void SetTimer()
        {
            dispatcherTimer.Start();
        }

        private void StopTimer()
        {
            dispatcherTimer.Stop();
        }

        private void ClearTable_Button_Click(object sender, RoutedEventArgs e)
        {
            dbConnector.ClearPingTable();
            RefreshData();
        }

        private void Help_active(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Help_run(object sender, ExecutedRoutedEventArgs e)
        {
            Debug.WriteLine("help...");
            string url = "https://github.com/tdrebes/PingLogger";
            try
            {
                Process.Start(url);
            }
            catch
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }

        void notifyIcon_Click(object sender, EventArgs e)
        {
            //ShowQuickLaunchMenu();
        }

        public static Icon CreateTextIcon(string str)
        {
            int fontsize;
            if (str.Length >= 3)
            {
                fontsize = 10;
            }
            else
            {
                fontsize = 16;
            }
            Font fontToUse = new Font("Microsoft Sans Serif", fontsize, System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);
            System.Drawing.Brush brushToUse = new SolidBrush(System.Drawing.Color.White);
            Bitmap bitmapText = new Bitmap(16, 16);
            Graphics g = System.Drawing.Graphics.FromImage(bitmapText);

            g.Clear(System.Drawing.Color.Transparent);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
            g.DrawString(str, fontToUse, brushToUse, -4, -2);
            IntPtr hIcon = bitmapText.GetHicon();
            Icon icon = System.Drawing.Icon.FromHandle(hIcon);
            brushToUse.Dispose();
            bitmapText.Dispose();
            g.Dispose();

            return icon;

        }
        private void NotifyIconMenuExit_Click(object sender, System.EventArgs e)
        {
            nIcon.Visible = false;
            Environment.Exit(0);
        }

        private void NotifyIconMenuShowHide_Click(object sender, System.EventArgs e)
        {
            ToggleVisibility();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case WindowState.Maximized:
                    break;
                case WindowState.Minimized:
                    Debug.WriteLine("minized");
                    //hide / minimize to tray:
                    ToggleVisibility();
                    break;
                case WindowState.Normal:

                    break;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            nIcon.Visible = false;
        }

        private void Close_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            nIcon.Visible = false;
            Environment.Exit(0);
        }

        private void Close_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ToggleVisibility()
        {
            if (Visibility == Visibility.Hidden)
            {
                isVisible = true;
                Visibility = Visibility.Visible;
                System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action(delegate ()
                    {
                        this.WindowState = WindowState.Normal;
                        this.Activate();
                    })
                );
            }
            else
            {
                isVisible = false;
                Visibility = Visibility.Hidden;
            }
        }

        private void PauseOnMinimize_Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            pauseGraphOnMinimize = true;
        }

        private void PauseOnMinimize_Checkbox_Unchecked(object sender, RoutedEventArgs e)
        {
            pauseGraphOnMinimize = false;
        }
    }
}
