# ⚡ HIZLI BAŞLATMA REHBERİ

## 🎯 3 Basit Adım

### 1️⃣ Sabah İlk Başlatma (Günde 1 defa)
```bash
cd /Users/sezginoztemir/Repos/Projects/ecommerce
dotnet build
```
⏱️ **Süre:** ~25 saniye

---

### 2️⃣ Gün İçinde Her Başlatma
```bash
cd /Users/sezginoztemir/Repos/Projects/ecommerce/ecommerce.Web
./dev.sh
```
⏱️ **Süre:** ~5-10 saniye ✅

**VEYA daha hızlı (hot reload yok):**
```bash
./run-turbo.sh
```
⏱️ **Süre:** ~2-3 saniye 🚀

---

### 3️⃣ Restart (Uygulama açıkken)
```bash
# Terminal'de Ctrl+C bas
# Sonra:
./restart.sh
```
⏱️ **Süre:** ~2 saniye ⚡

---

## 🤔 Hangi Script'i Kullanmalıyım?

| Durum | Script | Neden |
|-------|--------|-------|
| **Normal geliştirme** | `./dev.sh` | Hot reload + Hızlı |
| **Sadece test edeceğim** | `./run-turbo.sh` | En hızlı başlatma |
| **Hızlı restart** | `./restart.sh` | Build yok, direkt başlat |
| **Core/Domain değiştirdim** | `dotnet build` (root) | Tüm DLL'leri yenile |

---

## 💾 Geliştirme Kuralları

### ✅ SADECE ecommerce.Web değiştiriyorsan:
→ `./dev.sh` veya `./run-turbo.sh` kullan

### ⚠️ Common/ecommerce.Core değiştirdiysen:
→ Önce root'tan `dotnet build` yap, sonra `./dev.sh`

### ⚠️ ecommerce.Domain.Shared değiştirdiysen:
→ Önce root'tan `dotnet build` yap, sonra `./dev.sh`

---

## 🎉 Hız Karşılaştırması

| Yöntem | Önceki | Şimdi | İyileşme |
|--------|--------|-------|----------|
| **İlk başlatma** | ~90s | ~5s | **18x HIZLI** |
| **Restart** | ~60s | ~2s | **30x HIZLI** |
| **Test** | ~60s | ~3s | **20x HIZLI** |

---

## 🚨 Sorun Giderme

### Port 5100 meşgul:
```bash
pkill -f "dotnet"
lsof -ti:5100 | xargs kill -9
```

### Build hatası:
```bash
cd /Users/sezginoztemir/Repos/Projects/ecommerce
dotnet clean
dotnet build
```

### I18N/Localization hatası:
Uygulama başladıysa `Program.cs` içinde I18N servisi DI'a kayıtlı ✅

---

**İyi kodlamalar! 🚀**

