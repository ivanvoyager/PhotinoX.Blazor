# PhotinoX.Blazor

[![NuGet Version](https://img.shields.io/nuget/v/PhotinoX.Blazor.svg)](https://www.nuget.org/packages/PhotinoX.Blazor)
[![Build](https://github.com/ivanvoyager/PhotinoX.Blazor/actions/workflows/build.yml/badge.svg)](https://github.com/ivanvoyager/PhotinoX.Blazor/actions/workflows/build.yml)
[![License](https://img.shields.io/github/license/ivanvoyager/PhotinoX.Blazor?label=license)](https://github.com/ivanvoyager/PhotinoX.Blazor/blob/master/LICENSE)
[![NuGet Downloads](https://img.shields.io/nuget/dt/PhotinoX.Blazor.svg)](https://www.nuget.org/packages/PhotinoX.Blazor)

Blazor integration for [**PhotinoX**](https://github.com/ivanvoyager/PhotinoX) (native OS WebView host).

- **Windows**: WebView2
- **macOS**: WKWebView
- **Linux**: WebKitGTK 4.1

> **Note:** `PhotinoX.Blazor` is an independent fork of [tryphotino/photino.Blazor](https://github.com/tryphotino/photino.Blazor) under the Apache‑2.0 license and is **not affiliated** with the original project or organization.

---

## Quick start

```csharp
[STAThread]
static void Main(string[] args)
{
    var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

    appBuilder.Services
        .AddLogging();

    // Register the root component and selector for the main window.
    appBuilder.RootComponents.Add<App>("app");

    var app = appBuilder.Build();

    // Customize the native Photino window.
    app.MainBlazorWindow.Window
        .SetIconFile("favicon.ico")
        .SetTitle("PhotinoX Blazor Sample");

    AppDomain.CurrentDomain.UnhandledException += (_, error) =>
    {
        app.MainBlazorWindow.Window.ShowMessage(
            "Fatal exception",
            error.ExceptionObject?.ToString() ?? "Unknown fatal exception.");
    };

    app.Run();
}
```

## Application and window model

`PhotinoX.Blazor` separates application-level services from window-level Blazor hosting:

- `PhotinoBlazorApp` owns the shared service provider and application lifecycle.
- `PhotinoBlazorWindow` represents one native Photino window hosting Blazor content.
- Each `PhotinoBlazorWindow` has its own root components, WebView manager, dispatcher, and message pipeline.

This makes multi-window scenarios explicit and avoids sharing window-specific Blazor state between native windows.

Root components should be configured before the corresponding `PhotinoBlazorWindow.Show()` call.

## Multiple windows

```csharp
[STAThread]
static void Main(string[] args)
{
    var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

    appBuilder.Services
        .AddLogging();

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

    window1.Window.RegisterWindowCreatedHandler((_, _) =>
    {
        window2.Show();
    });

    app.Run();
}
```

## Core (ecosystem)

- [**PhotinoX**](https://github.com/ivanvoyager/PhotinoX) - .NET wrapper around the native layer.
- [**PhotinoX.Native**](https://github.com/ivanvoyager/PhotinoX.Native) - native binaries for Windows/macOS/Linux.
- [**PhotinoX.Server**](https://github.com/ivanvoyager/PhotinoX.Server) - optional local static-file server for SPA/static assets.
- [**PhotinoX.Samples**](https://github.com/ivanvoyager/PhotinoX.Samples) - sample projects showcasing common scenarios.

---

## Install

```bash
dotnet add package PhotinoX.Blazor
```
`PhotinoX.Native` provides the native WebView host binaries and must be available for the target runtime identifier.
> Package targets **net8.0; net9.0; net10.0**. CI builds use the latest **.NET 10 SDK**.

## Samples

- https://github.com/ivanvoyager/PhotinoX.Blazor/blob/master/Samples

## Requirements

- **.NET 10 SDK** (build)
- **Target frameworks:** `net8.0; net9.0; net10.0` (package supports all three)
- Runtime deps: see [**PhotinoX.Native**](https://www.nuget.org/packages/PhotinoX.Native) (`runtimes/<rid>/native/`)
- **Windows:** WebView2 Runtime  
  Required component: **Microsoft.Web.WebView2** (Edge WebView2)  
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

PhotinoX.Blazor is licensed under **Apache‑2.0**.