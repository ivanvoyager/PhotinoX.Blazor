using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Photino.NET;

namespace Photino.Blazor;

internal sealed class PhotinoWebViewManager : WebViewManager
{
    private readonly PhotinoWindow _window;
    private readonly Channel<string> _outgoingMessages;
    private readonly Channel<string> _incomingMessages;
    private readonly Task _outgoingMessagesTask;
    private readonly Task _incomingMessagesTask;
    private readonly CancellationTokenSource _cts = new();
    private readonly CancellationToken _cancellationToken;
    private int _messagePumpsStopped;

    private static readonly TimeSpan s_rendererDisposeTimeout = TimeSpan.FromSeconds(5);

    internal const string BlazorAppScheme = "app";

    internal static readonly Uri AppBaseUri = new ($"{BlazorAppScheme}://localhost/");

    internal PhotinoWebViewManager(
        PhotinoWindow window,
        IServiceProvider provider,
        Dispatcher dispatcher,
        IFileProvider fileProvider,
        JSComponentConfigurationStore jsComponents,
        IOptions<PhotinoBlazorAppConfiguration> config)
        : base(provider, dispatcher, config.Value.AppBaseUri, fileProvider, jsComponents, config.Value.HostPage)
    {
        ArgumentNullException.ThrowIfNull(window);

        _window = window;

        _outgoingMessages = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        _incomingMessages = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        _window.WebMessageReceived += WebMessageReceived;
        _window.Closed += WindowClosed;

        _cancellationToken = _cts.Token;

        _outgoingMessagesTask = Task.Run(() => ProcessOutgoingMessagesAsync(_cancellationToken), _cancellationToken);
        _incomingMessagesTask = Task.Run(() => ProcessIncomingMessagesAsync(_cancellationToken), _cancellationToken);
    }

    private bool AreMessagePumpsStopped => Volatile.Read(ref _messagePumpsStopped) != 0;

    internal Stream? HandleWebRequestCore(string url, out string? contentType)
    {
        // It would be better if we were told whether this is a navigation request, but
        // since we're not, guess.
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            contentType = null;
            return null;
        }

        var localPath = uri.LocalPath;
        var hasFileExtension = localPath.LastIndexOf('.') > localPath.LastIndexOf('/');

        // Remove query parameters before attempting to retrieve the file. For example: http://localhost/_content/Blazorise/button.js?v=1.0.7.0
        var queryIndex = url.IndexOf('?');
        if (queryIndex >= 0)
            url = url[..queryIndex];

        if (url.StartsWith(AppBaseUri.ToString(), StringComparison.Ordinal)
            && TryGetResponseContent(url, !hasFileExtension, out _, out _,
                out var content, out var headers))
        {
            headers.TryGetValue("Content-Type", out contentType);
            return content;
        }

