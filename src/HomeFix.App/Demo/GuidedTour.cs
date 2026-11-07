using HomeFix.App.Output;
using HomeFix.Core.Devices;
using HomeFix.Core.People;
using HomeFix.Core.Repairs;
using HomeFix.Core.Rules;
using HomeFix.Core.Workshop;

namespace HomeFix.App.Demo
{
    /// <summary>
    /// A short story of one working day in the workshop, told step by step: two technicians, four
    /// clients and four kinds of device. It shows every requirement of the assignment at work and
    /// leaves the workshop with jobs at several stages, ready for the menu. Dates are counted back
    /// from <c>today</c>, so the story stays true whatever day it is run.
    /// </summary>
    internal static class GuidedTour
    {
        private const string WorkshopName = "HomeFix Workshop - Aleppo";

        /// <summary>Plays the story; <paramref name="connect"/> may attach listeners before anything happens.</summary>
        public static ServiceWorkshop Play(TextWriter output, DateTime today, Action<ServiceWorkshop>? connect = null)
        {
            var workshop = new ServiceWorkshop(WorkshopName);
            connect?.Invoke(workshop);
            var printer = new BoardPrinter(output);

            output.WriteLine($"{workshop.Name} - guided tour, {today:dd/MM/yyyy}");
            output.WriteLine(new string('=', 60));

            Step(output, 1, "Staff and intake");
            var fadi = new Technician("Fadi Jabri", 45_000m);
            var ruba = new Technician("Ruba Atassi", 55_000m, isGasCertified: true);
            Hire(workshop, fadi, output);
            Hire(workshop, ruba, output);

            var dishwasher = new Dishwasher(Card("DW-2025-00418", "Electrolux", "EEM48321L", new Client("Dima Kassab", "0947 315 862"), today, -9, -2), 14, isBuiltIn: true);
            var heater = new WaterHeater(Card("WH-2024-00077", "Ariston", "Pro1 Eco 80", new Client("Tarek Hijazi", "0936508271"), today, -8, 3) with { WarrantyUntil = today.AddDays(200) }, 80, HeatSource.Gas);
            var microwave = new MicrowaveOven(Card("MW-2023-01290", "Panasonic", "NN-ST34", new Client("Salma Qudsi", "+963 958 604 117"), today, -7, -3), 1000, isInverter: true);
            var vacuum = new VacuumCleaner(Card("VC-2025-00031", "Dyson", "V11", new Client("Wael Halabi", "0993-271-640"), today, -3, 2) with { IsUrgent = true }, isCordless: true, hasHepaFilter: true);
            foreach (HomeDevice device in new HomeDevice[] { dishwasher, heater, microwave, vacuum })
            {
                workshop.Receive(device);
                output.WriteLine($"  Received {device} for {device.Client}, promised {device.PromisedOn:dd/MM/yyyy}");
            }

            Step(output, 2, "Inspection");
            Diagnose(dishwasher, "drain pump blocked, door seal torn", output);
            Diagnose(heater, "pilot flame goes out: thermocouple worn", output);
            Diagnose(microwave, "no heating: magnetron burnt", output);
            Diagnose(vacuum, "runs for two minutes only: battery cells worn", output);

            Step(output, 3, "Repairs - an event texts the client as soon as a repair is done");
            Repair(dishwasher, fadi, 2.5m, output, new SparePart("drain pump", 85_000m), new SparePart("door seal", 40_000m));
            try
            {
                heater.BeginRepair(fadi);
            }
            catch (InvalidOperationException refused)
            {
                output.WriteLine($"  Refused: {refused.Message}");
            }

            Repair(heater, ruba, 1.5m, output, new SparePart("thermocouple", 35_000m));
            microwave.WaitForParts("magnetron 2M246");
            output.WriteLine($"  {microwave.SerialNo} waits for a part: {microwave.MissingPart}");
            vacuum.BeginRepair(fadi);
            vacuum.InstallPart(new SparePart("battery pack", 120_000m));
            vacuum.LogLabor(1m);
            output.WriteLine($"  {vacuum.SerialNo} is on the bench of {fadi.Name} (urgent job, not finished yet)");

            Step(output, 4, "One list, four kinds of device: each describes and prices itself");
            printer.PrintBoard(workshop);

            Step(output, 5, $"Deadline review on {today:dd/MM/yyyy} - an event alerts the manager");
            int late = workshop.ReviewDeadlines(today);
            output.WriteLine($"  Late devices reported: {late}");
            output.WriteLine($"  Reviewed again: {workshop.ReviewDeadlines(today)} new (a late device is reported once)");

            Step(output, 6, "Pickup");
            dishwasher.HandOver(today);
            output.WriteLine($"  {dishwasher.SerialNo} handed back to {dishwasher.Client.FullName}; paid {Tariff.Format(dishwasher.ComputeRepairCost())}");
            printer.PrintList("Repaired devices", workshop.Repaired());
            printer.PrintList("Still in service", workshop.StillInService());

            Step(output, 7, "Searches written as delegates");
            printer.PrintList("Under warranty", workshop.FindAll(device => device.IsUnderWarranty));
            printer.PrintList("Urgent jobs", workshop.FindAll(device => device.IsUrgent));
            printer.PrintList("Cost above 200,000 SYP", workshop.FindAll(CostsMoreThan200K));

            Step(output, 8, "Statistics kept by static members");
            printer.PrintStatistics();

            Step(output, 9, "A new promise for the late microwave");
            workshop.Postpone(microwave.SerialNo, today.AddDays(5));
            output.WriteLine($"  {microwave.SerialNo} is now promised for {microwave.PromisedOn:dd/MM/yyyy}; late now: {workshop.ReviewDeadlines(today)}");

            return workshop;
        }

        // A method with the shape of DeviceFilter, passed by name to FindAll.
        private static bool CostsMoreThan200K(HomeDevice device)
        {
            return device.ComputeRepairCost() > 200_000m;
        }

        private static DeviceIntake Card(string serialNo, string brand, string model, Client client, DateTime today, int daysSinceIntake, int promisedInDays)
        {
            return new DeviceIntake
            {
                SerialNo = serialNo,
                Brand = brand,
                ModelName = model,
                Client = client,
                ReceivedOn = today.AddDays(daysSinceIntake),
                PromisedOn = today.AddDays(promisedInDays),
            };
        }

        private static void Step(TextWriter output, int number, string title)
        {
            output.WriteLine();
            output.WriteLine($"Step {number} - {title}");
        }

        private static void Hire(ServiceWorkshop workshop, Technician technician, TextWriter output)
        {
            workshop.Hire(technician);
            string gas = technician.IsGasCertified ? ", gas certified" : string.Empty;
            output.WriteLine($"  Hired {technician.Name}: {Tariff.Format(technician.HourlyRate)} per hour{gas}");
        }

        private static void Diagnose(HomeDevice device, string findings, TextWriter output)
        {
            device.Diagnose(findings);
            output.WriteLine($"  {device.SerialNo}: {device.Findings}");
        }

        private static void Repair(HomeDevice device, Technician technician, decimal hours, TextWriter output, params SparePart[] parts)
        {
            device.BeginRepair(technician);
            foreach (SparePart part in parts)
            {
                device.InstallPart(part);
            }

            device.LogLabor(hours);
            output.WriteLine($"  {device.SerialNo} repaired by {technician.Name}: {string.Join(", ", parts.Select(part => part.Name))}; {hours} h");
            device.FinishRepair();
        }
    }
}
