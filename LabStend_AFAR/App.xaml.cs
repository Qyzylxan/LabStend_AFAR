using Microsoft.Extensions.DependencyInjection;

namespace LabStend_AFAR
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            // Начальные размеры окна
            window.Width = 1200;
            window.Height = 1000;

            // Минимальные размеры окна
            window.MinimumWidth = 600;
            window.MinimumHeight = 200;

            // Позиция окна на экране
            window.X = 100;
            window.Y = 100;

            return window;
        }
    }
}