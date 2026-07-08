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
        // Bu uç nokta, URL'den aldığı Issue ID ve Staff ID ile Stored Procedure'ü tetikler.
        [HttpPost("{issueId}/assign/{staffId}")]
        public async Task<IActionResult> AssignIssue(int issueId, int staffId)
        {
            try
            {
                // Entity Framework Core üzerinden doğrudan SQL Stored Procedure çağrısı yapıyoruz
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC sp_AssignIssue @IssueId = {issueId}, @StaffId = {staffId}");

                return Ok(new { Message = "Görev başarıyla ilgili personele atandı ve durum 'InProgress' olarak güncellendi." });
            }
            catch (Exception ex)
            {
                // SQL tarafında (örneğin Transaction içinde) bir hata fırlatılırsa burada yakalıyoruz
                return BadRequest(new { Error = "Atama sırasında bir hata oluştu.", Detail = ex.Message });
            }
        }
    }
}