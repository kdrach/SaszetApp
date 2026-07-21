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

        private class RefCountedSemaphore
        {
            public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
            public int RefCount { get; set; }
        }

        private static readonly Dictionary<string, RefCountedSemaphore> _userLocks = new();

        public async Task<Cat> AddCatAsync(string userId, CatCreateDto dto, CancellationToken cancellationToken)
        {
            RefCountedSemaphore userLock;
            lock (_userLocks)
            {
                if (!_userLocks.TryGetValue(userId, out userLock))
                {
                    userLock = new RefCountedSemaphore { RefCount = 1 };
                    _userLocks[userId] = userLock;
                }
                else
                {
                    userLock.RefCount++;
                }
            }

            bool lockAcquired = false;
            try
            {
                await userLock.Semaphore.WaitAsync(cancellationToken);
                lockAcquired = true;
                var catCount = await _dbContext.Cats.CountAsync(c => c.UserId == userId, cancellationToken);
                if (catCount >= 20)
                {
                    throw new InvalidOperationException("Maximum number of cats reached.");
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

            return _mapper.MapToCat(catEntity);
            }
            finally
            {
                if (lockAcquired)
                {
                    userLock.Semaphore.Release();
                }
                lock (_userLocks)
                {
                    userLock.RefCount--;
                    if (userLock.RefCount == 0)
                    {
                        _userLocks.Remove(userId);
                        userLock.Semaphore.Dispose();
                    }
                }
            }
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
