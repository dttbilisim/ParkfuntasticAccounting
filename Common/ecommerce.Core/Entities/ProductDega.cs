using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class ProductDega:AuditableEntity<int>{


    public string Code { get; set; }

    public string ? Depo1 { get; set; }
    public string ? Depo2 { get; set; }
    public string ? Depo3 { get; set; }
    public string ? Depo4 { get; set; }
    public string ? Depo5 { get; set; }
    public string ? Depo6 { get; set; }

    public string ? Depo_1 { get; set; }
    public string ? Depo_2 { get; set; }
    public string ? Depo_3 { get; set; }
    public string ? Depo_4 { get; set; }
    public string ? Depo_5 { get; set; }
    public string ? Depo_6 { get; set; }

    public string ? Manufacturer { get; set; }

    public string ? Name { get; set; }

    public string ? OrjinalKod { get; set; }

    public decimal SalePriceContact { get; set; }

    public string ? SalePriceContactCurrency { get; set; }

    public string ? SpecialField9 { get; set; }

    public string ? Unit { get; set; }
}
