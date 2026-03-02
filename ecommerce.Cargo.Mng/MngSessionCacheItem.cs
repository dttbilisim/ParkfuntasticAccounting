namespace ecommerce.Cargo.Mng
{
    [Serializable]
    public class MngSessionCacheItem
    {
        public string AccessToken { get; set; } = null!;

        public string RefreshToken { get; set; } = null!;
    }
}