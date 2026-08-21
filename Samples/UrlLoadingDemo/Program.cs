using Photino.Blazor;

namespace UrlLoadingDemo;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var builder = PhotinoBlazorApp.CreateBuilder(args);

        builder.RootComponents.Add<App>("#app");

        await using var app = builder.Build();

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