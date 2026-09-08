using Microsoft.Extensions.DependencyInjection;

namespace SchoolManagement.WPF.Services;

public sealed class ScopedExecutor : IScopedExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ScopedExecutor(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task RunAsync(Func<IServiceProvider, Task> work)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        await work(scope.ServiceProvider);
    }

    public async Task<TResult> RunAsync<TResult>(Func<IServiceProvider, Task<TResult>> work)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        return await work(scope.ServiceProvider);
    }
}
