using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using Photino.Blazor.EmbeddedMudBlazorSample.Components;

namespace Photino.Blazor.EmbeddedMudBlazorSample;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        ConfigureServices(appBuilder.Services);

        appBuilder.RootComponents.Add<App>("app");

        var app = appBuilder.Build();

        var iconPath = ExtractEmbeddedResourceToTempFile("favicon.ico")
            ?? throw new InvalidOperationException("Embedded favicon.ico was not found.");

        app.MainBlazorWindow.Window
            .SetSize(1400, 800)
            .SetLogVerbosity(0)
            .SetIconFile(iconPath)
            .SetTitle("Photino.Blazor Embedded MudBlazor Sample");

        AppDomain.CurrentDomain.UnhandledException += (_, error) =>
        {
            app.MainBlazorWindow.Window.ShowMessage(
                "Fatal exception",
                error.ExceptionObject?.ToString() ?? "Unknown fatal exception.");
        };

        app.Run();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging();

        services.AddSingleton<IFileProvider>(_ =>
            new ManifestEmbeddedFileProvider(typeof(Program).Assembly, "wwwroot"));

        services.AddMudServices();
    }

    private static string? ExtractEmbeddedResourceToTempFile(string fileName)
    {
        var resourceNamespace = typeof(Program).Namespace!;
        var resourceName = $"{resourceNamespace}.wwwroot.{fileName}";
        var assembly = Assembly.GetExecutingAssembly();

        using var resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream is null)
        {
            Console.WriteLine($"Resource {resourceName} not found.");
            return null;
        }

        var tempFile = Path.Combine(Path.GetTempPath(), fileName);

        using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write);
        resourceStream.CopyTo(fileStream);

        return tempFile;
    }
}
