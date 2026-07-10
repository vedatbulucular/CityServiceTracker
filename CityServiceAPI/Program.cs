using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CityServiceAPI.Data;
using CityServiceAPI.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════
// 1. VERİTABANI
// ═══════════════════════════════════════════════════════════════
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ═══════════════════════════════════════════════════════════════
// 2. REPOSITORY PATTERN — DEPENDENCY INJECTION KAYDI
//    "IIssueRepository istendi mi? IssueRepository örneğini ver."
//    Scoped: Her HTTP isteği için ayrı bir örnek oluşturulur
//    (EF Core DbContext ile aynı yaşam döngüsü — bu önemli).
// ═══════════════════════════════════════════════════════════════
builder.Services.AddScoped<IIssueRepository, IssueRepository>();

// ═══════════════════════════════════════════════════════════════
// 3. JWT KİMLİK DOĞRULAMA
// ═══════════════════════════════════════════════════════════════
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// ═══════════════════════════════════════════════════════════════
// 4. DİĞER SERVİSLER
// ═══════════════════════════════════════════════════════════════
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

// ═══════════════════════════════════════════════════════════════
// 5. GLOBAL EXCEPTION HANDLER (Middleware Pipeline'ının İLK sırası)
//
//    Neden önemli?
//    • Controller'lardaki try-catch bloklarını kaldırır — tek merkezden yönetim.
//    • ex.Message veya stack trace asla istemciye sızmaz (güvenlik).
//    • Standart RFC 7807 "Problem Details" formatında JSON döner.
//    • Benzersiz TraceId üretir — log dosyalarında hatayı takip etmeni sağlar.
//    • Development modunda detay gösterir, Production'da sadece genel mesaj.
// ═══════════════════════════════════════════════════════════════
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        // Orijinal exception'ı middleware pipeline'dan al
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        // Loglama: Gerçek hata detayı sadece sunucu loglarına gider, istemciye değil
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        logger.LogError(exception,
            "İşlenmeyen hata yakalandı. TraceId: {TraceId} | Path: {Path}",
            traceId, context.Request.Path);

        // İstemciye dönecek HTTP status kodunu belirle
        var statusCode = exception switch
        {
            // Buraya özel exception tipleri ekleyebilirsin:
            // NotFoundException => HttpStatusCode.NotFound,
            // UnauthorizedAccessException => HttpStatusCode.Forbidden,
            _ => HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        // RFC 7807 Problem Details formatı — endüstri standardı hata response yapısı
        await context.Response.WriteAsJsonAsync(new
        {
            Status  = context.Response.StatusCode,
            Error   = "Sunucu tarafında bir hata oluştu.",
            TraceId = traceId,   // Bu ID ile log dosyasında asıl hatayı bulabilirsin
            // Development ortamında detay ekle, Production'da ekleme
            Detail  = app.Environment.IsDevelopment() ? exception?.Message : null
        });
    });
});

// ═══════════════════════════════════════════════════════════════
// 6. MIDDLEWARE PIPELINE SIRASI (sıra kritik — değiştirme)
// ═══════════════════════════════════════════════════════════════
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication(); // Önce kim olduğunu belirle
app.UseAuthorization();  // Sonra yetkisini kontrol et

app.MapControllers();

app.Run();