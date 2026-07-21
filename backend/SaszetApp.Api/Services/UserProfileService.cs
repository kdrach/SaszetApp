using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SaszetApp.Api.Data;
using SaszetApp.Api.DTOs;
using SaszetApp.Api.Mappers;
using SaszetApp.Api.Models;

namespace SaszetApp.Api.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly AppDbContext _dbContext;
        private readonly IScanQuotaService _scanQuotaService;
        private readonly IUserProfileMapper _mapper;

        public UserProfileService(AppDbContext dbContext, IScanQuotaService scanQuotaService, IUserProfileMapper mapper)
        {
            _dbContext = dbContext;
            _scanQuotaService = scanQuotaService;
            _mapper = mapper;
        }

        public async Task<User> GetProfileAsync(string userId, CancellationToken cancellationToken)
        {
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
                    userEntity = await _dbContext.Users.Include(u => u.Cats).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
                }
            }

            var remainingScans = await _scanQuotaService.GetRemainingScansAsync(userId, cancellationToken);

            return _mapper.MapToUser(userEntity, remainingScans);
        }

        public async Task<Cat> AddCatAsync(string userId, CatCreateDto dto, CancellationToken cancellationToken)
        {
            var userExists = await _dbContext.Users.AnyAsync(u => u.Id == userId, cancellationToken);
            if (!userExists)
            {
                _dbContext.Users.Add(new UserEntity { Id = userId });
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

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
                try
                {
                    var catCount = await _dbContext.Cats.CountAsync(c => c.UserId == userId, cancellationToken);
                    if (catCount >= 20)
                    {
                        throw new InvalidOperationException("Maximum number of cats reached.");
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
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return _mapper.MapToCat(catEntity);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }

        public async Task<bool> DeleteCatAsync(string userId, Guid catId, CancellationToken cancellationToken)
        {
            var cat = await _dbContext.Cats.FirstOrDefaultAsync(c => c.Id == catId && c.UserId == userId, cancellationToken);
            if (cat == null)
            {
                return false;
            }

            _dbContext.Cats.Remove(cat);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
