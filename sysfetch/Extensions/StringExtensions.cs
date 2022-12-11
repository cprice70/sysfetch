using System.Text.RegularExpressions;

namespace sysfetch.Extensions
{
    public static class StringExtensions
    {
        public static string GetValue(this string str, string parameter, string separator = "=")
        {
            var results = str.Split("\n");
            var values = Array.FindAll(results, s => s.Contains(parameter));
            if (!values.Any()) return string.Empty;
            
            Match match = Regex.Match(values[0], "\"([^\"]*)\""); 
            
            return match.Success ? match.Groups[1].Value : values[0][(values[0].LastIndexOf(separator) + 1)..].Trim();
        }
    }
}
