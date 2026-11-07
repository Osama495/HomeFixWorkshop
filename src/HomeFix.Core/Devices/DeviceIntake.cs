using HomeFix.Core.People;

namespace HomeFix.Core.Devices
{
    /// <summary>
    /// The intake card filled in at the counter when a device is dropped off. Every device kind
    /// receives the same card, so the constructors of the device classes stay short and the
    /// checks of these values are written once, in <see cref="HomeDevice"/>.
    /// </summary>
    public sealed record DeviceIntake
    {
        /// <summary>The serial number printed on the device's label.</summary>
        public required string SerialNo { get; init; }

        /// <summary>The maker, for example "Ariston".</summary>
        public required string Brand { get; init; }

        /// <summary>The maker's model name.</summary>
        public required string ModelName { get; init; }

        /// <summary>The owner of the device.</summary>
        public required Client Client { get; init; }

        /// <summary>The day the device was dropped off.</summary>
        public required DateTime ReceivedOn { get; init; }

        /// <summary>The day the workshop promises to have it ready.</summary>
        public required DateTime PromisedOn { get; init; }

        /// <summary>The last day of the manufacturer's warranty, when the device has one.</summary>
        public DateTime? WarrantyUntil { get; init; }

        /// <summary>True when the client pays extra for the job to go first.</summary>
        public bool IsUrgent { get; init; }
    }
}
