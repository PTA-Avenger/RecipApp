using SkiaSharp.Views.Maui.Controls.Hosting;
using Microcharts.Maui;
namespace RecipeApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()  // ✅ Make sure 'App.xaml' and 'App.xaml.cs' exist
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            })
            .UseSkiaSharp();

        return builder.Build();
    }
}
