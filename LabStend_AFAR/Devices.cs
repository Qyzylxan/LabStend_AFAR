using System;
using System.Collections.Generic;
using System.Text;

namespace LabStend_AFAR
{
    public class Devices
    {
        // Класс Аттенюатора
        public class Attenuator
        {
            private bool[] bitword = new bool[6];
            public Label[] buttons6 = new Label[6]; // подумать над доступом

            public Attenuator(Label[] buttons)
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
        // Класс Фазовращателя
        public class Phaser
        {
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
            private int mode;
            public LNA()
            {
                mode = 0;
            }
            public void Set(int m)
            {
                mode = m;
            }
            public int Get()
            {
                return mode;
            }
        }
    }



}
