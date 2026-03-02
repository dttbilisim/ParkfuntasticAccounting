namespace ecommerce.Admin.Domain.Dtos.CurrencyDto;
public class BistDto{
    public class Result
    {
        public string currentstr { get; set; }
        public decimal current { get; set; }
        public string changeratestr { get; set; }
        public double changerate { get; set; }
        public string minstr { get; set; }
        public double min { get; set; }
        public string maxstr { get; set; }
        public double max { get; set; }
        public string openingstr { get; set; }
        public double opening { get; set; }
        public string closingstr { get; set; }
        public double closing { get; set; }
        public string time { get; set; }
        public string date { get; set; }
        public DateTime datetime { get; set; }
    }

    public class Root
    {
        public bool success { get; set; }
        public List<Result> result { get; set; }
    }
}
