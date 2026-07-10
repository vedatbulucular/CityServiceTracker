using Microsoft.EntityFrameworkCore;
using CityServiceAPI.Data;
using CityServiceAPI.Models;

namespace CityServiceAPI.Repositories;

/// <summary>
/// IIssueRepository'nin Entity Framework Core implementasyonu.
/// Tüm veritabanı erişim kodu burada toplanmıştır.
/// Controller bu sınıfı doğrudan bilmez; sadece IIssueRepository interface'ini bilir.
/// </summary>
public class IssueRepository : IIssueRepository
{
    private readonly AppDbContext _context;

    // AppDbContext, Dependency Injection tarafından otomatik olarak enjekte edilir.
    public IssueRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Issue>> GetAllAsync()
    {
        return await _context.Issues.ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Issue?> GetByIdAsync(int id)
    {
        return await _context.Issues.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<Issue> CreateAsync(Issue issue)
    {
        // İş kuralları: Sunucu tarafında zaman damgası ve başlangıç durumu atanır.
        // Böylece istemci bu alanları istediği gibi manipüle edemez.
        issue.ReportedAt = DateTime.UtcNow;
        issue.Status = "Pending";

        _context.Issues.Add(issue);
        await _context.SaveChangesAsync();
        return issue;
    }

    /// <inheritdoc />
    public async Task<Issue?> UpdateAsync(int id, Issue updatedIssue)
    {
        // Kaydın var olup olmadığını kontrol et
        var exists = await _context.Issues.AnyAsync(e => e.Id == id);
        if (!exists) return null; // null → Controller'da NotFound

        updatedIssue.Id = id; // URL'deki ID'yi entity'ye yaz (güvenlik önlemi)
        _context.Entry(updatedIssue).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return updatedIssue;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id)
    {
        var issue = await _context.Issues.FindAsync(id);
        if (issue == null) return false; // false → Controller'da NotFound

        // İlgili atamaları da temizle (cascade delete yoksa manuel yapılır)
        var relatedAssignments = await _context.Assignments
            .Where(a => a.IssueId == id)
            .ToListAsync();

        if (relatedAssignments.Any())
            _context.Assignments.RemoveRange(relatedAssignments);

        _context.Issues.Remove(issue);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task AssignIssueAsync(int issueId, int staffId)
    {
        // SQL Injection'a kapalı — ExecuteSqlInterpolatedAsync parametreli sorgu kullanır.
        // Stored Procedure atomik olarak çalışır: atama + durum güncellemesi tek transaction'da.
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC sp_AssignIssue @IssueId = {issueId}, @StaffId = {staffId}");
    }

    /// <inheritdoc />
    public async Task<Issue?> UpdateImageUrlAsync(int id, string imageUrl)
    {
        var issue = await _context.Issues.FindAsync(id);
        if (issue == null) return null; // null → Controller'da NotFound

        issue.ImageUrl = imageUrl;
        await _context.SaveChangesAsync();
        return issue;
    }
}
