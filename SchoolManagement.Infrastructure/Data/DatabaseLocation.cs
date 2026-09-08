using Microsoft.Data.Sqlite;

namespace SchoolManagement.Infrastructure.Data;

/// <summary>
/// Resolves where the SQLite file lives.
/// </summary>
/// <remarks>
/// The configured connection string keeps a plain file name
/// ("Data Source=school_management.db"). A relative name is rebased onto the
/// per user application data folder, because the installation directory of a
/// published application is usually read only. An absolute path in configuration
/// is honoured as is, which is what a deployment sharing the file on a server needs.
/// </remarks>
public static class DatabaseLocation
{
    public const string DefaultFileName = "school_management.db";

    public static string ApplicationDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SchoolManagement");

    public static string Resolve(string? connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(
            string.IsNullOrWhiteSpace(connectionString)
                ? $"Data Source={DefaultFileName}"
                : connectionString);

        var dataSource = string.IsNullOrWhiteSpace(builder.DataSource)
            ? DefaultFileName
            : builder.DataSource;

        if (!Path.IsPathRooted(dataSource))
        {
            Directory.CreateDirectory(ApplicationDataDirectory);
            dataSource = Path.Combine(ApplicationDataDirectory, dataSource);
        }
        else
        {
            var directory = Path.GetDirectoryName(dataSource);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        builder.DataSource = dataSource;
        return builder.ToString();
    }

    public static string GetFilePath(string resolvedConnectionString) =>
        new SqliteConnectionStringBuilder(resolvedConnectionString).DataSource;
}
