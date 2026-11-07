using System.Globalization;
using System.Text;

namespace HomeFix.Core.Rules
{
    /// <summary>
    /// Validation of every value that enters the workshop. Each rule depends on its input alone,
    /// so no instance is ever needed and the class is static. The domain classes use a rule to
    /// throw on a bad value and the console uses the very same rule to ask once more: one rule,
    /// one verdict, in both layers.
    /// </summary>
    public static class DataRules
    {
        private const string DatePattern = "dd/MM/yyyy";
        private const int FirstAcceptedYear = 2000;
        private const int MaxPromiseDays = 90;
        private const decimal MaxHoursPerEntry = 24m;
        private const decimal MaxPrice = 100_000_000m;
        private const int MaxFullNameLength = 60;
        private const int SerialDigits = 5;

        /// <summary>How dates are typed and printed in the workshop: day/month/year.</summary>
        public static string DateInputFormat => DatePattern;

        /// <summary>
        /// Checks a serial number of the form PP-YYYY-NNNNN, for example DW-2025-00418:
        /// the prefix of the device kind, the year of manufacture (2000 to this year) and a
        /// five-digit sequence that is not all zeros.
        /// </summary>
        /// <param name="serialNo">The number to check.</param>
        /// <param name="prefix">The two capital letters of the device kind.</param>
        public static bool IsValidSerial(string? serialNo, string prefix)
        {
            if (serialNo is null || prefix.Length != 2 || !prefix.All(char.IsAsciiLetterUpper))
            {
                return false;
            }

            string[] parts = serialNo.Split('-');
            return parts.Length == 3
                && parts[0] == prefix
                && IsAcceptedYear(parts[1])
                && parts[2].Length == SerialDigits
                && parts[2].All(char.IsAsciiDigit)
                && parts[2].Any(digit => digit != '0');
        }

        /// <summary>
        /// Checks a Syrian mobile number. Spaces or dashes between the digits are allowed, and the
        /// international forms +9639… and 009639… are accepted as well as 09….
        /// </summary>
        public static bool IsValidMobile(string? mobile)
        {
            return mobile is not null && ToLocalForm(mobile) is not null;
        }

        /// <summary>Rewrites a valid mobile number in its local form, 09 followed by eight digits.</summary>
        /// <exception cref="ArgumentException">The text is not a Syrian mobile number.</exception>
        public static string NormalizeMobile(string mobile)
        {
            return ToLocalForm(mobile) ?? throw new ArgumentException("Not a Syrian mobile number.", nameof(mobile));
        }

        /// <summary>
        /// Checks a person's name: two words or more, each of at least two letters (Arabic or
        /// Latin); a hyphen or an apostrophe may join letters inside a word, as in Al-Masri.
        /// </summary>
        public static bool IsValidFullName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            string tidy = Tidy(name);
            string[] words = tidy.Split(' ');
            return tidy.Length <= MaxFullNameLength && words.Length >= 2 && words.All(IsNameWord);
        }

        /// <summary>Checks that a device was received on a real day, not after <paramref name="today"/>.</summary>
        public static bool IsValidIntakeDate(DateTime receivedOn, DateTime today)
        {
            return receivedOn.Year >= FirstAcceptedYear && receivedOn.Date <= today.Date;
        }

        /// <summary>Checks that a promised date is on or after the intake day and at most 90 days later.</summary>
        public static bool IsValidPromise(DateTime receivedOn, DateTime promisedOn)
        {
            int days = (promisedOn.Date - receivedOn.Date).Days;
            return days >= 0 && days <= MaxPromiseDays;
        }

        /// <summary>Checks one entry of working time: more than zero and no more than a day.</summary>
        public static bool IsValidHours(decimal hours)
        {
            return hours > 0m && hours <= MaxHoursPerEntry;
        }

        /// <summary>Checks an amount of money: more than zero and below 100 million pounds.</summary>
        public static bool IsValidPrice(decimal amount)
        {
            return amount > 0m && amount < MaxPrice;
        }

        /// <summary>Parses day/month/year text (dd/MM/yyyy); 31/02 and the like give false.</summary>
        public static bool TryReadDate(string? text, out DateTime date)
        {
            return DateTime.TryParseExact(text?.Trim(), DatePattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        /// <summary>Removes spaces at both ends and shrinks every run of spaces inside to one.</summary>
        public static string Tidy(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            var result = new StringBuilder(text.Length);
            foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (result.Length > 0)
                {
                    result.Append(' ');
                }

                result.Append(word);
            }

            return result.ToString();
        }

        private static bool IsAcceptedYear(string text)
        {
            return text.Length == 4
                && text.All(char.IsAsciiDigit)
                && int.Parse(text, CultureInfo.InvariantCulture) is var year
                && year >= FirstAcceptedYear
                && year <= DateTime.Today.Year;
        }

        private static bool IsNameWord(string word)
        {
            bool joinedLetters = word.All(c => char.IsLetter(c) || c == '-' || c == '\'');
            return word.Length >= 2 && joinedLetters && char.IsLetter(word[0]) && char.IsLetter(word[^1]);
        }

        // 09xxxxxxxx for a Syrian mobile number in any accepted spelling; null for anything else.
        private static string? ToLocalForm(string text)
        {
            string digits = string.Concat(text.Where(c => c != ' ' && c != '-'));
            if (digits.StartsWith("+963", StringComparison.Ordinal))
            {
                digits = "0" + digits[4..];
            }
            else if (digits.StartsWith("00963", StringComparison.Ordinal))
            {
                digits = "0" + digits[5..];
            }

            bool isLocal = digits.Length == 10 && digits.StartsWith("09", StringComparison.Ordinal) && digits.All(char.IsAsciiDigit);
            return isLocal ? digits : null;
        }
    }
}
