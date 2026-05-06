using System;
using System.Collections.Generic;
using System.Text;

namespace LabStend_AFAR
{
    public class MUAF
    {   

    }

    // Класс Аттенюатора
    public class Attenuator
    {
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
        public double[] PhaseShifts = { 5.6, 11.2, 22.5, 45, 90, 180};
        private bool[] bitword = new bool[6];
        public Label[] buttons6 = new Label[6]; // подумать над доступом

        public Phaser(Label[] buttons)
        {
            for (int i = 0; i < buttons6.Length; i++)
            {
                buttons6[i] = buttons[i];
                bitword[i] = false;
            }
        }
        public void Set(int n)
        {
            bitword[n] = !bitword[n];
        }
        public bool Get(int n)
        {
            return bitword[n];
        }

    }

    // Класс Малошумящего усилителя (МШУ)
    public class LNA
    {
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
        public void SendCommand(byte command, bool mode) {
            switch (mode) {
                case false: break;
                case true: ; break;
                
            }
        }
    }
}


