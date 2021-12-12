using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SohatNotebook.DataService.Data;
using SohatNotebook.DataService.IConfiguration;
using SohatNotebook.Entities.DbSet;
using SohatNotebook.Entities.Dtos.Generic;
using SohatNotebook.Entities.Dtos.Incoming;
using SohatNotebook.Configuration.Messages;

namespace SohatNotebook.Api.Controllers.v1;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UsersController : BaseController
{
    public UsersController(
        IUnitOfWork unitOfWork,
        UserManager<IdentityUser> userManager) : base(unitOfWork, userManager)
    { }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _unitOfWork.Users.All();
        var result = new PagedResult<User>();
        result.Page = 1;
        result.Content = users.ToList();
        result.ResultCount = users.Count();
        return Ok(result);
    }

    [HttpGet]
    [Route("GetUser", Name = "GetUser")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _unitOfWork.Users.GetById(id);
        var result = new Result<User>();
        if (user != null)
        {
            result.Content = user;
            return Ok(user);
        }
        result.Error = PopulateError(404, 
                            ErrorMessages.User.NotFound, 
                            ErrorMessages.Generic.NotFound);
        return NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> AddUser(UserDto user)
    {
        var _user = new User()
        {
            Status = 1,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            Country = user.Country,
            DateOfBirth = Convert.ToDateTime(user.DateOfBirth),
        };
        await _unitOfWork.Users.Add(_user);
        await _unitOfWork.CompleteAsync();
        return CreatedAtRoute("GetUser", new { id = _user.Id }, user);
    }

}
