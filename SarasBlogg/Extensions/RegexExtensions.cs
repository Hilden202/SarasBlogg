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
        public static string ToRegexPattern(this string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return string.Empty;

            var map = new Dictionary<char, string>
            {
                ['a'] = "[a4@]",
                ['o'] = "[o0]",
                ['e'] = "[e3]",
                ['i'] = "[i1|!]",
                ['u'] = "[uüv]",
                ['c'] = "[ck]",
                ['s'] = "[s$5]",
                ['g'] = "[g9]"
            };

            return string.Concat(word.ToLower().Select(c =>
                map.TryGetValue(c, out var replacement) ? replacement : Regex.Escape(c.ToString())
            ));
        }

    }

}
