using Photino.NET;

namespace Photino.Blazor;
// Most UI platforms have a built-in SynchronizationContext/Dispatcher, e.g. Windows Forms and WPF,
// which Blazor can normally use directly.
//
// Photino provides an application dispatcher, but Blazor still needs a SynchronizationContext
// that serializes renderer work items, flows ExecutionContext, and sets SynchronizationContext.Current
// while executing component work.
//
// During pre-dispatch initialization, or after native dispatch has been disabled during shutdown,
// work items are executed directly instead of going through the Photino dispatcher.
internal sealed class PhotinoSynchronizationContext : SynchronizationContext
{
    private static readonly ContextCallback s_executionContextThunk = state =>
    {
        var item = (WorkItem)state!;
        item.SynchronizationContext.ExecuteSynchronously(null, item.Callback, item.State);
    };

    private static readonly Action<Task, object?> s_backgroundWorkThunk = (task, state) =>
    {
        var item = (WorkItem)state!;
        item.SynchronizationContext.ExecuteBackground(item);
    };

    private readonly PhotinoDispatcher _dispatcher;

    internal PhotinoSynchronizationContext(PhotinoDispatcher dispatcher)
        : this(dispatcher, new State())
    {
    }

    private PhotinoSynchronizationContext(PhotinoDispatcher dispatcher, State state)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    private readonly State _state;

    public event UnhandledExceptionEventHandler? UnhandledException;

