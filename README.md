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

```C#
[STAThread]
static void Main(string[] args)
{
	var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

	appBuilder.Services
		.AddLogging();

	// register root component and selector
	appBuilder.RootComponents.Add<App>("app");

	var app = appBuilder.Build();

	// customize window
	app.MainWindow
		.SetIconFile("favicon.ico")
		.SetTitle("Photino Blazor Sample");

	AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
	{
		app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString());
	};

	app.Run();
}
```

## Core (ecosystem)

- [**PhotinoX**](https://github.com/ivanvoyager/PhotinoX) - .NET wrapper around the native layer.
- [**PhotinoX.Native**](https://github.com/ivanvoyager/PhotinoX.Native) - native binaries for Windows/macOS/Linux.
- [**PhotinoX.Server**](https://github.com/ivanvoyager/PhotinoX.Server) - optional static-file server (avoids CORS/ESM issues).
- [**PhotinoX.Samples**](https://github.com/ivanvoyager/PhotinoX.Samples) - sample projects showcasing common scenarios.

---

## Install

```bash
dotnet add package PhotinoX.Blazor
```
(Ensure `PhotinoX.Native` is available at runtime for your target RID.)
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