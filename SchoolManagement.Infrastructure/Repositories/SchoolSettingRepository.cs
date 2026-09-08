using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Interfaces;
using SchoolManagement.Infrastructure.Data;

namespace SchoolManagement.Infrastructure.Repositories;

public class SchoolSettingRepository : Repository<SchoolSetting>, ISchoolSettingRepository
{
    public SchoolSettingRepository(SchoolDbContext context) : base(context)
    {
    }

    /// <summary>
    /// The settings table always holds a single row, created by the seeder.
    /// A defensive in memory default is returned if it is missing so that printing
    /// a receipt never crashes on a fresh or manually edited database.
    /// </summary>
    public async Task<SchoolSetting> GetSettingsAsync(
        bool trackChanges = false,
        CancellationToken cancellationToken = default)
    {
        var settings = await Query(trackChanges)
            .OrderBy(setting => setting.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return settings ?? new SchoolSetting { SchoolName = "School" };
    }
}
