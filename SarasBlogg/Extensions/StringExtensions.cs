using System.Text.RegularExpressions;

namespace SarasBlogg.Extensions
{
    public static class StringExtensions
    {
        public static string LimitLength(this string? str, int maxLength, bool stripHtml = false)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            // Ta bort HTML-taggar om flaggan är true
            if (stripHtml)
            {
                str = Regex.Replace(str, "<.*?>", string.Empty);
            }

            // Korta texten om den är för lång
            if (str.Length <= maxLength)
                return str;

            return str.Substring(0, maxLength) + "...";
        }
    }
}
