using Microsoft.Extensions.DependencyInjection;

namespace Photino.Blazor;

internal sealed class PhotinoWindowServiceProvider : IServiceProvider, IServiceScopeFactory, IServiceScope, IAsyncDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IServiceScope _scope;
    private readonly IPhotinoWebResourceHandler _resourceHandler;
    private HttpClient? _httpClient;

    internal PhotinoWindowServiceProvider(IServiceProvider rootServices, IPhotinoWebResourceHandler resourceHandler)
    {
        ArgumentNullException.ThrowIfNull(rootServices);
        ArgumentNullException.ThrowIfNull(resourceHandler);

        _scopeFactory = rootServices.GetRequiredService<IServiceScopeFactory>();
        _scope = _scopeFactory.CreateScope();
        _resourceHandler = resourceHandler;
    }

    private PhotinoWindowServiceProvider(IServiceScopeFactory scopeFactory, IServiceScope scope, IPhotinoWebResourceHandler resourceHandler)
    {
        _scopeFactory = scopeFactory;
        _scope = scope;
        _resourceHandler = resourceHandler;
    }

    public IServiceProvider ServiceProvider => this;

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IServiceProvider))
            return this;

        if (serviceType == typeof(IServiceScopeFactory))
            return this;

        if (serviceType == typeof(IPhotinoWebResourceHandler))
            return _resourceHandler;

        if (serviceType == typeof(HttpClient))
            return _httpClient ??= new HttpClient(new PhotinoHttpHandler(_resourceHandler))
            {
                BaseAddress = PhotinoWebViewManager.AppBaseUri
            };

        return _scope.ServiceProvider.GetService(serviceType);
    }

    public IServiceScope CreateScope()
    {
        return new PhotinoWindowServiceProvider(
            _scopeFactory,
            _scopeFactory.CreateScope(),
            _resourceHandler);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _scope.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();

        if (_scope is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        else
            _scope.Dispose();
    }
}