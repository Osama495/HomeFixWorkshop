using System.Globalization;
using HomeFix.Core.Devices;
using HomeFix.Core.Repairs;
using HomeFix.Core.Rules;
using HomeFix.Core.Workshop;

namespace HomeFix.App.Output
{
    /// <summary>Prints the workshop's board, lists of devices and the statistics of the whole program.</summary>
    internal sealed class BoardPrinter
    {
        private readonly TextWriter output;

        public BoardPrinter(TextWriter output)
        {
            this.output = output;
        }

        /// <summary>The status board of every device, then the total cost of all repairs.</summary>
        public void PrintBoard(ServiceWorkshop workshop)
        {
            this.output.WriteLine(string.Format(CultureInfo.InvariantCulture, "  {0,-14} {1,-58} {2,-15} {3,13}", "Serial", "Device", "Stage", "Cost"));
            this.output.WriteLine("  " + new string('-', 103));
            foreach (string line in workshop.StatusBoard())
            {
                this.output.WriteLine("  " + line);
            }

            this.output.WriteLine("  " + new string('-', 103));
            this.output.WriteLine($"  Total of all repairs: {Tariff.Format(workshop.TotalRepairCost())}");
        }

        /// <summary>A titled list of devices, one per line; prints "(none)" for an empty list.</summary>
        public void PrintList(string title, IReadOnlyList<HomeDevice> devices)
        {
            this.output.WriteLine($"  {title}: {devices.Count}");
            if (devices.Count == 0)
            {
                this.output.WriteLine("    (none)");
            }

            foreach (HomeDevice device in devices)
            {
                this.output.WriteLine($"    {device}  [{device.Stage}]");
            }
        }

        /// <summary>The counts and the total read from the static members of <see cref="HomeDevice"/>.</summary>
        public void PrintStatistics()
        {
            this.output.WriteLine($"  Devices still in the workshop : {HomeDevice.InWorkshopCount}");
            this.output.WriteLine($"  Ready for pickup              : {HomeDevice.ReadyForPickupCount}");
            foreach (RepairStage stage in Enum.GetValues<RepairStage>())
            {
                this.output.WriteLine($"    {stage,-15} {HomeDevice.CountAt(stage)}");
            }

            this.output.WriteLine($"  Collected for closed jobs     : {Tariff.Format(HomeDevice.TotalCollected)}");
        }
    }
}
