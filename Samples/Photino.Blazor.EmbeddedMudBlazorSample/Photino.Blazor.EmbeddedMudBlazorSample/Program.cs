using Photino.Blazor.EmbeddedMudBlazorSample.Components;
using MudBlazor.Services;
using Photino.Blazor;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;

namespace Photino.Blazor.EmbeddedMudBlazorSample
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            PhotinoBlazorAppBuilder appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

            ConfigureServices(appBuilder.Services);

            appBuilder.RootComponents.Add<App>("app");

            PhotinoBlazorApp app = appBuilder.Build();

            string iconPath = ExtractEmbeddedResourceToTempFile("favicon.ico");

            app.MainWindow
                .SetSize(1400, 800)
                .SetLogVerbosity(0)
                .SetIconFile(iconPath)
                .SetTitle("Photino.Blazor Embedded MudBlazor Sample");

            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString());
            };

            app.Run();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
            services.AddSingleton<IFileProvider>(_ => new ManifestEmbeddedFileProvider(typeof(Program).Assembly, "wwwroot"));
            services.AddMudServices();
        }

        private static string ExtractEmbeddedResourceToTempFile(string fileName)
        {
            string resourceNamespace = typeof(Program).Namespace;
            string resourceName = $"{resourceNamespace}.wwwroot.{fileName}";

            Assembly assembly = Assembly.GetExecutingAssembly();

            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    Console.WriteLine($"Resource {resourceName} not found.");
                    return null;
                }

                string tempFile = Path.Combine(Path.GetTempPath(), fileName);

                using (FileStream fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                {
                    resourceStream.CopyTo(fileStream);
                }

                return tempFile;
            }
        }
    }
}
