using System;
using System.Collections.Generic;
using System.Text;

namespace LabStend_AFAR
{
    public class MUAF
    {
        // константы идентификаторов устройсты для распознавания БКУ во время адресации команд
        public const byte bkuID = 0b00001111;
        public const byte lnaID = 0b00000001;
        public const byte attID = 0b00000010;
        public const byte phID = 0b00000011;
        public const byte rcomID = 0b00000100;
    }

    // Класс Малошумящего усилителя (МШУ)
    public class LNA
    {
        public string Name = "МШУ";
        public int No = 0;

        private byte mode;
        public LNA()
        {
            mode = 0;
        }
        public void Set(byte m)
        {
            mode = m;
        }
        public int Get()
        {
            return mode;
        }
        public void SendCommand(byte command, bool mode)
        {
            switch (mode)
            {
                case false: break;
                case true:; break;

            }
        }
    }

    // Класс Аттенюатора
    public class Attenuator
    {
        public string Name = "Аттенюатор";
        public int No = 0;

        public double[] AttenuationLevels = {0.5, 1, 2, 4, 8, 16 };
        private byte bitword = 0b00000000;
        public Label bitWordLabel = new Label(); // подумать над доступом

        public Attenuator(Label bitWordLabel)
        {
            bitWordLabel = bitWordLabel;
            bitword = 0;
            
        }
        public void Set(byte word)
        {
            bitword = word;
        }
        public byte Get()
        {
            return bitword;
        }
    }

    // Класс Фазовращателя
    public class Phaser
    {
        public string Name = "Фазовращатель";
        public int No = 0;

        public double[] PhaseShifts = { 5.6, 11.2, 22.5, 45, 90, 180};
        private byte bitword = 0b00000000;
        public Label[] buttons6 = new Label[6]; // подумать над доступом

        public Phaser(Label bitWordLabel)
        {
            for (int i = 0; i < buttons6.Length; i++)
            {
                bitWordLabel = bitWordLabel;
                bitword = 0;
            }
        }
        public void Set(byte word)
        {
            bitword = word;
        }
        public byte Get()
        {
            return bitword;
        }

    }

    
}


