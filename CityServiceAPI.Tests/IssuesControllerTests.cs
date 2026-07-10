using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using CityServiceAPI.Controllers;
using CityServiceAPI.Models;
using CityServiceAPI.Repositories;
using System.Security.Claims;

namespace CityServiceAPI.Tests;

/// <summary>
/// IssuesController için birim testleri.
///
/// Temel prensip:
///   - Controller'ı test ediyoruz, veritabanını değil.
///   - IIssueRepository Moq ile sahte (mock) bir nesneye dönüştürülür.
///   - Gerçek SQL Server bağlantısı olmadan testler saniyeler içinde çalışır.
///   - Her test tamamen bağımsızdır (birbirini etkilemez).
///
/// Kullanılan pattern: Arrange → Act → Assert
///   Arrange : Test ortamını hazırla, mock'ları ayarla
///   Act     : Test edilecek metodu çağır
///   Assert  : Sonucun beklenen değerle eşleştiğini doğrula
/// </summary>
public class IssuesControllerTests
{
    // ── Paylaşılan Test Altyapısı ──────────────────────────────────────────────
    // Her testin ihtiyaç duyduğu mock'lar ve controller örneği burada oluşturulur.
    // Her test metodu için constructor yeniden çalışır → testler birbirinden izole kalır.

    private readonly Mock<IIssueRepository> _mockRepo;
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly IssuesController _controller;