        contentType = null;
        return null;
    }

    protected override void NavigateCore(Uri absoluteUri)
    {
        _window.Load(absoluteUri);
    }

    protected override void SendMessage(string message)
    {
        if (AreMessagePumpsStopped || _window.IsClosed)
        {
#if DEBUG
            // Useful for testing renderer NotifyUnhandledException shutdown behavior.
            // if (IsBlazorUnhandledExceptionNotification(message)) return;
#endif
            ThrowWebViewMessagePumpStopped();
        }

        if (!_outgoingMessages.Writer.TryWrite(message))
            ThrowWebViewMessagePumpStopped();
    }

    private void WebMessageReceived(object? sender, string message)
    {
        if (AreMessagePumpsStopped)
            return;

        if (!_incomingMessages.Writer.TryWrite(message) && !AreMessagePumpsStopped)
            Log("Failed to enqueue incoming WebView message because the message pump is closed.");
    }

    private void WindowClosed(object? sender, EventArgs e)
    {
        StopAcceptingMessages();
    }

    private async Task ProcessOutgoingMessagesAsync(CancellationToken cancellationToken)
    {
        var reader = _outgoingMessages.Reader;

        try
        {
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (reader.TryRead(out var message))
                {
                    if (AreMessagePumpsStopped)
                        return;

                    try
                    {
                        await _window.SendWebMessageAsync(message, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (InvalidOperationException) when (AreMessagePumpsStopped || _window.IsClosed)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        Log($"Error sending message to WebView: {ex}");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ProcessIncomingMessagesAsync(CancellationToken cancellationToken)
    {
        var reader = _incomingMessages.Reader;

        try
        {
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (reader.TryRead(out var message))
                {
                    if (AreMessagePumpsStopped)
                        return;

                    try
                    {
                        // TODO: Photino should provide the source URL for the message so this can be validated.
                        // Until then, messages are treated as originating from the app host.
                        MessageReceived(AppBaseUri, message);
                    }
                    catch (Exception ex)
                    {
                        Log($"Error processing message from WebView: {ex}");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Fail("Incoming WebView message pump was canceled before observing channel completion.");
        }
    }

    private void StopAcceptingMessages()
    {
        if (Interlocked.Exchange(ref _messagePumpsStopped, 1) != 0)
            return;

        _window.WebMessageReceived -= WebMessageReceived;
        _window.Closed -= WindowClosed;

        // Stop accepting new messages and wake the message pumps.
        _outgoingMessages.Writer.TryComplete();
        _incomingMessages.Writer.TryComplete();

        // Cancel after completion to keep pump shutdown orderly.
        try
        {
            _cts.Cancel();
        }
        catch
        {
            // ignored
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        ExceptionDispatchInfo? disposeException = null;

        StopAcceptingMessages();

        try
        {
            await DisposeBaseAsyncWithTimeout().ConfigureAwait(false);
        }
        catch (Exception ex) when (IsExpectedWebViewShutdownException(ex))
        {
            // Expected during shutdown after the WebView transport has been closed.
        }
        catch (Exception ex)
        {
            disposeException = ExceptionDispatchInfo.Capture(ex);
        }

        try
        {
            await Task.WhenAll(_outgoingMessagesTask, _incomingMessagesTask).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Log($"Error stopping message pumps: {ex}");
        }
        finally
        {
            _cts.Dispose();
        }

        disposeException?.Throw();
    }

    private async Task DisposeBaseAsyncWithTimeout()
    {
        var disposeTask = base.DisposeAsyncCore().AsTask();

        using var timeoutCts = new CancellationTokenSource();
        var timeoutTask = Task.Delay(s_rendererDisposeTimeout, timeoutCts.Token);

        if (await Task.WhenAny(disposeTask, timeoutTask).ConfigureAwait(false) == disposeTask)
        {
            await timeoutCts.CancelAsync().ConfigureAwait(false);
            await disposeTask.ConfigureAwait(false);
            return;
        }

        Log($"Timed out while disposing Blazor renderer after {s_rendererDisposeTimeout}.");

        _ = disposeTask.ContinueWith(
            task =>
            {
                Log($"Blazor renderer disposal failed after timeout: {task.Exception}");
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }

    internal void Log(string message)
    {
        _window.Log(message);
    }

#if DEBUG
    private static bool IsBlazorUnhandledExceptionNotification(string message)
    {
        //Microsoft.AspNetCore.Components.WebView.IpcCommon.Serialize(string messageType, object[] args)
        return message.StartsWith("__bwv:[\"NotifyUnhandledException\",", StringComparison.Ordinal);
    }
#endif

    private bool IsExpectedWebViewShutdownException(Exception exception)
    {
        if (exception is OperationCanceledException canceledException)
            return canceledException.CancellationToken == _cancellationToken;

        if (exception is AggregateException aggregateException)
        {
            if (aggregateException.InnerExceptions.Count == 0)
                return false;

            foreach (var innerException in aggregateException.InnerExceptions)
            {
                if (!IsExpectedWebViewShutdownException(innerException))
                    return false;
            }

            return true;
        }

        return false;
    }

    [DoesNotReturn]
    private void ThrowWebViewMessagePumpStopped()
    {
        throw new OperationCanceledException("The Photino WebView message pump has been stopped.", _cancellationToken);
    }
}