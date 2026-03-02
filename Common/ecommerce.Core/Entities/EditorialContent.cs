using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;

namespace ecommerce.Core.Entities;

public class EditorialContent : AuditableEntity<int>
{
    public EditorialContentType Type { get; set; }

    public string Slug { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Content { get; set; }

    public string? Category { get; set; }

    public string Thumbnail { get; set; } = null!;

    public string? Video { get; set; }
    public bool IsDefault{get;set;}

    public DateTime PublishDate { get; set; }
    public DateTime? EndDate{get;set;}

    public int Order { get; set; }
}