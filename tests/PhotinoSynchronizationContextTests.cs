using Photino.Blazor;
using Photino.NET;

namespace PhotinoX.Blazor.Tests;

[TestClass]
public sealed class PhotinoSynchronizationContextTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(5);

    [TestMethod]
    public async Task InvokeAsync_AsyncActionWithYield_Completes()
    {
        var context = new PhotinoSynchronizationContext(new PhotinoDispatcher());

        var callbackStarted = false;
        var callbackCompleted = false;

        await context.InvokeAsync(async () =>
        {
            callbackStarted = true;

            await Task.Yield();

            callbackCompleted = true;
        }).WaitAsync(s_timeout);

        Assert.IsTrue(callbackStarted);
        Assert.IsTrue(callbackCompleted);
    }

    [TestMethod]
    public async Task InvokeAsync_AsyncFunctionWithYield_ReturnsResult()
    {
        var context = new PhotinoSynchronizationContext(new PhotinoDispatcher());

        var result = await context.InvokeAsync(async () =>
        {
            await Task.Yield();
            return 42;
        }).WaitAsync(s_timeout);

        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public async Task InvokeAsync_AsyncContinuation_IsSerializedWithQueuedWork()
    {
        var context = new PhotinoSynchronizationContext(new PhotinoDispatcher());
        var executionOrder = new List<string>();

        var firstTask = context.InvokeAsync(async () =>
        {
            executionOrder.Add("first-start");

            await Task.Yield();

            executionOrder.Add("first-complete");
        });

        var secondTask = context.InvokeAsync(() =>
        {
            executionOrder.Add("second");
        });

        await Task.WhenAll(firstTask, secondTask).WaitAsync(s_timeout);

        Assert.AreSequenceEqual(
            [
                "first-start",
                "first-complete",
                "second"
            ],
            executionOrder);
    }

    [TestMethod]
    public async Task InvokeAsync_ExceptionAfterYield_FaultsReturnedTask()
    {
        var context = new PhotinoSynchronizationContext(new PhotinoDispatcher());

        var task = context.InvokeAsync(async () =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Expected test exception.");
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await task.WaitAsync(s_timeout));

        Assert.AreEqual("Expected test exception.", exception.Message);
        Assert.IsTrue(task.IsFaulted);
    }

    [TestMethod]
    public async Task InvokeAsync_CancellationAfterYield_CancelsReturnedTask()
    {
        var context = new PhotinoSynchronizationContext(new PhotinoDispatcher());

        using var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        cancellationTokenSource.Cancel();

        var task = context.InvokeAsync(async () =>
        {
            await Task.Yield();
            token.ThrowIfCancellationRequested();
        });

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await task.WaitAsync(s_timeout));

        Assert.IsTrue(task.IsCanceled);
    }

    [TestMethod]
    public async Task InvokeAsync_SynchronouslyCompletedTask_Completes()
    {
        var context = new PhotinoSynchronizationContext(new PhotinoDispatcher());

        var callbackInvoked = false;

        var task = context.InvokeAsync(() =>
        {
            callbackInvoked = true;
            return Task.CompletedTask;
        });

        await task.WaitAsync(s_timeout);

        Assert.IsTrue(callbackInvoked);
        Assert.IsTrue(task.IsCompletedSuccessfully);
    }

    [TestMethod]
    public async Task InvokeAsync_SynchronouslyCompletedFunction_ReturnsResult()
    {
        var context = new PhotinoSynchronizationContext(new PhotinoDispatcher());

        var task = context.InvokeAsync(() => Task.FromResult(42));

        var result = await task.WaitAsync(s_timeout);

        Assert.AreEqual(42, result);
        Assert.IsTrue(task.IsCompletedSuccessfully);
    }
}