using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SohatNotebook.DataService.Data;
using SohatNotebook.DataService.IConfiguration;
using SohatNotebook.Entities.DbSet;
using SohatNotebook.Entities.Dtos.Incoming;

namespace SohatNotebook.Api.Controllers 
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private IUnitOfWork _unitOfWork;

        public UsersController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _unitOfWork.Users.All();
            return Ok(users);
        }

        [HttpGet]
        [Route("GetUser", Name = "GetUser")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var user = await _unitOfWork.Users.GetById(id);
            return Ok(user);
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
            return CreatedAtRoute("GetUser", new { id = _user.Id}, user);
        }

    }
}