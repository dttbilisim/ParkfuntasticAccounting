# Admin uygulaması sunucuda açılmıyorsa (500.30)

Deploy başarılı ama site 500.30 veriyorsa aşağıdakileri kontrol et.

## 1. Sunucuda gerçek hatayı gör (stdout log)

Bu projede `web.config` ile stdout log açık. Deploy sonrası:

- **Klasör:** `C:\inetpub\wwwroot\admin.yedeksen.com\logs\`
- **Dosya:** `stdout_*.log` (tarih/saat damgalı)
- Bu dosyada uygulamanın neden çöktüğü yazar (exception mesajı, stack trace).

İlk bakılacak yer burası.

## 2. .NET 9 runtime

Proje **net9.0** hedefliyor. Sunucuda:

- **ASP.NET Core 9.0 Hosting Bundle** kurulu olmalı.
- İndirme: https://dotnet.microsoft.com/download/dotnet/9.0 (Hosting bundle)
- Kurulum sonrası IIS’i yeniden başlat.

Eski sürüm (6/7/8) varsa 500.30 alırsınız.

## 3. Connection string (IIS / ortam)

Uygulama başlarken `ConnectionStrings:ApplicationDbContext` okuyor. Sunucuda:

- **IIS** → Site → Configuration Editor → `connectionStrings` veya
- **Application settings** ile `ConnectionStrings__ApplicationDbContext` değerini verin.

Ya da `appsettings.Production.json` publish çıktısında olacak şekilde ayarlayıp orada connection string’i tanımlayın.

Boş/yanlış olursa açılışta net bir hata fırlatılır (stdout log’ta görünür).

## 4. Redis / PostgreSQL erişimi

Sunucudan:

- PostgreSQL (Hangfire + uygulama DB) ve
- Redis

portlarına erişim olmalı (firewall, farklı sunucudaysa IP/port). İlk istekte veya Hangfire başlarken hata alıyorsanız stdout log’a bakın.

## 5. Klasör izinleri

IIS uygulama havuzu kullanıcısı:

- `C:\inetpub\wwwroot\admin.yedeksen.com` ve
- `C:\inetpub\wwwroot\admin.yedeksen.com\logs`

için okuma (ve logs için yazma) yetkisine sahip olmalı.

---

**Özet:** Önce `logs\stdout_*.log` dosyasına bakın; oradaki exception mesajı nedeni gösterir. En sık nedenler: .NET 9 yüklü değil, connection string yok/yanlış, Redis/DB’ye erişim yok.
