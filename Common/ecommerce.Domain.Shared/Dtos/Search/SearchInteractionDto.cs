using System;
using System.Collections.Generic;

namespace ecommerce.Domain.Shared.Dtos.Search
{
    public class SearchInteractionDto
    {
        public string SearchTerm { get; set; } = null!;
        public long ProductId { get; set; }
        public string InteractionType { get; set; } = null!; // "Click", "AddToCart"
        public int Rank { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int? UserId { get; set; }
        
        // Field-level tracking for ML optimization
        public List<string>? MatchedFields { get; set; } = new();
        public Dictionary<string, double>? FieldScores { get; set; } = new();
        public string? PrimaryMatchField { get; set; } // Field with highest score
    }
}
