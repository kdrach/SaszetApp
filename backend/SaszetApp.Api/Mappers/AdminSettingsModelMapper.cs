using SaszetApp.Api.Data;
using SaszetApp.Api.Models.Admin;

namespace SaszetApp.Api.Mappers
{
    public class AdminSettingsModelMapper : IAdminSettingsModelMapper
    {
        public GlobalSettingsDto MapToGlobalSettingsDto(SystemSettingEntity? limitEntity, SystemSettingEntity? daysEntity)
        {
            return new GlobalSettingsDto
            {
                GlobalScanLimit = limitEntity != null && int.TryParse(limitEntity.Value, out var l) ? l : 5,
                ScanLimitRollingDays = daysEntity != null && int.TryParse(daysEntity.Value, out var d) ? d : 7
            };
        }

        public UserLimitDto MapToUserLimitDto(UserScanLimitEntity entity)
        {
            return new UserLimitDto
            {
                UserId = entity.UserId,
                MaxScans = entity.MaxScans
            };
        }

        public UserLimitDetailsDto MapToUserLimitDetailsDto(string userId, int maxScans, int usage, System.DateTime windowStart)
        {
            return new UserLimitDetailsDto
            {
                UserId = userId,
                MaxScans = maxScans,
                Usage = usage,
                LastReset = windowStart.ToString("o")
            };
        }
    }
}
