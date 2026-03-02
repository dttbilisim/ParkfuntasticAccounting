namespace ecommerce.Admin.Domain.Dtos.CustomerWorkPlanDto
{
    public class CustomerWorkPlanUpsertDto
    {
        public int? Id { get; set; }
        public int SalesPersonId { get; set; }
        public int CustomerId { get; set; }
        public int DayOfWeek { get; set; } // 0=Pazar, 1=Pazartesi, ..., 6=Cumartesi
        public int MonthId { get; set; }
    }
}


