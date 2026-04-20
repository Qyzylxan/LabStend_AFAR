using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;

namespace LabStend_AFAR
{
    public class COMport
    {
        // Функция подключения к COM-порту
        public static string Connect(SerialPort port, string portName)
        {
            try
            {
                port = new SerialPort(portName, 115200);
                port.Open();
                return "(подключён)";
                // Пока тут реализовано жёстко подключение к БКУ по COM5
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка подключения COM-порта: {e.Message}");
            }

            return "(не подключён)";
        }

    }
}
