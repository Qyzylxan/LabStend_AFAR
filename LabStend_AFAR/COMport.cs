using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;

namespace LabStend_AFAR
{
    public static class COMport
    {
        static bool isBKUconnected = false;

        static string portNameBKU = "";
        static readonly int baudBKU = 115200;
        static readonly int baudPI = 9600;

        public static SerialPort serialPortBKU;
        public static SerialPort serialPortPI;

        public static List<string> availablePorts;
        public static List<string> availablePortNames = new List<string>();

        static SerialPort testPort;


        // Функция инициализации COM-портов
        public static void Init() {
            // Создание объекта списка доступных COM-портов
            availablePorts = new List<string>();

            // Создание объекта COM-порта для БКУ
            serialPortBKU = new SerialPort("COM1", baudBKU);

            testPort = new SerialPort();
            
        }


        // Функция ручного подключения к COM-порту
        public static void ConnectToCOM(SerialPort port, Label labelStatusBKU, Label statusLabel,
                                            int pickedPortIndex, char device)
        {
            
            string pickedPort = availablePortNames.ElementAt(pickedPortIndex);
            int baudRate = 9600;
            switch (device) {
                case 'e': baudRate = 115200; break;
                default: break;
            }
            try
            {
                port = new SerialPort(pickedPort, baudBKU);
                port.Open();


                labelStatusBKU.Text = "(подключён)";

                return;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка подключения COM-порта: {e.Message}");
                statusLabel.Text += $"\nРучное подключение к порту {pickedPort} не удалось.";
                statusLabel.TextColor = Colors.Red;
            }
            labelStatusBKU.Text = "(не подключён)";
            
            return;
        }

        // Функция автоподключения к COM-порту
        public static void AutoConnectToBKU(SerialPort port, Label labelStatusBKU, Label statusLabel, 
                                                    Picker COMportPicker, List<string> availablePorts)
        {
            // LoadAvailablePorts(statusLabel, COMportPicker, availablePorts);
            // Thread.Sleep(500);

            // Автоматический поиск БКУ (ESP32 с загруженным СПО БКУ)
            
            AutoDetectBKU(statusLabel, COMportPicker, availablePortNames, port, labelStatusBKU);
            Thread.Sleep(500);
            
            try
            {
                port.PortName = portNameBKU;
                port.Open();
                labelStatusBKU.Text = "(подключён)";
                
                return;
                
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка подключения COM-порта: {e.Message}");

            }
            labelStatusBKU.Text = "(не подключён)";
             
            return;
        }


        // Загрузка списка доступных COM-портов
        public static void LoadAvailablePorts(Label statusLabel, Picker[] COMportPickers, List<string> availablePorts)
        {
            availablePorts = null;
            availablePorts = new List<string>();
            try
            {
                
                //RefreshButton.IsEnabled = false;
                //ConnectionActivity.IsRunning = true;
                //ConnectionActivity.IsVisible = true;
                
                statusLabel.Text += "Поиск COM-порта...";
                statusLabel.TextColor = Colors.Orange;

                // Запускаем сканирование в отдельном потоке
                // var ports = await Task.Run(() => SerialPort.GetPortNames());
                string[] ports = SerialPort.GetPortNames();

                
                availablePorts = ports.OrderBy(p => p).ToList();

                availablePortNames = availablePorts;
                foreach (Picker p in COMportPickers){
                    p.ItemsSource = availablePorts;
                }

                if (availablePorts.Count == 0)
                {
                    foreach (Picker p in COMportPickers)
                    {
                        p.Title = "Нет COM-портов";
                    }
                    statusLabel.Text += "\nНет достуных COM-портов.";
                    statusLabel.TextColor = Colors.Red;
                }
                else
                {
                    foreach (Picker p in COMportPickers)
                    {
                        p.Title = "Список COM-портов";
                    }
                    statusLabel.Text += "\nНайдены COM-порты";
                    statusLabel.TextColor = Colors.Green;

                }
            }
            catch (Exception ex)
            {
                statusLabel.Text += "\nОшибка поиска COM-порта";
                statusLabel.TextColor = Colors.Red;
                
            }
            finally
            {
                /*
                RefreshButton.IsEnabled = true;
                ConnectionActivity.IsRunning = false;
                ConnectionActivity.IsVisible = false;
                */
            }
        }

        private static void AutoDetectBKU(Label statusLabel, Picker COMportPicker, List<string> availablePorts, SerialPort portBKU, Label labelStatusBKU)
        {
            statusLabel.Text += "\nАвтопоиск БКУ...";
            Thread.Sleep(500);

            foreach (string portName in availablePorts)
            {

                try
                {
                    //using (var testPort = new SerialPort(portName, baudBKU))

                    var testPort = new SerialPort(portName, baudBKU);
                    testPort.ReadTimeout = 50;
                    testPort.WriteTimeout = 50;

                    testPort.Open();

                    // Отправка команду запроса идентификации
                    testPort.WriteLine("BKU?\n");

                    // Ожидание ответа
                    string response = "";
                    try
                    {
                        response = testPort.ReadLine();
                    }
                    catch (TimeoutException) { }

                    if (response.Contains("BKU!"))
                    {
                        // ESP32 найден
                        COMportPicker.SelectedItem = portName;
                        statusLabel.Text += $"\nБКУ обнаружен на порту {portName}";
                        statusLabel.TextColor = Colors.Green;

                        portNameBKU = portName;
                        testPort.Close();
                        return;
                    }
                        
                    testPort.Close();
                        

                }
                catch
                {
                    statusLabel.Text += "\nОшибка автопоиска БКУ";
                    statusLabel.TextColor = Colors.Red;
                    
                }
            }
            statusLabel.Text += "\nБКУ не найден автоматически. Выберите порт вручную.";
            statusLabel.TextColor = Colors.Orange;
            return;

        }

        // Запись состояний МШУ в БКУ
        public static void WriteBKU() { 
            
        
        }

    }



}
