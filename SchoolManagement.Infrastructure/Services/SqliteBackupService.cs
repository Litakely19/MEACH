using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Exceptions;
using SchoolManagement.Infrastructure.Data;

namespace SchoolManagement.Infrastructure.Services;

/// <summary>
/// Backs the database up with SQLite's own "VACUUM INTO", which writes a
/// transactionally consistent and compacted copy while the application keeps
/// running. A plain file copy could capture a half written page.
/// </summary>
public class SqliteBackupService : IDatabaseBackupService
{
    private readonly SchoolDbContext _context;
    private readonly ILogger<SqliteBackupService> _logger;

    public SqliteBackupService(SchoolDbContext context, ILogger<SqliteBackupService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public string DatabaseFilePath =>
        DatabaseLocation.GetFilePath(_context.Database.GetConnectionString() ?? string.Empty);

    public async Task BackupAsync(string destinationPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new DomainException("Choose where the backup should be written.");
        }

        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(destinationPath))
        {
            File.Delete(destinationPath);
        }

        // SQLite does not accept a parameter for the VACUUM INTO target, so the path
        // has to be inlined. It comes from a Save dialog, is required to be a rooted
        // path and has its quotes escaped, which is what the EF1002 warning asks for.
        var target = Path.GetFullPath(destinationPath).Replace("'", "''");

#pragma warning disable EF1002
        await _context.Database.ExecuteSqlRawAsync($"VACUUM INTO '{target}';", cancellationToken);
#pragma warning restore EF1002

        _logger.LogInformation("Database backed up to {Path}", destinationPath);
    }

    public string BuildSuggestedFileName() =>
        $"school_management_backup_{DateTime.Now:yyyyMMdd_HHmm}.db";
}
