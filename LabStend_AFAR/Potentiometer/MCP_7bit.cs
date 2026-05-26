using System;
using System.Collections.Generic;
using System.Text;

namespace LabStend_AFAR.Potentiometer
{

    public class MCP_7bit
    {
        // Структура команд потенциометра
        // Инкремент/Декремент - 8 бит:


        // Запись/Чтение данных - 16 бит:
        //  Байт команд                     |   Байт данных
        //  15  14  13  12  11  10  9   8   |   7   6   5   4   3   2   1   0
        //  A   A   A   A   C   C   D   D   |   D   D   D   D   D   D   D   D

        //  C1  C0
        //  0   0   Запись
        //  0   1   инкремент
        //  1   0   декремент
        //  1   1   Чтение
        public static readonly byte writeData = 0b00000000;
        public static readonly byte inc = 0b00000100;
        public static readonly byte dec = 0b00001000;
        public static readonly byte readData = 0b00001100;

        // Адресные биты
        //              MOSI    |   MISO
        //  Пот№1       0000    |   1111
        //  Пот№2       0001    |   1111
        public static readonly byte pot1_MOSI = 0b00000000;
        public static readonly byte pot2_MOSI = 0b00010000;


        public static int GetMode(char mode) {
            if (mode == 'w') {
                return ((int)writeData<<8);
            }
            return 0;
        }
    }
}
