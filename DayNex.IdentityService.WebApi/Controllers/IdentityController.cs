using DayNex.IdentityService.Application.Interface;
using Microsoft.AspNetCore.Mvc;

namespace DayNex.IdentityService.WebApi.Controllers
{
    [ApiController]                   
    [Route("api/identity")]          
    public class IdentityController : ControllerBase
    {
        private readonly IUserProfileService _profileService;

        public IdentityController(IUserProfileService profileService) => _profileService = profileService;

        [HttpGet("me")]
        public async Task<IActionResult> GetMe(
            [FromQuery] string externalId, [FromQuery] string email, [FromQuery] string displayName,
            CancellationToken cancellationToken)
        {
            var result = await _profileService.GetOrProvisionAsync(externalId, email, displayName, cancellationToken);
            return result.Succeeded ? Ok(result.Data) : Problem(result.Error);
        }
    }
}