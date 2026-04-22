using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebAppMulti.Database.Auth;

namespace WebAppMulti.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IUserStore _userStore;

        public AuthController(IConfiguration config, IUserStore userStore)
        {
            _config = config;
            _userStore = userStore;
        }

        //[HttpGet("login/{username}")]
        //public IActionResult Login(string username)
        //{
        //    var user = _userStore.FindByUsername(username);
        //    if (user == null)
        //        return NotFound("User not found");

        //    var principal = _userStore.CreatePrincipal(user);
        //    return Ok(new
        //    {
        //        User = user.UserName,
        //        Roles = _userStore.GetRoles(user)
        //    });
        //}

        // POST: api/auth/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // TODO: replace with real password check
            var user = _userStore.FindByUsername(request.Username);
            if (user == null || request.Password != "string") // dummy password check
                return Unauthorized("Invalid username or password");

            var roles = _userStore.GetRoles(user);
            var token = GenerateJwtToken(user, roles);

            return Ok(new
            {
                token,
                user = user.UserName,
                roles
            });
        }



        private string GenerateJwtToken(DummyUser user, List<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Add all role claims
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }



    //[HttpPost("login")]
    //public IActionResult Login([FromBody] LoginRequest request)
    //{
    //    // TODO: Replace with real user validation (e.g. EF Core)
    //    if (request.Username == "string" && request.Password == "string")
    //    {
    //        var token = GenerateJwtToken(request.Username, "TBD");
    //        return Ok(new { token });
    //    }

    //    return Unauthorized("Invalid username or password");
    //}

    //private string GenerateJwtToken(string username,  string role)
    //{
    //    var claims = new[]
    //    {
    //        new Claim(ClaimTypes.Name, username),
    //        new Claim(ClaimTypes.Role, role), // <- role goes here
    //        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    //    };

    //    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
    //    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    //    var token = new JwtSecurityToken(
    //        issuer: _config["Jwt:Issuer"],
    //        audience: _config["Jwt:Issuer"],
    //        claims: claims,
    //        expires: DateTime.UtcNow.AddHours(1),
    //        signingCredentials: creds);

    //    return new JwtSecurityTokenHandler().WriteToken(token);
    //}
//}

public record LoginRequest(string Username, string Password);
}
