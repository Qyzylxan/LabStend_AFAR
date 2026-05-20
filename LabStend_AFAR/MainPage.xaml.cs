using System.IO.Ports;
using System.Text;
using System.Collections.Generic;
using static LabStend_AFAR.MUAF;

using static LabStend_AFAR.BKU;
using LabStend_AFAR.D2XX;

namespace LabStend_AFAR
{
    public partial class MainPage : ContentPage
    {
        double f = 2;       // ГГц
        double c = 0.3;     // м/с * 10^9 
        double thetaMax = 30; // Макс. угол отклонения луча, град

        double lambda; // Длина волны, м
        double d;   // Шаг решётки, м


        double deg = Math.PI / 180;
        double eps = 0.001; // Машинный эпсилон

        // Объявление объектов устройств
        FT245 PI;
        // нужно по 4 устройства 
        Attenuator att;
        Phaser ph;
        LNA lna;

        // Флаг выбора режима записи команд (БКУ <-> ПИ)
        bool writeMode = false;


        public Command ExitCommand { get; }

        public MainPage()
        {

            lambda = c / f;
            d = lambda / (1 + Math.Sin(thetaMax*deg));

            // Инициализация
            InitializeComponent();
            Console.WriteLine("Запуск СПО...");

            // Запуск COM-портов
            BKU.Init();

            // Создание команды выхода как объекта команды
            ExitCommand = new Command(OnExit);



            // Объявление объетов классов Устройств
            PI = new FT245();

            att = new Attenuator(LabelAttBitWord);
            ph = new Phaser(LabelPhBitWord);
            lna = new LNA();

            // Списки портов
            Picker[] COMportPickers = { COMportPickerBKU, COMportPickerPI };

            Thread.Sleep(1000);
            // BKU.Init();

            LoadAvailablePorts(StatusLabel, COMportPickers, availablePorts);
            AutoConnectToBKU(serialPortBKU, LabelStatus_BKU, StatusLabel, COMportPickerBKU, availablePorts); 
            // В будущем заменить здесь и в COMport.cs на множество портов

        }


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
            PI.OpenPort();

        }




        // Кнопка Ок угла отклонения луча АФАР (БУАФ)
        private void OnClickedOkBUAF(object? sender, EventArgs e) {
            Button button = (Button)sender;

            string errorMessageBlockName = "БУАФ";

            double phaseShift = 0;  // фазовый сдвиг между соседними элементами АФАР
            // Парсинг введённых данных
            if (double.TryParse(EntryAngle.Text, out double angle))
            {
                // В пределах максимального угла отклонения?
                if (angle < -thetaMax)
                {
                    angle = -thetaMax;
                    EntryAngle.Text = $"{-thetaMax}";
                }
                else if (angle > thetaMax)
                {
                    angle = thetaMax;
                    EntryAngle.Text = $"{thetaMax}";
                }
                else {
                    phaseShift = PhaseDelay(angle);
                }
            }
            else
            {
                // Если не распарсил текст с ввода
                WriteToStatusLabel($"Текст с поля ввода не распознан. Введите число от {-thetaMax} до {thetaMax}", errorMessageBlockName);
                return;
            }

            byte id = phID;

            // Установка значений сдвигов фазы на каждый ФВ -------------------------------------
            if (phaseShift >= 0)
            {
                PhaseWrite(0, EntryPh, LabelPhBitWord, id, 0, "Фазовращатель 1");
                PhaseWrite(phaseShift, EntryPh2, LabelPhBitWord2, id, 1, "Фазовращатель 2");
                PhaseWrite(2 * phaseShift, EntryPh3, LabelPhBitWord3, id, 2, "Фазовращатель 3");
                PhaseWrite(3 * phaseShift, EntryPh4, LabelPhBitWord4, id, 3, "Фазовращатель 4");
            }
            else {
                PhaseWrite(-3 * phaseShift, EntryPh, LabelPhBitWord, id, 3, "Фазовращатель 1");
                PhaseWrite(-2 * phaseShift, EntryPh2, LabelPhBitWord2, id, 2, "Фазовращатель 2");
                PhaseWrite(-phaseShift, EntryPh3, LabelPhBitWord3, id, 1, "Фазовращатель 3");
                PhaseWrite(0, EntryPh4, LabelPhBitWord4, id, 0, "Фазовращатель 4");
            }
        }

        // Функция расчёта угла отклонения луча АФАР
        double PhaseDelay(double angle) {
            return 360*d*Math.Sin(angle*deg)/lambda; // Результат возвращается в градусах
        }

        //-------------------------------------

