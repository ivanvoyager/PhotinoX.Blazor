using Microsoft.Extensions.DependencyInjection;

namespace Photino.Blazor;

internal sealed class PhotinoWindowServiceProvider : IServiceProvider, IServiceScopeFactory, IServiceScope, IAsyncDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IServiceScope _scope;
    private readonly IPhotinoWebResourceHandler _resourceHandler;
    private readonly Uri _appBaseUri;
    private HttpClient? _httpClient;

    internal PhotinoWindowServiceProvider(IServiceProvider rootServices, IPhotinoWebResourceHandler resourceHandler, Uri appBaseUri)
    {
        ArgumentNullException.ThrowIfNull(rootServices);
        ArgumentNullException.ThrowIfNull(resourceHandler);
        ArgumentNullException.ThrowIfNull(appBaseUri);

        _scopeFactory = rootServices.GetRequiredService<IServiceScopeFactory>();
        _scope = _scopeFactory.CreateScope();
        _resourceHandler = resourceHandler;
        _appBaseUri = appBaseUri;
    }

    private PhotinoWindowServiceProvider(IServiceScopeFactory scopeFactory, IServiceScope scope, IPhotinoWebResourceHandler resourceHandler, Uri appBaseUri)
    {
        _scopeFactory = scopeFactory;
        _scope = scope;
        _resourceHandler = resourceHandler;
        _appBaseUri = appBaseUri;
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
                BaseAddress = _appBaseUri
            };

        return _scope.ServiceProvider.GetService(serviceType);
    }

    public IServiceScope CreateScope()
    {
        return new PhotinoWindowServiceProvider(_scopeFactory, _scopeFactory.CreateScope(), _resourceHandler, _appBaseUri);
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