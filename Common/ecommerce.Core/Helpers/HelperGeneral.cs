using System.Text.RegularExpressions;
namespace ecommerce.Core.Helpers;
public class HelperGeneral{
    public  string RemoveTurkishCharacters(string input)
    {
        input = input.Trim();
        input = Regex.Replace(input, "[^\\w\\@-]", "-").ToLower();
        Dictionary<string, string> replacements = new Dictionary<string, string>{
            {"ğ", "g"},
            {"ü", "u"},
            {"ş", "s"},
            {"ı", "i"},
            {"ö", "o"},
            {"ç", "c"},
            
            {"Ğ", "G"},
            {"Ü", "U"},
            {"Ş", "S"},
            {"I", "i"},
            {"Ö", "O"},
            {"Ç", "C"},
            {"--", ""},
            {"---", ""},
            {"----", ""}
        };
        input = replacements.Keys.Aggregate(input, (current, key) => Regex.Replace(current, key, replacements[key]));
        while (input.IndexOf("--") > -1) {
            input = input.Replace("--", "-");
        }

        return input;
    } 
}
