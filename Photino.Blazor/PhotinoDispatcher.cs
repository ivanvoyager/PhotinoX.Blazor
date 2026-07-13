using Microsoft.AspNetCore.Components;

namespace Photino.Blazor;

internal sealed class PhotinoDispatcher : Dispatcher
{
    private readonly PhotinoSynchronizationContext _context;

    public PhotinoDispatcher(PhotinoSynchronizationContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _context.UnhandledException += (_, e) => OnUnhandledException(e);
    }

    public override bool CheckAccess() => SynchronizationContext.Current == _context;

    public override Task InvokeAsync(Action workItem)
    {
        if (CheckAccess())
        {
            workItem();
            return Task.CompletedTask;
        }

        return _context.InvokeAsync(workItem);
    }

    public override Task InvokeAsync(Func<Task> workItem)
    {
        if (CheckAccess())
        {
            return workItem();
        }

        return _context.InvokeAsync(workItem);
    }

    public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
    {
        if (CheckAccess())
        {
            return Task.FromResult(workItem());
        }

        return _context.InvokeAsync<TResult>(workItem);
    }

    public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
    {
        if (CheckAccess())
        {
            return workItem();
        }

        return _context.InvokeAsync<TResult>(workItem);
    }
}