using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Photino.Blazor.Tests;

[TestClass]
public sealed class PhotinoBlazorAppBuilderTests
{
    [TestMethod]
    public void CreateBuilder_ConfiguresDefaultBlazorOptions()
    {
        var builder = PhotinoBlazorApp.CreateBuilder(useDefaults: false);

        using var services = builder.Services.BuildServiceProvider();
        var options = services.GetRequiredService<IOptions<PhotinoBlazorOptions>>().Value;

        Assert.AreEqual(new Uri("app://localhost/"), options.AppBaseUri);
        Assert.AreEqual("index.html", options.HostPage);
    }

    [TestMethod]
    public void ConfigureBlazor_ConfiguresBlazorOptions()
    {
        var expectedBaseUri = new Uri("custom://host/");
        const string expectedHostPage = "main.html";

        var builder = PhotinoBlazorApp.CreateBuilder(useDefaults: false);

        builder.ConfigureBlazor(options =>
        {
            options.AppBaseUri = expectedBaseUri;
            options.HostPage = expectedHostPage;
        });

        using var services = builder.Services.BuildServiceProvider();
        var options = services.GetRequiredService<IOptions<PhotinoBlazorOptions>>().Value;

        Assert.AreEqual(expectedBaseUri, options.AppBaseUri);
        Assert.AreEqual(expectedHostPage, options.HostPage);
    }

    [TestMethod]
    public void UseFileProvider_ReplacesDefaultFileProvider()
    {
        var expected = new NullFileProvider();
        var builder = PhotinoBlazorApp.CreateBuilder(useDefaults: false);

        builder.UseFileProvider(_ => expected);

        using var services = builder.Services.BuildServiceProvider();
        var actual = services.GetRequiredService<IFileProvider>();

        Assert.AreSame(expected, actual);
    }

    [TestMethod]
    public void ConfigureBlazor_ThrowsWhenHostPageIsEmpty()
    {
        var builder = PhotinoBlazorApp.CreateBuilder(useDefaults: false);

        builder.ConfigureBlazor(options => options.HostPage = string.Empty);

        using var services = builder.Services.BuildServiceProvider();

        Assert.ThrowsExactly<OptionsValidationException>(() =>
            _ = services.GetRequiredService<IOptions<PhotinoBlazorOptions>>().Value);
    }

    [TestMethod]
    public void ConfigureBlazor_ThrowsWhenAppBaseUriIsRelative()
    {
        var builder = PhotinoBlazorApp.CreateBuilder(useDefaults: false);

        builder.ConfigureBlazor(options => options.AppBaseUri = new Uri("relative", UriKind.Relative));

        using var services = builder.Services.BuildServiceProvider();

        Assert.ThrowsExactly<OptionsValidationException>(() =>
            _ = services.GetRequiredService<IOptions<PhotinoBlazorOptions>>().Value);
    }

    [TestMethod]
    public void Build_ThrowsWhenRootComponentsAreEmpty()
    {
        var builder = PhotinoBlazorApp.CreateBuilder(useDefaults: false);

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());

        Assert.AreEqual("At least one root component must be configured before building the application.", exception.Message);
    }

    [TestMethod]
    public void Build_ThrowsWhenRootComponentTypeIsMissing()
    {
        var builder = PhotinoBlazorApp.CreateBuilder(useDefaults: false);

        builder.RootComponents.Add(new RootComponent
        {
            Selector = "app"
        });

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());

        Assert.AreEqual("RootComponent requires ComponentType.", exception.Message);
    }

    [TestMethod]
    public void Build_ThrowsWhenRootComponentSelectorIsEmpty()
    {
        var builder = PhotinoBlazorApp.CreateBuilder(useDefaults: false);

        builder.RootComponents.Add(new RootComponent
        {
            ComponentType = typeof(TestComponent),
            Selector = string.Empty
        });

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());

        Assert.AreEqual("RootComponent requires Selector.", exception.Message);
    }

    private sealed class TestComponent : ComponentBase;
}