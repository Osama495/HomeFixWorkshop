using HomeFix.Core.People;
using HomeFix.Core.Rules;

namespace HomeFix.Core.Devices
{
    /// <summary>
    /// A water heater. Its tank is always descaled, and the price grows with the tank; a gas heater
    /// must be opened by a certified technician and gets a safety check, and a solar heater is
    /// repaired where it stands, on the roof.
    /// </summary>
    public sealed class WaterHeater : HomeDevice
    {
        private const string Prefix = "WH";
        private const int MinTankLiters = 10;
        private const int MaxTankLiters = 300;
        private const decimal DescalingFeePer10Liters = 8_000m;
        private const decimal GasDifficulty = 1.40m;    // the burner and its valve take longer to service
        private const decimal GasSafetyCheckFee = 60_000m;
        private const decimal RoofAccessFee = 90_000m;

        private readonly int tankLiters;
        private readonly HeatSource heatSource;

        /// <summary>Receives a water heater.</summary>
        /// <param name="intake">The intake card; its serial number starts with WH.</param>
        /// <param name="tankLiters">The size of the tank (10 to 300 liters).</param>
        /// <param name="heatSource">How the heater heats its water.</param>
        public WaterHeater(DeviceIntake intake, int tankLiters, HeatSource heatSource)
            : base(Prefix, intake)
        {
            if (tankLiters < MinTankLiters || tankLiters > MaxTankLiters)
            {
                throw new ArgumentOutOfRangeException(nameof(tankLiters), $"A tank holds {MinTankLiters} to {MaxTankLiters} liters.");
            }

            if (!Enum.IsDefined(heatSource))
            {
                throw new ArgumentOutOfRangeException(nameof(heatSource), "Unknown heat source.");
            }

            this.tankLiters = tankLiters;
            this.heatSource = heatSource;
        }

        /// <summary>The two letters every water heater serial number starts with.</summary>
        public static string SerialPrefix => Prefix;

        /// <summary>The size of the tank, in liters.</summary>
        public int TankLiters => this.tankLiters;

        /// <summary>How the heater heats its water.</summary>
        public HeatSource HeatSource => this.heatSource;

        /// <inheritdoc/>
        public override string Kind => "Water Heater";

        /// <summary>
        /// Diagnosis, parts, labor (heavier on gas) and descaling by the tank's size are covered by
        /// a warranty; the gas safety check or the roof visit is always paid.
        /// </summary>
        public override decimal ComputeRepairCost()
        {
            decimal difficulty = this.heatSource == HeatSource.Gas ? GasDifficulty : 1m;
            decimal descaling = Math.Ceiling(this.tankLiters / 10m) * DescalingFeePer10Liters;
            decimal onSite = this.heatSource switch
            {
                HeatSource.Gas => GasSafetyCheckFee,
                HeatSource.Solar => RoofAccessFee,
                _ => 0m,
            };
            decimal repair = Tariff.DiagnosisFee + this.PartsTotal + this.LaborCharge(difficulty) + descaling;

            return this.Bill(repair, onSite);
        }

        /// <inheritdoc/>
        public override string Describe()
        {
            return $"{base.Describe()} ({this.tankLiters} L, {this.heatSource.ToString().ToLowerInvariant()})";
        }

        /// <summary>A gas heater is opened only by a technician certified for gas appliances.</summary>
        protected override void CheckTechnician(Technician technician)
        {
            if (this.heatSource == HeatSource.Gas && !technician.IsGasCertified)
            {
                throw new InvalidOperationException($"{technician.Name} is not certified for gas; {this.SerialNo} needs a gas technician.");
            }
        }
    }
}
