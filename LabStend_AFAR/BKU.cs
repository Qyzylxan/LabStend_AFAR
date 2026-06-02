using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using static LabStend_AFAR.MUAF;

namespace LabStend_AFAR
{
    public static class BKU
    {
        static bool isBKUconnected = false;

        static string portNameBKU = "";
        static readonly int baudBKU = 115200;
        

        public static SerialPort serialPortBKU;


        public static List<string> availablePorts;
        public static List<string> availablePortNames = new List<string>();

        static SerialPort testPort;
        static int delay = 100;

        // Функция инициализации COM-портов
        public static void Init() {
            // Создание объекта списка доступных COM-портов
            availablePorts = new List<string>();

            // Создание объекта COM-порта для БКУ
            serialPortBKU = new SerialPort("COM1", baudBKU);
            serialPortBKU.WriteTimeout = 500;

            testPort = new SerialPort();
            
        }


        // Функция ручного подключения к COM-порту
        public static string ConnectToCOM(SerialPort port, Label labelStatusBKU, Label statusLabel,
                                            int pickedPortIndex, char device)
        {
            string pickedPort;
            try
            { 
                pickedPort = availablePortNames.ElementAt(pickedPortIndex); 
            }
            catch (ArgumentOutOfRangeException e) {
                return "Нет доступных портов либо порт не выбран";    
            }

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

                return "Подключён";
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка подключения COM-порта: {e.Message}");
                //statusLabel.TextColor = Colors.Red;

                labelStatusBKU.Text = "(не подключён)";
                return $"\nРучное подключение к порту {pickedPort} не удалось.";
            }
            
        }

        // Функция автоподключения к COM-порту
        public static string AutoConnectToBKU(SerialPort port, Label labelStatusBKU, Label statusLabel, 
                                                    Picker COMportPicker, List<string> availablePorts)
        {
            // LoadAvailablePorts(statusLabel, COMportPicker, availablePorts);
            // Thread.Sleep(500);

            // Автоматический поиск БКУ (ESP32 с загруженным СПО БКУ)
            
            AutoDetectBKU(statusLabel, COMportPicker, port, labelStatusBKU);
            Thread.Sleep(delay);
            
            try
            {
                port.PortName = portNameBKU;
                port.Open();
                labelStatusBKU.Text = "(подключён)";
                
                return "Подключён";
                
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка подключения COM-порта: {e.Message}");

            }
            labelStatusBKU.Text = "(не подключён)";
             
            return "Ошибка подключения COM-порта";
        }


        // Загрузка списка доступных COM-портов
        public static string LoadAvailablePorts(Label statusLabel, Picker[] COMportPickers)
        {
            availablePorts = null;
            availablePorts = new List<string>();
            try
            {
                
                //RefreshButton.IsEnabled = false;
                //ConnectionActivity.IsRunning = true;
                //ConnectionActivity.IsVisible = true;
                
                //statusLabel.Text += "\nПоиск COM-порта...";
                //statusLabel.TextColor = Colors.Orange;

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
                    return "Нет достуных COM-портов.";
                    //statusLabel.TextColor = Colors.Red;
                }
                else
                {
                    foreach (Picker p in COMportPickers)
                    {
                        p.Title = "Список COM-портов";
                    }
                    return "Найдены COM-порты";
                    //statusLabel.TextColor = Colors.Green;

                }
            }
            catch (Exception ex)
            {
                return "Ошибка поиска COM-порта";
                //statusLabel.TextColor = Colors.Red;
                
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

        private static string AutoDetectBKU(Label statusLabel, Picker COMportPicker, SerialPort portBKU, Label labelStatusBKU)
        {
            //statusLabel.Text += "\nАвтопоиск БКУ...";
            Thread.Sleep(delay);
            byte[] bkuRequest = { bkuID};

            foreach (string portName in availablePorts)
            {

                try
                {
                    //using (var testPort = new SerialPort(portName, baudBKU))

                    var testPort = new SerialPort(portName, baudBKU);
                    testPort.ReadTimeout = 100;
                    testPort.WriteTimeout = 100;

                    testPort.Open();

                    // Отправка команду запроса идентификации (бывш. "BKU?\n")
                    testPort.Write(bkuRequest, 0, 1);

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
                        //statusLabel.Text += $"\nБКУ обнаружен на порту {portName}";
                        //statusLabel.TextColor = Colors.Green;

                        portNameBKU = portName;
                        testPort.Close();
                        return $"\nБКУ обнаружен на порту {portName}";
                    }
                        
                    testPort.Close();
                        

                }
                catch
                {
                    //statusLabel.Text += "\nБКУ: Ошибка автопоиска";
                    //statusLabel.TextColor = Colors.Red;
                    return "Ошибка автопоиска";
                }
            }
            //statusLabel.Text += "\nБКУ не найден автоматически. Выберите порт вручную.";
            //statusLabel.TextColor = Colors.Orange;
            return "БКУ не найден автоматически. Выберите порт вручную.";

        }

        // Запись команд в БКУ
        public static string WriteBKU(byte[] buffer, Label StatusLabel) {
            // Проверка на доступность порта перед записью команды
            if (serialPortBKU == null || !serialPortBKU.IsOpen)
            {
                Console.WriteLine("БКУ: Порт не найден");
                return "Порт не найден";
            }
            else
            {
                // запись команды в порт
                serialPortBKU.Write(buffer, 0, buffer.Length);
                return $"Запись в порт {serialPortBKU.PortName}";
            }

        }

        

    }



}
