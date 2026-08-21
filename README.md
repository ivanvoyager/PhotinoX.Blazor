[![PhotinoX Logo](https://raw.githubusercontent.com/ivanvoyager/PhotinoX/refs/heads/master/assets/photinox-logo.png)](https://github.com/ivanvoyager/PhotinoX)

# PhotinoX.Blazor

[![NuGet Version](https://img.shields.io/nuget/v/PhotinoX.Blazor.svg)](https://www.nuget.org/packages/PhotinoX.Blazor)
[![Build](https://github.com/ivanvoyager/PhotinoX.Blazor/actions/workflows/build.yml/badge.svg)](https://github.com/ivanvoyager/PhotinoX.Blazor/actions/workflows/build.yml)
[![License](https://img.shields.io/github/license/ivanvoyager/PhotinoX.Blazor?label=license)](https://github.com/ivanvoyager/PhotinoX.Blazor/blob/master/LICENSE)
[![NuGet Downloads](https://img.shields.io/nuget/dt/PhotinoX.Blazor.svg)](https://www.nuget.org/packages/PhotinoX.Blazor)

Blazor integration for [**PhotinoX**](https://github.com/ivanvoyager/PhotinoX) desktop applications.

`PhotinoX.Blazor` extends the application model provided by [PhotinoX.App](https://github.com/ivanvoyager/PhotinoX.App) with support for running Blazor applications in native Photino windows, including root components, static web resources, URL loading policies, and multiple Blazor windows.

- **Windows:** WebView2
- **macOS:** WKWebView
- **Linux:** WebKitGTK 4.1

`PhotinoX.Blazor` is an independent fork of [tryphotino/photino.Blazor](https://github.com/tryphotino/photino.Blazor) under the Apache-2.0 license and is **not affiliated** with the original project or organization.

## Package architecture

The managed PhotinoX packages form a layered application stack:

```text
PhotinoX
└── PhotinoX.App
    └── PhotinoX.Blazor
```

- `PhotinoX` provides the native-first application, dispatcher, window, and WebView APIs.
- `PhotinoX.App` adds dependency injection, configuration, logging, environment information, application initialization, settings binding, and application lifetime management.
- `PhotinoX.Blazor` builds on `PhotinoX.App` and adds Blazor-specific application and window hosting.

`PhotinoBlazorApp` is a Blazor facade over `PhotinoApp`. Common application services, configuration, environment information, logging, application initialization, and disposal are provided by `PhotinoX.App`.

## Features

- Modern `PhotinoBlazorApp.CreateBuilder(...)` application startup
- Dependency injection through `IServiceCollection`
- Configuration through `ConfigurationManager`
- Logging through `ILoggingBuilder`
- Environment information through `PhotinoEnvironment`
- Blazor root components
- Configurable host page and application base URI
- Physical, embedded, composite, or custom `IFileProvider`
- Main-window configuration before WebView initialization
- Default, main-window, and named-window settings
- Multiple independent Blazor windows
- Per-window service scope, dispatcher, synchronization context, and WebView manager
- Blazor WebView-style URL loading policy
- Synchronous and asynchronous application disposal
- Unified `app` custom scheme on Windows, macOS, and Linux

## Quick start

```csharp
using Photino.Blazor;

namespace MyApp;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var builder = PhotinoBlazorApp.CreateBuilder(args);

        builder.RootComponents.Add<App>("#app");

        builder.ConfigureMainWindow(window =>
        {
            window
                .SetTitle("My PhotinoX Blazor App")
                .SetSize(1200, 800);
        });

        await using var app = builder.Build();

        return app.Run();
    }
}
```

`Build()` creates the application, root service provider, and main `PhotinoBlazorWindow`. The native window is initialized when the application is run.

`Run()` starts the main Blazor content and runs the native application message loop. It does not dispose the application after the message loop exits. The caller must dispose the application through `using`, `await using`, `Dispose()`, or `DisposeAsync()`.

On Windows, `PhotinoApplication.Run()` performs native window initialization and runs the message loop on an STA thread when the calling thread is not an STA thread. This allows `PhotinoBlazorApp` to be used from an asynchronous `Main` method.

## Application builder

`PhotinoBlazorAppBuilder` is a Blazor facade over `PhotinoAppBuilder`.

It exposes:

- `Services`
- `Configuration`
- `Environment`
- `Logging`
- `RootComponents`
- `ConfigureApplication(...)`
- `ConfigureBeforeDispose(...)`
- `ConfigureContainer(...)`
- `ConfigureMainWindow(...)`
- `ConfigureServices(...)`
- `ConfigureBlazor(...)`
- `UseFileProvider(...)`
- `UseAppServicesInitialization(...)`

Create a builder:

```csharp
var builder = PhotinoBlazorApp.CreateBuilder(args);
```

Create a builder with explicit application options:

```csharp
var builder = PhotinoBlazorApp.CreateBuilder(new PhotinoAppOptions
{
    Args = args,
    EnvironmentName = "Development",
    ContentRootPath = AppContext.BaseDirectory,
    WebRootPath = "wwwroot"
});
```

Disable the common `PhotinoX.App` defaults:

```csharp
var builder = PhotinoBlazorApp.CreateBuilder(args, useDefaults: false);
```

`useDefaults: false` disables the common configuration sources, logging defaults, and `PhotinoAppSettings` binding. Required Blazor services are always registered.

## Services

Register application services directly:

```csharp
builder.Services.AddSingleton<MyService>();
```

Or use the fluent configuration API:

```csharp
builder.ConfigureServices(services =>
{
    services.AddSingleton<MyService>();
    services.AddMudServices();
});
```

The built application exposes the root service provider:

```csharp
await using var app = builder.Build();

var service = app.Services.GetRequiredService<MyService>();
```

Each `PhotinoBlazorWindow` uses a child service scope. The window service provider also supplies a window-specific `HttpClient` and `IPhotinoWebResourceHandler`, while other scoped application services are resolved from that child scope.

### Custom service providers

Custom dependency injection containers are supported through `IServiceProviderFactory<TContainerBuilder>`.

For example, to use Autofac, install `Autofac.Extensions.DependencyInjection`:

```bash
dotnet add package Autofac.Extensions.DependencyInjection
```

Configure Autofac as the root service provider:

```csharp
var builder = PhotinoBlazorApp.CreateBuilder(args);

builder.ConfigureContainer(
    new AutofacServiceProviderFactory(),
    container =>
    {
        container.RegisterModule<ApplicationModule>();
    });

builder.RootComponents.Add<App>("#app");

using var app = builder.Build();
return app.Run();
```

The configured provider is used for application services and as the foundation for per-window Blazor service scopes. Implementing `IHostBuilder` or manually building a separate container is not required.

## Configuration and logging

`PhotinoBlazorAppBuilder` exposes the configuration, environment, and logging APIs from `PhotinoAppBuilder`:

```csharp
builder.Configuration["PhotinoX:MainWindow:Window:Title"] = "My Blazor App";
builder.Logging.AddFilter("MyApp", LogLevel.Debug);
```

With defaults enabled, configuration is loaded from:

- `appsettings.json`
- `appsettings.{EnvironmentName}.json`
- environment variables
- command-line arguments

The built application exposes the same application state:

```csharp
var configuration = app.Configuration;
var environment = app.Environment;
var services = app.Services;
var application = app.Application;
var dispatcher = app.Dispatcher;
```

## Blazor options

`PhotinoBlazorOptions` configures the Blazor WebView host:

```csharp
builder.ConfigureBlazor(options =>
{
    options.AppBaseUri = new Uri("app://localhost/");
    options.HostPage = "index.html";
});
```

The default values are:

```text
AppBaseUri = app://localhost/
HostPage   = index.html
```

With defaults enabled, options can also be configured through the `PhotinoX:Blazor` configuration section:

```json
{
  "PhotinoX": {
    "Blazor": {
      "AppBaseUri": "app://localhost/",
      "HostPage": "index.html"
    }
  }
}
```

`AppBaseUri` must be an absolute URI, and `HostPage` must not be empty.

## Static web resources

By default, `PhotinoX.Blazor` creates a `PhysicalFileProvider` for `PhotinoEnvironment.WebRootPath`.

Configure the web root through application options:

```csharp
var builder = PhotinoBlazorApp.CreateBuilder(new PhotinoAppOptions
{
    Args = args,
    WebRootPath = "wwwroot"
});
```

Replace the default file provider:

```csharp
builder.UseFileProvider(_ =>
    new ManifestEmbeddedFileProvider(typeof(Program).Assembly, "wwwroot"));
```

The provider factory receives the built root service provider:

```csharp
builder.UseFileProvider(services =>
{
    var environment = services.GetRequiredService<PhotinoEnvironment>();
    return new PhysicalFileProvider(environment.WebRootPath);
});
```

An explicitly configured provider replaces the default `PhysicalFileProvider`.

## Root components

Configure main-window root components before calling `Build()`:

```csharp
builder.RootComponents.Add<App>("#app");
```

Root component parameters are also supported:

```csharp
builder.RootComponents.Add<App>("#app", new Dictionary<string, object?>
{
    ["Title"] = "PhotinoX"
});
```

At least one root component must be configured for the main window.

## Main window configuration

Configure the main native window before its WebView is initialized:

```csharp
builder.ConfigureMainWindow(window =>
{
    window
        .SetTitle("PhotinoX Blazor App")
        .SetSize(1400, 800)
        .SetDevToolsEnabled(true)
        .SetIconFile("favicon.ico");
});
```

Multiple callbacks are applied in registration order:

```csharp
builder.ConfigureMainWindow(window => window.SetTitle("My App"));
builder.ConfigureMainWindow(window => window.SetSize(1200, 800));
```

The effective order is:

```text
Built-in window defaults
→ PhotinoX:WindowDefaults
→ PhotinoX:MainWindow
→ ConfigureMainWindow callbacks
```

Window settings are applied before the Blazor WebView manager is created. This is important for browser initialization parameters, WebView2 user-data folders, browser security settings, and other initialization-time options.

## Application configuration

Configure the underlying `PhotinoApplication`:

```csharp
builder.ConfigureApplication(application =>
{
    application.ShutdownMode = PhotinoShutdownMode.OnMainWindowClose;

    application.ShutdownRequested += (_, e) =>
    {
        if (e.Reason == PhotinoShutdownRequestReason.Application)
        {
            // e.Cancel = true;
        }
    };
});
```

`PhotinoX.Blazor` configures `OnMainWindowClose` as its default shutdown mode. User callbacks are applied afterward and can override that value.

## Application lifetime

`PhotinoBlazorApp.Run()` starts the main Blazor renderer and delegates the native lifetime to `PhotinoApp.Run()` and `PhotinoApplication.Run()`.

Use synchronous disposal:

```csharp
using var app = builder.Build();
return app.Run();
```

Use asynchronous disposal when the application or registered services require asynchronous cleanup:

```csharp
await using var app = builder.Build();
return app.Run();
```

Register code that must run before Blazor windows and application services are disposed:

```csharp
builder.ConfigureBeforeDispose(app =>
{
    var service = app.Services.GetRequiredService<MyService>();
    service.SaveState();

    app.MainWindow.Log("Application is shutting down.");
});
```

Callbacks execute in registration order while the Blazor windows and root service provider are still available.

If a callback throws, subsequent callbacks are not invoked. Blazor windows and the root service provider are still disposed.

## Application and window model

`PhotinoX.Blazor` separates application-level services from window-level Blazor hosting:

- `PhotinoBlazorApp` is the Blazor application facade over `PhotinoApp`.
- `PhotinoBlazorWindow` represents one native `PhotinoWindow` hosting Blazor content.
- Each `PhotinoBlazorWindow` has its own root components, WebView manager, Blazor dispatcher and synchronization context, service scope, resource handler, and message pipeline.
- Application services, configuration, logging, environment information, and the native application lifetime are shared through `PhotinoApp`.

This architecture allows multiple Blazor windows without sharing renderer-specific state between native windows.

## Multiple windows

Create a secondary window:

```csharp
var window = app.CreateWindow();

window.RootComponents.Add<Settings>("#app");

window.Window.SetTitle("Settings");
window.Show();
```

Create a secondary window and add its root component in one call:

```csharp
var window = app.CreateWindow<Settings>("#app");
window.Show();
```

Configure a window before its WebView is initialized:

```csharp
var window = app.CreateWindow<Settings>(
    "#app",
    configure: nativeWindow =>
    {
        nativeWindow
            .SetTitle("Settings")
            .SetSize(700, 500);
    });

window.Show();
```

Window configuration callbacks must be used for settings that affect WebView initialization.

## Named window configuration

Configure named windows in `appsettings.json`:

```json
{
  "PhotinoX": {
    "WindowDefaults": {
      "Window": {
        "Width": 900,
        "Height": 600,
        "CenterOnInitialize": true
      }
    },
    "Windows": {
      "Settings": {
        "Window": {
          "Title": "Settings",
          "Width": 700,
          "Height": 500,
          "StartUrl": "/settings"
        },
        "Browser": {
          "DevToolsEnabled": true
        }
      }
    }
  }
}
```

Apply the named configuration:

```csharp
var window = app.CreateWindow<Settings>(
    "#app",
    configurationName: "Settings");

window.Show();
```

The effective configuration is:

```text
WindowDefaults + Windows[Settings] + configure callback
```

For Blazor windows, relative startup URLs are passed to the Blazor WebView as application routes. They are not resolved as physical paths through `PhotinoEnvironment.WebRootPath`.

## Application scheme

`PhotinoX.Blazor` uses the `app` custom scheme on Windows, macOS, and Linux:

```text
app://localhost/
```

The previous upstream Windows `http` workaround is not used. `PhotinoX.Native` supports custom-scheme registration and navigation in WebView2.

## URL loading

`PhotinoBlazorWindow.UrlLoading` provides a Blazor WebView-style policy for top-level URL navigation:

```csharp
app.MainBlazorWindow.UrlLoading += (_, e) =>
{
    if (e.Url.Host.Equals("blocked.example.com", StringComparison.OrdinalIgnoreCase))
    {
        e.UrlLoadingStrategy = UrlLoadingStrategy.CancelLoad;
        return;
    }

    if (e.Url.Scheme == Uri.UriSchemeHttp || e.Url.Scheme == Uri.UriSchemeHttps)
        e.UrlLoadingStrategy = UrlLoadingStrategy.OpenExternally;
};
```

Default behavior:

- URLs within the application base URI load inside the WebView.
- URLs outside the application base URI open through the operating system, typically in the default browser.
- `target="_blank"` links and JavaScript `window.open(...)` requests open externally.
- Navigation can be canceled through `UrlLoadingStrategy.CancelLoad`.

Available strategies:

```csharp
UrlLoadingStrategy.OpenInWebView
UrlLoadingStrategy.OpenExternally
UrlLoadingStrategy.CancelLoad
```

External URLs should only be loaded inside the WebView when the content is trusted.

## Showing windows

`PhotinoBlazorWindow.Show()` starts the Blazor renderer and shows the native window on the current thread.

On Windows, the current thread must be an STA thread when `Show()` initializes the native window for the first time.

For the main window, use:

```csharp
app.Run();
```

`PhotinoBlazorApp.Run()` delegates native window creation and message-loop execution to `PhotinoApplication.Run()`, which provides automatic STA handling on Windows.

Secondary windows should be created and shown on the application UI thread. Use `PhotinoBlazorApp.Dispatcher` when window operations originate from another thread.

## Key differences from the upstream Photino.Blazor project

| Area | Upstream Photino.Blazor | PhotinoX.Blazor |
|---|---|---|
| Application model | Independent Blazor-specific application and service model | Blazor facade over `PhotinoX.App` |
| Application composition | Blazor services configured independently | Uses `PhotinoAppBuilder`, DI, configuration, logging, environment, and application initialization |
| Window hosting | Main window and renderer services are mostly application-level | Every `PhotinoBlazorWindow` owns window-specific renderer state and a service scope |
| Multiple windows | Primarily designed around one application-level Blazor window | Explicit multi-window model with independent root components and WebView managers |
| Application scheme | Uses `http` on Windows and `app` on Linux/macOS | Uses `app` on all supported platforms |
| URL loading policy | No Blazor WebView-style policy for normal top-level navigation | Provides `UrlLoading`, `UrlLoadingEventArgs`, and `UrlLoadingStrategy` |
| Disposal | Application-specific cleanup | Integrates with `PhotinoApp` synchronous and asynchronous disposal |


## Core (ecosystem)

- [**PhotinoX**](https://github.com/ivanvoyager/PhotinoX) - managed .NET wrapper around the native layer.
- [**PhotinoX.App**](https://github.com/ivanvoyager/PhotinoX.App) - application composition layer for PhotinoX desktop applications.
- [**PhotinoX.Native**](https://github.com/ivanvoyager/PhotinoX.Native) - native binaries for Windows/macOS/Linux.
- [**PhotinoX.Server**](https://github.com/ivanvoyager/PhotinoX.Server) - optional local static-file server for SPA/static assets.
- [**PhotinoX.Samples**](https://github.com/ivanvoyager/PhotinoX.Samples) - sample projects showcasing common scenarios.

## Install

```bash
dotnet add package PhotinoX.Blazor
```

`PhotinoX.Blazor` depends on `PhotinoX.App`, which depends on `PhotinoX`. Platform-specific native binaries are provided by the PhotinoX runtime packages.
> Package targets **net8.0; net9.0; net10.0**.

## Samples

- https://github.com/ivanvoyager/PhotinoX.Blazor/tree/master/Samples

## Requirements

- **.NET 10 SDK** (build)
- **Target frameworks:** `net8.0; net9.0; net10.0`
- Runtime deps: see [**PhotinoX.Native**](https://www.nuget.org/packages/PhotinoX.Native) (`runtimes/<rid>/native/`)
- **Windows:** Microsoft Edge WebView2 Runtime  
  https://learn.microsoft.com/microsoft-edge/webview2/
- **macOS:** WKWebView (system WebKit)  
  https://developer.apple.com/documentation/webkit/wkwebview/
- **Linux:** WebKitGTK 4.1 (runtime + dev packages)  
  https://webkitgtk.org/

## Build from source

```bash
dotnet restore Photino.Blazor/PhotinoX.Blazor.csproj
dotnet build   Photino.Blazor/PhotinoX.Blazor.csproj -c Release
dotnet pack    Photino.Blazor/PhotinoX.Blazor.csproj -c Release -o artifacts
```
> CI: see [`.github/workflows/build.yml`](https://github.com/ivanvoyager/PhotinoX.Blazor/blob/master/.github/workflows/build.yml) (build + pack + upload `.nupkg`/`.snupkg`).

## Contributing

Issues and PRs are welcome. Keep PRs focused, minimal, and consistent with the rest of PhotinoX.

## License

PhotinoX.Blazor is licensed under **Apache-2.0**.