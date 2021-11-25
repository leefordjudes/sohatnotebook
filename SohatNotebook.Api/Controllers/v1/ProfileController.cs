using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SohatNotebook.DataService.IConfiguration;
using SohatNotebook.Entities.Dtos.Incoming.Profile;

namespace SohatNotebook.Api.Controllers.v1
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ProfileController : BaseController
    {
        public ProfileController(
            IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager) : base(unitOfWork, userManager)
        {}

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var loggedInUser = await _userManager.GetUserAsync(HttpContext.User);
            if (loggedInUser == null)
            {
                return BadRequest("User not foune");
            }

            var profile = await _unitOfWork.Users.GetByIdentityId(new Guid(loggedInUser.Id));
            if (profile == null)
            {
                return BadRequest("User not foune");
            }

            return Ok(profile);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto profile)
        {
            // check If the model is valid
            if(!ModelState.IsValid)
            {
                return BadRequest("Invalid payload");
            }

            var loggedInUser = await _userManager.GetUserAsync(HttpContext.User);
            if (loggedInUser == null)
            {
                return BadRequest("User not foune");
            }

            var userProfile = await _unitOfWork.Users.GetByIdentityId(new Guid(loggedInUser.Id));
            if (userProfile == null)
            {
                return BadRequest("User not foune");
            }

            userProfile.Country = profile.Country;
            userProfile.Address = profile.Address;
            userProfile.MobileNumber = profile.MobileNumber;
            userProfile.Sex = profile.Sex;
            userProfile.UpdateDate = DateTime.UtcNow;

            var isUpdated = await _unitOfWork.Users.UpdateUserProfile(userProfile);
            if(isUpdated)
            {
                await _unitOfWork.CompleteAsync();
                return Ok(userProfile);
            }
            return BadRequest("Something went wrong, please try again later");
        }
    }
}