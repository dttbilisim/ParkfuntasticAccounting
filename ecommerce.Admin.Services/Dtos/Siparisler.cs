using System.Xml.Serialization;
namespace ecommerce.Admin.Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
[XmlRoot("SIPARISLER")]
public class Siparisler{
    [XmlElement("SIPARIS")] public List<Siparis> SiparisList{get;set;} = new();
}
public class Siparis{
    [XmlElement("SIPARIS_NO")] public string SiparisNo{get;set;} = null!;
    [XmlElement("status")] public int Status{get;set;} // Status alanı
    [XmlElement("status_tanim")] public string StatusTanim{get;set;} = string.Empty;
    [XmlElement("tcno")] public string Tcno{get;set;} = string.Empty;
    [XmlElement("PAZARYERI_KARGOKODU")] public string PazarYeriKargoKodu{get;set;} = string.Empty;
    [XmlElement("CariHesapKodu")] public string CariHesapKodu{get;set;} = string.Empty;
    [XmlElement("CariHesapOzelKodu")] public string CariHesapOzelKodu{get;set;} = string.Empty;
    [XmlElement("POSTA")] public string Posta{get;set;} = string.Empty;
    [XmlElement("TARIH")]
    public string TarihString{
        get{return Tarih.ToString("yyyy-MM-dd");}
        set{Tarih = DateTime.Parse(value);}
    }
    [XmlIgnore] public DateTime Tarih{get;set;}
    [XmlElement("sipariszaman")] public string SiparisZaman{get;set;} = string.Empty;
    [XmlElement("ISKONTO")] public decimal Iskonto{get;set;}
    [XmlElement("NET_TOPLAM")] public decimal NetToplam{get;set;}
    [XmlElement("TeslimAlici")] public string TeslimAlici{get;set;} = string.Empty;
    [XmlElement("TeslimAdresi")] public string TeslimAdresi{get;set;} = string.Empty;
    [XmlElement("TeslimTelefon")] public string TeslimTelefon{get;set;} = string.Empty;
    [XmlElement("teslimsekli")] public string TeslimSekli{get;set;} = string.Empty;
    [XmlElement("tasiyicifirma")] public string TasiyiciFirma{get;set;} = string.Empty;
    [XmlElement("teslimkod1")] public string TeslimKod1{get;set;} = string.Empty;
    [XmlElement("teslimkod4")] public string TeslimKod4{get;set;} = string.Empty;
    [XmlElement("teslimil")] public string TeslimIl{get;set;} = string.Empty;
    [XmlElement("teslimilce")] public string TeslimIlce{get;set;} = string.Empty;
    [XmlElement("faturaalici")] public string FaturaAlici{get;set;} = string.Empty;
    [XmlElement("faturaAdresi")] public string FaturaAdresi{get;set;} = string.Empty;
    [XmlElement("faturaTelefon")] public string FaturaTelefon{get;set;} = string.Empty;
    [XmlElement("faturavergino")] public string FaturaVergiNo{get;set;} = string.Empty;
    [XmlElement("faturavergidairesi")] public string FaturaVergiDairesi{get;set;} = string.Empty;
    [XmlElement("faturail")] public string FaturaIl{get;set;} = string.Empty;
    [XmlElement("faturailce")] public string FaturaIlce{get;set;} = string.Empty;
    [XmlElement("kargokodu")] public string KargoKodu{get;set;} = string.Empty;
    [XmlElement("ODEME_SEKLI_TANIM")] public string OdemeSekliTanim{get;set;} = string.Empty;
    [XmlElement("ODEME_SEKLI")] public int OdemeSekli{get;set;}
    [XmlElement("kargo_odemesi")] public string KargoOdemesi{get;set;} = string.Empty;
    [XmlArray("SATIRLAR")]
    [XmlArrayItem("SATIR")]
    public List<Satir> Satirlar{get;set;} = new();
}
public class Satir{
    [XmlElement("BIRIM")] public string Birim{get;set;} = string.Empty;
    [XmlElement("FIYAT")] public decimal Fiyat{get;set;}
    [XmlElement("KDV")] public int Kdv{get;set;}
    [XmlElement("KOD")] public string Kod{get;set;} = string.Empty;
    [XmlElement("MIKTAR")] public int Miktar{get;set;}
    [XmlElement("URUNADI")] public string UrunAdi{get;set;} = string.Empty;
    [XmlElement("FATURA_ADI")] public string FaturaAdi{get;set;} = string.Empty;
    [XmlElement("VARKOD")] public string Varkod{get;set;} = string.Empty;
    [XmlElement("BARCODE")] public string Barcode{get;set;} = string.Empty;
    [XmlElement("MIAD")] public string Miad{get;set;} = string.Empty;
    [XmlElement("DOVIZ")] public string Doviz{get;set;} = string.Empty;
}
