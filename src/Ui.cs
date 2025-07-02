using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;

namespace StarvingArtistsScript
{
    public static class DpiHelper
    {
        public static void MakeDpiAware()
        {
            var lifetime = new ClassicDesktopStyleApplicationLifetime();
            BuildAvaloniaApp().SetupWithLifetime(lifetime);
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
        }
    }

    public class App : Application
    {
        public override void Initialize() { }
    }
}
