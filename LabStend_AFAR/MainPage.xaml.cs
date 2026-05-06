using System.IO.Ports;
using System.Text;
using System.Collections.Generic;
using static LabStend_AFAR.MUAF;

using static LabStend_AFAR.COMport;

namespace LabStend_AFAR
{
    public partial class MainPage : ContentPage
    {
        double f = 2; // ГГц
        double c = 0.3; // м/с * 10^9 
        double thetaMax = 30; // Макс. угол отклонения луча, град

        double lambda; // Длина волны, м
        double d; // Шаг решётки, м


        double deg = Math.PI / 180;
        double eps = 0.001; // Машинный эпсилон

        Label[] buttons6Ph;

        Attenuator att;
        Phaser ph;
        LNA lna;


        //
        bool writeMode = false;

        //
        byte attenuationWord;

        //

        public Command ExitCommand { get; }

        public MainPage()
        {

            lambda = c / f;
            d = lambda / (1 + Math.Sin(thetaMax*deg));

            // Инициализация
            InitializeComponent();
            Console.WriteLine("Запуск СПО...");

            // Запуск COM-портов
            COMport.Init();

            // Создание команды выхода как объекта команды
            ExitCommand = new Command(OnExit);



            // объявление полей битов (неактуально, упразднить или модифицировать)

            
            buttons6Ph = new Label[] {
                ButtonPh1, ButtonPh2, ButtonPh3, ButtonPh4, ButtonPh5, ButtonPh6
            };

            // Объявление объетов классов Устройств
            att = new Attenuator(LabelAttBitWord);
            ph = new Phaser(buttons6Ph);
            lna = new LNA();

            // Списки портов
            Picker[] COMportPickers = { COMportPickerBKU, COMportPickerPI };

            Thread.Sleep(1000);
            COMport.Init();

            LoadAvailablePorts(StatusLabel, COMportPickers, availablePorts);
            AutoConnectToBKU(serialPortBKU, LabelStatus_BKU, StatusLabel, COMportPickerBKU, availablePorts); 
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

        // Функция нажатия кнопки ручного подключения к БКУ
        private void OnClickedBKUConnect(object? sender, EventArgs e)
        {
            Button button = (Button)sender;
            ConnectToCOM(serialPortBKU, LabelStatus_BKU, StatusLabel, COMportPickerBKU.SelectedIndex, 'e');

        }
        // Функция нажатия кнопки ручного подключения к ПИ
        private void OnClickedPIConnect(object? sender, EventArgs e)
        {
            Button button = (Button)sender;
            ConnectToCOM(serialPortPI, LabelStatus_PI, StatusLabel, COMportPickerPI.SelectedIndex, 'f');

        }



        // Функция нажатия кнопки
        
        private void OnClickedBitPh(object? sender, EventArgs e)
        {
            Button button = (Button)sender;
            int index = Array.IndexOf(ph.buttons6, button); // рассмотреть целесообразность Array.IndexOf()

            ph.Set(index);
            if (ph.Get(index) == true) button.Text = "1";
            else button.Text = "0";

        }


        // Кнопка Ок угла отклонения луча АФАР (БУАФ)
        private void OnClickedOkBUAF(object? sender, EventArgs e) {
            Button button = (Button)sender;

            double thetaShift = 0;
            if (double.TryParse(EntryAngle.Text, out double angle))
            {
                if (angle < -thetaMax)
                {
                    angle = -thetaMax;
                    EntryAngle.Text = $"{thetaMax}";
                }
                if (angle > thetaMax)
                {
                    angle = thetaMax;
                    EntryAngle.Text = $"{thetaMax}";
                }
                else {
                    thetaShift = PhaseDelay(angle);
                }

            }
            else
            {

            }

        }
        double PhaseDelay(double angle) {
            return 2*Math.PI*d*Math.Sin(angle*deg)/lambda;
        }


        //Радиокнопки выбора режима работы МШУ
        private void OnClickedRadioButtonLNA(object? sender, EventArgs e) {
            RadioButton radioButton = (RadioButton)sender;

            OkHandlerLNA(radioButton);
        }


        private void OkHandlerLNA(RadioButton radioButton) {
            byte code;
            switch (radioButton.Value)
            {
                case "1": code = 1; break;
                case "2": code = 2; break;
                case "0": code = 0; break;
                default: code = 0; break;
            }
            lna.Set(code);
            lna.SendCommand(code, writeMode);
        }




        // Кнопка Ок аттенюатора
        private void OnClickedOkButtonAtt(object? sender, EventArgs e) 
        {
            Button button = (Button)sender;

            OkHandlerAtt(EntryAmp);
            
        }
        private void OnClickedOkButtonAtt2(object? sender, EventArgs e)
        {
            Button button = (Button)sender;

            if (serialPortBKU == null || !serialPortBKU.IsOpen)
            {
                Console.WriteLine("Порт не найден");
                return;
            }
            OkHandlerAtt(EntryAmp2);

        }



        private void OkHandlerAtt(Entry entryAmp) {
            string errorMessageBlockName = "Аттенюатор 1";
            if (double.TryParse(EntryAmp.Text, out double attenuationValue))
            {
                if (attenuationValue < 0)
                {
                    attenuationValue = 0;
                    entryAmp.Text = $"{attenuationValue}";
                }
                if (attenuationValue > 31.5)
                {
                    attenuationValue = 31.5;
                    entryAmp.Text = $"{attenuationValue}";
                }
            }
            else
            {
                StatusLabel.Text += $"\n{errorMessageBlockName}: Текст с поля ввода не распознан. Введите число от 0 до 31.5";
                // Если не распарсил текст с ввода
            }

            attenuationWord = 0; // установка значения битовой посылки в исходный 00000000
            byte flag = 1<<6;

            for (int i = 5; i >= 0; i--)
            {
                if (attenuationValue / att.AttenuationLevels[i] > (1 - eps))
                {
                    attenuationWord = (byte)(attenuationWord ^ flag);
                    attenuationValue -= att.AttenuationLevels[i];
                }
                flag >>= 1;
            }

            // Вывод команды
            Console.WriteLine(attenuationWord);
            Console.WriteLine(Convert.ToString(attenuationWord, 2));

            LabelAttBitWord.Text = Convert.ToString(attenuationWord, 2).PadLeft(8, '0');


            // Проверка на доступность порта перед записью команды
            if (serialPortBKU == null || !serialPortBKU.IsOpen)
            {
                StatusLabel.Text += "\nПорт не найден";
                Console.WriteLine("Порт не найден");
                return;
            }

        }


        public void OnToggledMode(object? sender, EventArgs a) {
            Switch switcher = (Switch)sender;
            writeMode = switcher.IsToggled;
        }



        // Обработчик кнопки выбора режима коммутатора
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

        private void PI_connect_Clicked(System.Object sender, System.EventArgs e)
        {

        }
    }


    // ------------- КЛАССЫ -------------------------------

    
    // Добавить класс/функции в Main битовых посылок
}

