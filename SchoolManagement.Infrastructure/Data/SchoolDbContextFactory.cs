using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SchoolManagement.Infrastructure.Data;

/// <summary>
/// Used only by the EF Core command line tools, so that
/// "dotnet ef migrations add" can run against this project without booting the UI.
/// </summary>
public class SchoolDbContextFactory : IDesignTimeDbContextFactory<SchoolDbContext>
{
    public SchoolDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SchoolDbContext>()
            .UseSqlite("Data Source=school_management_design.db")
            .Options;

        return new SchoolDbContext(options);
    }
}
