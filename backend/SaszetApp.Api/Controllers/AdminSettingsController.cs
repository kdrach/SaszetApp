using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaszetApp.Api.Data;
using SaszetApp.Api.Models.Admin;
using SaszetApp.Api.Mappers;

namespace SaszetApp.Api.Controllers
{
    /// <summary>
    /// Controller for managing administrative settings such as global and user scan limits.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminPolicy")]
    public class AdminSettingsController(AppDbContext dbContext, IAdminSettingsModelMapper mapper) : ControllerBase
    {
        private readonly AppDbContext _dbContext = dbContext;
        private readonly IAdminSettingsModelMapper _mapper = mapper;

        /// <summary>
        /// Gets the global scan settings.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The global settings DTO.</returns>
        [HttpGet("global")]
        public async Task<IActionResult> GetGlobalSettings(CancellationToken cancellationToken = default)
        {
            var limitEntity = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalScanLimit", cancellationToken);
            var daysEntity = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "ScanLimitRollingDays", cancellationToken);

            var dto = _mapper.MapToGlobalSettingsDto(limitEntity, daysEntity);
            return Ok(dto);
        }

        /// <summary>
        /// Updates the global scan settings.
        /// </summary>
        /// <param name="dto">The new global settings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated global settings DTO.</returns>
        [HttpPut("global")]
        public async Task<IActionResult> UpdateGlobalSettings([FromBody] GlobalSettingsDto dto, CancellationToken cancellationToken = default)
        {
            var limitEntity = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalScanLimit", cancellationToken);
            if (limitEntity == null)
            {
                _dbContext.SystemSettings.Add(new SystemSettingEntity { Key = "GlobalScanLimit", Value = dto.GlobalScanLimit.ToString() });
            }
            else
            {
                limitEntity.Value = dto.GlobalScanLimit.ToString();
            }

            var daysEntity = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "ScanLimitRollingDays", cancellationToken);
            if (daysEntity == null)
            {
                _dbContext.SystemSettings.Add(new SystemSettingEntity { Key = "ScanLimitRollingDays", Value = dto.ScanLimitRollingDays.ToString() });
            }
            else
            {
                daysEntity.Value = dto.ScanLimitRollingDays.ToString();
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Ok(dto);
        }

        /// <summary>
        /// Gets the specific scan limit for a given user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user limit DTO.</returns>
        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUserLimit(string userId, CancellationToken cancellationToken = default)
        {
            var userLimit = await _dbContext.UserScanLimits.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
            if (userLimit == null) return NotFound();
            return Ok(_mapper.MapToUserLimitDto(userLimit));
        }

        /// <summary>
        /// Updates the specific scan limit for a given user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="dto">The updated user limit data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated user limit DTO.</returns>
        [HttpPut("users/{userId}")]
        public async Task<IActionResult> UpdateUserLimit(string userId, [FromBody] UserLimitDto dto, CancellationToken cancellationToken = default)
        {
            if (userId != dto.UserId) return BadRequest();

            var userLimit = await _dbContext.UserScanLimits.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
            if (userLimit == null)
            {
                userLimit = new UserScanLimitEntity { UserId = userId, MaxScans = dto.MaxScans };
                _dbContext.UserScanLimits.Add(userLimit);
            }
            else
            {
                userLimit.MaxScans = dto.MaxScans;
            }
            
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Ok(dto);
        }

        /// <summary>
        /// Gets a paginated list of all users' limits and usages.
        /// </summary>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A paginated result containing user limits.</returns>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsersLimits(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 50, 
            CancellationToken cancellationToken = default)
        {
            var globalLimitEntity = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalScanLimit", cancellationToken);
            var globalLimit = globalLimitEntity != null && int.TryParse(globalLimitEntity.Value, out var l) ? l : 5;

            var rollingDaysEntity = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "ScanLimitRollingDays", cancellationToken);
            var rollingDays = rollingDaysEntity != null && int.TryParse(rollingDaysEntity.Value, out var d) ? d : 7;

            var windowStart = System.DateTime.UtcNow.AddDays(-rollingDays);

            var explicitUserIds = _dbContext.UserScanLimits.Select(l => l.UserId);
            var activeUserIds = _dbContext.UserScanUsages
                .Where(u => u.ScannedAt >= windowStart)
                .Select(u => u.UserId);

            var allUserIdsQuery = explicitUserIds.Union(activeUserIds).Distinct();

            var totalCount = await allUserIdsQuery.CountAsync(cancellationToken);

            var pagedUserIds = await allUserIdsQuery
                .OrderBy(id => id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var explicitLimits = await _dbContext.UserScanLimits
                .Where(l => pagedUserIds.Contains(l.UserId))
                .ToListAsync(cancellationToken);

            var usages = await _dbContext.UserScanUsages
                .Where(u => u.ScannedAt >= windowStart && pagedUserIds.Contains(u.UserId))
                .GroupBy(u => u.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var result = new List<UserLimitDetailsDto>();
            foreach (var userId in pagedUserIds)
            {
                var explicitLimit = explicitLimits.FirstOrDefault(l => l.UserId == userId);
                var maxScans = explicitLimit != null ? explicitLimit.MaxScans : globalLimit;
                var usageCount = usages.FirstOrDefault(u => u.UserId == userId)?.Count ?? 0;

                result.Add(_mapper.MapToUserLimitDetailsDto(userId, maxScans, usageCount, windowStart));
            }

            var pagedResult = new PagedResult<UserLimitDetailsDto>
            {
                Items = result,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(pagedResult);
        }
    }
}
