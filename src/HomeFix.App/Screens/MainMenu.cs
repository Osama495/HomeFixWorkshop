using HomeFix.App.Input;
using HomeFix.App.Output;
using HomeFix.Core.Devices;
using HomeFix.Core.People;
using HomeFix.Core.Repairs;
using HomeFix.Core.Rules;
using HomeFix.Core.Workshop;

namespace HomeFix.App.Screens
{
    /// <summary>
    /// The workshop's main menu. Every option is a delegate (<see cref="Action"/>) stored with its key
    /// and title, so the loop that shows and runs the options does not change when one is added.
    /// </summary>
    internal sealed class MainMenu
    {
        private readonly ServiceWorkshop workshop;
        private readonly ConsolePrompt prompt;
        private readonly TextWriter output;
        private readonly BoardPrinter printer;
        private readonly IntakeForm intakeForm;
        private readonly Func<DateTime> today;
        private readonly List<MenuOption> options;

        public MainMenu(ServiceWorkshop workshop, ConsolePrompt prompt, TextWriter output, Func<DateTime> today)
        {
            this.workshop = workshop;
            this.prompt = prompt;
            this.output = output;
            this.today = today;
            this.printer = new BoardPrinter(output);
            this.intakeForm = new IntakeForm(prompt, output);
            this.options = new List<MenuOption>
            {
                new("1", "Status board and total cost", () => this.printer.PrintBoard(this.workshop)),
                new("2", "Receive a device", this.ReceiveDevice),
                new("3", "Write down a diagnosis", () => this.PickDevice().Diagnose(this.prompt.Text("Findings"))),
                new("4", "Put a job on hold for a part", () => this.PickDevice().WaitForParts(this.prompt.Text("Missing part"))),
                new("5", "Put a device on the bench", () => this.PickDevice().BeginRepair(this.PickTechnician())),
                new("6", "Fit a spare part", () => this.PickDevice().InstallPart(this.AskPart())),
                new("7", "Log working hours", () => this.PickDevice().LogLabor(
                    this.prompt.Amount("Hours", DataRules.IsValidHours, "More than 0 and at most 24 hours, with a dot for parts of an hour: 1.5"))),
                new("8", "Finish a repair", () => this.PickDevice().FinishRepair()),
                new("9", "Hand a device back", this.HandBack),
                new("10", "Postpone a promised date", this.Postpone),
                new("11", "Review deadlines", this.ReviewDeadlines),
                new("12", "Repaired and in-service lists", this.ShowLists),
                new("13", "Statistics of the whole program", () => this.printer.PrintStatistics()),
                new("14", "Search the devices", this.Search),
            };
        }

        /// <summary>Shows the menu until the user chooses 0 or the input ends.</summary>
        public void Run()
        {
            this.output.WriteLine($"{this.workshop.Name} - today is {this.today():dd/MM/yyyy}.");
            while (true)
            {
                this.output.WriteLine();
                foreach (MenuOption option in this.options)
                {
                    this.output.WriteLine($"  {option.Key,2}. {option.Title}");
                }

                this.output.WriteLine("   0. Close HomeFix");
                string choice = this.prompt.Text("Choose");
                if (choice == "0")
                {
                    return;
                }

                MenuOption? chosen = this.options.Find(option => option.Key == choice);
                if (chosen is null)
                {
                    this.output.WriteLine("  There is no such option.");
                    continue;
                }

                try
                {
                    chosen.Run();
                }
                catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException)
                {
                    // A refused step is told to the user; the menu goes on.
                    this.output.WriteLine("  Not done: " + error.Message);
                }
            }
        }

        private void ReceiveDevice()
        {
            HomeDevice device = this.intakeForm.Fill(this.workshop, this.today());
            this.output.WriteLine($"  Received {device} for {device.Client}.");
        }

        private void HandBack()
        {
            HomeDevice device = this.PickDevice();
            device.HandOver(this.today());
            this.output.WriteLine($"  {device.SerialNo} handed back to {device.Client.FullName}; paid {Tariff.Format(device.ComputeRepairCost())}.");
        }

        private void Postpone()
        {
            HomeDevice device = this.PickDevice();
            DateTime newDay = this.prompt.Date(
                "New promised day",
                day => day > device.PromisedOn && DataRules.IsValidPromise(device.ReceivedOn, day),
                "A later day than the current promise, within 90 days of intake.");
            this.workshop.Postpone(device.SerialNo, newDay);
            this.output.WriteLine($"  {device.SerialNo} is now promised for {device.PromisedOn:dd/MM/yyyy}.");
        }

        private void ReviewDeadlines()
        {
            int reported = this.workshop.ReviewDeadlines(this.today());
            this.output.WriteLine(reported == 0 ? "  No new late device." : $"  Late devices reported: {reported}");
        }

        private void ShowLists()
        {
            this.printer.PrintList("Repaired devices", this.workshop.Repaired());
            this.printer.PrintList("Still in service", this.workshop.StillInService());
        }

        // Every search is a DeviceFilter. The first three are ready-made lambdas; the last two are
        // built after the user types the value they compare with, so each entry stores how to build it.
        private void Search()
        {
            DateTime day = this.today();
            var searches = new List<(string Title, Func<DeviceFilter> Build)>
            {
                ("Under warranty", () => device => device.IsUnderWarranty),
                ("Urgent jobs", () => device => device.IsUrgent),
                ("Late today", () => device => device.IsLate(day)),
                ("Devices of one client", this.OfClient),
                ("Costing more than an amount", this.CostingMoreThan),
            };

            for (int i = 0; i < searches.Count; i++)
            {
                this.output.WriteLine($"    {i + 1}. {searches[i].Title}");
            }

            (string title, Func<DeviceFilter> build) = searches[this.prompt.Number("Search", 1, searches.Count) - 1];
            this.printer.PrintList(title, this.workshop.FindAll(build()));
        }

        private DeviceFilter OfClient()
        {
            string mobile = this.prompt.Mobile("Client's mobile");
            return device => device.Client.Mobile == mobile;
        }

        private DeviceFilter CostingMoreThan()
        {
            decimal amount = this.prompt.Money("More than");
            return device => device.ComputeRepairCost() > amount;
        }

        private HomeDevice PickDevice()
        {
            string serialNo = this.prompt.Text(
                "Device serial no.",
                text => this.workshop.Find(text) is not null,
                "No device with that serial number; option 1 lists them.");
            return this.workshop.Find(serialNo)!;
        }

        private Technician PickTechnician()
        {
            IReadOnlyList<Technician> staff = this.workshop.Technicians;
            if (staff.Count == 0)
            {
                throw new InvalidOperationException("No technician works here yet.");
            }

            for (int i = 0; i < staff.Count; i++)
            {
                string gas = staff[i].IsGasCertified ? ", gas certified" : string.Empty;
                this.output.WriteLine($"    {i + 1}. {staff[i].Name} ({Tariff.Format(staff[i].HourlyRate)}/h{gas})");
            }

            return staff[this.prompt.Number("Technician", 1, staff.Count) - 1];
        }

        private SparePart AskPart()
        {
            string name = this.prompt.Text("Part");
            decimal price = this.prompt.Money("Unit price");
            int quantity = this.prompt.Number("Quantity", 1, 20);
            return new SparePart(name, price, quantity);
        }

        // One line of the menu: what to type, what it says and what it does.
        private sealed record MenuOption(string Key, string Title, Action Run);
    }
}
