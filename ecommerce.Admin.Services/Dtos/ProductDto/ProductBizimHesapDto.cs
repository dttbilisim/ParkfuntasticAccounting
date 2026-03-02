using Newtonsoft.Json;
namespace ecommerce.Admin.Domain.Dtos.ProductDto;
public class ProductBizimHesapDto{
   // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Barkod
    {
        [JsonProperty("#cdata-section")]
        public string cdatasection { get; set; }
    }

    public class Detay
    {
        [JsonProperty("#cdata-section")]
        public string cdatasection { get; set; }
    }

    public class KatYolu
    {
        [JsonProperty("#cdata-section")]
        public string cdatasection { get; set; }
    }

    public class Marka
    {
        [JsonProperty("#cdata-section")]
        public string cdatasection { get; set; }
    }

    public class Resim
    {
        [JsonProperty("#cdata-section")]
        public string cdatasection { get; set; }
    }

    public class Root
    {
        [JsonProperty("?xml")]
        public Xml xml { get; set; }
        public Urunler urunler { get; set; }
    }

    public class StokKod
    {
        [JsonProperty("#cdata-section")]
        public string cdatasection { get; set; }
    }

    public class Urun
    {
        public StokKod stok_kod { get; set; }
        public Barkod barkod { get; set; }
        public UrunAd urun_ad { get; set; }
        public Varyant varyant { get; set; }
        public Marka marka { get; set; }
        public int stok { get; set; }
        public string satis_fiyat { get; set; }
        public string para_birim { get; set; }
        public int kdv { get; set; }
        public Detay detay { get; set; }
        public KatYolu kat_yolu { get; set; }
        public Resim resim { get; set; }
    }

    public class UrunAd
    {
        [JsonProperty("#cdata-section")]
        public string cdatasection { get; set; }
    }

    public class Urunler
    {
        public List<Urun> urun { get; set; }
    }

    public class Varyant
    {
        [JsonProperty("#cdata-section")]
        public string cdatasection { get; set; }
    }

    public class Xml
    {
        [JsonProperty("@version")]
        public string version { get; set; }

        [JsonProperty("@encoding")]
        public string encoding { get; set; }
    }


}
