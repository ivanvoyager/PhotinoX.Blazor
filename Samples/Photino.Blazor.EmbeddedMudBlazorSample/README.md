# Embedding MudBlazor's wwwroot Content into a Self-Contained Photino .NET Application

## Overview

When building a fully self-contained .NET desktop application using Photino and MudBlazor, you may encounter issues with embedding MudBlazor's static content (e.g., JavaScript, CSS) into the executable (.exe). This is due to the way MSBuild processes files during compilation.

By default, MSBuild embeds files before dependencies like MudBlazor generate their external content, leading to missing assets at runtime. To solve this, we separate concerns into two projects:

1. `Common` Project: Responsible for acquiring and storing MudBlazor's static files.
2. `Photino.Blazor.EmbeddedMudBlazorSample` Project: Embeds these files and configures the EmbeddedFileProvider to access them at runtime.

### Step 1: Creating and Configuring the Common Project

To ensure that MudBlazor's static files are copied to the publish directory, making them available for embedding, create a separate `C# Class Library` project (e.g., `Common`) that includes MudBlazor as a dependency and ensures its files are published. Your `.csproj` should look like this:
`Common.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
        <Configuration>Release</Configuration>
        <IsPublishable>true</IsPublishable>
    </PropertyGroup>
    
    <ItemGroup>
        <Content Include="wwwroot\**\*" CopyToPublishDirectory="Always" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="MudBlazor" Version="7.15.0" />
    </ItemGroup>
</Project>
```

### Step 2: Modify the Main Project to Embed the Common Project Files

The main project will reference the `Common` project’s published assets and embed them into the final executable. We will also add the following dependencies:
- `MudBlazor` - While not strictly necessary, as we could use the `Common` project as a dependency, for developing it was easier and I just manage the build process with a simple script
- `Microsoft.Extensions.FileProviders.Embedded` - This is required so that our application can search for the files in the embedded files of the executable

**NOTE: FileProviders.Embedded NEEDS a manifest file to be able to know what the file structure is, we need to add the following to the .csproj file to generate this manifest when building our project**:
```xml

	<PropertyGroup>
		<GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
	</PropertyGroup>
```

We embed the MudBlazor static files from the `Common` project and other static files like so:
```xml
	<ItemGroup>
		<EmbeddedResource Include="wwwroot\**" />
		<EmbeddedResource Include="..\Common\bin\Release\net8.0\publish\**" />
		<EmbeddedResource Remove="..\Common\bin\Release\net8.0\publish\*.*" />
	</ItemGroup>
```

 Your `.csproj` should now look like this:
`Photino.Blazor.EmbeddedMudBlazorSample.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

	<PropertyGroup>
		<OutputType>WinExe</OutputType>
		<TargetFramework>net8.0</TargetFramework>
		<ApplicationIcon>wwwroot/favicon.ico</ApplicationIcon>
		<PublishSingleFile>true</PublishSingleFile>
		<SelfContained>true</SelfContained>
		<IncludeAllContentForSelfExtract>true</IncludeAllContentForSelfExtract>
		<GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
		<DebugType>embedded</DebugType>
		<PublishAot>false</PublishAot>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Microsoft.Extensions.FileProviders.Embedded" Version="9.0.0" />
		<PackageReference Include="MudBlazor" Version="7.15.0" />
		<PackageReference Include="Photino.Blazor" Version="3.2.0" />
	</ItemGroup>

	<ItemGroup Condition="$(RuntimeIdentifier.StartsWith('win'))">
		<RdXmlFile Include="rd.xml" />
		<RdXmlFile Include="Microsoft.AspNetCore.Components.Web.rd.xml" />
	</ItemGroup>

	<ItemGroup>
		<EmbeddedResource Include="wwwroot\**" />
		<EmbeddedResource Include="..\Common\bin\Release\net8.0\publish\**" />
		<EmbeddedResource Remove="..\Common\bin\Release\net8.0\publish\*.*" />
	</ItemGroup>

	<ItemGroup>
		<Content Update="wwwroot\**">
			<CopyToOutputDirectory>Always</CopyToOutputDirectory>
		</Content>
	</ItemGroup>

	<ItemGroup>
		<None Include="wwwroot\favicon.ico" />
	</ItemGroup>

	<ItemGroup>
		<Folder Include="wwwroot\_content\" />
	</ItemGroup>

</Project>
```

### Step 3: Configure Embedded File Provider

In our `Program.cs` file we will configure the embedded file provider service like so:
`Program.cs`
```csharp
private static void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<IFileProvider>(_ => new ManifestEmbeddedFileProvider(typeof(Program).Assembly, "wwwroot"));
    services.AddMudServices();
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite($"Data Source={databasePath}"));
}
```

### Step 4: Handling SetIconFile Issue

Photino's `SetIconFile(string iconFile)` method expects a physical file path, but since our icons are embedded resources, they do not exist on disk. To solve this, we extract the embedded icon to a temporary file and pass its path to `SetIconFile`.

`Program.cs`
```csharp
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
```

Now we can set the icon like this (still in `Program.cs`):
```csharp
string iconPath = ExtractEmbeddedResourceToTempFile("favicon.ico");
app.MainWindow.SetIconFile(iconPath);
```

### Step 4: Create Build Script

The only thing left to do is to create a build script to ensure that the `Common` project builds before our `Photino.Blazor.EmbeddedMudBlazorSample` project to be able to embed MudBlazor's static files. We only need this for producing a final executable since MudBlazor is in both `Common` and `Photino.Blazor.EmbeddedMudBlazorSample`, so when developing we don't need to use it.

```bash
dotnet publish -c Release ".\Common\Common.csproj"
dotnet publish -c Release ".\Photino.Blazor.EmbeddedMudBlazorSample\Photino.Blazor.EmbeddedMudBlazorSample.csproj"
```

## Conclusion

By following these steps, you ensure that MudBlazor's static assets and application icons are correctly embedded in your self-contained Photino application.

If you encounter any issues, ensure:

- The `Common` project is built and published before embedding.
- The `EmbeddedResource` includes the correct paths.
- The `EmbeddedFileProvider` is configured properly.
- The manifest XML file for embedded resources is generated.
- The path given to `SetIconFile()` is correct after calling `ExtractEmbeddedResourceToTempFile()` and the virtual path the icon is extracted from is also correct.

With this setup, you can distribute a single executable containing all necessary assets.