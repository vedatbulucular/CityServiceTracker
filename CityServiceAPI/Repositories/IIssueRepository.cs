using CityServiceAPI.Models;

namespace CityServiceAPI.Repositories;

/// <summary>
/// Issue (Sorun Bildirimi) verilerine erişimin sözleşmesi.
/// Controller bu interface'e bağlanır; EF Core implementasyonu
/// Dependency Injection tarafından enjekte edilir.
/// Bu sayede Controller'ı gerçek veritabanı olmadan test edebilirsin
/// (mock bir IIssueRepository yazman yeterli).
/// </summary>
public interface IIssueRepository
{
    /// <summary>Sistemdeki tüm bildirimleri listeler.</summary>
    Task<IEnumerable<Issue>> GetAllAsync();

    /// <summary>ID'ye göre tek bir bildirimi getirir; bulunamazsa null döner.</summary>
    Task<Issue?> GetByIdAsync(int id);

    /// <summary>Yeni bir bildirim oluşturur ve veritabanına kaydeder.</summary>
    Task<Issue> CreateAsync(Issue issue);

    /// <summary>
    /// Mevcut bir bildirimi günceller.
    /// ID uyuşmazlığında false döner (BadRequest için).
    /// Kayıt bulunamazsa null döner (NotFound için).
    /// </summary>
    Task<Issue?> UpdateAsync(int id, Issue updatedIssue);

    /// <summary>
    /// Bildirimi ve ilgili tüm atamaları siler.
    /// Kayıt bulunamazsa false döner.
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// sp_AssignIssue stored procedure'ünü çağırarak görevi bir personele atar.
    /// Stored procedure içinde atomik olarak çalışır.
    /// </summary>
    Task AssignIssueAsync(int issueId, int staffId);

    /// <summary>
    /// Bir bildirimin fotoğrafını günceller.
    /// Kayıt bulunamazsa null döner.
    /// </summary>
    Task<Issue?> UpdateImageUrlAsync(int id, string imageUrl);
}