    public Task InvokeAsync(Action action)
    {
        var completion = new PhotinoSynchronizationTaskCompletionSource<Action, object?>(action);
        ExecuteSynchronouslyIfPossible(static state =>
        {
            var completion = (PhotinoSynchronizationTaskCompletionSource<Action, object?>)state!;
            try
            {
                completion.Callback();
                completion.SetResult(null);
            }
            catch (OperationCanceledException)
            {
                completion.SetCanceled();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        }, completion);

        return completion.Task;
    }

    public Task InvokeAsync(Func<Task> asyncAction)
    {
        var completion = new PhotinoSynchronizationTaskCompletionSource<Func<Task>, object?>(asyncAction);
        ExecuteSynchronouslyIfPossible(static async state =>
        {
            var completion = (PhotinoSynchronizationTaskCompletionSource<Func<Task>, object?>)state!;
            try
            {
                await completion.Callback().ConfigureAwait(false);
                completion.SetResult(null);
            }
            catch (OperationCanceledException)
            {
                completion.SetCanceled();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        }, completion);

        return completion.Task;
    }

    public Task<TResult> InvokeAsync<TResult>(Func<TResult> function)
    {
        var completion = new PhotinoSynchronizationTaskCompletionSource<Func<TResult>, TResult>(function);
        ExecuteSynchronouslyIfPossible(static state =>
        {
            var completion = (PhotinoSynchronizationTaskCompletionSource<Func<TResult>, TResult>)state!;
            try
            {
                var result = completion.Callback();
                completion.SetResult(result);
            }
            catch (OperationCanceledException)
            {
                completion.SetCanceled();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        }, completion);

        return completion.Task;
    }

    public Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> asyncFunction)
    {
        var completion = new PhotinoSynchronizationTaskCompletionSource<Func<Task<TResult>>, TResult>(asyncFunction);
        ExecuteSynchronouslyIfPossible(static async state =>
        {
            var completion = (PhotinoSynchronizationTaskCompletionSource<Func<Task<TResult>>, TResult>)state!;
            try
            {
                var result = await completion.Callback().ConfigureAwait(false);
                completion.SetResult(result);
            }
            catch (OperationCanceledException)
            {
                completion.SetCanceled();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        }, completion);

        return completion.Task;
    }

    // Runs the callback asynchronously.
    //
    // NOTE: this must always run async. It's not legal here to execute the work item synchronously.
    public override void Post(SendOrPostCallback d, object? state)
    {
        lock (_state.Lock)
        {
            _state.Task = Enqueue(_state.Task, d, state, forceAsync: true);
        }
    }

    // Runs the callback synchronously.
    public override void Send(SendOrPostCallback d, object? state)
    {
        Task antecedent;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_state.Lock)
        {
            antecedent = _state.Task;
            _state.Task = completion.Task;
        }

        // We have to block. That's the contract of Send - we don't expect this to be used
        // in many scenarios in Components.
        //
        // Using Wait here is ok because the antecedent task will never throw.
        antecedent.Wait();

        ExecuteSynchronously(completion, d, state);
    }

    // shallow copy
    public override SynchronizationContext CreateCopy()
    {
        return new PhotinoSynchronizationContext(_dispatcher, _state);
    }

    // Similar to Post, but it can run the work item synchronously if the context is not busy.
    //
    // This is the main code path used by components, we want to be able to run async work but only dispatch
    // if necessary.
    private void ExecuteSynchronouslyIfPossible(SendOrPostCallback d, object? state)
    {
        TaskCompletionSource completion;
        lock (_state.Lock)
        {
            if (!_state.Task.IsCompleted)
            {
                _state.Task = Enqueue(_state.Task, d, state);
                return;
            }

            // We can execute this synchronously because nothing is currently running
            // or queued.
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _state.Task = completion.Task;
        }

        ExecuteSynchronously(completion, d, state);
    }

    private Task Enqueue(Task antecedent, SendOrPostCallback d, object? state, bool forceAsync = false)
    {
        // If we get here, it means that a callback is being explicitly queued. Let's instead add it to the queue and yield.
        //
        // We use our own queue here to maintain the execution order of the callbacks scheduled here. Also,
        // we need a queue rather than just scheduling an item in the thread pool - those items would immediately
        // block and hurt scalability.
        //
        // We need to capture the execution context so we can restore it later. This code is similar to
        // the call path of ThreadPool.QueueUserWorkItem and System.Threading.QueueUserWorkItemCallback.
        ExecutionContext? executionContext = null;
        if (!ExecutionContext.IsFlowSuppressed())
        {
            executionContext = ExecutionContext.Capture();
        }

        var flags = forceAsync ? TaskContinuationOptions.RunContinuationsAsynchronously : TaskContinuationOptions.None;
        return antecedent.ContinueWith(s_backgroundWorkThunk, new WorkItem(
            SynchronizationContext: this,
            ExecutionContext: executionContext,
            Callback: d,
            State: state
        ), CancellationToken.None, flags, TaskScheduler.Default);
    }

    private void ExecuteSynchronously(
        TaskCompletionSource? completion,
        SendOrPostCallback d,
        object? state)
    {
        // Anything run on the sync context should be dispatched through Photino when
        // dispatch is available, so native window/WebView access stays on the dispatcher thread.
        if (!_dispatcher.Invoke(Execute))
        {
            Execute();
        }

        void Execute()
        {
            var original = Current;
            try
            {
                Interlocked.Increment(ref _state.BusyCount);
                SetSynchronizationContext(this);
                d(state);
            }
            finally
            {
                Interlocked.Decrement(ref _state.BusyCount);
                SetSynchronizationContext(original);

                completion?.SetResult();
            }
        }
    }

    private void ExecuteBackground(WorkItem item)
    {
        if (item.ExecutionContext == null)
        {
            try
            {
                ExecuteSynchronously(null, item.Callback, item.State);
            }
            catch (Exception ex)
            {
                DispatchException(ex);
            }

            return;
        }

        // Perf - using a static thunk here to avoid a delegate allocation.
        try
        {
            ExecutionContext.Run(item.ExecutionContext, s_executionContextThunk, item);
        }
        catch (Exception ex)
        {
            DispatchException(ex);
        }
    }

    private void DispatchException(Exception ex)
    {
        UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(ex, isTerminating: false));
    }

    private class State
    {
        internal int BusyCount;
        private bool IsBusy => Volatile.Read(ref BusyCount) > 0; // Just for debugging
#if NET9_0_OR_GREATER
        public readonly Lock Lock = new();
#else
        public readonly object Lock = new();
#endif
        public Task Task = Task.CompletedTask;

        public override string ToString()
        {
            return $"{{ Busy: {IsBusy}, Pending Task: {Task} }}";
        }
    }

    private record WorkItem(
        PhotinoSynchronizationContext SynchronizationContext,
        ExecutionContext? ExecutionContext,
        SendOrPostCallback Callback,
        object? State);

    private class PhotinoSynchronizationTaskCompletionSource<TCallback, TResult>(TCallback callback)
        : TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously)
    {
        public TCallback Callback { get; } = callback;
    }
}