using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SohatNotebook.DataService.Data;
using SohatNotebook.Entities.DbSet;
using SohatNotebook.Entities.Dtos.Incoming;

namespace SohatNotebook.Api.Controllers 
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetUsers()
        {
            var users = _context.Users.Where(x => x.Status == 1).ToList();
            return Ok(users);
        }

        [HttpGet("GetUser")]
        public IActionResult GetUser(Guid id)
        {
            var user = _context.Users.FirstOrDefault(x => x.Id == id);
            return Ok(user);
        }

        [HttpPost]
        public IActionResult AddUser(UserDto user)
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
            _context.Users.Add(_user);
            _context.SaveChanges();
            return Ok();    // we should return 201, but now we return 200
        }

    }
}