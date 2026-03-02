namespace ecommerce.Admin.Domain.Dtos.CustomerWorkPlanDto
{
    public class CustomerWorkPlanListDto
    {
        public int Id { get; set; }
        public int SalesPersonId { get; set; }
        public string SalesPersonName { get; set; } = null!;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = null!;
        public string CustomerCode { get; set; } = null!;
        public int DayOfWeek { get; set; }
        public string DayName { get; set; } = null!;
        public int MonthId { get; set; }
        public string MonthName { get; set; } = null!;
        public DateTime CreatedDate { get; set; }
    }
}


