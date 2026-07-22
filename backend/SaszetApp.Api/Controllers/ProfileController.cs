using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaszetApp.Api.DTOs;
using SaszetApp.Api.Services;

namespace SaszetApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "CustomerPolicy")]
    public class ProfileController : ControllerBase
    {
        private readonly IUserProfileService _userProfileService;

        public ProfileController(IUserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
        }

        [HttpGet]
        public async Task<IActionResult> GetProfileAsync(CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var userModel = await _userProfileService.GetProfileAsync(userId, cancellationToken);
            return Ok(userModel);
        }

        [HttpPost("cats")]
        public async Task<IActionResult> AddCatAsync([FromBody] CatCreateDto dto, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            try
            {
                var catModel = await _userProfileService.AddCatAsync(userId, dto, cancellationToken);
                return CreatedAtAction(nameof(GetProfileAsync), null, catModel);
            }
            catch (InvalidOperationException ex) when (ex.Message == "Maximum number of cats reached.")
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("cats/{id}")]
        public async Task<IActionResult> DeleteCatAsync(Guid id, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var deleted = await _userProfileService.DeleteCatAsync(userId, id, cancellationToken);
            
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
