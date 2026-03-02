using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.DiscountDto
{
    [AutoMap(typeof(Discount))]
    public class DiscountListDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public DiscountType DiscountType { get; set; }

        public decimal? DiscountPercentage { get; set; }

        public decimal? DiscountAmount { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool RequiresCouponCode { get; set; }

        public int Status { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }
    }
}