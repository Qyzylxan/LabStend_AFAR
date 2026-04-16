namespace LabStend_AFAR
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        Button[] buttons6Att;
        Button[] buttons6Ph;

        Attenuator att;
        Phaser ph;

        public MainPage()
        {
            InitializeComponent();

            buttons6Att = new Button[] {
                ButtonAtt1, ButtonAtt2, ButtonAtt3, ButtonAtt4, ButtonAtt5, ButtonAtt6
            };

            att = new Attenuator(buttons6Att);
            //ph = new Phaser();
        }

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


        int bit = 0;

        // Функция нажатия кнопки
        private void OnClickedBit(object? sender, EventArgs e) 
        {            
            bit = bit ^ 1;
            Button button = (Button)sender;
            int index = Array.IndexOf(att.buttons6, button); // рассмотреть целесообразность Array.IndexOf()

            att.Set(index);
            if (att.Get(index) == true) button.Text = "1";
            else button.Text = "0";

        }
        private void OnClickedBitPh(object? sender, EventArgs e)
        {
            bit = bit ^ 1;
            Button button = (Button)sender;
            int index = Array.IndexOf(ph.buttons6, button); // рассмотреть целесообразность Array.IndexOf()

            att.Set(index);
            if (att.Get(index) == true) button.Text = "1";
            else button.Text = "0";

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
}

