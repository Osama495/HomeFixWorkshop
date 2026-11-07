using HomeFix.Core.Rules;

namespace HomeFix.Core.Devices
{
    /// <summary>
    /// A microwave oven. Its high-voltage capacitor is discharged before anyone touches the inside,
    /// and every repaired oven is tested for radiation leakage; inverter models also need their
    /// power board calibrated.
    /// </summary>
    public sealed class MicrowaveOven : HomeDevice
    {
        private const string Prefix = "MW";
        private const int MinPowerWatts = 500;
        private const int MaxPowerWatts = 3_000;
        private const decimal CapacitorDischargeFee = 20_000m;
        private const decimal LeakageTestFee = 35_000m;
        private const decimal InverterCalibrationFee = 45_000m;

        private readonly int powerWatts;
        private readonly bool isInverter;

        /// <summary>Receives a microwave oven.</summary>
        /// <param name="intake">The intake card; its serial number starts with MW.</param>
        /// <param name="powerWatts">The cooking power (500 to 3000 watts).</param>
        /// <param name="isInverter">True for an inverter model.</param>
        public MicrowaveOven(DeviceIntake intake, int powerWatts, bool isInverter)
            : base(Prefix, intake)
        {
            if (powerWatts < MinPowerWatts || powerWatts > MaxPowerWatts)
            {
                throw new ArgumentOutOfRangeException(nameof(powerWatts), $"A microwave oven has {MinPowerWatts} to {MaxPowerWatts} watts.");
            }

            this.powerWatts = powerWatts;
            this.isInverter = isInverter;
        }

        /// <summary>The two letters every microwave serial number starts with.</summary>
        public static string SerialPrefix => Prefix;

        /// <summary>The cooking power, in watts.</summary>
        public int PowerWatts => this.powerWatts;

        /// <summary>True for an inverter model.</summary>
        public bool IsInverter => this.isInverter;

        /// <inheritdoc/>
        public override string Kind => "Microwave Oven";

        /// <summary>
        /// Diagnosis, parts, labor and the inverter calibration are covered by a warranty; the two
        /// safety steps, discharging the capacitor and the leakage test, are always paid.
        /// </summary>
        public override decimal ComputeRepairCost()
        {
            decimal calibration = this.isInverter ? InverterCalibrationFee : 0m;
            decimal repair = Tariff.DiagnosisFee + this.PartsTotal + this.LaborCharge(1m) + calibration;

            return this.Bill(repair, CapacitorDischargeFee + LeakageTestFee);
        }

        /// <inheritdoc/>
        public override string Describe()
        {
            string board = this.isInverter ? "inverter" : "transformer";
            return $"{base.Describe()} ({this.powerWatts} W, {board})";
        }
    }
}
