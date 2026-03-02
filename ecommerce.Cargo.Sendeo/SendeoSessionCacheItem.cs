namespace ecommerce.Cargo.Sendeo
{
    [Serializable]
    public class SendeoSessionCacheItem
    {
        public int CustomerId { get; set; }

        public string CustomerTitle { get; set; } = null!;

        public string Token { get; set; } = null!;
    }
}