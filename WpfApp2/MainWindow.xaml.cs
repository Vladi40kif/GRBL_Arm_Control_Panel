using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using Microsoft.Win32;
using System.IO.Ports;
using SharpDX.DirectInput;
using System.Timers;

namespace WpfApp1
{

    public partial class MainWindow : Window
    {
        DateTime time = DateTime.Now;
        SerialPort _serialPort;
        bool sendOk;

        Joystick joystick;
        readonly Timer JoystickTimer, SendJoysticDataTimer;

        double X, Y, Z;
        int joystickX, joystickY, joystickZ;
        int S;
       
        public MainWindow()
        {
            sendOk = true;

            joystick = JoystickInit();

            JoystickTimer = new Timer(70);
            JoystickTimer.Elapsed += JoystickTimedEvent;
            JoystickTimer.AutoReset = true;
            JoystickTimer.Start();

            SendJoysticDataTimer = new Timer(100);
            SendJoysticDataTimer.Elapsed += SendJoysticDataTimerEvant;
            SendJoysticDataTimer.AutoReset = true;

            _serialPort = new SerialPort();

            InitializeComponent();

            Button_Start.IsEnabled = false;
            Button_Stop.IsEnabled = false;

            InitCOMsList();

            _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
        }

        //serial

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

            string msg = sp.ReadExisting().Trim();

            this.Dispatcher.Invoke(() =>
            {
                AddToListBox(msg);
            });

            if (!sendOk && msg == "ok")
                sendOk = true;

        }
        void SendMsnAndWaitResp(string msg)
        {
            _serialPort.Write(msg);
            sendOk = false;
        }

        // control

        private Joystick JoystickInit()
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

            return joystick;
        }
        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {

            double stp = 1.5;
            switch (e.Key)
            {
                case System.Windows.Input.Key.F1:        //x++
                    X += stp;
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

            SendMsnAndWaitResp(STR);
            AddToListBox(STR);

            this.Dispatcher.Invoke(() => {
                Lable_X.Content = X.ToString();
                Lable_Y.Content = Y.ToString();
                Lable_Z.Content = Z.ToString();
                Lable_S.Content = S.ToString();
            });
        }
        private void SendJoysticDataTimerEvant(Object source, ElapsedEventArgs e)
        {
            if (!sendOk)
                return;

            if (X != joystickX || Y != joystickY || Z != joystickZ)
            {

                X += joystickX;
                Y += joystickY;
                Z += joystickZ;

                string STR = "G0 X" + X.ToString() + " Y" + Y.ToString() + " Z" + Z.ToString() + "\n";

                SendMsnAndWaitResp(STR);
            }
        }
        private void JoystickTimedEvent(Object source, ElapsedEventArgs e)
        {

            string pos = "++++++++++++";

            joystick.Poll();
            var datas = joystick.GetBufferedData();

            foreach (var state in datas)
            {
                pos = state.ToString();
                //"Offset: Buttons5, Value: 128 Timestamp: 35556109 Sequence: 6"

                if (pos.Substring(0, 6) == "Offset")
                {
                    if (pos.Contains("Buttons") && pos[25] == '1')
                    {
                        switch (pos[15]) {
                            case '4':
                                if (S >= 60)
                                    break;
                                S+=10;
                                _serialPort.Write("M3 S" + S.ToString() + "\n");
                                //AddToListBox("M3 S" + S.ToString() + "\n");
                                break;
                            case '5':
                                if (S <= 0)
                                    break;
                                S-=10;
                                _serialPort.Write("M3 S" + S.ToString() + "\n");
                                //AddToListBox("M3 S" + S.ToString() + "\n");
                                break;
                            default:
                                break;
                        }

                    }
                    else
                    {
                        char axis = pos[8];
                        int val = int.Parse(pos.Substring(pos.LastIndexOf("Value: ") + 7, pos.IndexOf(" Timestamp") - pos.LastIndexOf("Value: ") - 7)) / 6553 - 5;

                        if (val == 1 || val == -1)
                            val = 0;

                        switch (axis)
                        {
                            case 'X':
                                joystickZ = val * -1;
                                break;
                            case 'Y':
                                joystickY = val * -1;
                                break;
                            case 'Z':
                                joystickX = val;
                                break;

                        };
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        Label_JoystickX.Content = joystickX;
                        Label_JoystickY.Content = joystickY;
                        Label_JoystickZ.Content = joystickZ;
                    });

                }
            }
        }

        //view  element

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
        private void Button_Send_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBox_ShowSendData.IsChecked == true)
                this.Dispatcher.Invoke(() => AddToListBox(TextBox_InputCommand.Text));

            _serialPort.WriteLine(TextBox_InputCommand.Text);
        }
        private void Button_ClearConsole_Click(object sender, RoutedEventArgs e)
        {
            ListBox_Chat.Items.Clear();
        }

        private void CheckBox_JoysticSync_Checked(object sender, RoutedEventArgs e)
        {
            SendJoysticDataTimer.Enabled = true;
        }
        private void CheckBox_JoysticSync_Unchecked(object sender, RoutedEventArgs e)
        {
            SendJoysticDataTimer.Enabled = false;
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
        
    }
}
