using FTD2XX_NET;
using System;
using System.Collections.Generic;
using System.Text;
using static FTD2XX_NET.FTDI;
using static LabStend_AFAR.MUAF;

namespace LabStend_AFAR.D2XX
{
    public class FT245
    {
        FTDI port;
        uint portIndex;
        uint baudRate;
        char pinConfig;
        uint writeTimeout;
        uint readTimeout;
        
        // Параметры COM-порта 
        string comPortName = "COM6";

        // Конструктор объекта
        public FT245() {
            port = new FTDI();
            portIndex = 0;
            baudRate = 9600;
            pinConfig = ' ';
            writeTimeout = 500;
            readTimeout = 500;

        }
        
        // Запись команд в ПИ
        public void WritePI(byte[] buffer, Label StatusLabel)
        {
            // Структура битовой посылки: 0000 0000 , 0000 0000 , 0000 0000
            //                              command   addr        id

            // Структура посылки в ПИ:
            //  0   0   0   0       0   0   0   0
            //  s3  s2  s1  s0      sig clk Dph Dat

            // Структура выводов в Мультиплексоре
            //  15  14  13  12  11  10  9   8   7   6   5   4   3   2   1   0
            //  LE4 LE3 LE2 LE1 LE4 LE3 LE2 LE1 LE4 LE3 LE2 LE1 Da4 Da3 Da2 Da1
            //  |   Ph      |   |   At      |   |           LNA             |

            int delay = 200; // задержка в миллисекундах

            OpenPort();
            Thread.Sleep(100);

            // Маршрутизация
            // выбор типа устройства
            byte sn = 0b00000000; // Биты данных для переключателей (задействовать 2-4 биты, считая от младшего)
            switch (buffer[0]) {
                case MUAF.lnaID: sn |= 0b00000100; break;
                case MUAF.attID: sn |= 0b00001000; break;
                case MUAF.phID: sn |= 0b00001100; break;
                default: break;
            }
            // выбор устройства по номеру
            sn |= buffer[1];

            // Массив буфера данных для работы с FT_Write();
            byte[] dataBuffer = { 0x00 };
            // Сброс в "0" всех битов перед началом отправки данных
            dataBuffer[0] = 0x00;
            WriteCommand(dataBuffer);

            byte commandBit;
            if (buffer[0] != MUAF.lnaID) {
                for (int i = 0; i < 8; i++)
                {
                    commandBit = buffer[2];
                    dataBuffer[0] = (byte)((commandBit >> i) & 0x00000001);
                    dataBuffer[0] <<= ((sn & 0b00000100) >> 2); // Dph или Dat, при этом на LNA отдельной шины не выделено!

                    Thread.Sleep(delay);
                    if (WriteCommand(dataBuffer) == FT_STATUS.FT_OK) Console.WriteLine("\tОК");
                    Thread.Sleep(delay);
                }
            }
            if (buffer[0] == MUAF.lnaID)
            {
                for (int i = 0; i < 8; i++)
                {
                    dataBuffer[0] = sn;
                    dataBuffer[0] <<= 1;

                    commandBit = buffer[2];
                    dataBuffer[0] |= (byte)((commandBit >> i) & 0x00000001);
                    dataBuffer[0] <<= 4; // Dlna

                    Thread.Sleep(delay);
                    if (WriteCommand(dataBuffer) == FT_STATUS.FT_OK) Console.WriteLine("\tОК");
                    Thread.Sleep(delay);
                }
            }
            else { }

                // Триггер LE после отправки команды
                dataBuffer[0] = sn;
            dataBuffer[0] <<= 4;
            dataBuffer[0] ^= 0b00001000;
            WriteCommand(dataBuffer);
            dataBuffer[0] ^= 0b00001000;
            WriteCommand(dataBuffer);

            // Сброс в "0" всех битов перед завершением отправки данных
            dataBuffer[0] = 0x00;
            WriteCommand(dataBuffer);

        port.Close();
        }
        // --------------------------------- Служебные функции-обёртки
        public FT_STATUS OpenPort()
        {
            FT_STATUS status;

            // Установка скорости
            port.SetBaudRate(baudRate);
            port.SetTimeouts(readTimeout, writeTimeout);

            // Открытие порта Преобразователя (по индексу 0)
            status = port.OpenByIndex(portIndex);
            if (status != FTDI.FT_STATUS.FT_OK)
            {
                Console.WriteLine("Не удалось открыть устройство");
                return status;
            }

            // ucMask требуемое значение для битовой маски режима.
            // Это устанавливает, какие биты работают как входы, какие как выходы.
            // Значение бита 0 устанавливает соответствующий вывод как вход,
            // а 1 устанавливает соответствующий вывод как выход. 
            byte mask = 0xFF;   // Все выводы как выходы
            switch (pinConfig)
            {
                case 'w': mask = 0xFF; break;
                case 'r': mask = 0x00; break;
                default: break;
            }

            // Включение асинхронного режима Bit Bang
            status = port.SetBitMode(mask, FTDI.FT_BIT_MODES.FT_BIT_MODE_ASYNC_BITBANG);

            if (status != FTDI.FT_STATUS.FT_OK)
            {
                Console.WriteLine("Не удалось включить Bit Bang режим");
                port.Close();
                return status;

            }
            return status;
        }

        FT_STATUS WriteCommand(byte[] commandBytes)
        {
            
            FT_STATUS status = FT_STATUS.FT_OK;

            uint bytesWritten = 0;
            try
            {
                if (!port.IsOpen)
                {
                    Console.WriteLine($" - порт устройства {port.GetCOMPort} закрыт. переоткрытие...");
                    port.OpenByIndex(0);
                }

                status = port.Write(commandBytes, 1, ref bytesWritten);
                if (status != FT_STATUS.FT_OK)
                {
                    Console.WriteLine("Запись не удалась");
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine(" - Запись: данные отсутствуют");
                status = FT_STATUS.FT_FAILED_TO_WRITE_DEVICE;
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e.Message);
                status = FT_STATUS.FT_DEVICE_NOT_FOUND;
                //ChangePortName(port);
            }
            return status;
        }


    }
}
