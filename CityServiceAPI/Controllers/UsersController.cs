using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CityServiceAPI.Data;
using CityServiceAPI.Models;

namespace CityServiceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        // Sadece kimlik doğrulaması yapmış (login olmuş) kullanıcılar erişebilir
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            // Şifre hash'i (veya başka hassas alanlar) hiçbir zaman dışarı gönderilmez.
            // Select ile sadece gönderilmesi gereken alanları seçiyoruz.
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.Role,
                    u.CreatedAt
                    // PasswordHash bu listede YOK — kasitlı olarak gönderilmiyor
                })
                .ToListAsync();

            return Ok(users);
        }

        // POST: api/Users
        // Sisteme yeni bir kullanıcı (vatandaş veya personel) ekler
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Şifre zorunludur.");
            }

            var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
            {
                return Conflict("Bu e-posta adresi zaten kayıtlı.");
            }

            // Gelen düz metin şifreyi BCrypt ile hash'le.
            // work factor 12: güvenlik ile performans arasında endüstri standardı denge.
            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Role = request.Role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Yaratılan kullanıcıyı döndürürken de şifre hash'ini gizle
            return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role,
                user.CreatedAt
            });
        }
    }

    // İstemciden gelen kullanıcı kayıt bilgileri — düz şifre "password" alanında gelir
    public class CreateUserRequest
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string Role { get; set; }
    }
}