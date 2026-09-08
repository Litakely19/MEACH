namespace SchoolManagement.WPF.Services;

/// <summary>
/// Runs a unit of work inside its own dependency injection scope.
/// </summary>
/// <remarks>
/// A desktop process has no request boundary, so without this every view model
/// would hold one long lived <c>DbContext</c> for hours. Each call here creates a
/// fresh scope, therefore a fresh context and unit of work, and disposes it when
/// the operation ends.
/// </remarks>
public interface IScopedExecutor
{
    Task RunAsync(Func<IServiceProvider, Task> work);

    Task<TResult> RunAsync<TResult>(Func<IServiceProvider, Task<TResult>> work);
}
