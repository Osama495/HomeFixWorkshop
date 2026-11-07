using System.Globalization;

namespace HomeFix.Core.Rules
{
    /// <summary>
    /// The charges that apply to every kind of device, in Syrian pounds. They are constants, so no
    /// part of the program can change a price while it runs; charges that belong to one kind of
    /// device are kept in that device's class.
    /// </summary>
    public static class Tariff
    {
        private const decimal DiagnosisFeeAmount = 50_000m;
        private const decimal UrgentLaborMarkupRate = 0.25m;
        private const string CurrencyCode = "SYP";

        /// <summary>The price of inspecting a device and writing down its fault.</summary>
        public static decimal DiagnosisFee => DiagnosisFeeAmount;

        /// <summary>The share added to the labor of an urgent job (0.25 = 25 %).</summary>
        public static decimal UrgentLaborMarkup => UrgentLaborMarkupRate;

        /// <summary>Writes an amount the way receipts show it, for example "165,000 SYP".</summary>
        public static string Format(decimal amount)
        {
            return amount.ToString("N0", CultureInfo.InvariantCulture) + " " + CurrencyCode;
        }
    }
}
