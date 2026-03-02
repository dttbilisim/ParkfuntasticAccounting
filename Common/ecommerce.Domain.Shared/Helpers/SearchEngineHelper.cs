using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

using Nest;
using ecommerce.Domain.Shared.Models;

namespace ecommerce.Domain.Shared.Helpers
{
    public static class SearchEngineHelper
    {
        public static string ProcessRomanNumerals(string input, Dictionary<string, string> romanToNumeric, HashSet<string> technicalVTerms)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i].ToUpper();
                if (romanToNumeric.TryGetValue(word, out string? numeric))
                {
                    // Special logic for "V" to avoid converting "V-belt" or technical terms containing V
                    if (word == "V")
                    {
                        bool isTechnical = false;
                        if (i + 1 < words.Length)
                        {
                            var next = words[i + 1].ToLower();
                            if (technicalVTerms.Any(t => next.Contains(t)))
                                isTechnical = true;
                        }
                        if (i > 0)
                        {
                            var prev = words[i - 1].ToLower();
                             if (technicalVTerms.Any(t => prev.Contains(t)))
                                isTechnical = true;
                        }

                        if (!isTechnical) words[i] = numeric;
                    }
                    else
                    {
                        words[i] = numeric;
                    }
                }
            }
            return string.Join(" ", words);
        }

        public static List<string> ExpandSynonyms(string[] words, Dictionary<string, List<string>> synonymDict)
        {
            var expanded = new List<string>(words);
            foreach (var word in words)
            {
                if (synonymDict.TryGetValue(word, out var synonyms))
                {
                    foreach (var syn in synonyms)
                    {
                        if (!expanded.Contains(syn, StringComparer.OrdinalIgnoreCase))
                            expanded.Add(syn);
                    }
                }
            }
            return expanded.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static QueryContainer BuildSmartSearchQuery(string term, SearchMetadataContainer metadata)
        {
            var termType = SearchTermClassifier.Classify(term);

            if (termType == TermType.Code)
            {
                // CODE STRATEGY
                return new BoolQuery
                {
                    Should = new List<QueryContainer>
                    {
                        // Dynamic Multipliers from AppSettings
                        new TermQuery { Field = "OemCode", Value = term, Boost = metadata.Boosts.OemCodeTerm * metadata.Boosts.CodeMatchMultiplier },
                        new TermQuery { Field = "PartNumber", Value = term, Boost = metadata.Boosts.PartNumberTerm * metadata.Boosts.CodeMatchMultiplier },
                        new TermQuery { Field = "ProductName.keyword", Value = term, Boost = metadata.Boosts.ProductNameKeyword }, 
                        
                        // Match/Wildcard for Codes
                        new MatchQuery { Field = "OemCode", Query = term, Boost = metadata.Boosts.OemCodeMatch * metadata.Boosts.CodeMatchSecondaryMultiplier },
                        new MatchQuery { Field = "PartNumber", Query = term, Boost = metadata.Boosts.PartNumberMatch * metadata.Boosts.CodeMatchSecondaryMultiplier },
                        
                        // Lower Boost for Text Fields (Removed CaseInsensitive for compatibility)
                        new WildcardQuery { Field = "ProductName", Value = $"*{term}*", Boost = metadata.Boosts.ProductNameMatch * metadata.Boosts.TextMatchReductionMultiplier },
                        
                        new WildcardQuery { Field = "PartNumber", Value = $"*{term}*", Boost = metadata.Boosts.PartNumberWildcard }
                    }
                };
            }
            else if (termType == TermType.Text)
            {
                // TEXT STRATEGY - COMPATIBILITY MODE
                var lowerTerm = term.ToLowerInvariant();
                var textQueries = new List<QueryContainer>
                {
                    new TermQuery { Field = "ProductName.keyword", Value = term, Boost = metadata.Boosts.ProductNameKeyword },
                    new TermQuery { Field = "ManufacturerName.keyword", Value = term, Boost = metadata.Boosts.ManufacturerKeyword },

                    new TermQuery { Field = "PartNumber", Value = term, Boost = metadata.Boosts.PartNumberTerm * metadata.Boosts.TextPartNumberSuppressionMultiplier }
                };

                // Dynamic Field Weights based on User Settings
                // Using ratios from Keywords to define relative importance of Identity (Model) vs Content (Name)
                double modelWeight = metadata.Boosts.BaseModelKeyword / (metadata.Boosts.ProductNameKeyword > 0 ? metadata.Boosts.ProductNameKeyword : 1);
                double manufacturerWeight = metadata.Boosts.ManufacturerKeyword / (metadata.Boosts.ProductNameKeyword > 0 ? metadata.Boosts.ProductNameKeyword : 1);
                
                // Ensure model identity always has a strong signal (at least 5x and up to 100x if settings dictate)
                modelWeight = Math.Clamp(modelWeight * 10, 5, 100); 
                manufacturerWeight = Math.Clamp(manufacturerWeight * 5, 3, 50);

                var tokens = term.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var shouldQueries = new List<QueryContainer>();
                var mustQueries = new List<QueryContainer>();

                // Mandatory Word Matching: EVERY word must exist in the index (ProductName, Model, or Brand)
                // This prevents "alakasiz" results from appearing just because one word matches.
                foreach (var token in tokens)
                {
                    if (token.Length > 1) // Ignore single characters for mandatory match unless they are digits
                    {
                        mustQueries.Add(new MultiMatchQuery
                        {
                            Fields = new[] { "ProductName", "BaseModelName", "ManufacturerName", "PartNumber" },
                            Query = token,
                            Fuzziness = token.Any(char.IsDigit) ? Fuzziness.EditDistance(0) : Fuzziness.Auto,
                            Operator = Operator.And,
                            Boost = 1
                        });
                    }
                }

                // RADICAL PRODUCT NAME PRIORITY (The "Perfect Match" User requested)
                // We use MatchPhrase for exact sequence with a MASSIVE boost
                shouldQueries.Add(new MatchPhraseQuery
                {
                    Field = "ProductName",
                    Query = term,
                    Boost = metadata.Boosts.ProductNameFuzzy * 200.0 // 200x boost for perfect product name match
                });

                // Model Identity Anchor (Massive boost for the first word in Model field)
                if (tokens.Length > 0 && tokens[0].Length > 2 && tokens[0].All(char.IsLetter))
                {
                    shouldQueries.Add(new MatchQuery
                    {
                        Field = "BaseModelName",
                        Query = tokens[0],
                        Boost = metadata.Boosts.ProductNameFuzzy * 100.0
                    });
                }

                return new BoolQuery
                {
                    Must = mustQueries,
                    Should = shouldQueries
                };
            }
            else // Hybrid
            {
                // DEFAULT/HYBRID STRATEGY
                return new BoolQuery
                {
                    Should = new List<QueryContainer>
                    {
                        new TermQuery { Field = "OemCode", Value = term, Boost = metadata.Boosts.OemCodeTerm },
                        new TermQuery { Field = "PartNumber", Value = term, Boost = metadata.Boosts.PartNumberTerm },
                        new TermQuery { Field = "BaseModelName.keyword", Value = term, Boost = metadata.Boosts.BaseModelKeyword },
                        new TermQuery { Field = "ManufacturerName.keyword", Value = term, Boost = metadata.Boosts.ManufacturerKeyword },
                        new TermQuery { Field = "ProductName.keyword", Value = term, Boost = metadata.Boosts.ProductNameKeyword },
                        
                        new MatchQuery { Field = "OemCode", Query = term, Boost = metadata.Boosts.OemCodeMatch },
                        new MatchQuery { Field = "PartNumber", Query = term, Boost = metadata.Boosts.PartNumberMatch },
                        new WildcardQuery { Field = "ProductName", Value = $"*{term}*", Boost = metadata.Boosts.ProductNameMatch },
                        new MatchQuery { Field = "DotPartName", Query = term, Boost = metadata.Boosts.DotPartNameMatch },

                        // Wildcard restricted to length > 2
                        term.Length > 2 ? new WildcardQuery { Field = "OemCode", Value = $"*{term}*", Boost = metadata.Boosts.OemCodeWildcard } : null,
                        term.Length > 2 ? new WildcardQuery { Field = "PartNumber", Value = $"*{term}*", Boost = metadata.Boosts.PartNumberWildcard } : null,

                        new MatchQuery 
                        { 
                            Field = "ProductName", 
                            Query = term, 
                            Fuzziness = term.Length > 6 ? Fuzziness.EditDistance(2) : (term.Length > 4 ? Fuzziness.EditDistance(1) : Fuzziness.EditDistance(0)),
                            Boost = metadata.Boosts.ProductNameFuzzy 
                        },
                        new MatchQuery 
                        { 
                            Field = "BaseModelName", 
                            Query = term, 
                            Fuzziness = term.Length > 6 ? Fuzziness.EditDistance(2) : (term.Length > 4 ? Fuzziness.EditDistance(1) : Fuzziness.EditDistance(0)),
                            Boost = metadata.Boosts.BaseModelFuzzy 
                        },
                        new MatchQuery 
                        { 
                            Field = "ManufacturerName", 
                            Query = term, 
                            Fuzziness = term.Length > 6 ? Fuzziness.EditDistance(2) : (term.Length > 4 ? Fuzziness.EditDistance(1) : Fuzziness.EditDistance(0)),
                            Boost = metadata.Boosts.ManufacturerFuzzy 
                        }
                    }
                };
            }
        }
    }
}
