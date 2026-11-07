using HomeFix.Core.Devices;
using HomeFix.Core.People;
using HomeFix.Core.Repairs;

// The static counters of HomeDevice are shared by the whole test run, so the tests run one at a time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace HomeFix.Tests
{
    /// <summary>Builds the objects the tests need, with valid values unless a test says otherwise.</summary>
    internal static class Make
    {
        private static int nextSerial = 100;

        public static DateTime Today => DateTime.Today;

        public static Client Client() => new("Dima Kassab", "0947315862");

        public static Technician Technician(decimal hourlyRate = 40_000m, bool gas = false) => new("Fadi Jabri", hourlyRate, gas);

        /// <summary>An intake card with a fresh serial number of the given kind.</summary>
        public static DeviceIntake Card(string prefix, int daysSinceIntake = 5, int promisedInDays = 5, DateTime? warrantyUntil = null, bool urgent = false)
        {
            nextSerial++;
            return new DeviceIntake
            {
                SerialNo = $"{prefix}-2024-{nextSerial:D5}",
                Brand = "Brand",
                ModelName = "Model 1",
                Client = Client(),
                ReceivedOn = Today.AddDays(-daysSinceIntake),
                PromisedOn = Today.AddDays(promisedInDays),
                WarrantyUntil = warrantyUntil,
                IsUrgent = urgent,
            };
        }

        public static Dishwasher Dishwasher(int placeSettings = 12, bool builtIn = false, DeviceIntake? card = null) =>
            new(card ?? Card("DW"), placeSettings, builtIn);

        public static WaterHeater WaterHeater(int liters = 50, HeatSource source = HeatSource.Electric, DeviceIntake? card = null) =>
            new(card ?? Card("WH"), liters, source);

        public static MicrowaveOven Microwave(int watts = 800, bool inverter = false, DeviceIntake? card = null) =>
            new(card ?? Card("MW"), watts, inverter);

        public static VacuumCleaner Vacuum(bool cordless = false, bool hepa = false, DeviceIntake? card = null) =>
            new(card ?? Card("VC"), cordless, hepa);

        /// <summary>Takes a device through diagnosis and onto the bench of <paramref name="technician"/>.</summary>
        public static T OnBench<T>(T device, Technician technician, decimal hours, params SparePart[] parts)
            where T : HomeDevice
        {
            device.Diagnose("fault found");
            device.BeginRepair(technician);
            foreach (SparePart part in parts)
            {
                device.InstallPart(part);
            }

            device.LogLabor(hours);
            return device;
        }
    }
}
