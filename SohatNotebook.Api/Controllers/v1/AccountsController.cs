using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SohatNotebook.Authentication.Configuration;
using SohatNotebook.Authentication.Models.DTO.Incoming;
using SohatNotebook.Authentication.Models.DTO.Outgoing;
using SohatNotebook.DataService.IConfiguration;
using SohatNotebook.Entities.DbSet;

namespace SohatNotebook.Api.Controllers.v1
{
    public class AccountsController : BaseController
    {
        // Class provided by AspNetCore Identity Framework
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtConfig _jwtConfig;
        public AccountsController(
            IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager,
            IOptionsMonitor<JwtConfig> optionMonitor) : base(unitOfWork)
        {
            _userManager = userManager;
            _jwtConfig = optionMonitor.CurrentValue;
        }
        // Register Action
        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequestDto registrationDto)
        {
            // check the model or obj we are receiving is valid 
            if (ModelState.IsValid)
            {
                // Check if the email already exists
                var userExist = await _userManager.FindByEmailAsync(registrationDto.Email);
                if(userExist != null) // email is already in the table
                {
                    return BadRequest(new UserRegistrationResponseDto() {
                        Errors = new List<string>() {"Email already in use"},
                        Success = false,
                    });
                }
                
                // Add the user
                var newUser = new IdentityUser() {
                    Email = registrationDto.Email,
                    UserName = registrationDto.Email,
                // todo: build email functionality for send the email to confirm the user with given email
                    EmailConfirmed = true,  
                };
                var isCreated = await _userManager.CreateAsync(newUser, registrationDto.Password);
                
                
                // when the registration has failed
                if(!isCreated.Succeeded)
                {
                    return BadRequest(new UserRegistrationResponseDto() {
                        Errors = isCreated.Errors.Select(x => x.Description).ToList(),
                        Success = false,
                    });
                }

                // adding user to the database
                var _user = new User()
                {
                    IdentityId = new Guid(newUser.Id),
                    Status = 1,
                    FirstName = registrationDto.FirstName,
                    LastName = registrationDto.LastName,
                    Email = registrationDto.Email,
                    Phone = "",
                    Country = "",
                    DateOfBirth = DateTime.UtcNow, //Convert.ToDateTime(user.DateOfBirth),
                }; 
                
                await _unitOfWork.Users.Add(_user);
                await _unitOfWork.CompleteAsync();

                // create a jwt token & return back to the user
                var token = GenerateJwtToken(newUser);
                // return back to the user
                return Ok(new UserRegistrationResponseDto() {
                    Success = true,
                    Token = token,
                });
                
            }
            return BadRequest(new UserRegistrationResponseDto() {
                Success = false,
                Errors = new List<string>() {"Invalid payload"},
            });
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto loginDto)
        {
            if (ModelState.IsValid)
            {
                // 1 - Check if email exist
                var exUser = await _userManager.FindByEmailAsync(loginDto.Email);
                if(exUser == null)
                {
                    return BadRequest(new UserLoginResponseDto() {
                        Errors = new List<string>() {"Invalid authentication request"},
                        Success = false,
                    });
                }
                // 2 - check if the user has a valid password
                var isCorrect = await _userManager.CheckPasswordAsync(exUser, loginDto.Password);
                if(isCorrect)
                {
                    // var jwtToken = await GenerateJwtToken(exUser);
                    var jwtToken = GenerateJwtToken(exUser);
                    return Ok(new UserLoginResponseDto() {
                        Success = true,
                        Token = jwtToken,
                    });
                }

                return BadRequest(new UserRegistrationResponseDto() {
                    Errors = new List<string>() {"Invalid login request"},
                    Success = false,
                });
                
            }

            return BadRequest(new UserRegistrationResponseDto() {
                Errors = new List<string>() {"Invalid payload"},
                Success = false,
            });
        }

        // private async Task<AuthResult> GenerateJwtToken(IdentityUser user)
        private string GenerateJwtToken(IdentityUser user)
        {
            // the handler is going to be responsible for creating the token
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            // Get the security key
            var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);
            Console.WriteLine("Gen: {0}", _jwtConfig.Secret);

            var tokenDescriptor = new SecurityTokenDescriptor {
                Subject = new ClaimsIdentity(new [] {
                    new Claim("Id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                }),
                // todo update the expiration time 
                Expires = DateTime.UtcNow.AddHours(3), // usually 5-10 minutes
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature      // todo review the algorithm
                ),
            };

            // generate the security token
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            // convert the security obj token into a string
            var jwtToken = jwtTokenHandler.WriteToken(token);

            return jwtToken;
            // var refreshToken = new RefreshToken()
            // {
            //     JwtId = token.Id,
            //     IsUsed = false,
            //     IsRevoked =false,
            //     UserId = user.Id,
            //     AddedDate = DateTime.UtcNow,
            //     ExpiryDate = DateTime.UtcNow.AddMonths(6),
            //     Token = RandomString(35)+Guid.NewGuid()
            // };

            // await _apiDbContext.RefreshTokens.AddAsync(refreshToken);
            // await _apiDbContext.SaveChangesAsync();

            // return new AuthResult() {
            //     Token = jwtToken,
            //     Success = true,
            //     RefreshToken = refreshToken.Token,
            // };
        }
        
    }
}