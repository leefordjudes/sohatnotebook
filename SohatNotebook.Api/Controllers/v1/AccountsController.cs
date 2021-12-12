using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SohatNotebook.Authentication.Configuration;
using SohatNotebook.Authentication.Models.DTO.Generic;
using SohatNotebook.Authentication.Models.DTO.Incoming;
using SohatNotebook.Authentication.Models.DTO.Outgoing;
using SohatNotebook.DataService.IConfiguration;
using SohatNotebook.Entities.DbSet;

namespace SohatNotebook.Api.Controllers.v1;

public class AccountsController : BaseController
{
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly JwtConfig _jwtConfig;
    public AccountsController(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        UserManager<IdentityUser> userManager,
        TokenValidationParameters tokenValidationParameters,
        IOptionsMonitor<JwtConfig> optionMonitor) : base(mapper, unitOfWork, userManager)
    {
        _userManager = userManager;
        _jwtConfig = optionMonitor.CurrentValue;
        _tokenValidationParameters = tokenValidationParameters;
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
            if (userExist != null) // email is already in the table
            {
                return BadRequest(new UserRegistrationResponseDto()
                {
                    Errors = new List<string>() { "Email already in use" },
                    Success = false,
                });
            }

            // Add the user
            var newUser = new IdentityUser()
            {
                Email = registrationDto.Email,
                UserName = registrationDto.Email,
                // todo: build email functionality for send the email to confirm the user with given email
                EmailConfirmed = true,
            };
            var isCreated = await _userManager.CreateAsync(newUser, registrationDto.Password);


            // when the registration has failed
            if (!isCreated.Succeeded)
            {
                return BadRequest(new UserRegistrationResponseDto()
                {
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
            var token = await GenerateJwtToken(newUser);
            // return back to the user
            return Ok(new UserRegistrationResponseDto()
            {
                Success = true,
                Token = token.JwtToken,
                RefreshToken = token.RefreshToken
            });

        }
        return BadRequest(new UserRegistrationResponseDto()
        {
            Success = false,
            Errors = new List<string>() { "Invalid payload" },
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
            if (exUser == null)
            {
                return BadRequest(new UserLoginResponseDto()
                {
                    Errors = new List<string>() { "Invalid authentication request" },
                    Success = false,
                });
            }
            // 2 - check if the user has a valid password
            var isCorrect = await _userManager.CheckPasswordAsync(exUser, loginDto.Password);
            if (isCorrect)
            {
                var jwtToken = await GenerateJwtToken(exUser);
                return Ok(new UserLoginResponseDto()
                {
                    Success = true,
                    Token = jwtToken.JwtToken,
                    RefreshToken = jwtToken.RefreshToken
                });
            }

            return BadRequest(new UserRegistrationResponseDto()
            {
                Errors = new List<string>() { "Invalid login request" },
                Success = false,
            });

        }

        return BadRequest(new UserRegistrationResponseDto()
        {
            Errors = new List<string>() { "Invalid payload" },
            Success = false,
        });
    }

    [HttpPost]
    [Route("RefreshToken")]
    public async Task<IActionResult> RefreshToken([FromBody] TokenRequestDto tokenRequestDto)
    {
        if (ModelState.IsValid)
        {
            // Check if the token is valid
            var result = await VerifyToken(tokenRequestDto);
            if (result == null)
            {
                return BadRequest(new UserRegistrationResponseDto()
                {
                    Errors = new List<string>() { "token validation failed" },
                    Success = false,
                });
            }
            return Ok(result);
        }
        return BadRequest(new UserRegistrationResponseDto()
        {
            Errors = new List<string>() { "Invalid payload" },
            Success = false,
        });
    }


    private async Task<TokenData> GenerateJwtToken(IdentityUser user)
    {
        // the handler is going to be responsible for creating the token
        var jwtTokenHandler = new JwtSecurityTokenHandler();
        // Get the security key
        var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);
        Console.WriteLine("Gen: {0}", _jwtConfig.Secret);

        Console.WriteLine("0.expTime:{0}", _jwtConfig.ExpiryTimeFrame.ToString());
        Console.WriteLine("1.UtcNow:{0} \t1a.UtcNow.add:{1}", DateTime.UtcNow, DateTime.UtcNow.Add(_jwtConfig.ExpiryTimeFrame));
        Console.WriteLine("2.UtcNow.ToLocalTime:{0} \t2a.UtcNow.ToLocalTime.add:{1}", DateTime.UtcNow.ToLocalTime(), DateTime.UtcNow.ToLocalTime().Add(_jwtConfig.ExpiryTimeFrame));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] {
                    new Claim("Id", user.Id),
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                }),
            // todo update the expiration time 
            // AddHours(5).AddMinutes(30)
            // Expires = DateTime.UtcNow.ToLocalTime().Add(_jwtConfig.ExpiryTimeFrame), // usually 5-10 minutes
            Expires = DateTime.UtcNow.Add(_jwtConfig.ExpiryTimeFrame), // usually 5-10 minutes
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature      // todo review the algorithm
            ),
        };

        // generate the security token
        var token = jwtTokenHandler.CreateToken(tokenDescriptor);
        // convert the security obj token into a string
        var jwtToken = jwtTokenHandler.WriteToken(token);

        // Generate a refresh token
        var refreshToken = new RefreshToken()
        {
            AddedDate = DateTime.UtcNow,
            Token = $"{RandomStringGenerator(25)}_{Guid.NewGuid()}",
            UserId = user.Id,
            IsRevoked = false,
            IsUsed = false,
            Status = 1,
            JwtId = token.Id,
            ExpiryDate = DateTime.UtcNow.AddMonths(6)
        };

        await _unitOfWork.RefreshTokens.Add(refreshToken);
        await _unitOfWork.CompleteAsync();

        var tokenData = new TokenData
        {
            JwtToken = jwtToken,
            RefreshToken = refreshToken.Token
        };

        return tokenData;
    }

    private async Task<AuthResult> VerifyToken(TokenRequestDto tokenRequestDto)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            // Validation 1 - Validation Jwt token format
            // we need to check the validity of the token
            var principal = tokenHandler.ValidateToken(
                tokenRequestDto.Token,
                _tokenValidationParameters,
                out var validatedToken);

            // Validation 2 - validate encryption algorithm
            // we need to validate the results that has been generated for us
            // validate if the string is an actual valid JWT token, not a random string
            if (validatedToken is JwtSecurityToken jwtSecurityToken)
            {
                // check if the jwt token is created with the same algorithm as our jwt token
                var result = jwtSecurityToken.Header.Alg
                    .Equals(SecurityAlgorithms.HmacSha256,
                        StringComparison.InvariantCultureIgnoreCase
                    );
                if (!result)
                    return null;
            }

            // Validation 3 - validate expiry date
            // we need to check the expiry date of the token
            var utcExpiryDate = long.Parse(principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
            // convert to data to check
            var expiryDate = UnixTimeStampToDateTime(utcExpiryDate);

            // checking if the jwt token has expired
            Console.WriteLine(" utcExpiryDate: {0}, \n expiryDate: {1},\n DateTime.UtcNow: {2}", utcExpiryDate, expiryDate, DateTime.UtcNow);
            // if(expiryDate > DateTime.UtcNow.AddHours(5).AddMinutes(30)) {
            if (expiryDate > DateTime.UtcNow)
            {
                // Console.WriteLine("expiryDate: {0}, DateTime.UtcNow: {1}", expiryDate, DateTime.UtcNow.AddHours(5).AddMinutes(30));
                return new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>() { "Jwt token has not expired" }
                };
            }

            // Validation 4 - validate existence of the token
            // check if the refresh token exists
            var refreshTokenExist = await _unitOfWork.RefreshTokens.GetByRefreshToken(tokenRequestDto.RefreshToken);

            if (refreshTokenExist == null)
            {
                return new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>() { "Invalid refresh token" }
                };
            }

            // Check the expiry date of a refresh token
            if (refreshTokenExist.ExpiryDate < DateTime.UtcNow)
            {
                return new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>() { "Refresh token has expired, please login again" }
                };
            }

            // if refresh token has been used or not
            // Validation 5 - validate if used or not
            if (refreshTokenExist.IsUsed)
            {
                return new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>() { "refresh token has been used, it can not be used" }
                };
            }

            // check refresh token if it has been revoked
            // Validation 6 - validate if revoked or not
            if (refreshTokenExist.IsRevoked)
            {
                return new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>() { "refresh token has been revoked, it can not be used" }
                };
            }
            // Validation 7 - validate the token id
            var jti = principal.Claims.SingleOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
            if (refreshTokenExist.JwtId != jti)
            {
                return new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>() { "refresh token reference does not match the jwt token" }
                };
            }

            // Start processing and get a new token
            // Update current token
            refreshTokenExist.IsUsed = true;
            var updateResult = await _unitOfWork.RefreshTokens.MarkRefreshTokenAsUsed(refreshTokenExist);
            if (updateResult)
            {
                await _unitOfWork.CompleteAsync();

                // Get the user to generate a new jwt token
                var dbUser = await _userManager.FindByIdAsync(refreshTokenExist.UserId);
                if (dbUser == null)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>() { "Error processing request" }
                    };
                }

                var token = await GenerateJwtToken(dbUser);
                return new AuthResult()
                {
                    Token = token.JwtToken,
                    Success = true,
                    RefreshToken = token.RefreshToken,
                };
            }

            return new AuthResult()
            {
                Success = false,
                Errors = new List<string>() { "Error processing request" }
            };

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            // Todo: Add better error handling, and add a logger
            return null;
        }
    }

    private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        // sets the time to 1, jan, 1970
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        // add the number of seconds from 1 Jan 1970
        // dateTime = dateTimeVal.AddSeconds(unixTimeStamp).ToLocalTime();
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
        return dateTime;
    }

    private string RandomStringGenerator(int length)
    {
        var random = new Random();
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(
            Enumerable.Repeat(chars, length)
            .Select(x => x[random.Next(x.Length)])
            .ToArray()
        );
    }

}
