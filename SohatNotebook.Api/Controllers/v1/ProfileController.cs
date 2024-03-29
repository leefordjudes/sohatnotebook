using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SohatNotebook.Configuration.Messages;
using SohatNotebook.DataService.IConfiguration;
using SohatNotebook.Entities.DbSet;
using SohatNotebook.Entities.Dtos.Errors;
using SohatNotebook.Entities.Dtos.Generic;
using SohatNotebook.Entities.Dtos.Incoming.Profile;
using SohatNotebook.Entities.Dtos.Outgoing.Profile;

namespace SohatNotebook.Api.Controllers.v1;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ProfileController : BaseController
{
    public ProfileController(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        UserManager<IdentityUser> userManager) : base(mapper, unitOfWork, userManager)
    { }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var result = new Result<ProfileDto>();
        var loggedInUser = await _userManager.GetUserAsync(HttpContext.User);
        if (loggedInUser == null)
        {
            result.Error = new Error() 
            {
                Code = 400,
                Message = "User not found",
                Type = "Bad Request"
            };
            return BadRequest(result);
        }

        var profile = await _unitOfWork.Users.GetByIdentityId(new Guid(loggedInUser.Id));
        if (profile == null)
        {
            result.Error = PopulateError(400, ErrorMessages.Profile.NotFound, ErrorMessages.Generic.BadRequest); 
            return BadRequest(result);
        }

        var mappedProfile = _mapper.Map<ProfileDto>(profile);

        result.Content = mappedProfile;
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto profile)
    {
        var result = new Result<ProfileDto>();
        // check If the model is valid
        if (!ModelState.IsValid)
        {
            result.Error = PopulateError(400, ErrorMessages.Generic.InvalidPayload, ErrorMessages.Generic.BadRequest); 
            return BadRequest(result);
        }

        var loggedInUser = await _userManager.GetUserAsync(HttpContext.User);
        if (loggedInUser == null)
        {
            result.Error = PopulateError(400, ErrorMessages.Profile.NotFound, ErrorMessages.Generic.BadRequest); 
            return BadRequest(result);
        }

        var userProfile = await _unitOfWork.Users.GetByIdentityId(new Guid(loggedInUser.Id));
        if (userProfile == null)
        {
            result.Error = PopulateError(400, ErrorMessages.Profile.NotFound, ErrorMessages.Generic.BadRequest); 
            return BadRequest(result);
        }

        userProfile.Country = profile.Country;
        userProfile.Address = profile.Address;
        userProfile.MobileNumber = profile.MobileNumber;
        userProfile.Sex = profile.Sex;
        userProfile.UpdateDate = DateTime.UtcNow;

        var isUpdated = await _unitOfWork.Users.UpdateUserProfile(userProfile);
        if (isUpdated)
        {
            await _unitOfWork.CompleteAsync();
            var mappedProfile = _mapper.Map<ProfileDto>(userProfile);
            result.Content = mappedProfile;
            return Ok(result);
        }
        result.Error = PopulateError(500, ErrorMessages.Generic.SomethingWentWrong, ErrorMessages.Generic.UnableToProcess); 
        return BadRequest(result);
    }
}
