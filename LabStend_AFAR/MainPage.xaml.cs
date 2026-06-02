using System.IO.Ports;
using System.Text;
using System.Collections.Generic;
using static LabStend_AFAR.MUAF;
using LabStend_AFAR.Potentiometer; 

using static LabStend_AFAR.BKU;
using LabStend_AFAR.D2XX;

namespace LabStend_AFAR
{
    public partial class MainPage : ContentPage
    {
        double f = 2;       // ГГц
        double c = 0.3;     // м/с * 10^9 
        double thetaMax = 90; // Макс. угол отклонения луча по модулю, град

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
            WriteToStatusLabel("Запуск СПО...");

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

            Thread.Sleep(100);
            // BKU.Init();

            WriteToStatusLabel(PI.OpenPort(LabelStatus_PI), "ПИ");

            LoadAvailablePorts(StatusLabel, COMportPickers, "БКУ");
            WriteToStatusLabel(AutoConnectToBKU(serialPortBKU, LabelStatus_BKU, StatusLabel, COMportPickerBKU, availablePorts), "Автопоиск БКУ"); 
            // В будущем заменить здесь и в COMport.cs на множество портов

        }


        // Функция нажатия кнопки ручного подключения к БКУ
        private void OnClickedBKUConnect(object? sender, EventArgs e)
        {
            Button button = (Button)sender;
            string message = ConnectToCOM(serialPortBKU, LabelStatus_BKU, StatusLabel, COMportPickerBKU.SelectedIndex, 'e');
            WriteToStatusLabel(message, "БКУ");

        }
        // Функция нажатия кнопки ручного подключения к ПИ
        private void OnClickedPIConnect(object? sender, EventArgs e)
        {
            Button button = (Button)sender;
            WriteToStatusLabel(PI.OpenPort(LabelStatus_PI), "ПИ");

        }




