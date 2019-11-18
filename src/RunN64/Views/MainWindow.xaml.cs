using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RunN64.ViewModels;

namespace RunN64.Views
{
    public class MainWindow : Window
    {
        public MainWindow(Emulator emulator)
        {
            EmulatorViewModel vm = new EmulatorViewModel(emulator);
            DataContext = vm;
            InitializeComponent();

            var logWindow = new LogWindow();
            logWindow.Show();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}