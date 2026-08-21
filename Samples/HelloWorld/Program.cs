using System;
using Photino.Blazor;

namespace HelloWorld;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var appBuilder = PhotinoBlazorApp.CreateBuilder(args);

        // Register the root component for the main window.
        appBuilder.RootComponents.Add<App>("app");

        appBuilder.ConfigureMainWindow(window =>
        {
            // Customize the native Photino window.
            window
                .SetIconFile("favicon.ico")
                .SetTitle("PhotinoX Hello World");
        });

        using var app = appBuilder.Build();

        AppDomain.CurrentDomain.UnhandledException += (_, error) =>
        {
            app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject?.ToString() ?? "Unknown fatal exception.");
        };

        app.Run();
    }

}
