// Services/AmountInWords.cs
using System.Globalization;

namespace SchoolPortal.API.Services
{
    // Converts a whole-rupee amount to English words for the "in words Rs."
    // line on the printed receipt, e.g. 2600 -> "Two Thousand Six Hundred".
    // Paise are intentionally dropped — school fee amounts are always whole rupees.
    public static class AmountInWords
    {
        private static readonly string[] Ones =
        {
            "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
            "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
            "Seventeen", "Eighteen", "Nineteen"
        };

        private static readonly string[] Tens =
        {
            "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
        };

        public static string Convert(decimal amount)
        {
            var whole = (long)Math.Floor(Math.Abs(amount));
            if (whole == 0) return "Zero Rupees Only";

            var parts = new List<string>();

            var crore = whole / 10000000; whole %= 10000000;
            var lakh = whole / 100000; whole %= 100000;
            var thousand = whole / 1000; whole %= 1000;
            var hundred = whole / 100; whole %= 100;

            if (crore > 0) parts.Add(TwoDigit((int)crore) + " Crore");
            if (lakh > 0) parts.Add(TwoDigit((int)lakh) + " Lakh");
            if (thousand > 0) parts.Add(TwoDigit((int)thousand) + " Thousand");
            if (hundred > 0) parts.Add(Ones[hundred] + " Hundred");
            if (whole > 0) parts.Add(TwoDigit((int)whole));

            return string.Join(" ", parts) + " Rupees Only";
        }

        private static string TwoDigit(int n)
        {
            if (n < 20) return Ones[n];
            var tens = n / 10;
            var ones = n % 10;
            return ones == 0 ? Tens[tens] : $"{Tens[tens]} {Ones[ones]}";
        }
    }
}
