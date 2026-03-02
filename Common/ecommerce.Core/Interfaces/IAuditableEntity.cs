namespace ecommerce.Core.Interfaces {
    public interface IAuditableEntity {
        DateTime CreatedDate { get; set; }
        DateTime? ModifiedDate { get; set; }
        DateTime? DeletedDate { get; set; }
        int Status { get; set; }
        int CreatedId { get; set; }
        int? ModifiedId { get; set; }
        int? DeletedId { get; set; }
    }

    public interface IAuditableEntity<T> : IEntity<T>, IAuditableEntity {

    }
}
