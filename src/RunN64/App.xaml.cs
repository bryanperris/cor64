using Avalonia;
using Avalonia.Markup.Xaml;

namespace RunN64
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
   }
}