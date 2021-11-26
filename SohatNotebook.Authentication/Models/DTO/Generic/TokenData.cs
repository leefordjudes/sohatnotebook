using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SohatNotebook.Authentication.Models.DTO.Generic;

public class TokenData
{
    public string JwtToken { get; set; }
    public string RefreshToken { get; set; }
}
