namespace SchoolManagement.Application.Interfaces;

/// <summary>
/// Brings the database to a usable state at startup: applies pending migrations
/// and makes sure the roles, the administrator account, the payment types, the
/// school settings and (on an empty roster) related demo data exist.
/// </summary>
public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
