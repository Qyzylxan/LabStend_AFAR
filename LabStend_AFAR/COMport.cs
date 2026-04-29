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



        // Функция подключения к COM-порту
        public async static void ConnectToBKU(SerialPort port, Label labelStatusBKU, Label statusLabel, Picker COMportPicker, List<string> availablePorts)
        {
            LoadAvailablePorts(statusLabel, COMportPicker, availablePorts);

            // Опционально: автоматически пытаемся найти ESP32
            AutoDetectESP32(statusLabel, COMportPicker, availablePorts, port, labelStatusBKU);

            try
            {
                port = new SerialPort(portNameBKU, 115200);
                port.Open();
                labelStatusBKU.Text = "(подключён)";
                
                return;
                // Пока тут реализовано жёстко подключение к БКУ по COM5
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

        private static void AutoDetectESP32(Label statusLabel, Picker COMportPicker, List<string> availablePorts, SerialPort portBKU, Label labelStatusBKU)
        {
            statusLabel.Text = "Автопоиск ESP32...";

            foreach (var portName in availablePorts)
            {
                try
                {
                    using (var testPort = new SerialPort(portName, 115200))
                    {
                        testPort.ReadTimeout = 500;
                        testPort.WriteTimeout = 500;
                        testPort.Open();

                        // Отправка команду запроса идентификации
                        testPort.WriteLine("IDENTIFY");

                        // Ожидание ответа
                        var response = testPort.ReadLine();

                        if (response.Contains("ESP32") || response.Contains("PE43702"))
                        {
                            // ESP32 найден
                            COMportPicker.SelectedItem = portName;
                            statusLabel.Text = $"ESP32 обнаружен на порту {portName}";
                            statusLabel.TextColor = Colors.Green;

                            portNameBKU = portName;
                        }
                        
                        testPort.Close();
                        
                    }

                }
                catch
                {
                    // Продолжение поиска
                }
            }
            statusLabel.Text = "ESP32 не найден автоматически. Выберите порт вручную.";
            statusLabel.TextColor = Colors.Orange;
            return;

        }
    }



}
