using HomeFix.App.Input;
using HomeFix.Core.Devices;
using HomeFix.Core.People;
using HomeFix.Core.Rules;
using HomeFix.Core.Workshop;

namespace HomeFix.App.Screens
{
    /// <summary>
    /// The intake counter: asks for the details of a dropped-off device and builds an object of the
    /// right kind. Each kind is one entry of a table that pairs its serial prefix with a builder, so
    /// a new kind of device needs one new entry and no other change here.
    /// </summary>
    internal sealed class IntakeForm
    {
        private readonly ConsolePrompt prompt;
        private readonly TextWriter output;
        private readonly List<KindOption> kinds;

        public IntakeForm(ConsolePrompt prompt, TextWriter output)
        {
            this.prompt = prompt;
            this.output = output;
            this.kinds = new List<KindOption>
            {
                new("Dishwasher", Dishwasher.SerialPrefix, intake => new Dishwasher(
                    intake, this.prompt.Number("Place settings", 4, 18), this.prompt.YesNo("Built into a kitchen cabinet?"))),
                new("Water heater", WaterHeater.SerialPrefix, intake => new WaterHeater(
                    intake, this.prompt.Number("Tank size in liters", 10, 300), this.AskHeatSource())),
                new("Microwave oven", MicrowaveOven.SerialPrefix, intake => new MicrowaveOven(
                    intake, this.prompt.Number("Power in watts", 500, 3000), this.prompt.YesNo("Inverter model?"))),
                new("Vacuum cleaner", VacuumCleaner.SerialPrefix, intake => new VacuumCleaner(
                    intake, this.prompt.YesNo("Cordless (battery)?"), this.prompt.YesNo("Has a HEPA filter?"))),
            };
        }

        /// <summary>Asks for everything on the intake card, builds the device and hands it to the workshop.</summary>
        public HomeDevice Fill(ServiceWorkshop workshop, DateTime today)
        {
            for (int i = 0; i < this.kinds.Count; i++)
            {
                this.output.WriteLine($"    {i + 1}. {this.kinds[i].Name}");
            }

            KindOption kind = this.kinds[this.prompt.Number("Kind of device", 1, this.kinds.Count) - 1];
            string serialNo = this.prompt.Text(
                $"Serial no., e.g. {kind.Prefix}-2025-00042",
                text => DataRules.IsValidSerial(text, kind.Prefix) && workshop.Find(text) is null,
                $"Use the form {kind.Prefix}-YYYY-NNNNN, for a device not already in the workshop.");
            string brand = this.prompt.Text("Brand");
            string modelName = this.prompt.Text("Model");
            Client client = this.FindOrRegisterClient(workshop);
            DateTime promisedOn = this.prompt.Date(
                "Promised ready on",
                day => DataRules.IsValidPromise(today, day),
                "Choose a day from today up to 90 days later.");
            DateTime? warrantyUntil = this.prompt.YesNo("Still under the maker's warranty?")
                ? this.prompt.Date("Warranty ends on", day => day >= today, "The warranty must still be running today.")
                : null;

            var intake = new DeviceIntake
            {
                SerialNo = serialNo,
                Brand = brand,
                ModelName = modelName,
                Client = client,
                ReceivedOn = today,
                PromisedOn = promisedOn,
                WarrantyUntil = warrantyUntil,
                IsUrgent = this.prompt.YesNo("Urgent job (+25 % on labor)?"),
            };

            HomeDevice device = kind.Build(intake);
            workshop.Receive(device);
            return device;
        }

        // A client who already left a device is found by mobile number instead of being registered twice.
        private Client FindOrRegisterClient(ServiceWorkshop workshop)
        {
            string mobile = this.prompt.Mobile("Client's mobile");

            HomeDevice? earlier = workshop.Devices.FirstOrDefault(device => device.Client.Mobile == mobile);
            if (earlier is not null)
            {
                this.output.WriteLine($"    Known client: {earlier.Client}");
                return earlier.Client;
            }

            string name = this.prompt.Text("Client's full name", DataRules.IsValidFullName, "First and last name, in letters.");
            return new Client(name, mobile);
        }

        private HeatSource AskHeatSource()
        {
            HeatSource[] sources = Enum.GetValues<HeatSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                this.output.WriteLine($"    {i + 1}. {sources[i]}");
            }

            return sources[this.prompt.Number("Heat source", 1, sources.Length) - 1];
        }

        // One kind of device the counter can receive: its name, its serial prefix and how to build it.
        private sealed record KindOption(string Name, string Prefix, Func<DeviceIntake, HomeDevice> Build);
    }
}
