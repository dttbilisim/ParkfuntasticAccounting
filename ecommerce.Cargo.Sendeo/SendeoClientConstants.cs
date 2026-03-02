namespace ecommerce.Cargo.Sendeo
{
    public class SendeoClientConstants
    {
        public const string UserAgent = "ecommerce";

        public const string BaseUrl = "https://api.sendeo.com.tr/api";

        public const string TokenPath = "/Token/LoginAES";
        public const string AuthCacheKey = "Sendeo:Auth:{0}";
        public const int TokenExpireInMinutes = 20 * 60;

        public const string CargoGetCityDistrictsPath = "/Cargo/GetCityDistricts";
        public const string CargoSetDeliveryPath = "/Cargo/SETDELIVERY";
        public const string CargoCancelDeliveryPath = "/Cargo/CANCELDELIVERY";
        public const string CargoTrackDeliveryPath = "/Cargo/TRACKDELIVERY";
        public const string CargoListPath = "/Cargo/GetCargoList";

        public const string CityCacheKey = "Sendeo:City:{0}";
        public const int CityCacheTimeInMinutes = 60;
    }
}