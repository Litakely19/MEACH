using SchoolManagement.Application.DTOs.Settings;

namespace SchoolManagement.Application.Interfaces;

public interface ISchoolSettingsService
{
    Task<SchoolSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(UpdateSchoolSettingsRequest request, CancellationToken cancellationToken = default);
}
