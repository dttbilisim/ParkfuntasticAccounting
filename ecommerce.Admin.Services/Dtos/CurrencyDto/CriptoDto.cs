namespace ecommerce.Admin.Domain.Dtos.CurrencyDto;
public class CriptoDto{
    public class Result
    {
        public double changeHour { get; set; }
        public string changeHourstr { get; set; }
        public double changeDay { get; set; }
        public string changeDaystr { get; set; }
        public double changeWeek { get; set; }
        public string changeWeekstr { get; set; }
        public string volumestr { get; set; }
        public double volume { get; set; }
        public string currency { get; set; }
        public string pricestr { get; set; }
        public decimal price { get; set; }
        public string marketCapstr { get; set; }
        public double marketCap { get; set; }
        public string circulatingSupply { get; set; }
        public string code { get; set; }
        public string name { get; set; }
    }

    public class Root
    {
        public bool success { get; set; }
        public List<Result> result { get; set; }
    }

}
