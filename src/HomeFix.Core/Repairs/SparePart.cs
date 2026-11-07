using HomeFix.Core.Rules;

namespace HomeFix.Core.Repairs
{
    /// <summary>
    /// A spare part fitted into a device. It describes something that already happened,
    /// so every value is fixed by the constructor and only read afterwards.
    /// </summary>
    public sealed class SparePart
    {
        private const int MaxQuantity = 20;

        private readonly string name;
        private readonly decimal unitPrice;
        private readonly int quantity;

        /// <summary>Creates the record of a part; refuses an empty name, a bad price or quantity.</summary>
        /// <param name="name">What the part is, for example "drain pump".</param>
        /// <param name="unitPrice">The price of one piece, in Syrian pounds.</param>
        /// <param name="quantity">How many pieces were fitted (1 to 20).</param>
        public SparePart(string name, decimal unitPrice, int quantity = 1)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A spare part needs a name.", nameof(name));
            }

            if (!DataRules.IsValidPrice(unitPrice))
            {
                throw new ArgumentOutOfRangeException(nameof(unitPrice), "A part costs more than zero and less than 100 million pounds.");
            }

            if (quantity < 1 || quantity > MaxQuantity)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), $"Between 1 and {MaxQuantity} pieces can be fitted at once.");
            }

            this.name = DataRules.Tidy(name);
            this.unitPrice = unitPrice;
            this.quantity = quantity;
        }

        /// <summary>What the part is.</summary>
        public string Name => this.name;

        /// <summary>The price of one piece.</summary>
        public decimal UnitPrice => this.unitPrice;

        /// <summary>How many pieces were fitted.</summary>
        public int Quantity => this.quantity;

        /// <summary>The price of all the pieces together.</summary>
        public decimal LineTotal => this.unitPrice * this.quantity;

        /// <inheritdoc/>
        public override string ToString() => $"{this.quantity} x {this.name}";
    }
}
