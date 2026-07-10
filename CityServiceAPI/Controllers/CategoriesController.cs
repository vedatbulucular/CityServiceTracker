using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CityServiceAPI.Data;
using CityServiceAPI.Models;

namespace CityServiceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Categories
        // Sistemdeki tüm kategorileri liste halinde dışarıya sunar
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories.ToListAsync();
            return Ok(categories);
        }

        // POST: api/Categories
        // Sisteme dışarıdan (veya .http dosyasından) yeni bir kategori eklenmesini sağlar
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] Category newCategory)
        {
            if (newCategory == null) 
            {
                return BadRequest("Kategori verisi boş olamaz.");
            }

            _context.Categories.Add(newCategory);
            await _context.SaveChangesAsync();

            return Ok(newCategory);
        }
    }
}