        // Кнопка Ок угла отклонения луча АФАР (БУАФ)
        private void OnClickedOkBUAF(object? sender, EventArgs e) {
            Button button = (Button)sender;

            string blockName = "БУАФ";

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
                    phaseShift = PhaseShift(angle);
                }
            }
            else
            {
                // Если не распарсил текст с ввода
                WriteToStatusLabel($"Текст с поля ввода не распознан. Введите число от {-thetaMax} до {thetaMax}", blockName);
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

        // Функция расчёта сдвига фазы по заданному углу отклонения луча АФАР
        double PhaseShift(double angle) {
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

            string blockName = "МШУ 1";
            // Определение битов адреса
            int gainByte = 0;
            byte addr;
            switch (groupName) {
                case "Gain": addr = 0; break;
                case "Gain2": addr = 1; blockName = "МШУ 2"; break;
                case "Gain3": addr = 2; blockName = "МШУ 3"; break;
                case "Gain4": addr = 3; blockName = "МШУ 4"; break;
                    default: addr = 0; blockName = "МШУ 1"; break;
            }
            
            byte id = lnaID;

            // Определение битов команды
            int lnaMode;
            switch (Value)
            {
                case "1": lnaMode = 5; break;
                case "2": lnaMode = 2; break;
                case "0": lnaMode = 0; break;
                default: lnaMode = 0; break;
            }
            WriteToStatusLabel($"Уровень напряжения контроля: {lnaMode} В \t", blockName);
            

            
            gainByte = MCP_7bit.Nmax / MCP_7bit.Vmax * lnaMode; // Запись команды на установку значений

            byte addrPOT = addr%2 == 0? MCP_7bit.pot1_MOSI: MCP_7bit.pot2_MOSI;

            gainByte = gainByte | (addrPOT<<8);
            gainByte = gainByte | (MCP_7bit.writeData << 8);

            SendCommand(id, addr, gainByte, writeMode);
        }

        //----------------------------

        // Обработчик нажатия кнопки Ок Аттенюатора
        private void OnClickedOkButtonAtt(object? sender, EventArgs e) 
        {
            Button button = (Button)sender;

            // Структура битовой посылки: 0000 0000 , 0000 0000 , 0000 0000
            //                            command     addr        id
            // Определение битов адреса; соответствие поля ввода блоку аттенюатора
            string blockName = "Аттенюатор 1";
            byte addr = 0;
            Entry entry = EntryAmp;
            Label labelBit = LabelAttBitWord;

            if (button == OkButtonAtt) { }
            else if (button == OkButtonAtt2) { addr = 1; entry = EntryAmp2; blockName = "Аттенюатор 2"; labelBit = LabelAttBitWord2; }
            else if (button == OkButtonAtt3) { addr = 2; entry = EntryAmp3; blockName = "Аттенюатор 3"; labelBit = LabelAttBitWord3; }
            else if (button == OkButtonAtt4) { addr = 3; entry = EntryAmp4; blockName = "Аттенюатор 4"; labelBit = LabelAttBitWord4; }

            byte id = attID;

            // Парсинг вводимого текста
            if (!double.TryParse(entry.Text, out double attenuationValue))
            {
                // Если не распарсил текст с ввода
                WriteToStatusLabel("Текст с поля ввода не распознан. Введите число от 0 до 31,5", blockName);
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
                WriteToStatusLabel("Значение ослабления не кратно шагу 0,5 дБ. Округление...", blockName);
                entry.Text = $"{attenuationValue - value}";
            }

            // Вывод команды в поля
            Console.WriteLine(attenuationWord);
            Console.WriteLine(Convert.ToString(attenuationWord, 2));

            labelBit.Text = Convert.ToString(attenuationWord, 2).PadLeft(8, '0');

            SendCommand(id, addr, attenuationWord, writeMode);

        }

        //----------------------------------

        // Кнопка ОК Фазовращателя
        private void OnClickedOkButtonPh(object? sender, EventArgs e)
        {
            Button button = (Button)sender;

            // Структура битовой посылки: 0000 0000 , 0000 0000 , 0000 0000
            //                              command   addr        id

            // Определение битов адреса; соответствие поля ввода блоку фазовращателя
            string blockName = "Фазовращатель 1";
            byte addr = 0;
            Entry entry = EntryPh;
            Label labelBit = LabelPhBitWord;

            if (button == OkButtonPh) { }
            else if (button == OkButtonPh2) { addr = 1; entry = EntryPh2; blockName = "Фазовращатель 2"; labelBit = LabelPhBitWord2; }
            else if (button == OkButtonPh3) { addr = 2; entry = EntryPh3; blockName = "Фазовращатель 3"; labelBit = LabelPhBitWord3; }
            else if (button == OkButtonPh4) { addr = 3; entry = EntryPh4; blockName = "Фазовращатель 4"; labelBit = LabelPhBitWord4; }
            ;
            byte id = phID;

            // Парсинг вводимого текста
            if (!double.TryParse(entry.Text, out double phaseshiftValue))
            {
                // Если не распарсил текст с ввода
                WriteToStatusLabel("Текст с поля ввода не распознан. Введите число от 0 до 354,4", blockName);
                return;
            }
            PhaseWrite(phaseshiftValue, entry, labelBit, id, addr, blockName);
        }

        // Функция обработки введённого значения сдвига фазы
        private void PhaseWrite(double phaseshiftValue, Entry entry, Label labelBit, byte id, byte addr, string blockName) {
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
                WriteToStatusLabel("Значение сдвига фазы не кратно шагу 5,6 град. Округление...", blockName);
                entry.Text = $"{phaseshiftValue - value}";
            }

            // Вывод команды в поля
            Console.WriteLine(phaseshiftWord);
            Console.WriteLine(Convert.ToString(phaseshiftWord, 2));

            labelBit.Text = Convert.ToString(phaseshiftWord, 2).PadLeft(8, '0');

            SendCommand(id, addr, phaseshiftWord, writeMode);

        }

        // Отправка команды на устройство
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
                WriteToStatusLabel(BKU.WriteBKU(buffer, StatusLabel), "БКУ");
            }
        }
        private void SendCommand(byte id, byte addr, int twoByteWord, bool mode)
        {
            byte addrByte2 = (byte)((twoByteWord & 0b1111111100000000) >> 8);
            byte addrByte1 = (byte)(twoByteWord);

            // Формирование буфера-массива байт
            byte[] buffer = { id, addr, addrByte2 ,addrByte1 };

            if (writeMode == true)
            {
                PI.WritePI(buffer, StatusLabel);
            }
            else
            {
                WriteToStatusLabel(BKU.WriteBKU(buffer, StatusLabel), "БКУ");
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
            int rcomMode;
            switch (Value)
            {
                case "1": rcomMode = 1; break;
                case "2": rcomMode = 2; break;
                case "3": rcomMode = 3; break;
                case "0": rcomMode = 0; break;
                default: rcomMode = 0; break;
            }
            WriteToStatusLabel($"Переключение на выход: {rcomMode+1}", "Коммутатор");       
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

        // Функции-обработчики для работы с библиотеками
        void LoadAvailablePorts(Label StatusLabel, Picker[] COMportPickers, string blockName) {
            WriteToStatusLabel(BKU.LoadAvailablePorts(StatusLabel, COMportPickers));
        }


        // Функция вывода сообщений в StatusLabel
        public void WriteToStatusLabel(string message) {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            StatusLabel.Text += $"[{timestamp}] {message}\n";
        }
        public void WriteToStatusLabel(string message, string blockName)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            StatusLabel.Text += $"[{timestamp}] {blockName}: {message}\n";
            
        }

        
    }
  

    
}

