using Microsoft.AspNetCore.Authorization; // YENİ EKLENDİ: Yetkilendirme kütüphanesi
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CityServiceAPI.Data;
using CityServiceAPI.Models;
using Microsoft.AspNetCore.Hosting; // Klasör yollarını bulmak için eklendi

namespace CityServiceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IssuesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env; // Sunucudaki klasörleri tanıyacak rehberimiz

        // Constructor güncellendi: Artık hem veritabanını (context) hem de sunucu ortamını (env) tanıyor
        public IssuesController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: api/Issues
        [HttpGet]
        public async Task<IActionResult> GetIssues()
        {
            var issues = await _context.Issues.ToListAsync();
            return Ok(issues);
        }

        // POST: api/Issues
        [HttpPost]
        public async Task<IActionResult> CreateIssue([FromBody] Issue newIssue)
        {
            newIssue.ReportedAt = DateTime.Now;
            newIssue.Status = "Pending";

            _context.Issues.Add(newIssue);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetIssues), new { id = newIssue.Id }, newIssue);
        }

        // POST: api/Issues/5/assign/2
        [Authorize(Roles = "Staff")] // YENİ EKLENEN KİLİT: Sadece personeller atama yapabilir!
        [HttpPost("{issueId}/assign/{staffId}")]
        public async Task<IActionResult> AssignIssue(int issueId, int staffId)
        {
            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC sp_AssignIssue @IssueId = {issueId}, @StaffId = {staffId}");

                return Ok(new { Message = "Görev başarıyla ilgili personele atandı ve durum 'InProgress' olarak güncellendi." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = "Atama sırasında bir hata oluştu.", Detail = ex.Message });
            }
        }

        // PUT: api/Issues/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIssue(int id, [FromBody] Issue updatedIssue)
        {
            if (id != updatedIssue.Id)
            {
                return BadRequest("Kimlik eşleşmezliği! Lütfen geçerli bir ID girin.");
            }

            _context.Entry(updatedIssue).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Issues.Any(e => e.Id == id))
                {
                    return NotFound("Güncellenecek kayıt bulunamadı.");
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Issues/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIssue(int id)
        {
            var issue = await _context.Issues.FindAsync(id);
            if (issue == null)
            {
                return NotFound("Silinmek istenen kayıt bulunamadı.");
            }

            var relatedAssignments = await _context.Assignments.Where(a => a.IssueId == id).ToListAsync();
            if (relatedAssignments.Any())
            {
                _context.Assignments.RemoveRange(relatedAssignments);
            }

            _context.Issues.Remove(issue);
            await _context.SaveChangesAsync();

            return NoContent(); 
        }

        // FOTOĞRAF YÜKLEME (POST: api/Issues/2/upload-image)
        [HttpPost("{id}/upload-image")]
        public async Task<IActionResult> UploadImage(int id, IFormFile file)
        {
            // 1. Dosya seçilmiş mi kontrol et
            if (file == null || file.Length == 0)
                return BadRequest("Lütfen yüklenecek bir fotoğraf seçin.");

            // 2. Fotoğrafın ekleneceği sorunu (Issue) veritabanında bul
            var issue = await _context.Issues.FindAsync(id);
            if (issue == null)
                return NotFound("Fotoğraf eklenmek istenen bildirim bulunamadı.");

            // 3. Dosyaya benzersiz bir isim ver (Örn: 550e8400-e29b-41d4-a716-446655440000.jpg)
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;

            // 4. Dosyanın kaydedileceği fiziksel yolu oluştur (wwwroot/uploads/...)
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 5. Dosyayı fiziksel olarak sunucuya (uploads klasörüne) kopyala
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // 6. Veritabanındaki 'ImageUrl' alanını yeni dosyanın linkiyle güncelle
            issue.ImageUrl = $"/uploads/{uniqueFileName}";
            await _context.SaveChangesAsync();

            return Ok(new { 
                Message = "Fotoğraf başarıyla sisteme yüklendi.", 
                ImageUrl = issue.ImageUrl 
            });
        }
    }
}