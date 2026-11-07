namespace HomeFix.Core.Devices
{
    /// <summary>
    /// A vacuum cleaner. It is a small device, so a short bench check replaces the full diagnosis
    /// fee; a cordless model gets its battery tested, a corded one its cable and plug, and a HEPA
    /// filter is serviced in every job.
    /// </summary>
    public sealed class VacuumCleaner : HomeDevice
    {
        private const string Prefix = "VC";
        private const decimal BenchCheckFee = 25_000m;
        private const decimal BatteryTestFee = 30_000m;
        private const decimal CordTestFee = 10_000m;
        private const decimal HepaServiceFee = 15_000m;

        private readonly bool isCordless;
        private readonly bool hasHepaFilter;

        /// <summary>Receives a vacuum cleaner.</summary>
        /// <param name="intake">The intake card; its serial number starts with VC.</param>
        /// <param name="isCordless">True for a battery model.</param>
        /// <param name="hasHepaFilter">True when the cleaner has a HEPA filter.</param>
        public VacuumCleaner(DeviceIntake intake, bool isCordless, bool hasHepaFilter)
            : base(Prefix, intake)
        {
            this.isCordless = isCordless;
            this.hasHepaFilter = hasHepaFilter;
        }

        /// <summary>The two letters every vacuum cleaner serial number starts with.</summary>
        public static string SerialPrefix => Prefix;

        /// <summary>True for a battery model.</summary>
        public bool IsCordless => this.isCordless;

        /// <summary>True when the cleaner has a HEPA filter.</summary>
        public bool HasHepaFilter => this.hasHepaFilter;

        /// <inheritdoc/>
        public override string Kind => "Vacuum Cleaner";

        /// <summary>
        /// The bench check, parts, labor and the filter service are covered by a warranty; the
        /// electrical test (battery or cord) is always paid.
        /// </summary>
        public override decimal ComputeRepairCost()
        {
            decimal electricalTest = this.isCordless ? BatteryTestFee : CordTestFee;
            decimal filter = this.hasHepaFilter ? HepaServiceFee : 0m;
            decimal repair = BenchCheckFee + this.PartsTotal + this.LaborCharge(1m) + filter;

            return this.Bill(repair, electricalTest);
        }

        /// <inheritdoc/>
        public override string Describe()
        {
            string power = this.isCordless ? "cordless" : "corded";
            string filter = this.hasHepaFilter ? ", HEPA" : string.Empty;
            return $"{base.Describe()} ({power}{filter})";
        }
    }
}
