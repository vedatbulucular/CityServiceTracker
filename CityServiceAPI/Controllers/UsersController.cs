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
        public async Task<IActionResult> CreateUser([FromBody] User newUser)
        {
            // Gelen düz metin şifreyi BCrypt ile hash'le ve üstine yaz.
            // work factor 12: güvenlik ile performans arasında endüstri standardı denge.
            newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newUser.PasswordHash, workFactor: 12);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Yaratılan kullanıcıyı döndürürken de şifre hash'ini gizle
            return CreatedAtAction(nameof(GetUsers), new { id = newUser.Id }, new
            {
                newUser.Id,
                newUser.FirstName,
                newUser.LastName,
                newUser.Email,
                newUser.Role,
                newUser.CreatedAt
            });
        }
    }
}