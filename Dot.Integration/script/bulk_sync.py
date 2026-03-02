#!/usr/bin/env python3
"""
⚡ BULK SENKRONIZASYON - .NET DatBulkSyncService COMPLETE PORT
ProcessPartsBulkAsync retry logic dahil TÜM fonksiyonlar
"""

import requests
import psycopg2
import sys
from datetime import datetime
import xml.etree.ElementTree as ET
from typing import List, Dict, Optional
import time
import re

DB_CONFIG = {
    'host': 'localhost',
    'port': 5454,
    'user': 'myinsurer',
    'password': 'Posmdh0738',
    'database': 'MarketPlace'
}

DAT_CONFIG = {
    'auth_url': 'https://www.datgroup.com/VehicleRepairOnline/services/Authentication',
    'vehicle_url': 'https://www.datgroup.com/myClaim/soap/v2/VehicleSelectionService',
    'customer_number': '3800957',
    'customer_login': 'kardelenoto',
    'customer_password': 'Kardelen.Oto.0825',
    'interface_partner_number': '3800957',
    'interface_partner_signature': '3D63C087ED7E69B21F0ED593622EE02041D5CD966A079A787F0AADEB709B7545',
    'locale_country': 'tr',
    'locale_language': 'tr',
}

class DatApiClient:
    def __init__(self):
        self.token = None
        
    def get_token(self):
        print("🔑 DAT token alınıyor...")
        body = f'''<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:aut="http://sphinx.dat.de/services/Authentication">
   <soapenv:Header/>
   <soapenv:Body>
      <aut:generateToken>
         <request>
            <customerNumber>{DAT_CONFIG['customer_number']}</customerNumber>
            <customerLogin>{DAT_CONFIG['customer_login']}</customerLogin>
            <customerPassword>{DAT_CONFIG['customer_password']}</customerPassword>
            <interfacePartnerNumber>{DAT_CONFIG['interface_partner_number']}</interfacePartnerNumber>
            <interfacePartnerSignature>{DAT_CONFIG['interface_partner_signature']}</interfacePartnerSignature>
         </request>
      </aut:generateToken>
   </soapenv:Body>
</soapenv:Envelope>'''
        
        resp = requests.post(DAT_CONFIG['auth_url'], data=body, headers={'Content-Type': 'text/xml; charset=utf-8'})
        resp.raise_for_status()
        
        root = ET.fromstring(resp.text)
        token_elem = root.find('.//{http://sphinx.dat.de/services/Authentication}token') or root.find('.//token')
        
        if token_elem is not None and token_elem.text:
            self.token = token_elem.text
            print(f"✅ Token alındı!")
            return self.token
        raise Exception("Token bulunamadı!")
    
    def soap(self, body_xml):
        envelope = f'''<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:veh="http://sphinx.dat.de/services/VehicleSelectionService">
   <soapenv:Header>
      <DAT-AuthorizationToken>{self.token}</DAT-AuthorizationToken>
   </soapenv:Header>
   <soapenv:Body>
      {body_xml}
   </soapenv:Body>
</soapenv:Envelope>'''
        return requests.post(DAT_CONFIG['vehicle_url'], data=envelope, headers={'Content-Type': 'text/xml; charset=utf-8'})


