using Avalonia;
using Avalonia.Media;

namespace RunN64.ViewModels {
    public class LogViewItemViewModel : ViewModelBase {
        public string Message { get; }
        public Brush Color { get;}

        public LogViewItemViewModel(string message, Brush color) {
            Message = message;
            Color = color;
        }
    }
}