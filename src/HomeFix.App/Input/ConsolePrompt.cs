using System.Globalization;
using HomeFix.Core.Rules;

namespace HomeFix.App.Input
{
    /// <summary>
    /// Questions on the console. A question is repeated, with a hint, until the answer satisfies
    /// its rule. Input and output are a <see cref="TextReader"/> and a <see cref="TextWriter"/>,
    /// which lets the tests answer from a string.
    /// </summary>
    internal sealed class ConsolePrompt
    {
        private readonly TextReader input;
        private readonly TextWriter output;

        public ConsolePrompt(TextReader input, TextWriter output)
        {
            this.input = input;
            this.output = output;
        }

        /// <summary>Asks for text that <paramref name="rule"/> accepts; shows <paramref name="hint"/> after a wrong answer.</summary>
        public string Text(string label, Func<string, bool> rule, string hint)
        {
            while (true)
            {
                string answer = this.Ask(label);
                if (rule(answer))
                {
                    return answer;
                }

                this.output.WriteLine("    " + hint);
            }
        }

        /// <summary>Asks for any text that is not empty.</summary>
        public string Text(string label)
        {
            return this.Text(label, answer => answer.Length > 0, "Please type something.");
        }

        /// <summary>Asks for a whole number between <paramref name="min"/> and <paramref name="max"/>.</summary>
        public int Number(string label, int min, int max)
        {
            string answer = this.Text(
                $"{label} ({min}-{max})",
                text => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) && value >= min && value <= max,
                $"Type a whole number from {min} to {max}.");
            return int.Parse(answer, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Asks for an amount or a number of hours that <paramref name="rule"/> accepts. Only digits
        /// and a decimal point are read: a comma is refused, because "1,5" could mean 1.5 or 15.
        /// </summary>
        public decimal Amount(string label, Func<decimal, bool> rule, string hint)
        {
            const NumberStyles Plain = NumberStyles.AllowDecimalPoint;
            string answer = this.Text(
                label,
                text => decimal.TryParse(text, Plain, CultureInfo.InvariantCulture, out decimal value) && rule(value),
                hint);
            return decimal.Parse(answer, Plain, CultureInfo.InvariantCulture);
        }

        /// <summary>Asks for a date typed as dd/MM/yyyy that <paramref name="rule"/> accepts.</summary>
        public DateTime Date(string label, Func<DateTime, bool> rule, string hint)
        {
            string answer = this.Text(
                $"{label} ({DataRules.DateInputFormat})",
                text => DataRules.TryReadDate(text, out DateTime day) && rule(day),
                hint);
            DataRules.TryReadDate(answer, out DateTime date);
            return date;
        }

        /// <summary>Asks for a Syrian mobile number and returns it in its local form, 09xxxxxxxx.</summary>
        public string Mobile(string label)
        {
            string answer = this.Text(label, DataRules.IsValidMobile, "A mobile number is 09 followed by eight digits.");
            return DataRules.NormalizeMobile(answer);
        }

        /// <summary>Asks for an amount of money in Syrian pounds.</summary>
        public decimal Money(string label)
        {
            return this.Amount(label, DataRules.IsValidPrice, "A positive amount in Syrian pounds, digits only: 85000");
        }

        /// <summary>Asks a yes/no question.</summary>
        public bool YesNo(string label)
        {
            string answer = this.Text(
                label + " (y/n)",
                text => text is "y" or "Y" or "n" or "N",
                "Please type y (yes) or n (no).");
            return answer is "y" or "Y";
        }

        private string Ask(string label)
        {
            this.output.Write("  " + label + ": ");
            string? line = this.input.ReadLine();
            if (line is null)
            {
                // ReadLine gives null at the end of the input (a redirected file that ran out, or Ctrl+Z).
                throw new EndOfStreamException("The input ended.");
            }

            return line.Trim();
        }
    }
}
