using System;

namespace WebAppMulti.Helpers
{
    public static class StringExtensions
    {
        public static bool IsCapitalized(this string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            return char.IsUpper(input[0]);
        }
        public static string Capitalize(this string input)
        {
            var bobo = input.ToUpper();
            return bobo;
        }
    }

}
