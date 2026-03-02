#!/bin/bash

# SellerProduct Elasticsearch Indexing Cron Setup
# Bu script'i sadece bir kez çalıştırın

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
LOG_DIR="/var/log/sellerproduct"

# Log dizini oluştur
echo "📁 Creating log directory..."
sudo mkdir -p $LOG_DIR
sudo chmod 755 $LOG_DIR

# Cron job ekle
echo "⏰ Setting up cron job..."

# Mevcut crontab'ı yedekle
crontab -l > /tmp/crontab_backup_$(date +%Y%m%d_%H%M%S).txt 2>/dev/null

# Yeni cron job (her saat başı çalışır - incremental mode)
CRON_JOB="0 * * * * cd $SCRIPT_DIR && /usr/bin/python3 seller.py >> $LOG_DIR/incremental.log 2>&1"

# Cron job'u ekle (eğer yoksa)
(crontab -l 2>/dev/null | grep -v "seller.py"; echo "$CRON_JOB") | crontab -

echo "✅ Cron job eklendi:"
echo "   $CRON_JOB"
echo ""
echo "📋 Kurulum tamamlandı!"
echo ""
echo "🔧 Kullanım:"
echo "   - İlk çalıştırma (FULL INDEX):  python3 seller.py --full"
echo "   - Manuel incremental:           python3 seller.py"
echo "   - Log dosyası:                  tail -f $LOG_DIR/incremental.log"
echo "   - Cron job listesi:             crontab -l"
echo ""
echo "⚠️  ÖNEMLİ: İlk çalıştırmayı şimdi yapın:"
echo "   cd $SCRIPT_DIR && python3 seller.py --full"

