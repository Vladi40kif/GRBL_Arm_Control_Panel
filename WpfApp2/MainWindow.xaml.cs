using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using Microsoft.Win32;
using System.IO.Ports;
using System.Threading;
using SharpDX.DirectInput;

namespace WpfApp1
{

    public partial class MainWindow : Window
    {
        private DateTime time = DateTime.Now;
        private SerialPort _serialPort;
        double X, Y, Z;
        double joystickX, joystickY, joystickZ;
        int S;
        Thread joysticThread;
        public MainWindow()
        {
        
            X = Y = Z = 0.0;
            S = 0;

            joysticThread = new Thread(new ThreadStart(Joystick));
            joysticThread.Start();


            StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;

            _serialPort = new SerialPort();

            InitializeComponent();

            Button_Start.IsEnabled = false;
            Button_Stop.IsEnabled = false;

            InitCOMsList();

            _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
        }
        public void InitCOMsList()
        {
            ComboBox_COMs.Items.Clear();
            foreach (string COM in SerialPort.GetPortNames())
                ComboBox_COMs.Items.Add(COM);
        }
        private void Button_RefleshCOMsList_Click(object sender, RoutedEventArgs e)
        {
            if (_serialPort.IsOpen)
                _serialPort.Close();
            Button_Start.IsEnabled = false;
            Button_Stop.IsEnabled = false;
            InitCOMsList();
        }
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            this.Dispatcher.Invoke(() =>
            {
                AddToListBox(sp.ReadExisting().Trim());
            });
        }
        private void Button_Start_Click(object sender, RoutedEventArgs e)
        {
            Button_Stop.IsEnabled = true;
            Button_Start.IsEnabled = false;
            try
            {
                _serialPort.PortName = ComboBox_COMs.SelectedItem.ToString();
                _serialPort.BaudRate = int.Parse(ComboBox_Bundrate.Text);
                _serialPort.Open();
            }
            catch (Exception)
            {
                Button_Stop.IsEnabled = false;
                Button_Start.IsEnabled = false;
                MessageBox.Show("Some setings is wrong!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Button_Stop_Click(object sender, RoutedEventArgs e)
        {
            Button_Stop.IsEnabled = false;
            Button_Start.IsEnabled = true;

            _serialPort.Close();
        }
        private void ComboBox_COMs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBox_Bundrate.SelectedItem != null)
                Button_Start.IsEnabled = true;
        }
        private void ComboBox_Bundrate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBox_COMs.SelectedItem != null)
                Button_Start.IsEnabled = true;
        }
        private void Button_Send_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBox_ShowSendData.IsChecked == true)
                this.Dispatcher.Invoke(() => AddToListBox(TextBox_InputCommand.Text));

            _serialPort.WriteLine(TextBox_InputCommand.Text);
        }
        private void TextBox_InputCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                Button_Send_Click(this, new RoutedEventArgs());
        }
        private void AddToListBox(string msg)
        {
            time = DateTime.Now;
            string _time = time.ToString("HH:mm:ss") + "." + time.Millisecond.ToString();
            ListBox_Chat.Items.Add(CheckBox_ShowTime.IsChecked == true ? _time + ":\t" + msg : msg);
            var border = (Border)VisualTreeHelper.GetChild(ListBox_Chat, 0);
            var scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
            scrollViewer.ScrollToBottom();
        }
        private void Button_ClearConsole_Click(object sender, RoutedEventArgs e)
        {
            ListBox_Chat.Items.Clear();
        }
        private void Button_LogSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save";
            saveFileDialog.FileName = "*.txt";
            if (saveFileDialog.ShowDialog() == true)
            {
                var FilePath = saveFileDialog.FileName;
                using (var file = new StreamWriter(FilePath))
                {
                    foreach (var item in ListBox_Chat.Items)
                        file.WriteLine(item.ToString());
                }
            }
        }
        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            double stp = 1.5;
            switch (e.Key) {
                case System.Windows.Input.Key.F1:        //x++
                    X+=stp;
                    break;
                case System.Windows.Input.Key.F2:        //x--
                    X -= stp;
                    break;
                case System.Windows.Input.Key.F5:        //y++
                    Y += stp;
                    break;
                case System.Windows.Input.Key.F6:        //y--
                    Y -= stp;
                    break;
                case System.Windows.Input.Key.F3:        //z++
                    Z += stp;
                    break;
                case System.Windows.Input.Key.F4:       //z--
                    Z -= stp;
                    break;
                case System.Windows.Input.Key.Home:      //set zero
                    _serialPort.Write("G92 X0 Y0 Z0\n");
                    AddToListBox("G92 X0 Y0 Z0\n");
                    X = Y = Z = 0;
                    break;
                case System.Windows.Input.Key.PageUp:  //spindel++
                    if (S >= 60)
                        break;
                    S++;
                    _serialPort.Write("M3 S" + S.ToString() + "\n");
                    AddToListBox("M3 S" + S.ToString() + "\n");
                    break;
                case System.Windows.Input.Key.PageDown:   // spindel--
                    if (S <= 0)
                        break;
                    S--;
                    _serialPort.Write("M3 S" + S.ToString() + "\n");
                    AddToListBox("M3 S" + S.ToString() + "\n");
                    break;
                default:
                    return;
            }

            string STR = "G0 X" + X.ToString() + " Y" + Y.ToString() + " Z" + Z.ToString() + "\n";
            _serialPort.Write(STR);
            AddToListBox(STR);
            
            this.Dispatcher.Invoke(() => {
                Lable_X.Content = X.ToString();
                Lable_Y.Content = Y.ToString();
                Lable_Z.Content = Z.ToString();
                Lable_S.Content = S.ToString();
            });
        }
        void Joystick()
        { 
            var directInput = new DirectInput();
            var joystickGuid = Guid.Empty;

            foreach (var deviceInstance in directInput.GetDevices(SharpDX.DirectInput.DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
                joystickGuid = deviceInstance.InstanceGuid;

            if (joystickGuid == Guid.Empty)
                foreach (var deviceInstance in directInput.GetDevices(SharpDX.DirectInput.DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
                    joystickGuid = deviceInstance.InstanceGuid;

            if (joystickGuid == Guid.Empty)
            {
                Console.WriteLine("No joystick/Gamepad found.");
                Console.ReadKey();
                Environment.Exit(1);
            }


            var joystick = new Joystick(directInput, joystickGuid);

            Console.WriteLine("Found Joystick/Gamepad with GUID: {0}", joystickGuid);


            var allEffects = joystick.GetEffects();
            foreach (var effectInfo in allEffects)
                Console.WriteLine("Effect available {0}", effectInfo.Name);

            joystick.Properties.BufferSize = 128;
            joystick.Acquire();

            while (true)
            {
                joystick.Poll();
                var datas = joystick.GetBufferedData();
                foreach (var state in datas) {
                    string pos = state.ToString();
                    if (pos.Substring(0,6) == "Offset")
                    {
                        char axis = pos[8];
                        int val = int.Parse(pos.Substring(pos.LastIndexOf("Value: ") + 7, pos.IndexOf(" Timestamp") - pos.LastIndexOf("Value: ") - 7)) / 6553 - 5;
                        this.Dispatcher.Invoke(() =>
                        {
                            switch (axis)
                            {
                                case 'X':
                                    Label_JoystickX.Content = val.ToString();
                                    break;
                                case 'Y':
                                    Label_JoystickY.Content = val.ToString();
                                    break;
                                case 'Z':
                                    Label_JoystickZ.Content = val.ToString();
                                    break;
                               
                            }
                        });
                    }
                }
                Thread.Sleep(100);
            }
        }
   
    }
}
