using HomeFix.Core.People;
using HomeFix.Core.Rules;

namespace HomeFix.Tests
{
    /// <summary>The static validation class DataRules, and the receipt format of Tariff.</summary>
    public class RulesTests
    {
        [Theory]
        [InlineData("DW-2025-00418", "DW", true)]
        [InlineData("WH-2000-00001", "WH", true)]
        [InlineData("DW-2025-00418", "WH", false)]
        [InlineData("dw-2025-00418", "DW", false)]
        [InlineData("DW-1999-00418", "DW", false)]
        [InlineData("DW-2025-0418", "DW", false)]
        [InlineData("DW-2025-00000", "DW", false)]
        [InlineData("DW-2025-004A8", "DW", false)]
        [InlineData("DW-2025-00418-1", "DW", false)]
        [InlineData("DW202500418", "DW", false)]
        [InlineData("", "DW", false)]
        [InlineData(null, "DW", false)]
        [InlineData("D1-2025-00418", "D1", false)]
        public void Serial_numbers(string? serialNo, string prefix, bool valid)
        {
            Assert.Equal(valid, DataRules.IsValidSerial(serialNo, prefix));
        }

        [Fact]
        public void A_serial_number_cannot_carry_a_year_still_to_come()
        {
            int thisYear = DateTime.Today.Year;

            Assert.True(DataRules.IsValidSerial($"MW-{thisYear}-00001", "MW"));
            Assert.False(DataRules.IsValidSerial($"MW-{thisYear + 1}-00001", "MW"));
        }

        [Theory]
        [InlineData("0947315862", "0947315862")]
        [InlineData("0947 315 862", "0947315862")]
        [InlineData("0993-271-640", "0993271640")]
        [InlineData("+963947315862", "0947315862")]
        [InlineData("+963 958 604 117", "0958604117")]
        [InlineData("00963936508271", "0936508271")]
        public void Mobile_numbers_are_kept_in_their_local_form(string typed, string kept)
        {
            Assert.True(DataRules.IsValidMobile(typed));
            Assert.Equal(kept, DataRules.NormalizeMobile(typed));
            Assert.Equal(kept, new Client("Dima Kassab", typed).Mobile);
        }

        [Theory]
        [InlineData("0731586422")]
        [InlineData("094731586")]
        [InlineData("09473158627")]
        [InlineData("+962944123456")]
        [InlineData("09473158a2")]
        [InlineData("")]
        public void Other_numbers_are_refused(string typed)
        {
            Assert.False(DataRules.IsValidMobile(typed));
            Assert.Throws<ArgumentException>(() => DataRules.NormalizeMobile(typed));
        }

        [Fact]
        public void A_missing_mobile_number_is_refused()
        {
            Assert.False(DataRules.IsValidMobile(null));
        }

        [Theory]
        [InlineData("Dima Kassab", true)]
        [InlineData("  Adnan   Rifai ", true)]
        [InlineData("Ziad Al-Masri", true)]
        [InlineData("Rania O'Hara", true)]
        [InlineData("سلمى القدسي", true)]
        [InlineData("Hiba", false)]
        [InlineData("Z Rifai", false)]
        [InlineData("Hiba Z4im", false)]
        [InlineData("Hiba -Zaim", false)]
        [InlineData("Dima Kassab-", false)]
        [InlineData("   ", false)]
        [InlineData(null, false)]
        public void Person_names(string? name, bool valid)
        {
            Assert.Equal(valid, DataRules.IsValidFullName(name));
        }

        [Fact]
        public void A_name_longer_than_sixty_characters_is_refused()
        {
            string longName = new string('a', 40) + " " + new string('b', 30);

            Assert.False(DataRules.IsValidFullName(longName));
        }

        [Fact]
        public void A_device_is_received_on_a_real_day_that_has_come()
        {
            var today = new DateTime(2026, 10, 28);

            Assert.True(DataRules.IsValidIntakeDate(today, today));
            Assert.True(DataRules.IsValidIntakeDate(today.AddDays(-30), today));
            Assert.False(DataRules.IsValidIntakeDate(today.AddDays(1), today));
            Assert.False(DataRules.IsValidIntakeDate(new DateTime(1999, 12, 31), today));
        }

        [Fact]
        public void A_promise_falls_within_ninety_days_after_intake()
        {
            var receivedOn = new DateTime(2026, 10, 28);

            Assert.True(DataRules.IsValidPromise(receivedOn, receivedOn));
            Assert.True(DataRules.IsValidPromise(receivedOn, receivedOn.AddDays(90)));
            Assert.False(DataRules.IsValidPromise(receivedOn, receivedOn.AddDays(91)));
            Assert.False(DataRules.IsValidPromise(receivedOn, receivedOn.AddDays(-1)));
        }

        [Theory]
        [InlineData(0.25, true)]
        [InlineData(24, true)]
        [InlineData(0, false)]
        [InlineData(-2, false)]
        [InlineData(24.5, false)]
        public void Working_hours(double hours, bool valid)
        {
            Assert.Equal(valid, DataRules.IsValidHours((decimal)hours));
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(99_999_999, true)]
        [InlineData(0, false)]
        [InlineData(-5, false)]
        [InlineData(100_000_000, false)]
        public void Prices(int amount, bool valid)
        {
            Assert.Equal(valid, DataRules.IsValidPrice(amount));
        }

        [Theory]
        [InlineData("05/11/2026", 2026, 11, 5)]
        [InlineData(" 29/02/2028 ", 2028, 2, 29)]
        public void Dates_are_typed_day_first(string text, int year, int month, int day)
        {
            Assert.True(DataRules.TryReadDate(text, out DateTime date));
            Assert.Equal(new DateTime(year, month, day), date);
        }

        [Theory]
        [InlineData("31/02/2026")]
        [InlineData("29/02/2027")]
        [InlineData("2026-11-05")]
        [InlineData("05.11.2026")]
        [InlineData("")]
        [InlineData(null)]
        public void Other_dates_are_refused(string? text)
        {
            Assert.False(DataRules.TryReadDate(text, out _));
        }

        [Fact]
        public void Tidy_removes_extra_spaces()
        {
            Assert.Equal("Ariston Velis Evo", DataRules.Tidy("  Ariston   Velis  Evo "));
            Assert.Equal(string.Empty, DataRules.Tidy("   "));
        }

        [Theory]
        [InlineData(1_234_567, "1,234,567 SYP")]
        [InlineData(409_375, "409,375 SYP")]
        [InlineData(0, "0 SYP")]
        public void Receipts_write_amounts_with_thousands_separators(int amount, string written)
        {
            Assert.Equal(written, Tariff.Format(amount));
        }
    }
}
