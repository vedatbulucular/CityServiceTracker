using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CityServiceAPI.Data;
using BCrypt.Net; // BCrypt şifre doğrulama kütüphanesi

namespace CityServiceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // 1. Veritabanında sadece e-postaya göre kullanıcıyı bul
            //    (Şifreyi burada karşılaştırmıyoruz — BCrypt bunu güvenle yapacak)
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == request.Email);

            // 2. Kullanıcı yoksa VEYA BCrypt şifre doğrulaması başarısızsa hata dön.
            //    BCrypt.Verify: gelen düz şifreyi, DB'deki hash ile karşılaştırır.
            //    Zamanlama saldırılarına (timing attack) karşı güvenli.
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized("Geçersiz e-posta veya şifre.");
            }

            // 3. Kullanıcı bulunduysa ona özel Token (Dijital Kimlik) üret
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                // Token'ın içine kullanıcının temel bilgilerini yerleştiriyoruz (Claim)
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                // Token'ın geçerlilik süresi (Örn: 1 Saat)
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtString = tokenHandler.WriteToken(token);

            // 4. Token'ı istemciye geri gönder
            return Ok(new { Token = jwtString });
        }
    }

    // İstemciden gelecek olan giriş bilgilerini tutacak basit bir model
    public class LoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}