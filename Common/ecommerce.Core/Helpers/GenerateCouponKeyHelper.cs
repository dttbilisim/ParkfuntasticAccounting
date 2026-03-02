namespace ecommerce.Core.Helpers
{
    public static class GenerateCouponKeyHelper
    {
        private static Random random = new Random();

        public static string GenerateCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            var val = new string(Enumerable.Repeat(chars, 7).Select(s => s[random.Next(s.Length)]).ToArray());

            return val;
        }
    }
}
