using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Domain.Interfaces;

public interface ISchoolSettingRepository : IRepository<SchoolSetting>
{
    Task<SchoolSetting> GetSettingsAsync(bool trackChanges = false, CancellationToken cancellationToken = default);
}
