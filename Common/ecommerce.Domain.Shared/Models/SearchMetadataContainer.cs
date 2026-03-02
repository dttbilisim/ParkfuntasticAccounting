using System.Collections.Generic;
using Newtonsoft.Json;

namespace ecommerce.Domain.Shared.Models
{
    public class SearchMetadataContainer
    {
        public Dictionary<string, List<string>> Synonyms { get; set; } = new();
        public Dictionary<string, string> RomanNumerals { get; set; } = new();
        public HashSet<string> TechnicalVTerms { get; set; } = new();
        public SearchBoostSettings Boosts { get; set; } = new();
        
        // General Settings
        public bool ShouldGroupOems { get; set; } = true;
    }

    public class SearchBoostSettings
    {
        [JsonProperty("GroupCodeTerm")]
        public double OemCodeTerm { get; set; }
        
        public double PartNumberTerm { get; set; }
        public double BaseModelKeyword { get; set; }
        public double ManufacturerKeyword { get; set; }
        public double ProductNameKeyword { get; set; } 
        
        [JsonProperty("GroupCodeMatch")]
        public double OemCodeMatch { get; set; }
        
        public double PartNumberMatch { get; set; }
        public double ProductNameMatch { get; set; }
        public double DotPartNameMatch { get; set; }
        
        [JsonProperty("GroupCodeWildcard")]
        public double OemCodeWildcard { get; set; }
        
        public double PartNumberWildcard { get; set; }
        public double ProductNameFuzzy { get; set; }
        public double BaseModelFuzzy { get; set; }
        public double ManufacturerFuzzy { get; set; }
        public double NestedSubModel { get; set; }

        // Logic Multipliers
        public double CodeMatchMultiplier { get; set; }    // e.g. OemCodeTerm * 2.0
        public double CodeMatchSecondaryMultiplier { get; set; } // e.g. OemCodeMatch * 1.5
        public double TextMatchReductionMultiplier { get; set; } // e.g. ProductNameMatch * 0.5
        public double TextPartNumberSuppressionMultiplier { get; set; } // e.g. PartNumberTerm * 0.1
    }
}
