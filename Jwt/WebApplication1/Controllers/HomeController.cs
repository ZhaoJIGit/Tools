using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly string _secretKey = "this_is_a_secure_key_for_jwtthis_is_a_secure_key_for_jwt"; // 确保你的密钥足够复杂且安全

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel login)
        {
            // 这里通常会进行用户验证
            if (login.Username == "testuser" && login.Password == "password")
            {
                var token = GenerateToken(login.Username);
                return Ok(new { token });
            }

            return Unauthorized();
        }

        private string GenerateToken(string username)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_secretKey);
            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var credentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: null, // 发行者，通常可以指定为 null
                audience: null, // 接收者，通常可以指定为 null
                claims: claims,
                expires: DateTime.Now.AddMinutes(30), // 过期时间
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token); // 返回生成的 JWT
        }
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
