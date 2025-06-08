using System.Text.RegularExpressions;

namespace SarasBlogg.Extensions
{
    public static class RegexExtensions
    {
        public static bool ContainsForbiddenWord(this string input, string pattern)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pattern))
                return false;

            return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);
        }
    }
}
