using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;

namespace LabStend_AFAR
{
    public class COMport
    {
        bool isBKUconnected = false;
        static string portNameBKU = "";
        static Int32 baudBKU = 115200;

        public static SerialPort serialPortBKU;
        public static List<string> availablePorts;
        public static List<string> availablePortNames;

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
        public async static void ConnectToBKU(SerialPort port, Label labelStatusBKU, Label statusLabel,
                                            int pickedPortIndex)
        {
            
            string pickedPort = availablePortNames.ElementAt(pickedPortIndex);
            
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
                statusLabel.Text = $"Ручное подключение к порту {pickedPort} не удалось.";
                statusLabel.TextColor = Colors.Red;
            }
            labelStatusBKU.Text = "(не подключён)";
            
            return;
        }

        // Функция автоподключения к COM-порту
        public async static void AutoConnectToBKU(SerialPort port, Label labelStatusBKU, Label statusLabel, 
                                                    Picker COMportPicker, List<string> availablePorts)
        {
            LoadAvailablePorts(statusLabel, COMportPicker, availablePorts);
            Thread.Sleep(500);

            // Автоматический поиск БКУ (ESP32 с загруженным СПО БКУ)
            AutoDetectBKU(statusLabel, COMportPicker, availablePorts, port, labelStatusBKU);
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
        private static async void LoadAvailablePorts(Label statusLabel, Picker COMportPicker, List<string> availablePorts)
        {
            try
            {
                
                //RefreshButton.IsEnabled = false;
                //ConnectionActivity.IsRunning = true;
                //ConnectionActivity.IsVisible = true;
                
                statusLabel.Text = "Поиск COM-порта...";
                statusLabel.TextColor = Colors.Orange;

                // Запускаем сканирование в отдельном потоке
                var ports = await Task.Run(() => SerialPort.GetPortNames());

                
                availablePorts = ports.OrderBy(p => p).ToList();

                availablePortNames = availablePorts;
                COMportPicker.ItemsSource = availablePorts;

                if (availablePorts.Count == 0)
                {
                    COMportPicker.Title = "Нет COM-портов";
                    statusLabel.Text = "Нет достуных COM-портов.";
                    statusLabel.TextColor = Colors.Red;
                }
                else
                {
                    COMportPicker.Title = "Список COM-портов";
                    statusLabel.Text = "Найден COM-порт.";
                    statusLabel.TextColor = Colors.Green;

                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Ошибка поиска COM-порта";
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
            statusLabel.Text = "Автопоиск БКУ...";
            var testPort = new SerialPort("COM1", baudBKU);
            foreach (var portName in availablePorts)
            {
                try
                {
                    //using (var testPort = new SerialPort(portName, baudBKU))
                    testPort.PortName = portName;
                    testPort.BaudRate = baudBKU;
                    
                    {
                        testPort.ReadTimeout = 500;
                        testPort.WriteTimeout = 500;
                        testPort.Open();

                        // Отправка команду запроса идентификации
                        testPort.WriteLine("BKU?\n");

                        // Ожидание ответа
                        var response = testPort.ReadLine();

                        if (response.Contains("BKU!") || response.Contains("PE43702"))
                        {
                            // ESP32 найден
                            COMportPicker.SelectedItem = portName;
                            statusLabel.Text = $"БКУ обнаружен на порту {portName}";
                            statusLabel.TextColor = Colors.Green;

                            portNameBKU = portName;
                            testPort.Close();
                            break;
                        }
                        
                        testPort.Close();
                        
                    }

                }
                catch
                {
                    statusLabel.Text = "Ошибка автопоиска БКУ";
                    statusLabel.TextColor = Colors.Red;
                    
                }
            }
            statusLabel.Text = "БКУ не найден автоматически. Выберите порт вручную.";
            statusLabel.TextColor = Colors.Orange;
            return;

        }
    }



}
