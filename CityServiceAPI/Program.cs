using Microsoft.EntityFrameworkCore;
using CityServiceAPI.Data; // AppDbContext'in bulunduğu klasör

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabanı bağlantımızı (AppDbContext) sisteme servis olarak ekliyoruz
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. OpenAPI (API Dokümantasyonu) servisini ekliyoruz
builder.Services.AddOpenApi();

builder.Services.AddControllers();

// Uygulamayı inşa ediyoruz
var app = builder.Build();

// HTTP istek boru hattını (pipeline) yapılandırıyoruz
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

// Gelecekte Endpoint'lerimizi veya Controller'larımızı buraya ekleyeceğiz

app.Run();
