# CityServiceTracker (Bölge Hizmet Yönetimi)

CityServiceTracker, vatandaşların bölgelerindeki altyapı, çevre ve temizlik gibi sorunları yetkililere hızlıca bildirmesini sağlayan ve yetkili personelin bu sorunları takip edip çözüme kavuşturduğu **Uçtan Uca (Full-Stack)** bir otomasyon sistemidir.

## 🚀 Proje Mimarisi ve Kullanılan Teknolojiler

Bu proje, sektör standartları ve kurumsal en iyi uygulamalar (Best Practices) gözetilerek geliştirilmiştir.

### Backend (Arka Plan)
* **Framework:** C# / ASP.NET Core Web API (.NET 8)
* **Veritabanı:** SQL Server & Entity Framework Core (Code-First)
* **Mimari Yaklaşım:** Layered Architecture (Katmanlı Mimari), Repository Pattern
* **Güvenlik:**
  * JWT (JSON Web Token) tabanlı yetkilendirme (`[Authorize(Roles="Staff")]`)
  * BCrypt ile güvenli şifre hash'leme
  * `.env` mimarisi ile kriptografik ortam değişkenleri (DotNetEnv)
* **Hata Yönetimi:** RFC 7807 standartlarında Global Exception Handling Middleware
* **Test:** xUnit ve Moq kullanılarak yazılmış 9 adet birim testi (Unit Test)

### Frontend (Vitrin)
* **Teknolojiler:** HTML5, CSS3, Vanilla JavaScript (Fetch API)
* **Arayüz (UI):** Bootstrap 5 (Bootswatch Flatly & Dark temaları), FontAwesome İkonları
* **Kullanıcı Deneyimi (UX):** SweetAlert2 (Modern bildirimler), Skeleton Loading ekranları
* **Özellikler:** Tarayıcı Geolocation API (GPS Konum) entegrasyonu, Google Haritalar yönlendirmesi

---

## 🛠️ Kurulum ve Çalıştırma

Projeyi kendi bilgisayarınızda çalıştırmak için aşağıdaki adımları izleyin:

### 1. Gereksinimler
* [.NET 8 SDK](https://dotnet.microsoft.com/download)
* SQL Server (veya LocalDB)
* Git

### 2. Projeyi Klonlayın
```bash
git clone https://github.com/KULLANICI_ADINIZ/CityServiceTracker.git
cd CityServiceTracker/CityServiceAPI
```

### 3. Ortam Değişkenlerini (Güvenlik) Ayarlayın
Proje ana dizininde bulunan `.env.example` dosyasının adını `.env` olarak değiştirin veya kopyalayın. İçerisindeki `Jwt__Key` alanına rastgele, güçlü bir parola yazın.
```bash
cp .env.example .env
```

### 4. Veritabanını Hazırlayın
Entity Framework Core araçlarını kullanarak veritabanını oluşturun ve tohum verileri (seed data) yükleyin:
```bash
dotnet ef database update
```
*(Not: Bu işlem veritabanını oluşturur ve `admin@belediye.gov.tr` (Şifre: `admin123`) kullanıcısını otomatik olarak ekler).*

### 5. Projeyi Çalıştırın
```bash
dotnet run
```
API ayağa kalktığında tarayıcınızdan `http://localhost:<port>` veya `https://localhost:<port>` adresine giderek **Vatandaş Portalı** ve **Yönetim Paneli** yönlendirme ekranına (Landing Page) erişebilirsiniz.

---

## 📸 Ekran Görüntüleri ve Kullanım

* **Vatandaş Portalı:** Kullanıcılar kayıt olmadan, GPS konumlarıyla birlikte anında sorun (çukur, arıza vb.) bildirebilirler.
* **Personel Paneli:** JWT kimlik doğrulaması ile giriş yapan yetkililer, bekleyen sorunları haritada görüntüleyip görev olarak üstlenebilirler.

---

## 🧪 Testleri Çalıştırma

Projenin iş mantığını (Repository ve Controller) doğrulayan testleri çalıştırmak için:
```bash
cd ../CityServiceAPI.Tests
dotnet test
```

## 📄 Lisans
Bu proje MIT Lisansı ile lisanslanmıştır. Staj ve iş başvurularında portföy olarak sergilenebilir.
