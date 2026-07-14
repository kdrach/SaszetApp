using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaszetApp.Api.Data;

namespace SaszetApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminPolicy")]
    public class AdminSettingsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public AdminSettingsController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public class GlobalSettingsDto
        {
            public int GlobalScanLimit { get; set; } = 5;
            public int ScanLimitRollingDays { get; set; } = 7;
        }

        [HttpGet("global")]
        public async Task<IActionResult> GetGlobalSettings()
        {
            var limitEntity = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalScanLimit");
            var daysEntity = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "ScanLimitRollingDays");

            var dto = new GlobalSettingsDto
            {
                GlobalScanLimit = limitEntity != null && int.TryParse(limitEntity.Value, out var l) ? l : 5,
                ScanLimitRollingDays = daysEntity != null && int.TryParse(daysEntity.Value, out var d) ? d : 7
            };
            return Ok(dto);
        }

        [HttpPut("global")]
        public async Task<IActionResult> UpdateGlobalSettings([FromBody] GlobalSettingsDto dto)
        {
            var limitEntity = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalScanLimit");
            if (limitEntity == null)
            {
                _dbContext.SystemSettings.Add(new SystemSettingEntity { Key = "GlobalScanLimit", Value = dto.GlobalScanLimit.ToString() });
            }
            else
            {
                limitEntity.Value = dto.GlobalScanLimit.ToString();
            }

            var daysEntity = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "ScanLimitRollingDays");
            if (daysEntity == null)
            {
                _dbContext.SystemSettings.Add(new SystemSettingEntity { Key = "ScanLimitRollingDays", Value = dto.ScanLimitRollingDays.ToString() });
            }
            else
            {
                daysEntity.Value = dto.ScanLimitRollingDays.ToString();
            }

            await _dbContext.SaveChangesAsync(System.Threading.CancellationToken.None);
            return Ok(dto);
        }

        public class UserLimitDto
        {
            public string UserId { get; set; } = string.Empty;
            public int MaxScans { get; set; }
            public int Usage { get; set; }
            public string LastReset { get; set; } = string.Empty;
        }

        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUserLimit(string userId)
        {
            var userLimit = await _dbContext.UserScanLimits.FirstOrDefaultAsync(u => u.UserId == userId);
            if (userLimit == null) return NotFound();
            return Ok(new UserLimitDto { UserId = userLimit.UserId, MaxScans = userLimit.MaxScans });
        }

        [HttpPut("users/{userId}")]
        public async Task<IActionResult> UpdateUserLimit(string userId, [FromBody] UserLimitDto dto)
        {
            if (userId != dto.UserId) return BadRequest();

            var userLimit = await _dbContext.UserScanLimits.FirstOrDefaultAsync(u => u.UserId == userId);
            if (userLimit == null)
            {
                userLimit = new UserScanLimitEntity { UserId = userId, MaxScans = dto.MaxScans };
                _dbContext.UserScanLimits.Add(userLimit);
            }
            else
            {
                userLimit.MaxScans = dto.MaxScans;
            }
            
            await _dbContext.SaveChangesAsync(System.Threading.CancellationToken.None);
            return Ok(dto);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsersLimits()
        {
            var globalLimitEntity = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalScanLimit");
            var globalLimit = globalLimitEntity != null && int.TryParse(globalLimitEntity.Value, out var l) ? l : 5;

            var rollingDaysEntity = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "ScanLimitRollingDays");
            var rollingDays = rollingDaysEntity != null && int.TryParse(rollingDaysEntity.Value, out var d) ? d : 7;

            var windowStart = System.DateTime.UtcNow.AddDays(-rollingDays);

            var explicitLimits = await _dbContext.UserScanLimits.ToListAsync();
            var usages = await _dbContext.UserScanUsages
                .Where(u => u.ScannedAt >= windowStart)
                .GroupBy(u => u.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();

            var allUserIds = explicitLimits.Select(l => l.UserId)
                .Union(usages.Select(u => u.UserId))
                .Distinct();

            var result = new System.Collections.Generic.List<UserLimitDto>();
            foreach (var userId in allUserIds)
            {
                var explicitLimit = explicitLimits.FirstOrDefault(l => l.UserId == userId);
                var maxScans = explicitLimit != null ? explicitLimit.MaxScans : globalLimit;
                var usageCount = usages.FirstOrDefault(u => u.UserId == userId)?.Count ?? 0;

                result.Add(new UserLimitDto
                {
                    UserId = userId,
                    MaxScans = maxScans,
                    Usage = usageCount,
                    LastReset = windowStart.ToString("o")
                });
            }

            return Ok(result);
        }
    }
}
