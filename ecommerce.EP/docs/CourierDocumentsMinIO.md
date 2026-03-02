# Kurye Belgeleri: MinIO ile Depolama

Kurye başvuru belgeleri (Vergi Levhası, İmza Beyannamesi, Kimlik Fotokopisi, Sabıka Kaydı) şu an `CourierDocumentUploadService` ile sunucu diski (`FileHelper.GetUploadPath()` + `CourierDocuments/`) klasörüne yazılıyor. MinIO kullanmak için aşağıdakiler yeterli.

## Access Key ve Secret Key nasıl alınır?

MinIO'da API erişimi için **Access Key** ve **Secret Key** kullanılır. İki yöntem:

### 1. MinIO Web Konsolu (örn. http://92.204.172.3:9040)

1. Tarayıcıda MinIO Console adresine gidin (sizin kurulumda port 9040).
2. MinIO admin kullanıcı adı/şifresi ile giriş yapın (varsayılan: `minioadmin` / `minioadmin`).
3. Sol menüden **Access Keys** (veya **Identity** → **Access Keys**) bölümüne girin.
4. **Create access key** ile yeni bir anahtar çifti oluşturun.
5. Açılan pencerede **Access Key** ve **Secret Key** gösterilir; Secret Key sadece bir kez gösterilir. İkisini kopyalayıp `appsettings.json` içindeki `MinIO:AccessKey` ve `MinIO:SecretKey` alanlarına yapıştırın.

### 2. MinIO Client (mc) ile

Sunucuda veya MinIO erişimi olan bir makinede:

```bash
mc alias set myminio http://92.204.172.3:9040 MINIO_ADMIN_USER MINIO_ADMIN_PASSWORD
mc admin user svcacct add myminio MINIO_ADMIN_USER --access-key "courier-app-key" --secret-key "GÜÇLÜ_GİZLİ_ANAHTAR"
```

Oluşan access key ve secret key değerlerini `appsettings.json` → `MinIO:AccessKey` ve `MinIO:SecretKey` alanlarına yazın.

**Port notu:** S3 API genelde 9000 portunda çalışır; web konsol 9040 ise MinIO’yu 9040’ta da dinleyecek şekilde ayarladıysanız `appsettings`’te `Port: 9040` kullanın. Aksi halde API için `Port: 9000` deneyin.

## Sizin sağlamanız gerekenler (MinIO sunucu)

1. **Endpoint** – MinIO adresi (örn. `http://minio.sirket.local:9000` veya `https://minio.example.com`)
2. **Access Key** – MinIO kullanıcı erişim anahtarı
3. **Secret Key** – MinIO gizli anahtar
4. **Bucket adı** – Belgelerin atılacağı bucket (örn. `courier-documents`)

MinIO S3 uyumlu olduğu için .NET tarafında **AWSSDK.S3** veya **Minio** NuGet paketi kullanılabilir.

## Backend’de yapılacaklar

1. **NuGet**
   - `Minio` paketini ekleyin:  
     `dotnet add ecommerce.EP package Minio`

2. **Konfigürasyon** – `appsettings.json`:
   ```json
   "MinIO": {
     "Endpoint": "http://minio.sirket.local:9000",
     "AccessKey": "minio-access-key",
     "SecretKey": "minio-secret-key",
     "BucketName": "courier-documents",
     "UseSSL": false
   }
   ```

3. **Servis**
   - `ICourierDocumentUploadService` implementasyonu:
     - `SaveAsync(IFormFile)`: Stream’i MinIO’ya `PutObjectAsync(bucket, objectKey, stream, contentType)` ile yükleyin.
     - Object key: örn. `courier-documents/{applicationId veya guid}/{dosya-adi}` (benzersiz olsun).
     - Dönüş: Veritabanına yazılacak **object key** (veya tam path); örn. `courier-documents/abc123/vergi-levhasi.jpg`.

4. **Okuma (indirme / görüntüleme)**
   - **Presigned URL:** MinIO client ile `PresignedGetObjectAsync(bucket, objectKey, expirySeconds)` ile geçici indirme linki üretin. Admin veya mobil bu URL’e GET atarak dosyayı açar.
   - **API üzerinden:** Bir controller action’da (örn. `GET /api/CourierApplication/{id}/document/{documentType}`) MinIO’dan `GetObjectAsync` ile stream alıp `File(stream, contentType)` veya `FileStreamResult` dönün. Yetki kontrolü burada yapılır.

5. **Mevcut davranış**
   - Şu an DB’de `TaxPlatePath`, `SignatureDeclarationPath`, `IdCopyPath`, `CriminalRecordPath` **dosya yolu** (relative path) tutuluyor.
   - MinIO’da saklarken bu alanlara **object key** yazılabilir (örn. `courier-documents/guid/vergi.jpg`). Okuma tarafında bu key ile presigned URL veya proxy endpoint kullanırsınız.

## Presigned URL'de dış adres (SignatureDoesNotMatch önleme)

Sunucu MinIO'ya dahili IP (örn. 192.168.1.10) ile bağlanıyorsa, üretilen presigned URL'ler de o adresi içerir. Tarayıcıda link dış IP (örn. 92.204.172.3) ile açıldığında MinIO SignatureDoesNotMatch döner; imza URL'deki host ile hesaplanır. Bunu çözmek için EndpointForPresignedUrls (ve isteğe bağlı PortForPresignedUrls, UseSSLForPresignedUrls) kullanın. Örnek: Endpoint: 192.168.1.10, EndpointForPresignedUrls: 92.204.172.3, dışarıda HTTPS ise UseSSLForPresignedUrls: true.

## Özet

| Ne           | Nerede / Nasıl |
|-------------|-----------------|
| Yazma       | `CourierDocumentUploadService.SaveAsync` → MinIO `PutObjectAsync`, dönen key DB’e yazılır. |
| Okuma       | Presigned URL veya yeni bir API endpoint ile MinIO’dan stream alıp döndürün. |
| Konfig      | `appsettings.json` → `MinIO:Endpoint`, `AccessKey`, `SecretKey`, `BucketName`. İç/dış IP ayrımı için isteğe bağlı `EndpointForPresignedUrls`, `PortForPresignedUrls`, `UseSSLForPresignedUrls`. |
| Paket       | `Minio` NuGet (veya AWSSDK.S3). |

İsterseniz bir sonraki adımda `MinioCourierDocumentUploadService` sınıfını ve `appsettings` örneğini doğrudan proje içinde ekleyebilirim; MinIO endpoint, access key, secret key ve bucket adınızı yazmanız yeterli.
