using System;

namespace Photino.Blazor.Sample;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var appBuilder = PhotinoBlazorApp.CreateBuilder(args);

        // Register the root component for the main window.
        appBuilder.RootComponents.Add<App>("app");

        using var app = appBuilder.Build();

        // Customize the native Photino window.
        app.MainWindow
            .SetIconFile("favicon.ico")
            .SetTitle("PhotinoX Blazor Sample");

        AppDomain.CurrentDomain.UnhandledException += (_, error) =>
        {
            app.MainWindow.ShowMessage(
                "Fatal exception",
                error.ExceptionObject?.ToString() ?? "Unknown fatal exception.");
        };

        app.Run();
    }
}
