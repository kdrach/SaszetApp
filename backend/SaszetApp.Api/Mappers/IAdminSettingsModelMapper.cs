using SaszetApp.Api.Data;
using SaszetApp.Api.Models.Admin;

namespace SaszetApp.Api.Mappers
{
    public interface IAdminSettingsModelMapper
    {
        GlobalSettingsDto MapToGlobalSettingsDto(SystemSettingEntity? limitEntity, SystemSettingEntity? daysEntity);
        UserLimitDto MapToUserLimitDto(UserScanLimitEntity entity);
        UserLimitDetailsDto MapToUserLimitDetailsDto(string userId, int maxScans, int usage, System.DateTime windowStart);
    }
}
