using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ecommerce.Domain.Shared.Helpers
{
    public enum TermType
    {
        Text,
        Code,
        Hybrid
    }

    public static class SearchTermClassifier
    {
        private static readonly Regex CodePattern = new Regex(@"^(?=.*\d)[a-zA-Z0-9\-\.]+$", RegexOptions.Compiled);
        private static readonly Regex PureNumberPattern = new Regex(@"^\d+$", RegexOptions.Compiled);

        public static TermType Classify(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return TermType.Text;

            // Pure Numbers (e.g. "307", "206") -> Could be car model or part of code. Treat as Hybrid/Code
            // Ideally Hybrid implies we check both aggressively but maybe less fuzzy on name.
            if (PureNumberPattern.IsMatch(term))
            {
                // If length is long (e.g. 8200423132), it's definitely a code
                if (term.Length > 5) return TermType.Code;
                return TermType.Hybrid; 
            }

            // Alpha-numeric mixed (e.g. "GDB106", "1K0", "DOT-4")
            if (CodePattern.IsMatch(term))
            {
                // If it has digits and letters, it's very likely a code
                if (term.Any(char.IsDigit) && term.Any(char.IsLetter))
                {
                    return TermType.Code;
                }
            }

            // Default to Text (e.g. "disk", "balata", "fren")
            return TermType.Text;
        }
    }
}
