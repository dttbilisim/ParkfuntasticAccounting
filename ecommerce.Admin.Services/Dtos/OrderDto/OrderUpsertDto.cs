using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.OrderDto
{
    [AutoMap(typeof(Orders), ReverseMap = true)]
    public class OrderUpsertDto
    {
        public int? Id { get; set; }
        public OrderStatusType OrderStatusType { get; set; }
        public string OrderNumber { get; set; }
        public int CompanyId { get; set; }
        public int SellerId { get; set; }
        public PaymentType PaymentTypeId { get; set; }
        public decimal? DiscountTotal { get; set; } = 0;
        public int CargoId { get; set; }
        public decimal CargoPrice { get; set; }
        public string? CargoTrackNumber { get; set; }
        public string? CargoTrackUrl { get; set; }
        public string? CargoExternalId { get; set; }
        public DateTime? ShipmentDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? DeliveryTo { get; set; }
        public decimal ProductTotal { get; set; }
        public decimal OrderTotal { get; set; }
        public decimal GrandTotal { get; set; }
        public int SubmerhantCommissionRate { get; set; } = 0;
        public decimal SubmerhantCommisionTotal { get; set; } = 0;
        public decimal MerhanCommission { get; set; } = 0;
        public decimal IyzicoPaidTotal { get; set; } = 0;

        //iyzico bilgileri
        public string? PaymentToken { get; set; }
        public string? CardAssociation { get; set; }
        public string? CardFamily { get; set; }
        public string? CardType { get; set; }
        public string? CardBinNumber { get; set; }
        public int? Installment { get; set; } = 0;
        public bool PaymentStatus { get; set; } = false;

        public string PaymentStatusResult {

            get
            {
                if (PaymentStatus)
                    return "Ödendi";
                else
                    return "Ödenmedi";
            }
        }

     
    }
}