class DB:
    def __init__(self):
        self.conn = None
        
    def connect(self):
        print("💾 DB'ye bağlanılıyor...")
        self.conn = psycopg2.connect(**DB_CONFIG)
        self.conn.autocommit = True
        print("✅ DB bağlantısı OK!")
        
    def close(self):
        if self.conn:
            self.conn.close()
            print("\n💾 DB kapatıldı")
    
    def save_vts(self, vts):
        cur = self.conn.cursor()
        for vt in vts:
            cur.execute('''INSERT INTO "DotVehicleTypes" ("DatId", "Name", "CreatedDate", "LastSyncDate", "IsActive")
                VALUES (%s, %s, %s, %s, %s) ON CONFLICT ("DatId") 
                DO UPDATE SET "Name" = EXCLUDED."Name", "LastSyncDate" = EXCLUDED."LastSyncDate"
            ''', (str(vt['key']), vt['value'], datetime.utcnow(), datetime.utcnow(), True))
        print(f"✅ {len(vts)} VT kaydedildi!\n")
    
    def save_mans(self, mans, vt):
        if not mans: return
        cur = self.conn.cursor()
        for m in mans:
            cur.execute('''INSERT INTO "DotManufacturers" ("DatKey", "Name", "VehicleType", "CreatedDate", "LastSyncDate", "IsActive")
                VALUES (%s, %s, %s, %s, %s, %s) ON CONFLICT ("DatKey", "VehicleType")
                DO UPDATE SET "Name" = EXCLUDED."Name", "LastSyncDate" = EXCLUDED."LastSyncDate"
            ''', (str(m['key']), m['value'], vt, datetime.utcnow(), datetime.utcnow(), True))

    def save_bms(self, bms, vt, mk):
        if not bms: return
        cur = self.conn.cursor()
        for bm in bms:
            cur.execute('''INSERT INTO "DotBaseModels" ("DatKey", "Name", "VehicleType", "ManufacturerKey", "RepairIncomplete", "CreatedDate", "LastSyncDate", "IsActive")
                VALUES (%s, %s, %s, %s, %s, %s, %s, %s) ON CONFLICT ("DatKey", "VehicleType", "ManufacturerKey")
                DO UPDATE SET "Name" = EXCLUDED."Name", "LastSyncDate" = EXCLUDED."LastSyncDate"
            ''', (bm['key'], bm['value'], vt, mk, False, datetime.utcnow(), datetime.utcnow(), True))

    def save_sms(self, sms, vt, mk, bk):
        if not sms: return
        cur = self.conn.cursor()
        for sm in sms:
            cur.execute('''INSERT INTO "DotSubModels" ("DatKey", "Name", "VehicleType", "ManufacturerKey", "BaseModelKey", "CreatedDate", "LastSyncDate", "IsActive")
                VALUES (%s, %s, %s, %s, %s, %s, %s, %s) ON CONFLICT ("DatKey", "VehicleType", "ManufacturerKey", "BaseModelKey")
                DO UPDATE SET "Name" = EXCLUDED."Name", "LastSyncDate" = EXCLUDED."LastSyncDate"
            ''', (sm['key'], sm['value'], vt, mk, bk, datetime.utcnow(), datetime.utcnow(), True))

    def get_options_from_db(self, vt, mk, bk, sk):
        """DotOptions tablosundan classification bazlı options al - .NET GetOptionsGroupedByClassificationAsync"""
        cur = self.conn.cursor()
        cur.execute('''
            SELECT "Classification", "DatKey"
            FROM "DotOptions"
            WHERE "VehicleType" = %s AND "ManufacturerKey" = %s 
            AND "BaseModelKey" = %s AND "SubModelKey" = %s
            AND "IsActive" = true
            ORDER BY "Classification", "DatKey"
        ''', (vt, mk, bk, sk))
        
        rows = cur.fetchall()
        grouped = {}
        for classification, datkey in rows:
            if classification not in grouped:
                grouped[classification] = []
            grouped[classification].append(datkey)
        
        return grouped

    def save_compiled_code(self, ecode, vt, mk, bk, sk):
        cur = self.conn.cursor()
        cur.execute('''INSERT INTO "DotCompiledCodes" ("DatECode", "VehicleType", "ManufacturerKey", "BaseModelKey", "SubModelKey", "CreatedDate", "IsActive")
            VALUES (%s, %s, %s, %s, %s, %s, %s) ON CONFLICT ("DatECode") DO NOTHING
        ''', (ecode, vt, mk, bk, sk, datetime.utcnow(), True))

    def save_images(self, ecode, imgs):
        if not imgs: return
        cur = self.conn.cursor()
        for img in imgs:
            cur.execute('''INSERT INTO "DotVehicleImages" ("DatECode", "Aspect", "ImageType", "ImageFormat", "ImageBase64", "CreatedDate")
                VALUES (%s, %s, %s, %s, %s, %s) ON CONFLICT ("DatECode", "Aspect", "ImageType") DO NOTHING
            ''', (ecode, img['aspect'], img['imageType'], img['imageFormat'], img['imageBase64'], datetime.utcnow()))


def parse_xml(resp, tag, ns='http://sphinx.dat.de/services/VehicleSelectionService'):
    """Generic XML parser - namespace agnostic"""
    root = ET.fromstring(resp.text)
    items = []
    
    # namespace ile dene
    for elem in root.findall(f'.//{{{ns}}}{tag}'):
        if elem.get('key'):
            items.append({'key': elem.get('key'), 'value': elem.get('value')})
        elif elem.text:
            items.append(int(elem.text))
    
    # namespace olmadan dene
    if not items:
        for elem in root.findall(f'.//{tag}'):
            if elem.get('key'):
                items.append({'key': elem.get('key'), 'value': elem.get('value')})
            elif elem.text:
                items.append(int(elem.text))
    
    return items


