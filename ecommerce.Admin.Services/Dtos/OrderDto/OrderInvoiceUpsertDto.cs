using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.OrderDto;

[AutoMap(typeof(OrderInvoice), ReverseMap = true)]
public class OrderInvoiceUpsertDto{
    public int Id{get;set;}
    public InvoiceType InvoiceType{get;set;}
    public DateTime InvoiceDate{get;set;}
   
    public int OrderId{get;set;}
    public int CompanyId{get;set;}
    public string InvoicePath{get;set;}
    public string FileName{get;set;}
}

