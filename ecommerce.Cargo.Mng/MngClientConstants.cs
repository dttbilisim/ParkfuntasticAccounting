namespace ecommerce.Cargo.Mng
{
    public class MngClientConstants
    {
        public const string UserAgent = "ecommerce";

        public const string BaseUrl = "https://api.mngkargo.com.tr/mngapi/api";
        public const string SandboxBaseUrl = "https://testapi.mngkargo.com.tr/mngapi/api";
        
        public const string ApiVersionHeader = "X-Api-Version";
        public const string ClientIdHeader = "X-IBM-Client-Id";
        public const string ClientSecretHeader = "X-IBM-Client-Secret";

        public const string TokenPath = "/token";
        public const string AuthCacheKey = "Mng:Auth:{0}";

        public const string CreateBarcodePath = "/barcodecmdapi/createbarcode";
        public const string CancelShipmentPath = "/barcodecmdapi/cancelshipment";
        public const string UpdateShipmentPath = "/barcodecmdapi/updateshipment";
        
        public const string GetCitiesPath = "/cbsinfoapi/getcities";
        public const string GetDistrictsPath = "/cbsinfoapi/getdistricts/{cityCode}";
        public const string GetNeighborhoodsPath = "/cbsinfoapi/getneighborhoods/{cityCode}/{districtCode}";
        
        public const string CalculatePath = "/standardqueryapi/calculate";
        public const string CreateOrderPath = "/pluscmdapi/createDetailedOrder";
        public const string CreateReturnOrderPath = "/standardcmdapi/createReturnOrder";
        public const string CancelOrderPath = "/pluscmdapi/cancelOrderDelivery";
        public const string GetShipmentStatusPath = "/standardqueryapi/getshipmentstatus/{orderId}";
        public const string GetShipmentStatusByShipmentIdPath = "/standardqueryapi/getshipmentstatusByShipmentId/{shipmentId}";
    }
}