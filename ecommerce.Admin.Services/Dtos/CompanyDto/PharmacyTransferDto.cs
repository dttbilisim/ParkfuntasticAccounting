namespace ecommerce.Admin.Domain.Dtos.CompanyDto;
public class PharmacyTransferDto{
    public class Root
    {
        public string sEcho { get; set; }
        public int iTotalRecords { get; set; }
        public int iTotalDisplayRecords { get; set; }
        public List<List<string>> aaData { get; set; }
    }
}
