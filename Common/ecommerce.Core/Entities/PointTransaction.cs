using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;
namespace ecommerce.Core.Entities;
public class PointTransaction : AuditableEntity<int>{
    public int CompanyId{get;set;}
   
    public PointTransactionType PointTransactionType{get;set;} // enum dan tipleri gorebilirsin.
    public decimal Point{get;set;} = 0; // Puan
    public string ProcessCode{get;set;} // ornek order numarasi veya nereden geldigini belirten kod yazalim.
    public string Description{get;set;}
    public string ? Answered{get;set;}
    public DateTime? AnsweredDate{get;set;}
    
    public int SellerId{get;set;}
    
    [ForeignKey("CompanyId")] public Company Company{get;set;}
    [ForeignKey("SellerId")] public Company Seller{get;set;}
}
