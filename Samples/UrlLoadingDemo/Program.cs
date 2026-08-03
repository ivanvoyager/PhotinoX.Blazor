using Photino.Blazor;

namespace UrlLoadingDemo;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

        builder.RootComponents.Add<App>("#app");

        var app = builder.Build();

        app.MainWindow
            .SetTitle("PhotinoX.Blazor UrlLoading Demo")
            .SetUseOsDefaultSize(false)
            .SetUseOsDefaultLocation(false)
            .SetSize(960, 760)
            .Center();

        app.MainBlazorWindow.UrlLoading += (_, e) =>
        {
            Console.WriteLine($"UrlLoading: {e.Url} ({e.UrlLoadingStrategy})");

            if (e.Url.Host.Equals("blocked.example.com", StringComparison.OrdinalIgnoreCase))
            {
                e.UrlLoadingStrategy = UrlLoadingStrategy.CancelLoad;
                return;
            }

            if (e.Url.Scheme == Uri.UriSchemeHttp || e.Url.Scheme == Uri.UriSchemeHttps)
                e.UrlLoadingStrategy = UrlLoadingStrategy.OpenExternally;
        };

        return app.Run();
    }
}