        // Радиокнопки выбора режима работы МШУ
        private void OnClickedRadioButtonLNA(object? sender, EventArgs e) {
            RadioButton radioButton = (RadioButton)sender;
            if (radioButton.IsChecked)  { 
                OkHandlerLNA(radioButton.GroupName, radioButton.Value); 
            }
        }

        // Обработчик выбора радиокнопок МШУ
        private void OkHandlerLNA(string groupName, object Value) {
            // Структура битовой посылки: 0000 0000 , 0000 0000 , 0000 0000
            //                            command     addr        id

            // Определение битов адреса
            byte addr;
            switch (groupName) {
                case "Gain": addr = 0; break;
                case "Gain2": addr = 1; break;
                case "Gain3": addr = 2; break;
                case "Gain4": addr = 3; break;
                    default: addr = 0; break;
            }
            
            byte id = lnaID;

            // Определение битов команды
            byte lnaMode;
            switch (Value)
            {
                case "1": lnaMode = 1; break;
                case "2": lnaMode = 2; break;
                case "0": lnaMode = 0; break;
                default: lnaMode = 0; break;
            }
            StatusLabel.Text += $"\nРежим МШУ: { lnaMode}";

            //lna.Set(code);
            //lna.SendCommand(code, writeMode);

            SendCommand(id, addr, lnaMode);
        }

        //----------------------------

        // Обработчик нажатия кнопки Ок Аттенюатора
        private void OnClickedOkButtonAtt(object? sender, EventArgs e) 
        {
            Button button = (Button)sender;

            // Структура битовой посылки: 0000 0000 , 0000 0000 , 0000 0000
            //                            command     addr        id
            // Определение битов адреса; соответствие поля ввода блоку аттенюатора
            string errorMessageBlockName = "Аттенюатор 1";
            byte addr = 0;
            Entry entry = EntryAmp;
            Label labelBit = LabelAttBitWord;

            if (button == OkButtonAtt) { }
            else if (button == OkButtonAtt2) { addr = 1; entry = EntryAmp2; errorMessageBlockName = "Аттенюатор 2"; labelBit = LabelAttBitWord2; }
            else if (button == OkButtonAtt3) { addr = 2; entry = EntryAmp3; errorMessageBlockName = "Аттенюатор 3"; labelBit = LabelAttBitWord3; }
            else if (button == OkButtonAtt4) { addr = 3; entry = EntryAmp4; errorMessageBlockName = "Аттенюатор 4"; labelBit = LabelAttBitWord4; }

            byte id = attID;

            // Парсинг вводимого текста
            if (!double.TryParse(entry.Text, out double attenuationValue))
            {
                // Если не распарсил текст с ввода
                WriteToStatusLabel("Текст с поля ввода не распознан. Введите число от 0 до 31,5", errorMessageBlockName);
                return;
            }

            if (attenuationValue < 0)
            {
                attenuationValue = 0;
                entry.Text = $"{attenuationValue}";
            }
            if (attenuationValue > 31.5)
            {
                attenuationValue = 31.5;
                entry.Text = $"{attenuationValue}";
            }

            byte attenuationWord = 0; // установка значения битовой посылки в исходный 00000000
            double value = attenuationValue;
            byte flag = 1 << 6;

            // Цикл перевода полученного значения в битовую посылку
            for (int i = 5; i >= 0; i--)
            {
                if (value / att.AttenuationLevels[i] > (1 - eps))
                {
                    attenuationWord = (byte)(attenuationWord ^ flag);
                    value -= att.AttenuationLevels[i];
                }
                flag >>= 1;
            }
            
            // Проверка на кратность шагу
            if (value > eps)
            {
                WriteToStatusLabel("Значение ослабления не кратно шагу 0,5 дБ. Округление...", errorMessageBlockName);
                entry.Text = $"{attenuationValue - value}";
            }

            // Вывод команды в поля
            Console.WriteLine(attenuationWord);
            Console.WriteLine(Convert.ToString(attenuationWord, 2));

            labelBit.Text = Convert.ToString(attenuationWord, 2).PadLeft(8, '0');

            SendCommand(id, addr, attenuationWord);

        }

        //----------------------------------

