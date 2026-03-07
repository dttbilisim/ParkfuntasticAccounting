namespace ecommerce.Admin.Domain.Dtos.Plasiyer
{
    /// <summary>
    /// Plasiyer rota listesi için müşteri bilgisi — adres ve son ziyaret dahil.
    /// </summary>
    public class PlasiyerRotaCustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = null!;
        public string? CustomerAddress { get; set; }
        public string? CustomerCode { get; set; }
        public DateTime? LastVisitDate { get; set; }
        public string? LastVisitNote { get; set; }
    }

    /// <summary>
    /// Müşteri ziyaret detayı — rota notları listesi için.
    /// </summary>
    public class PlasiyerCustomerVisitDto
    {
        public int Id { get; set; }
        public DateTime VisitDate { get; set; }
        public string? VisitNote { get; set; }
    }
}
