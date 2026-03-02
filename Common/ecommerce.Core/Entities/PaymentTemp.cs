using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class PaymentTemp:AuditableEntity<int>{

    public bool PaymentStatus{get;set;}
}
