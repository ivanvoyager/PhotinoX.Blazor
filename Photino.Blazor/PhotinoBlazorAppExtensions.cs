using Microsoft.Extensions.DependencyInjection;
using Photino.NET;
using PhotinoX.App;

namespace Photino.Blazor;

/// <summary>
/// Provides builder factory methods for <see cref="PhotinoBlazorApp"/>.
/// </summary>
public static class PhotinoBlazorAppExtensions
{
    extension(PhotinoBlazorApp app)
    {
        /// <summary>
        /// Creates a builder for a Photino Blazor application.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <param name="useDefaults">
        /// <see langword="true"/> to configure default application configuration sources,
        /// logging, and application settings binding; otherwise, <see langword="false"/>.
        /// </param>
        /// <returns>A Photino Blazor application builder.</returns>
        public static PhotinoBlazorAppBuilder CreateBuilder(string[]? args = null, bool useDefaults = true) =>
            PhotinoBlazorApp.CreateBuilder(new PhotinoAppOptions { Args = args }, useDefaults);

        /// <summary>
        /// Creates a builder for a Photino Blazor application using the specified application options.
        /// </summary>
        /// <param name="options">The application options.</param>
        /// <param name="useDefaults">
        /// <see langword="true"/> to configure default application configuration sources,
        /// logging, and application settings binding; otherwise, <see langword="false"/>.
        /// </param>
        /// <returns>A Photino Blazor application builder.</returns>
        public static PhotinoBlazorAppBuilder CreateBuilder(PhotinoAppOptions options, bool useDefaults = true)
        {
            ArgumentNullException.ThrowIfNull(options);

            var appBuilder = PhotinoApp.CreateBuilder(options, useDefaults);
            appBuilder.Services.AddBlazorDesktop();
            appBuilder.Services.AddSingleton<PhotinoBlazorAppState>();
            appBuilder.ConfigureBeforeDispose(app =>
            {
                var state = app.Services.GetRequiredService<PhotinoBlazorAppState>();
                var blazorApp = state.App;
                try
                {
                    if (blazorApp is null)
                        return;

                    try
                    {
                        state.BeforeDispose?.Invoke(blazorApp);
                    }
                    finally
                    {
                        blazorApp.DisposeWindows();
                    }
                }
                finally
                {
                    blazorApp?.MarkDisposed();
                    state.BeforeDispose = null;
                    state.App = null;
                }
            });
            appBuilder.ConfigureApplication(application => application.ShutdownMode = PhotinoShutdownMode.OnMainWindowClose);

            return new PhotinoBlazorAppBuilder(appBuilder);
        }
    }
}