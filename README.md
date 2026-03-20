# PhotinoX.Blazor

[![NuGet Version](https://img.shields.io/nuget/v/PhotinoX.Blazor.svg)](https://www.nuget.org/packages/PhotinoX.Blazor)
[![Build](https://github.com/ivanvoyager/PhotinoX.Blazor/actions/workflows/build.yml/badge.svg)](https://github.com/ivanvoyager/PhotinoX.Blazor/actions/workflows/build.yml)
[![License](https://img.shields.io/github/license/ivanvoyager/PhotinoX.Blazor?label=license)](https://github.com/ivanvoyager/PhotinoX.Blazor/blob/master/LICENSE)
[![NuGet Downloads](https://img.shields.io/nuget/dt/PhotinoX.Blazor.svg)](https://www.nuget.org/packages/PhotinoX.Blazor)

Blazor integration for [**PhotinoX**](https://github.com/ivanvoyager/PhotinoX) (native OS WebView host).

- **Windows**: WebView2
- **macOS**: WKWebView
- **Linux**: WebKitGTK 4.1

> `PhotinoX.Blazor` is an independent fork of [tryphotino/photino.Blazor](https://github.com/tryphotino/photino.Blazor) under the Apache‑2.0 license and is **not affiliated** with the original project or organization.

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

## Install

```bash
dotnet add package PhotinoX.Blazor
```
(Ensure `PhotinoX.Native` is available at runtime for your target RID.)
> Package targets **net8.0; net9.0; net10.0**. CI builds use the latest **.NET 10 SDK**.

## Samples

- https://github.com/ivanvoyager/PhotinoX.Blazor/tree/master/Samples

## Requirements

- **.NET 10 SDK** (build)
- **Target frameworks:** `net8.0; net9.0; net10.0` (package supports all three)
- Runtime deps: see [**PhotinoX.Native**](https://www.nuget.org/packages/PhotinoX.Native) (`runtimes/<rid>/native/`)
- **Windows:** WebView2 Runtime
- **macOS:** WKWebView (system)
- **Linux:** WebKitGTK 4.1 development/runtime packages

## Build from source

```bash
dotnet restore Photino.Blazor/PhotinoX.Blazor.csproj
dotnet build   Photino.Blazor/PhotinoX.Blazor.csproj -c Release
dotnet pack    Photino.Blazor/PhotinoX.Blazor.csproj -c Release -o artifacts
```
> CI: see `.github/workflows/build.yml` (build + pack + upload `.nupkg`/`.snupkg`).

## Contributing

Issues and PRs are welcome. Keep changes minimal and performance-conscious.

## License

PhotinoX.Blazor is licensed under **Apache‑2.0**.  