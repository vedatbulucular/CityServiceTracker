using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CityServiceAPI.Data;
using CityServiceAPI.Models;

namespace CityServiceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IssuesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public IssuesController(AppDbContext context)
        {
            _context = context;
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

        // PUT: api/Issues/5 (GÜNCELLEME İŞLEMİ)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIssue(int id, [FromBody] Issue updatedIssue)
        {
            // URL'deki ID ile gönderilen verinin ID'si uyuşmuyorsa hata dön
            if (id != updatedIssue.Id)
            {
                return BadRequest("Kimlik eşleşmezliği! Lütfen geçerli bir ID girin.");
            }

            // Entity Framework'e bu nesnenin değiştirildiğini bildiriyoruz
            _context.Entry(updatedIssue).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Eğer bu ID'ye ait bir sorun veritabanında yoksa
                if (!_context.Issues.Any(e => e.Id == id))
                {
                    return NotFound("Güncellenecek kayıt bulunamadı.");
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // 204 No Content: İşlem başarılı ama geriye veri dönmeye gerek yok
        }

        // DELETE: api/Issues/5 (SİLME İŞLEMİ)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIssue(int id)
        {
            var issue = await _context.Issues.FindAsync(id);
            if (issue == null)
            {
                return NotFound("Silinmek istenen kayıt bulunamadı.");
            }

            // 1. ADIM: Önce bu soruna (Issue) bağlı olan tüm atamaları (Assignments) bul ve sil
            var relatedAssignments = await _context.Assignments.Where(a => a.IssueId == id).ToListAsync();
            if (relatedAssignments.Any())
            {
                _context.Assignments.RemoveRange(relatedAssignments);
            }

            // 2. ADIM: Bağlantılı kayıtlar temizlendiğine göre artık asıl sorunu silebiliriz
            _context.Issues.Remove(issue);
            await _context.SaveChangesAsync();

            return NoContent(); 
        }
    }
}