namespace ecommerce.Cargo.Sendeo.Models;

public enum DeliveryType
{
    FromLocation = 1,
    FromCustomer = 2,
    FromSupplier = 3,
    ReDelivery = 4,
    DeliveryPoint = 5,
    ReturnPoint = 6
}

public enum SendeoCargoStatus
{
    KargoSevkEmriAlindi = 101,
    BelgeDuzenlendi = 102,
    SubeTmYukleme = 103,
    TmSubeIndirme = 104,
    TmHatYukleme = 105,
    TmHatIndirme = 106,
    TmSubeYukleme = 107,
    SubeTmIndirme = 108,
    SubeDagitimYukleme = 109,
    SubeDagitimIndirme = 110,
    TeslimEdildi = 111,
    AliciTelefonuYanlis = 112,
    IadeTalebi = 113,
    AliciAdresindeYok = 114,
    AliciAdresiYanlis = 115,
    HasarliGonderi = 117,
    KayipKargo = 118,
    Devir = 119,
    EksikTeslimEdildi = 120,
    DagitimAlaniDisinda = 121,
    OdemeTipiKabulEdilmedi = 122,
    RandevuluTeslimat = 123,
    MobilDagitimBolgesi = 124,
    EksikKargo = 125,
    Yonlendirme = 126,
    HatAraciGecikmesi = 127,
    OlumsuzHavaKosullari = 128,
    TasimaFiyatiYuksekBulundu = 129,
    TeslimEdilemedi = 130,
    IadeGonderi = 131,
    OlcumTartim = 132,
    IptalGonderi = 133,
    IadeOnay = 134,
    IsEmriIadesi = 135,
    IadeRed = 136,
    RandevuluAlim = 137,
    GonderiAlindi = 138,
    IptalIsEmri = 139,
    KuryeTeslimatta = 140,
    KuryeZimmeteAlma = 141,
    KuryeZimmettenCikarma = 142,
    KayipKargoIsEmri = 143,
    IadeOlarakTeslim = 151
}

public enum CollectionType
{
    TahsilatsizKargo = 0,
    Nakit = 1,
    KrediKarti = 2
}

public enum BarcodeLabelType
{
    ZPL = 0,
    Base64 = 1,
    ZPLs = 2,
    ZPLs2 = 3,
    CustomZPL = 8,
    Base64Html = 9,
    Base64Text = 10
}