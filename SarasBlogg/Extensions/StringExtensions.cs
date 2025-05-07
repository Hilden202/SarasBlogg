namespace SarasBlogg.Extensions
{
    public static class StringExtensions
    {
        public static string LimitLength(this string str, int maxLength)
        {
            if(str != null)
            {
                if(str.Length <= maxLength)
                {
                    return str;
                }
                return str.Substring(0, maxLength) + "...";
            }
            return str;
        }
    }
}