        // Кнопка ОК Фазовращателя
        private void OnClickedOkButtonPh(object? sender, EventArgs e)
        {
            Button button = (Button)sender;

            // Структура битовой посылки: 0000 0000 , 0000 0000 , 0000 0000
            //                              command   addr        id
            // Определение битов адреса; соответствие поля ввода блоку фазовращателя
            string errorMessageBlockName = "Фазовращатель 1";
            byte addr = 0;
            Entry entry = EntryPh;
            Label labelBit = LabelPhBitWord;

            if (button == OkButtonPh) { }
            else if (button == OkButtonPh2) { addr = 1; entry = EntryPh2; errorMessageBlockName = "Фазовращатель 2"; labelBit = LabelPhBitWord2; }
            else if (button == OkButtonPh3) { addr = 2; entry = EntryPh3; errorMessageBlockName = "Фазовращатель 3"; labelBit = LabelPhBitWord3; }
            else if (button == OkButtonPh4) { addr = 3; entry = EntryPh4; errorMessageBlockName = "Фазовращатель 4"; labelBit = LabelPhBitWord4; }
            ;
            byte id = phID;

            // Парсинг вводимого текста
            if (!double.TryParse(entry.Text, out double phaseshiftValue))
            {
                // Если не распарсил текст с ввода
                WriteToStatusLabel("Текст с поля ввода не распознан. Введите число от 0 до 354,4", errorMessageBlockName);
                return;
            }
            PhaseWrite(phaseshiftValue, entry, labelBit, id, addr, errorMessageBlockName);
        }

        // Функция обработки введённого значения сдвига фазы
        private void PhaseWrite(double phaseshiftValue, Entry entry, Label labelBit, byte id, byte addr, string errorMessageBlockName) {
            if (phaseshiftValue < 0)
            {
                phaseshiftValue = 0;
                entry.Text = $"{phaseshiftValue}";
            }
            if (phaseshiftValue > 354.4)
            {
                phaseshiftValue = 354.4;
                entry.Text = $"{phaseshiftValue}";
            }

            byte phaseshiftWord = 0; // установка значения битовой посылки в исходный 00000000
            double value = phaseshiftValue;
            byte flag = 1 << 6;
            // Цикл перевода полученного значения в битовую посылку
            for (int i = 5; i >= 0; i--)
            {
                if (value / ph.PhaseShifts[i] > (1 - eps))
                {
                    phaseshiftWord = (byte)(phaseshiftWord ^ flag);
                    value -= ph.PhaseShifts[i];
                }
                flag >>= 1;
            }

            // Проверка на кратность шагу
            if (value > eps)
            {
                WriteToStatusLabel("Значение сдвига фазы не кратно шагу 5,6 град. Округление...", errorMessageBlockName);
                entry.Text = $"{phaseshiftValue - value}";
            }

            // Вывод команды в поля
            Console.WriteLine(phaseshiftWord);
            Console.WriteLine(Convert.ToString(phaseshiftWord, 2));

            labelBit.Text = Convert.ToString(phaseshiftWord, 2).PadLeft(8, '0');

            SendCommand(id, addr, phaseshiftWord);

        }

        // Отправка команды на устройство
        private void SendCommand(byte id, byte addr, byte byteWord)
        {
            // Формирование буфера-массива байт
            byte[] buffer = { id, addr, byteWord };

            if (writeMode == true)
            {
                PI.WritePI(buffer, StatusLabel);
            }
            else {
                BKU.WriteBKU(buffer, StatusLabel);
            }
        }
        private void SendCommand(byte id, byte addr, byte byteWord, bool mode)
        {
            // Формирование буфера-массива байт
            byte[] buffer = { id, addr, byteWord };

            if (mode == true)
            {
                PI.WritePI(buffer, StatusLabel);
            }
            else
            {
                BKU.WriteBKU(buffer, StatusLabel);
            }
        }


        // Обработчик выбора способа управления БУАФ (БКУ <-> ПИ)
        public void OnToggledControlMode(object? sender, EventArgs a)
        {
            Switch switcher = (Switch)sender;
            writeMode = switcher.IsToggled;
        }


        // Обработчик кнопки выбора режима коммутатора
        private void OnClickedRadioButtonRCOM(object? sender, EventArgs e) {
            RadioButton radioButton = (RadioButton)sender;
            if (radioButton.IsChecked)
            {
                OkHandlerRCOM(radioButton.Value);
            }
        }
        private void OkHandlerRCOM(object Value) {
            byte id = rcomID;
            byte addr = 0;
            // Определение битов команды
            byte rcomMode;
            switch (Value)
            {
                case "1": rcomMode = 1; break;
                case "2": rcomMode = 2; break;
                case "0": rcomMode = 0; break;
                default: rcomMode = 0; break;
            }
            StatusLabel.Text += $"\nРежим Коммутатора: {rcomMode}";


            SendCommand(id, addr, rcomMode, false);
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

        public void WriteToStatusLabel(string message) {
            StatusLabel.Text += message;
        }
        public void WriteToStatusLabel(string message, string blockName)
        {
            StatusLabel.Text += "\n" + blockName + ": " + message;
        }

        private void PI_connect_Clicked(System.Object sender, System.EventArgs e)
        {

        }
    }




    // ------------- КЛАССЫ -------------------------------


    // Добавить класс/функции в Main битовых посылок
}

