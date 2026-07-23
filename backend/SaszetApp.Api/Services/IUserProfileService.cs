using System;
using System.Threading;
using System.Threading.Tasks;
using SaszetApp.Api.DTOs;
using SaszetApp.Api.Models;

namespace SaszetApp.Api.Services
{
    public interface IUserProfileService
    {
        Task<User> GetProfileAsync(string userId, CancellationToken cancellationToken);
        Task<Cat> AddCatAsync(string userId, CatCreateDto dto, CancellationToken cancellationToken);
        Task<Cat> UpdateCatAsync(string userId, Guid catId, CatUpdateDto dto, CancellationToken cancellationToken);
        Task<bool> DeleteCatAsync(string userId, Guid catId, CancellationToken cancellationToken);
    }
}