def main():
    print("="*60)
    print("⚡ BULK SYNC - COMPLETE .NET PORT")
    print("="*60)
    
    start = datetime.now()
    api = DatApiClient()
    db = DB()
    
    stats = {'vts': 0, 'mans': 0, 'bms': 0, 'sms': 0, 'opts': 0, 'ecodes': 0, 'imgs': 0}
    
    try:
        db.connect()
        api.get_token()
        
        # VehicleTypes
        print("\n🚗 VehicleTypes...")
        resp = api.soap('<veh:getVehicleTypes><request><locale country="tr" datCountryIndicator="tr" language="tr"/><restriction>ALL</restriction></request></veh:getVehicleTypes>')
        resp.raise_for_status()
        vts = parse_xml(resp, 'vehicleType')
        db.save_vts(vts)
        stats['vts'] = len(vts)
        
        # İlk VT için test
        for vt in vts[:1]:
            print(f"\n📊 VT:{vt['key']} {vt['value']}")
            
            # Manufacturers
            resp = api.soap(f'<veh:getManufacturers><request><locale country="tr" datCountryIndicator="tr" language="tr"/><constructionTimeFrom>4040</constructionTimeFrom><constructionTimeTo>4840</constructionTimeTo><restriction>ALL</restriction><vehicleType>{vt["key"]}</vehicleType></request></veh:getManufacturers>')
            if resp.status_code == 500: continue
            resp.raise_for_status()
            mans = parse_xml(resp, 'manufacturer')
            db.save_mans(mans, vt['key'])
            stats['mans'] += len(mans)
            print(f"  └─ ✅ {len(mans)} manufacturers")
            
            # BMW test (key string or int)
            print(f"  DEBUG: First 3 manufacturers: {[(m['key'], type(m['key']).__name__) for m in mans[:3]]}")
            bmw_list = [m for m in mans if str(m['key']) == '130']
            print(f"  DEBUG: BMW found = {len(bmw_list)}")
            
            for m in bmw_list[:1]:
                print(f"    🔧 M:{m['key']} {m['value']}")
                
                # BaseModels
                resp = api.soap(f'<veh:getBaseModelsN><request><locale country="tr" datCountryIndicator="tr" language="tr"/><constructionTimeFrom>4040</constructionTimeFrom><constructionTimeTo>4840</constructionTimeTo><restriction>ALL</restriction><vehicleType>{vt["key"]}</vehicleType><manufacturer>{m["key"]}</manufacturer></request></veh:getBaseModelsN>')
                if resp.status_code == 500: continue
                resp.raise_for_status()
                bms = parse_xml(resp, 'baseModelN')
                db.save_bms(bms, vt['key'], str(m['key']))
                stats['bms'] += len(bms)
                print(f"        └─ {len(bms)} base models")
                
                # İlk BM
                for bm in bms[:1]:
                    print(f"        📦 BM:{bm['key']} {bm['value'][:30]}")
                    
                    # SubModels
                    resp = api.soap(f'<veh:getSubModels><request><locale country="tr" datCountryIndicator="tr" language="tr"/><constructionTimeFrom>4040</constructionTimeFrom><constructionTimeTo>4840</constructionTimeTo><restriction>ALL</restriction><vehicleType>{vt["key"]}</vehicleType><manufacturer>{m["key"]}</manufacturer><baseModel>{bm["key"]}</baseModel></request></veh:getSubModels>')
                    if resp.status_code == 500: continue
                    resp.raise_for_status()
                    sms = parse_xml(resp, 'subModel')
                    db.save_sms(sms, vt['key'], str(m['key']), bm['key'])
                    stats['sms'] += len(sms)
                    print(f"            └─ {len(sms)} sub models")
                    
                    # İlk SM için DatECode compile
                    for sm in sms[:1]:
                        print(f"            🎯 SM:{sm['key']} {sm['value'][:30]}")
                        
                        # DotOptions tablosundan options al (classification groupby)
                        opts_by_cls = db.get_options_from_db(vt['key'], str(m['key']), bm['key'], sm['key'])
                        
                        if not opts_by_cls:
                            print(f"                ⚠️  No options in DB, skipping...")
                            continue
                        
                        # Her classification'dan 1 option al (max 8)
                        all_opts = [opts[0] for cls, opts in sorted(opts_by_cls.items())[:8] if opts]
                        
                        print(f"                📊 {len(opts_by_cls)} classifications → {len(all_opts)} options")
                        
                        # Compile DatECode
                        opts_xml = ''.join([f'<selectedOptions>{opt}</selectedOptions>' for opt in all_opts])
                        body = f'<veh:compileDatECode><request><locale country="tr" datCountryIndicator="tr" language="tr"/><restriction>ALL</restriction><vehicleType>{vt["key"]}</vehicleType><manufacturer>{m["key"]}</manufacturer><baseModel>{bm["key"]}</baseModel><subModel>{sm["key"]}</subModel>{opts_xml}</request></veh:compileDatECode>'
                        
                        resp = api.soap(body)
                        
                        if resp.status_code == 500:
                            # Retry logic - .NET ProcessPartsBulkAsync
                            xml_err = resp.text
                            
                            # Parse error message
                            match = re.search(r'between (\d+) and (\d+) options are required', xml_err)
                            if match:
                                min_opts = int(match.group(1))
                                max_opts = int(match.group(2))
                                print(f"                ⚠️  API wants {min_opts}-{max_opts} options, we have {len(all_opts)}")
                                
                                # Retry: classification sayısını ayarla
                                if len(opts_by_cls) >= min_opts:
                                    retry_opts = [opts[0] for cls, opts in sorted(opts_by_cls.items())[:max_opts] if opts]
                                    opts_xml = ''.join([f'<selectedOptions>{opt}</selectedOptions>' for opt in retry_opts])
                                    body = f'<veh:compileDatECode><request><locale country="tr" datCountryIndicator="tr" language="tr"/><restriction>ALL</restriction><vehicleType>{vt["key"]}</vehicleType><manufacturer>{m["key"]}</manufacturer><baseModel>{bm["key"]}</baseModel><subModel>{sm["key"]}</subModel>{opts_xml}</request></veh:compileDatECode>'
                                    
                                    print(f"                🔄 Retry with {len(retry_opts)} options...")
                                    resp = api.soap(body)
                            
                            elif 'Wrong options' in xml_err:
                                # 2. option'ları dene
                                print(f"                ⚠️  Wrong options! Trying alternatives...")
                                retry_opts = [opts[1] if len(opts) > 1 else opts[0] for cls, opts in sorted(opts_by_cls.items())[:8] if opts]
                                opts_xml = ''.join([f'<selectedOptions>{opt}</selectedOptions>' for opt in retry_opts])
                                body = f'<veh:compileDatECode><request><locale country="tr" datCountryIndicator="tr" language="tr"/><restriction>ALL</restriction><vehicleType>{vt["key"]}</vehicleType><manufacturer>{m["key"]}</manufacturer><baseModel>{bm["key"]}</baseModel><subModel>{sm["key"]}</subModel>{opts_xml}</request></veh:compileDatECode>'
                                
                                print(f"                🔄 Retry with alternative options...")
                                resp = api.soap(body)
                        
                        if resp.status_code == 200:
                            root = ET.fromstring(resp.text)
                            ecode = root.find('.//{http://sphinx.dat.de/services/VehicleSelectionService}datECode') or root.find('.//datECode')
                            
                            if ecode is not None and ecode.text:
                                dat_ecode = ecode.text
                                print(f"                ✅ DatECode: {dat_ecode}")
                                db.save_compiled_code(dat_ecode, vt['key'], str(m['key']), bm['key'], sm['key'])
                                stats['ecodes'] += 1
                                
                                # Images
                                img_body = f'<veh:getImages><request><locale country="tr" datCountryIndicator="tr" language="tr"/><restriction>ALL</restriction><datECode>{dat_ecode}</datECode></request></veh:getImages>'
                                img_resp = api.soap(img_body)
                                
                                if img_resp.status_code == 200:
                                    img_root = ET.fromstring(img_resp.text)
                                    images = []
                                    for img in img_root.findall('.//{http://www.dat.de/vxs}image'):
                                        images.append({
                                            'aspect': (img.find('.//{http://www.dat.de/vxs}aspect') or img.find('.//aspect')).text or '',
                                            'imageType': (img.find('.//{http://www.dat.de/vxs}imageType') or img.find('.//imageType')).text or '',
                                            'imageFormat': (img.find('.//{http://www.dat.de/vxs}imageFormat') or img.find('.//imageFormat')).text or '',
                                            'imageBase64': (img.find('.//{http://www.dat.de/vxs}imageBase64') or img.find('.//imageBase64')).text or '',
                                        })
                                    
                                    if images:
                                        print(f"                🖼️  {len(images)} images")
                                        db.save_images(dat_ecode, images)
                                        stats['imgs'] += len(images)
                                time.sleep(0.1)
        
        elapsed = datetime.now() - start
        
        print("\n" + "="*60)
        print("🎉 TAMAMLANDI!")
        print(f"\n📊 İSTATİSTİKLER:")
        print(f"  📋 {stats['vts']} VT  |  🏭 {stats['mans']} Manu")
        print(f"  🚗 {stats['bms']} BM  |  📦 {stats['sms']} SM")
        print(f"  ⚙️  {stats['opts']} Opt  |  🔨 {stats['ecodes']} ECode")
        print(f"  🖼️  {stats['imgs']} Img")
        print(f"\n⏱️  Süre: {elapsed}")
        print("="*60)
        
        return 0
        
    except Exception as e:
        print(f"\n❌ HATA: {e}")
        import traceback
        traceback.print_exc()
        return 1
    finally:
        db.close()


if __name__ == "__main__":
    sys.exit(main())
