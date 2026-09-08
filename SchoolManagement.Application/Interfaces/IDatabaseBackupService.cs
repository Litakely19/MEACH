namespace SchoolManagement.Application.Interfaces;

public interface IDatabaseBackupService
{
    /// <summary>Absolute path of the live database file.</summary>
    string DatabaseFilePath { get; }

    /// <summary>Writes a consistent copy of the database to <paramref name="destinationPath"/>.</summary>
    Task BackupAsync(string destinationPath, CancellationToken cancellationToken = default);

    string BuildSuggestedFileName();
}
