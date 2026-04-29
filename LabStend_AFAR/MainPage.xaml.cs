using System.IO.Ports;
using System.Text;
using System.Collections.Generic;

using static LabStend_AFAR.COMport;

namespace LabStend_AFAR
{
    public partial class MainPage : ContentPage
    {
        double eps = 0.001;
        int count = 0;
        Label[] buttons6Att;
        Label[] buttons6Ph;

        Attenuator att;
        Phaser ph;
        LNA lna;

        SerialPort serialPortBKU;
        List<string> availablePorts;

        //
        double[] AttenuationLevels = {0.25, 0.5, 1, 2, 4, 8, 16};

        //
        byte attenuationWord;

        //

        public Command ExitCommand { get; }

        public MainPage()
        {
            // Инициализация
            InitializeComponent();

            // Объявление команды выхода
            ExitCommand = new Command(OnExit);

            buttons6Att = new Label[] {
                AttD1, AttD2, AttD3, AttD4, AttD5, AttD6
            };
            buttons6Ph = new Label[] {
                ButtonPh1, ButtonPh2, ButtonPh3, ButtonPh4, ButtonPh5, ButtonPh6
            };

            att = new Attenuator(buttons6Att);
            ph = new Phaser(buttons6Ph);
            lna = new LNA();

            ConnectToBKU(serialPortBKU, LabelStatus_BKU, StatusLabel, COMportPicker, availablePorts); // Пока тут реализовано жёстко подключение к БКУ по COM5
            // В будущем заменить здесь и в COMport.cs на множество портов
        }

        
        // Функция нажатия кнопки
        /*
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
        */


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
        private void OnClickedOkButton(object? sender, EventArgs e) 
        {
            Button button = (Button)sender;

            if (serialPortBKU == null || !serialPortBKU.IsOpen) {
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

            attenuationWord = 0; // установка значения битовой посылки в исходный 00000000
            byte flag = 1;

            for (int i = 0; i < 7; i++)
            {
                if (attenuationValue / AttenuationLevels[i] >= eps)
                {
                    attenuationWord = (byte)(attenuationWord ^ flag);
                    attenuationValue -= AttenuationLevels[i];
                }
                flag <<= 1;
            }
            Console.WriteLine(attenuationWord);
            Console.WriteLine(Convert.ToString(attenuationWord, 2));



        }



        private void OnClickedRadioButtonRCOM(object? sender, EventArgs e) { 
            
        }

        /// Обработчик выхода из программы
        private async void OnExit()
        {
            // Если есть активное подключение - закрываем его
            //if (isMCconnected && _serialPort != null && _serialPort.IsOpen)
            //{
            //    DisconnectDevice();
            //}

            // Запрашиваем подтверждение выхода
            bool confirm = await DisplayAlertAsync("Выход из программы",
                "Вы уверены, что хотите выйти?", "Да", "Нет");

            if (confirm)
            {
                // Закрываем приложение
                Application.Current.Quit();
            }
        }
    }


    // ------------- КЛАССЫ -------------------------------

    // Класс Аттенюатора
    public class Attenuator 
    {
        private bool[] bitword = new bool[6];
        public Label[] buttons6 = new Label[6]; // подумать над доступом

        public Attenuator(Label[] buttons) {
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

