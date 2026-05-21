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
            pinConfig = 'w';
            writeTimeout = 500;
            readTimeout = 500;
            //OpenPort();
        }
        
        // Запись команд в ПИ
        public void WritePI(byte[] buffer, Label StatusLabel)
        {
            // Структура битовой посылки buffer:    0000 0000 , 0000 0000 , 0000 0000
            //                                      command     addr        id

            // Структура посылки в ПИ:
            //  0   0   0   0       0   0   0   0
            //  s3  s2  s1  s0      Dg Dat Dph clk

            // Структура выводов в Мультиплексорах
            //  Мультиплексор №1 (МШУ)
            //  15      14      13      12      11      10      9       8       7       6       5       4       3       2       1       0
            //                                                  LE_G4   DAT_G4  LE_G3   DAT_G3  LE_G2   DAT_G2  LE_G1   DAT_G1  выкл    выкл

            //  Мультиплексор №2 (Аттенюатор)
            //  15      14      13      12      11      10      9       8       7       6       5       4       3       2       1       0
            //                                                  LE_At4  DAT_At4 LE_At3  DAT_At3 LE_At2  DAT_At2 LE_At1  DAT_At1 выкл    выкл

            //  Мультиплексор №3 (Фазовращатель)
            //  15      14      13      12      11      10      9       8       7       6       5       4       3       2       1       0
            //                                                  LE_Ph4  DAT_Ph4 LE_Ph3  DAT_Ph3 LE_Ph2  DAT_Ph2 LE_Ph1  DAT_Ph1 выкл    выкл

            int delay = 100;         // задержка в миллисекундах

            if (!port.IsOpen){
                StatusLabel.Text += OpenPort().ToString();
                Thread.Sleep(100);
            }

            FT_STATUS writeStatus;
            // Маршрутизация
            // выбор типа устройства из первого байта команды
            int selectedDeviceBit = 0; // Номер бита, приписанного к устройству
            switch (buffer[0]) {
                case MUAF.lnaID: selectedDeviceBit = 3; break;
                case MUAF.attID: selectedDeviceBit = 2; break;
                case MUAF.phID: selectedDeviceBit = 1; break;
                default: break;
            }

            // выбор устройства по номеру из второго байта команды
            byte deviceNo = 0b00000000;
            deviceNo = buffer[1];
            deviceNo++;             // начало отсчёта не с 0, а с 1
            deviceNo <<= 4;         // форматирование номера устройства под структуру посылки с ПИ
            deviceNo <<= 1;         // смещение на 1 (младшие биты резервированы под "выкл")

            // Массив буфера данных, нужен для работы с функцией FT_Write();
            byte[] dataBuffer = { 0x00 };
            // Сброс в "0" всех битов перед началом отправки данных
            dataBuffer[0] = 0x00;
            WriteCommand(dataBuffer);

            //dataBuffer[0] = deviceNo;
            byte commandBit;        // Приведённый байт команды

            dataBuffer[0] ^= 0b00000000; // Синхроимпульс (на первом бите) в положении "0"

            for (int i = 0; i < 8; i++)
            {

                commandBit = buffer[2];
                commandBit = (byte)(((commandBit >> i) & 0x00000001) << selectedDeviceBit);
                dataBuffer[0] = (byte)(commandBit | deviceNo);

                WriteCommand(dataBuffer);
                Thread.Sleep(delay);
                dataBuffer[0] ^= 0b00000001; // Синхроимпульс (на первом бите) в положении "1"
                WriteCommand(dataBuffer);
                Thread.Sleep(delay);
                dataBuffer[0] ^= 0b00000000; // Синхроимпульс (на первом бите) в положении "0"
            }
            // Триггер LE после отправки команды
            dataBuffer[0] = (byte)(deviceNo | 0b00010000); // Переключение мультиплексора на выводы LE
            
            dataBuffer[0] ^= (byte)(0b00000001 << selectedDeviceBit);
            WriteCommand(dataBuffer);
            Thread.Sleep(delay);

            dataBuffer[0] ^= (byte)(0b00000001 << selectedDeviceBit);
            WriteCommand(dataBuffer);
            

            // Сброс в "0" всех битов перед завершением отправки данных
            dataBuffer[0] = 0x00;
            writeStatus = WriteCommand(dataBuffer);
            if (writeStatus != FT_STATUS.FT_OK) {
                Console.WriteLine("\nOK");
            }

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
