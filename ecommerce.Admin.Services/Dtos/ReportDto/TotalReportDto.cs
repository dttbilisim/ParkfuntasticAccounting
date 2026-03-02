namespace ecommerce.Admin.Domain.Dtos.ReportDto;
public class TotalReportDto{
    public decimal NetSatisTutari{get;set;}
    public int SiparisAdeti{get;set;}
    public decimal IadeTutari{get;set;}
    public int IadeAdeti{get;set;}
    public decimal KampanyanliSiparis{get;set;}
    public decimal KampanyasizSiparis{get;set;}
    public int SepetteBekleyenUrunlerAdet{get;set;}
    public decimal SepetteBekleyenUrunlerToplami{get;set;}

    public int buyercount{get;set;}
    public int sellercount{get;set;}
    public int buyerandsellercount{get;set;}

    public int newbuyercount{get;set;}
    public int newsellercount{get;set;}
    public int newbuyerandsellercount{get;set;}
    
    public int KayitOlmusGirisYapmis{get;set;}
    public int KayitOlmusGirisYapmamis{get;set;}
    public int LoginOlmusSiparisVermemis{get;set;}
    public int LoginOlmusSiparisVermis{get;set;}
    public int ArananUrunSayisi{get;set;}
    public int ToplamKullaniciSayisi{get;set;}
    public int AktifEczaneSayisi{get;set;}
    public int AvmEczanesi{get;set;}
    public int CaddeEczanesi{get;set;}
    public int SemtEczanesi{get;set;}
    public int AsmEczanesi{get;set;}
    public int HastaneYakiniEczanesi{get;set;}
    public int IlaniOlanDepo{get;set;}
    public int IlaniOlanEczane{get;set;}
    public int IlaniOlanIlacFirmasi{get;set;}
    public int ReceteliIlan{get;set;}
    public int AnneBebekIlan{get;set;}
    public int BesinTakviyesiIlan{get;set;}
    public int MedikaliIlan{get;set;}
    public int KisiselBakimiIlan{get;set;}
    public int SaglikUrunleriIlan{get;set;}
    public int SarfMalzemeIlan{get;set;}
    public int KategorisizIlan{get;set;}
    public decimal ReceteliTotal{get;set;}
    public decimal AnneBebekTotal{get;set;}
    public decimal BesinTakviyesiTotal{get;set;}
    public decimal MedikalTotal{get;set;}
    public decimal KisiselTotal{get;set;}
    public decimal SaglikUrunleriTotal{get;set;}
    public decimal SarfMalzemeTotal{get;set;}
    public decimal KategorisizTotal{get;set;}
    public int PuanSayisi{get;set;}
    public int YorumSayisi{get;set;}
    public int SatilanUrunSayisi{get;set;}
    public int AktifUrun{get;set;}
    public int OasifUrun{get;set;}
    
}

