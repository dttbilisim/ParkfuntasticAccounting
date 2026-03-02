namespace ecommerce.Admin.Domain.Dtos.CurrencyDto;
public class ExchangeRates{
    public class Datum
    {
        public string code { get; set; }
        public string name { get; set; }
        public decimal rate { get; set; }
        public string calculatedstr { get; set; }
        public double calculated { get; set; }
    }

    public class Result
    {
        public string @base { get; set; }
        public string lastupdate { get; set; }
        public List<Datum> data { get; set; }
    }

    public class Root
    {
        public bool success { get; set; }
        public Result result { get; set; }
    }

}

