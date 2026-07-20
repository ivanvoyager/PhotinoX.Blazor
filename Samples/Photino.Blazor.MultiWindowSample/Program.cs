using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor.MultiWindowSample.Components;
using Photino.NET;

namespace Photino.Blazor.MultiWindowSample;

internal static class Program
{
    private static readonly List<PhotinoWindow> s_windows = [];
    private static bool s_isClosingAllWindows;

    [STAThread]
    private static void Main(string[] args)
    {
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        appBuilder.Services.AddLogging();

        appBuilder.RootComponents.Add<Window1>("app");

        var app = appBuilder.Build();

        var window1 = app.MainBlazorWindow;
        window1.Window
            .SetTitle("Window 1")
            .Load(new Uri("window1.html", UriKind.Relative));

        var window2 = app.CreateWindow<Window2>("app");
        window2.Window
            .SetTitle("Window 2")
            .Load(new Uri("window2.html", UriKind.Relative));

        RegisterWindow(window1.Window);
        RegisterWindow(window2.Window);

        window1.Window.RegisterCreatedHandler((_, _) =>
        {
            window2.Show();
        });

        AppDomain.CurrentDomain.UnhandledException += (_, error) =>
        {
            var message = error.ExceptionObject?.ToString() ?? "Unknown fatal exception.";
            var window = s_windows.Count > 0 ? s_windows[0] : window1.Window;

            window.ShowMessage("Fatal exception", message);
        };

        app.Run();
    }

    private static void RegisterWindow(PhotinoWindow window)
    {
        s_windows.Add(window);

        window.RegisterClosedHandler((_, _) =>
        {
            s_windows.Remove(window);
            CloseAllWindows();
        });
    }

    private static void CloseAllWindows()
    {
        if (s_isClosingAllWindows)
            return;

        s_isClosingAllWindows = true;

        foreach (var window in s_windows.ToArray())
        {
            window.Close();
        }
    }
}
