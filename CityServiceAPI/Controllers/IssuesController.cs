using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CityServiceAPI.Models;
using CityServiceAPI.Repositories;
using System.Security.Claims;

namespace CityServiceAPI.Controllers;

/// <summary>
/// Sorun Bildirimleri (Issues) için HTTP API endpoint'leri.
///
/// Bu controller artık:
/// - Doğrudan AppDbContext'e bağımlı DEĞİL (veri erişimi IIssueRepository'de)
/// - Try-catch blokları içermiyor (hatalar Global Exception Handler'da yakalanıyor)
/// - Sadece HTTP katmanının sorumluluklarını üstleniyor: istek al, doğrula, cevap dön
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class IssuesController : ControllerBase
{
    private readonly IIssueRepository _issueRepository;
    private readonly IWebHostEnvironment _env;

    public IssuesController(IIssueRepository issueRepository, IWebHostEnvironment env)
    {
        _issueRepository = issueRepository;
        _env = env;
    }

    // ─────────────────────────────────────────────────────────
    // GET: api/Issues
    // Tüm bildirimleri listeler (herkese açık)
    // ─────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetIssues()
    {
        var issues = await _issueRepository.GetAllAsync();
        return Ok(issues);
    }

    // ─────────────────────────────────────────────────────────
    // POST: api/Issues
    // Yeni bir sorun bildirimi oluşturur (herkese açık)
    // ─────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> CreateIssue([FromBody] Issue newIssue)
    {
        var created = await _issueRepository.CreateAsync(newIssue);
        return CreatedAtAction(nameof(GetIssues), new { id = created.Id }, created);
    }

    // ─────────────────────────────────────────────────────────
    // POST: api/Issues/{issueId}/assign
    // Görevi JWT'den okunan personele atar (sadece Staff rolü)
    // ─────────────────────────────────────────────────────────
    [Authorize(Roles = "Staff")]
    [HttpPost("{issueId}/assign")]
    public async Task<IActionResult> AssignIssue(int issueId)
    {
        // staffId'yi URL'den değil, JWT token'ının içinden okuyoruz.
        // Token'ı AuthController oluştururken NameIdentifier claim'ine ID'yi yazmıştık.
        var staffIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (staffIdClaim == null || !int.TryParse(staffIdClaim.Value, out int staffId))
            return Unauthorized(new { Error = "Token içinde geçerli bir kullanıcı kimliği bulunamadı." });

        // Hata fırlatırsa GlobalExceptionHandler yakalar — burada try-catch gerekmez.
        await _issueRepository.AssignIssueAsync(issueId, staffId);

        return Ok(new { Message = "Görev başarıyla ilgili personele atandı ve durum 'InProgress' olarak güncellendi." });
    }

    // ─────────────────────────────────────────────────────────
    // PUT: api/Issues/{id}
    // Bir bildirimi günceller
    // ─────────────────────────────────────────────────────────
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateIssue(int id, [FromBody] Issue updatedIssue)
    {
        var result = await _issueRepository.UpdateAsync(id, updatedIssue);

        // Repository null döndürdüyse kayıt bulunamadı demektir
        if (result == null)
            return NotFound(new { Error = $"ID={id} olan bildirim bulunamadı." });

        return NoContent();
    }

    // ─────────────────────────────────────────────────────────
    // DELETE: api/Issues/{id}
    // Bir bildirimi ve ilgili atamalarını siler
    // ─────────────────────────────────────────────────────────
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteIssue(int id)
    {
        var deleted = await _issueRepository.DeleteAsync(id);

        if (!deleted)
            return NotFound(new { Error = $"ID={id} olan bildirim bulunamadı." });

        return NoContent();
    }

    // ─────────────────────────────────────────────────────────
    // POST: api/Issues/{id}/upload-image
    // Fotoğraf yükleme — 4 katmanlı güvenlik kontrolü
    // ─────────────────────────────────────────────────────────
    [HttpPost("{id}/upload-image")]
    public async Task<IActionResult> UploadImage(int id, IFormFile file)
    {
        // ── Güvenlik Kontrol 1: Dosya var mı? ──
        if (file == null || file.Length == 0)
            return BadRequest(new { Error = "Lütfen yüklenecek bir fotoğraf seçin." });

        // ── Güvenlik Kontrol 2: Boyut sınırı (maks. 5 MB) ──
        const long maxFileSizeBytes = 5 * 1024 * 1024;
        if (file.Length > maxFileSizeBytes)
            return BadRequest(new { Error = "Dosya boyutu 5 MB sınırını aşıyor." });

        // ── Güvenlik Kontrol 3: Uzantı whitelist ──
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        var fileExtension = Path.GetExtension(file.FileName);
        if (!allowedExtensions.Contains(fileExtension))
            return BadRequest(new { Error = "Geçersiz dosya uzantısı. Sadece JPG, PNG, GIF ve WEBP kabul edilir." });

        // ── Güvenlik Kontrol 4: MIME type whitelist ──
        var allowedMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "image/jpeg", "image/png", "image/gif", "image/webp" };

        if (!allowedMimeTypes.Contains(file.ContentType))
            return BadRequest(new { Error = "Geçersiz dosya türü. Sadece resim dosyaları (JPEG, PNG, GIF, WEBP) kabul edilir." });

        // ── Dosyayı kaydet ──
        // Orijinal dosya adı HİÇ kullanılmıyor — sadece GUID + whitelist'ten geçen uzantı
        var uniqueFileName = Guid.NewGuid().ToString() + fileExtension.ToLower();
        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(fileStream);

        // ── Veritabanını güncelle (Repository üzerinden) ──
        var imageUrl = $"/uploads/{uniqueFileName}";
        var issue = await _issueRepository.UpdateImageUrlAsync(id, imageUrl);

        if (issue == null)
            return NotFound(new { Error = $"ID={id} olan bildirim bulunamadı." });

        return Ok(new
        {
            Message = "Fotoğraf başarıyla sisteme yüklendi.",
            ImageUrl = issue.ImageUrl
        });
    }
}