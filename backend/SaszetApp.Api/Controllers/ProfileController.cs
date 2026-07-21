using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaszetApp.Api.Data;
using SaszetApp.Api.DTOs;
using SaszetApp.Api.Mappers;
using SaszetApp.Api.Models;
using SaszetApp.Api.Services;

namespace SaszetApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IScanQuotaService _scanQuotaService;
        private readonly IUserProfileMapper _mapper;

        public ProfileController(AppDbContext dbContext, IScanQuotaService scanQuotaService, IUserProfileMapper mapper)
        {
            _dbContext = dbContext;
            _scanQuotaService = scanQuotaService;
            _mapper = mapper;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
        }

        [HttpGet]
        public async Task<IActionResult> GetProfileAsync(CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            var userEntity = await _dbContext.Users
                .Include(u => u.Cats)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (userEntity == null)
            {
                userEntity = new UserEntity { Id = userId };
                _dbContext.Users.Add(userEntity);
                try
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    var userEntry = _dbContext.ChangeTracker.Entries<UserEntity>().FirstOrDefault(e => e.Entity.Id == userId);
                    if (userEntry != null)
                    {
                        userEntry.State = EntityState.Detached;
                    }
                }
            }

            var remainingScans = await _scanQuotaService.GetRemainingScansAsync(userId, cancellationToken);

            var userModel = _mapper.MapToUser(userEntity, remainingScans);

            return Ok(userModel);
        }

        [HttpPost("cats")]
        public async Task<IActionResult> AddCatAsync([FromBody] CatCreateDto dto, CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            var catCount = await _dbContext.Cats.CountAsync(c => c.UserId == userId, cancellationToken);
            if (catCount >= 20)
            {
                return BadRequest("Maximum number of cats reached.");
            }

            var userExists = await _dbContext.Users.AnyAsync(u => u.Id == userId, cancellationToken);
            if (!userExists)
            {
                _dbContext.Users.Add(new UserEntity { Id = userId });
            }

            var catEntity = new CatEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = dto.Name,
                Breed = dto.Breed,
                Weight = dto.Weight,
                Allergies = dto.Allergies
            };

            _dbContext.Cats.Add(catEntity);
            
            if (!userExists)
            {
                try
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    var userEntry = _dbContext.ChangeTracker.Entries<UserEntity>().FirstOrDefault(e => e.Entity.Id == userId);
                    if (userEntry != null)
                    {
                        userEntry.State = EntityState.Detached;
                    }
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var catModel = _mapper.MapToCat(catEntity);

            return CreatedAtAction(nameof(GetProfileAsync), null, catModel);
        }

        [HttpDelete("cats/{id}")]
        public async Task<IActionResult> DeleteCatAsync(Guid id, CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            var cat = await _dbContext.Cats.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);
            if (cat == null)
            {
                return NotFound();
            }

            _dbContext.Cats.Remove(cat);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}
