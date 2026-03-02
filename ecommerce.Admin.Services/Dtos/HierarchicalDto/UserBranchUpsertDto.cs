namespace ecommerce.Admin.Domain.Dtos.HierarchicalDto
{
    public class UserBranchUpsertDto
    {
        public int? Id { get; set; }
        public int UserId { get; set; }
        public int BranchId { get; set; }
        public bool IsDefault { get; set; }
    }
}