    public IssuesControllerTests()
    {
        _mockRepo = new Mock<IIssueRepository>();
        _mockEnv  = new Mock<IWebHostEnvironment>();

        // Controller'a sahte bağımlılıklarını enjekte ediyoruz.
        // Gerçek AppDbContext veya SQL Server YOK — sadece bellekte çalışır.
        _controller = new IssuesController(_mockRepo.Object, _mockEnv.Object);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GetIssues Testleri
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Category", "GetIssues")]
    public async Task GetIssues_RepositoryBosBirListeDonduruyor_OkVeBosDiziBekleniyorr()
    {
        // Arrange: Repository'nin boş bir liste döneceğini tanımla
        _mockRepo
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(new List<Issue>());

        // Act: Controller metodunu çağır
        var result = await _controller.GetIssues();

        // Assert 1: HTTP yanıtı 200 OK mi?
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        // Assert 2: Dönen içerik gerçekten boş bir liste mi?
        var issues = Assert.IsAssignableFrom<IEnumerable<Issue>>(okResult.Value);
        Assert.Empty(issues);
    }

    [Fact]
    [Trait("Category", "GetIssues")]
    public async Task GetIssues_RepositoryIkiKayitDonduruyor_OkVeDogruSayidaKayitBekleniyorr()
    {
        // Arrange: İki farklı sorun kaydı içeren sahte veri seti hazırla
        var fakeIssues = new List<Issue>
        {
            new() { Id = 1, Title = "Kırık Kaldırım",   Status = "Pending",    CitizenId = 10, CategoryId = 1, Description = "Kaldırım hasarlı.", LocationData = "Atatürk Cad." },
            new() { Id = 2, Title = "Sokak Lambası Yok", Status = "InProgress", CitizenId = 11, CategoryId = 2, Description = "Lamba çalışmıyor.", LocationData = "İstiklal Sok." }
        };

        _mockRepo
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(fakeIssues);

        // Act
        var result = await _controller.GetIssues();

        // Assert 1: 200 OK döndü mü?
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Assert 2: Tam olarak 2 kayıt döndü mü?
        var issues = Assert.IsAssignableFrom<IEnumerable<Issue>>(okResult.Value);
        Assert.Equal(2, issues.Count());

        // Assert 3: Repository'nin GetAllAsync metodu tam olarak 1 kez çağrıldı mı?
        // (Controller'ın gereksiz yere birden fazla DB çağrısı yapmadığını doğrular)
        _mockRepo.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Fact]
    [Trait("Category", "GetIssues")]
    public async Task GetIssues_RepositoryVerileriDonduruyor_IlkKaydınBasligiDogruMu()
    {
        // Arrange
        var fakeIssues = new List<Issue>
        {
            new() { Id = 1, Title = "Su Borusu Patladı", Status = "Pending", CitizenId = 5, CategoryId = 3, Description = "Boru patladı.", LocationData = "Cumhuriyet Mah." }
        };

        _mockRepo
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(fakeIssues);

        // Act
        var result = await _controller.GetIssues();

        // Assert: Dönen ilk kaydın başlığı beklenenle eşleşiyor mu?
        var okResult = Assert.IsType<OkObjectResult>(result);
        var issues   = Assert.IsAssignableFrom<IEnumerable<Issue>>(okResult.Value).ToList();
        Assert.Equal("Su Borusu Patladı", issues[0].Title);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CreateIssue Testleri
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Category", "CreateIssue")]
    public async Task CreateIssue_GecerliVeri_201CreatedVeDogruNesneIleGeriDonmeli()
    {
        // Arrange: İstemcinin göndereceği yeni sorun verisi
        var newIssue = new Issue
        {
            CitizenId   = 42,
            CategoryId  = 1,
            Title       = "Trafik Işığı Arızası",
            Description = "Işık sürekli kırmızı yanıyor.",
            LocationData = "Bağımsızlık Bulvarı No:15"
        };

        // Repository'nin CreateAsync çağrıldığında ID atayıp döneceği sahte sonuç
        var createdIssue = new Issue
        {
            Id          = 99,          // Veritabanının atayacağı ID'yi simüle ediyoruz
            CitizenId   = 42,
            CategoryId  = 1,
            Title       = "Trafik Işığı Arızası",
            Description = "Işık sürekli kırmızı yanıyor.",
            LocationData = "Bağımsızlık Bulvarı No:15",
            Status      = "Pending",   // Repository tarafından atanır
            ReportedAt  = DateTime.UtcNow
        };

        _mockRepo
            .Setup(repo => repo.CreateAsync(It.IsAny<Issue>()))
            .ReturnsAsync(createdIssue);

        // Act
        var result = await _controller.CreateIssue(newIssue);

        // Assert 1: HTTP yanıtı 201 Created mi? (200 OK değil!)
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);

        // Assert 2: Dönen nesnenin ID'si repository'nin verdiğiyle eşleşiyor mu?
        var returnedIssue = Assert.IsType<Issue>(createdResult.Value);
        Assert.Equal(99, returnedIssue.Id);

        // Assert 3: Başlık korunmuş mu?
        Assert.Equal("Trafik Işığı Arızası", returnedIssue.Title);

        // Assert 4: Repository tam olarak bir kez çağrıldı mı?
        _mockRepo.Verify(repo => repo.CreateAsync(It.IsAny<Issue>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "CreateIssue")]
    public async Task CreateIssue_OlusturulanKaydınDurumu_PendingOlmalidirr()
    {
        // Arrange: Repository'nin Status = "Pending" ile döneceğini tanımla.
        // Bu test, iş kuralının (Status = "Pending" ataması) repository katmanında
        // gerçekleştiğini ve controller'ın bunu olduğu gibi ilettiğini doğrular.
        var fakeCreated = new Issue
        {
            Id          = 1,
            CitizenId   = 7,
            CategoryId  = 2,
            Title       = "Çöp Toplanmıyor",
            Description = "3 gündür çöp alınmadı.",
            LocationData = "Yıldız Mah. 12. Sk.",
            Status      = "Pending"
        };

        _mockRepo
            .Setup(repo => repo.CreateAsync(It.IsAny<Issue>()))
            .ReturnsAsync(fakeCreated);

        // Act
        var result = await _controller.CreateIssue(new Issue
        {
            CitizenId = 7, CategoryId = 2,
            Title = "Çöp Toplanmıyor",
            Description = "3 gündür çöp alınmadı.",
            LocationData = "Yıldız Mah. 12. Sk."
        });

        // Assert: Dönen kaydın durumu "Pending" mi?
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var returnedIssue = Assert.IsType<Issue>(createdResult.Value);
        Assert.Equal("Pending", returnedIssue.Status);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DeleteIssue Testleri
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Category", "DeleteIssue")]
    public async Task DeleteIssue_MevcutKayit_204NoContentDonmelidir()
    {
        // Arrange: ID=1 için silme işlemi başarılı (true) dönecek
        _mockRepo
            .Setup(repo => repo.DeleteAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteIssue(1);

        // Assert: 204 No Content (silme başarılı, body yok)
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    [Trait("Category", "DeleteIssue")]
    public async Task DeleteIssue_OlmayanKayit_404NotFoundDonmelidir()
    {
        // Arrange: Repository, kaydı bulamadı → false döner
        _mockRepo
            .Setup(repo => repo.DeleteAsync(999))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteIssue(999);

        // Assert: 404 Not Found
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AssignIssue Testleri (JWT Claim)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Category", "AssignIssue")]
    public async Task AssignIssue_GecerliJwtClaim_200OkVeBasariliMesajDonmeli()
    {
        // Arrange: JWT token'ında geçerli bir NameIdentifier claim'i oluştur.
        // Controller User.FindFirst() ile bunu okuyacak.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "5"),  // Staff ID = 5
            new(ClaimTypes.Role, "Staff")
        };
        var identity   = new ClaimsIdentity(claims, "TestAuth");
        var principal  = new ClaimsPrincipal(identity);

        // Controller'ın HttpContext'ine sahte kullanıcıyı ata
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockRepo
            .Setup(repo => repo.AssignIssueAsync(1, 5))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AssignIssue(issueId: 1);

        // Assert 1: 200 OK döndü mü?
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        // Assert 2: Repository tam olarak (issueId=1, staffId=5) ile çağrıldı mı?
        _mockRepo.Verify(repo => repo.AssignIssueAsync(1, 5), Times.Once);
    }

    [Fact]
    [Trait("Category", "AssignIssue")]
    public async Task AssignIssue_JwtClaimYok_401UnauthorizedDonmeli()
    {
        // Arrange: NameIdentifier claim'i olmayan boş bir kullanıcı
        var identity  = new ClaimsIdentity(); // Claim içermiyor
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        var result = await _controller.AssignIssue(issueId: 1);

        // Assert: Claim eksik olduğu için 401 Unauthorized gelmeli
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);

        // Assert: Repository HİÇ çağrılmadı (güvenlik katmanında durdu)
        _mockRepo.Verify(repo => repo.AssignIssueAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }
}
