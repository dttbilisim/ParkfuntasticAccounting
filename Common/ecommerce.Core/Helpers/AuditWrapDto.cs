namespace ecommerce.Core.Helpers {
    public class AuditWrapDto<T> where T:class {
        public int UserId { get; set; }
        public T Dto { get; set; } = null!;
    }
}
