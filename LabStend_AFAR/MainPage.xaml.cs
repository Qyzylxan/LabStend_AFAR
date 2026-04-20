using System.IO.Ports;
using System.Text;
using static LabStend_AFAR.COMport;

namespace LabStend_AFAR
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        Button[] buttons6Att;
        Button[] buttons6Ph;

        Attenuator att;
        Phaser ph;
        LNA lna;

        SerialPort serialPort1;

        //
        double[] AttenuationLevels = {0.5, 1, 2, 4, 8, 16};

        //
        bool[] attenuationWord = new bool[7];
        
        //

        public MainPage()
        {
            InitializeComponent();

            buttons6Att = new Button[] {
                ButtonAtt1, ButtonAtt2, ButtonAtt3, ButtonAtt4, ButtonAtt5, ButtonAtt6
            };
            buttons6Ph = new Button[] {
                ButtonPh1, ButtonPh2, ButtonPh3, ButtonPh4, ButtonPh5, ButtonPh6
            };

            att = new Attenuator(buttons6Att);
            ph = new Phaser(buttons6Ph);
            lna = new LNA();

            LabelStatus_BKU.Text = Connect(serialPort1, "COM5"); // Пока тут реализовано жёстко подключение к БКУ по COM5
            // В будущем заменить здесь и в COMport.cs на множество портов
        }

        
        // Функция нажатия кнопки
        private void OnCounterClicked(object? sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Нажато {count} разок";
            else if ((count > 1) && (count < 5))
                CounterBtn.Text = $"Нажато {count} раза";
            else
                CounterBtn.Text = $"Нажато {count} раз";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }



        // Функция нажатия кнопки
        private void OnClickedBit(object? sender, EventArgs e) 
        {            
            Button button = (Button)sender;
            int index = Array.IndexOf(att.buttons6, button); // рассмотреть целесообразность Array.IndexOf()

            att.Set(index);
            if (att.Get(index) == true) button.Text = "1";
            else button.Text = "0";

        }
        private void OnClickedBitPh(object? sender, EventArgs e)
        {
            Button button = (Button)sender;
            int index = Array.IndexOf(ph.buttons6, button); // рассмотреть целесообразность Array.IndexOf()

            att.Set(index);
            if (att.Get(index) == true) button.Text = "1";
            else button.Text = "0";

        }
        private void OnClickedRadioButtonLNA(object? sender, EventArgs e) {
            RadioButton radioButton = (RadioButton)sender;
            int code;
            switch (radioButton.Value) {
                case "1": code = 1; break;
                case "2": code = 2; break;
                case "0": code = 0; break;
                default: code = 0; break;
            }
                lna.Set(code);
        }
        private void OnClickedOkButton(object? sender, EventArgs e) {
            Button button = (Button)sender;

            if (serialPort1 == null || !serialPort1.IsOpen) {
                Console.WriteLine("Порт не найден");
                return;
            }

            if (double.TryParse(EntryAmp.Text, out double attenuationValue)) {
                if (attenuationValue < 0)
                {
                    attenuationValue = 0;
                    EntryAmp.Text = "0";
                }
                if (attenuationValue > 31.5)
                {
                    attenuationValue = 31.5;
                    EntryAmp.Text = "31.5";
                }
            }

            for (int i = 0; i < attenuationWord.Length; i++) {
                attenuationWord[i] = (attenuationValue%AttenuationLevels[i] == 0);
                //attenuationValue - AttenuationLevels[i];
            }
            
            

            
        }


    }

    // Класс Аттенюатора
    public class Attenuator 
    {
        private bool[] bitword = new bool[6];
        public Button[] buttons6 = new Button[6]; // подумать над доступом

        public Attenuator(Button[] buttons) {
            for (int i = 0; i<buttons6.Length; i++) {
                buttons6[i] = buttons[i];
                bitword[i] = false; 
            }
        }
        public void Set(int n) {
            bitword[n] = !bitword[n];
        }
        public bool Get(int n) {
            return bitword[n];
        }

    }
    // Класс Фазовращателя
    public class Phaser
    {
        private bool[] bitword = new bool[6];
        public Button[] buttons6 = new Button[6]; // подумать над доступом

        public Phaser(Button[] buttons)
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
    public class LNA {
        private int mode;
        public LNA() {
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
    // Добавить класс/функции в Main битовых посылок
}

