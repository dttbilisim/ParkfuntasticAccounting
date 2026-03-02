# 🚀 Hızlı Geliştirme Scriptleri

**SORUN:** `dotnet watch` her seferinde tüm bağımlı projeleri (ecommerce.Core, ecommerce.Domain.Shared, vs.) build alıyor ve çok yavaş!

**ÇÖZÜM:** Sadece `ecommerce.Web` projesini build alan özel scriptler oluşturduk.

---

## ⚡ EN HIZLI WORKFLOW (ÖNERİLEN)

### 1️⃣ **İlk Seferlik (Tüm Projeleri Build Et)**
```bash
cd /Users/sezginoztemir/Repos/Projects/ecommerce
dotnet build
```
**Süre:** ~25-30 saniye (sadece ilk seferlik)

### 2️⃣ **Günlük Geliştirme (Sadece Web Build)**
```bash
cd /Users/sezginoztemir/Repos/Projects/ecommerce/ecommerce.Web
./dev.sh
```
**Süre:** ~5-10 saniye! 🚀

### 3️⃣ **Hızlı Test (Build bile yapmadan)**
```bash
cd /Users/sezginoztemir/Repos/Projects/ecommerce/ecommerce.Web
./run-turbo.sh
```
**Süre:** ~2-3 saniye! ⚡

---

## 📋 Script Detayları

### ⭐ **`./dev.sh`** - Günlük Geliştirme İçin
**Ne yapar:**
- ✅ Eski process'leri temizler
- ✅ Port 5100'ü temizler
- ✅ **SADECE** `ecommerce.Web` projesini build eder (bağımlı projeler atlanır)
- ✅ Hot reload aktif
- ✅ Renkli çıktı

**Ne zaman kullan:** Normal geliştirme yaparken

**Parametreler:**
- `--no-build-deps` → Bağımlı projeleri build almaz
- `/p:BuildProjectReferences=false` → Referans edilen projeleri skip eder

---

### 🏎️ **`./run-turbo.sh`** - Hızlı Test İçin
**Ne yapar:**
- ✅ Eski process'leri temizler
- ✅ Port 5100'ü temizler
- ✅ **HİÇBİR ŞEY BUILD ETMEZ** (önceki build'i kullanır)
- ⚠️ Hot reload YOK (kod değişince manuel restart gerekir)

**Ne zaman kullan:** Hızlıca test etmek istediğinde

**Parametreler:**
- `--no-build` → Build atlanır
- `--no-restore` → NuGet restore atlanır

---

### 🔄 **`./watch.sh`** - Basit Mod
**Ne yapar:**
- Normal `dotnet watch` ama sadece Web projesi
- Hot reload aktif

**Ne zaman kullan:** Diğerleri çalışmazsa fallback olarak

---

## 🎯 Kullanım Senaryoları

| Senaryo | Komut | Süre | Açıklama |
|---------|-------|------|----------|
| **İlk başlatma** | `dotnet build` (root'ta) | ~25s | Tüm DLL'leri hazırla |
| **Günlük dev** | `./dev.sh` | ~5-10s | Sadece Web build + hot reload |
| **Hızlı test** | `./run-turbo.sh` | ~2-3s | Hiç build yok, direkt çalıştır |
| **Dependency değişti** | `dotnet build` (root'ta) + `./dev.sh` | ~30s | Önce tüm solution, sonra dev |

---

## 💡 PRO TİPLER

### 🔥 En Hızlı Workflow:
```bash
# Sabah işe başlarken (1 defa):
cd /Users/sezginoztemir/Repos/Projects/ecommerce
dotnet build

# Gün içinde sürekli:
cd ecommerce.Web
./dev.sh
```

### 🐛 Sadece ecommerce.Web kodunu değiştiriyorsan:
```bash
# Build bile yapmadan başlat:
./run-turbo.sh
```

### 🔧 Core/Domain.Shared değiştirdiysen:
```bash
# Root'tan tüm solution build:
cd /Users/sezginoztemir/Repos/Projects/ecommerce
dotnet build

# Sonra dev mode:
cd ecommerce.Web
./dev.sh
```

---

## ⚠️ Port 5100 Meşgul Hatası

Scriptler otomatik temizliyor ama manuel temizlemek istersen:

```bash
# Tüm dotnet process'lerini öldür
pkill -f "dotnet.*watch"
pkill -f "dotnet.*run"
pkill -f "ecommerce.Web"

# Port 5100'ü temizle
lsof -ti:5100 | xargs kill -9
```

---

## 🔧 Yaptığımız Optimizasyonlar

### 1. **`ecommerce.Web.csproj`**
```xml
<UseSharedCompilation>true</UseSharedCompilation>
<BuildInParallel>true</BuildInParallel>
<DebugType>embedded</DebugType>
```

### 2. **`Directory.Build.props`** (Tüm projelere global)
```xml
<UseSharedCompilation>true</UseSharedCompilation>
<BuildInParallel>true</BuildInParallel>
<RestorePackagesWithLockFile>false</RestorePackagesWithLockFile>
```

### 3. **Script Parametreleri**
- `--no-build-deps` → Bağımlı projeleri build almaz
- `/p:BuildProjectReferences=false` → Referansları skip eder
- `--no-build` → Build'i tamamen atlar (turbo mode)

---

## 📊 Performans Karşılaştırması

| Yöntem | Build Süresi | Bağımlı Projeler | Hot Reload |
|--------|--------------|------------------|------------|
| **Eski:** `dotnet watch` | ~60-90s | ✅ Tümü | ✅ |
| **dev.sh** | ~5-10s | ❌ Skip | ✅ |
| **run-turbo.sh** | ~2-3s | ❌ Skip | ❌ |

---

## 🎉 Sonuç

Artık **10 kat daha hızlı** çalışacaksın!

**En çok kullanacağın:**
```bash
cd /Users/sezginoztemir/Repos/Projects/ecommerce/ecommerce.Web
./dev.sh
```

**Son güncelleme:** 2025-10-16
