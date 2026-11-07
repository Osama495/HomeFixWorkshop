using HomeFix.Core.Rules;

namespace HomeFix.Core.Devices
{
    /// <summary>
    /// A dishwasher. Built-in units have to be pulled out of the kitchen cabinet before anyone
    /// can work on them, and every dishwasher is run once with water before it goes home.
    /// </summary>
    public sealed class Dishwasher : HomeDevice
    {
        private const string Prefix = "DW";
        private const int MinPlaceSettings = 4;
        private const int MaxPlaceSettings = 18;
        private const int LargeRackFrom = 14;              // place settings
        private const decimal LargeRackDifficulty = 1.15m; // a large tub is heavier to strip down
        private const decimal CabinetRemovalFee = 75_000m;
        private const decimal LeakTestFee = 30_000m;

        private readonly int placeSettings;
        private readonly bool isBuiltIn;

        /// <summary>Receives a dishwasher.</summary>
        /// <param name="intake">The intake card; its serial number starts with DW.</param>
        /// <param name="placeSettings">How many place settings the racks hold (4 to 18).</param>
        /// <param name="isBuiltIn">True when the unit is fitted inside a kitchen cabinet.</param>
        public Dishwasher(DeviceIntake intake, int placeSettings, bool isBuiltIn)
            : base(Prefix, intake)
        {
            if (placeSettings < MinPlaceSettings || placeSettings > MaxPlaceSettings)
            {
                throw new ArgumentOutOfRangeException(nameof(placeSettings), $"A dishwasher holds {MinPlaceSettings} to {MaxPlaceSettings} place settings.");
            }

            this.placeSettings = placeSettings;
            this.isBuiltIn = isBuiltIn;
        }

        /// <summary>The two letters every dishwasher serial number starts with.</summary>
        public static string SerialPrefix => Prefix;

        /// <summary>How many place settings the racks hold.</summary>
        public int PlaceSettings => this.placeSettings;

        /// <summary>True when the unit is fitted inside a kitchen cabinet.</summary>
        public bool IsBuiltIn => this.isBuiltIn;

        /// <inheritdoc/>
        public override string Kind => "Dishwasher";

        /// <summary>
        /// Diagnosis, parts and labor (heavier for a large tub), all covered by a warranty; the leak
        /// test and, for a built-in unit, taking it out of the cabinet are always paid.
        /// </summary>
        public override decimal ComputeRepairCost()
        {
            decimal difficulty = this.placeSettings >= LargeRackFrom ? LargeRackDifficulty : 1m;
            decimal removal = this.isBuiltIn ? CabinetRemovalFee : 0m;
            decimal repair = Tariff.DiagnosisFee + this.PartsTotal + this.LaborCharge(difficulty);

            return this.Bill(repair, LeakTestFee + removal);
        }

        /// <inheritdoc/>
        public override string Describe()
        {
            string fitting = this.isBuiltIn ? "built-in" : "freestanding";
            return $"{base.Describe()} ({this.placeSettings} place settings, {fitting})";
        }
    }
}
