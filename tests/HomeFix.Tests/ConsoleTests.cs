using System.Globalization;
using HomeFix.App.Demo;
using HomeFix.App.Input;
using HomeFix.App.Output;
using HomeFix.App.Screens;
using HomeFix.Core.Devices;
using HomeFix.Core.People;
using HomeFix.Core.Repairs;
using HomeFix.Core.Workshop;

namespace HomeFix.Tests
{
    /// <summary>The console program, driven with typed text instead of a keyboard.</summary>
    public class ConsoleTests
    {
        [Fact]
        public void The_guided_tour_tells_the_whole_story()
        {
            var output = new StringWriter();
            DateTime today = Make.Today;

            ServiceWorkshop workshop = GuidedTour.Play(output, today, played => Connect(played, output));
            string text = output.ToString();

            Assert.Equal(4, workshop.Devices.Count);
            Assert.Contains("Refused: Fadi Jabri is not certified for gas; WH-2024-00077 needs a gas technician.", text);
            Assert.Contains("[SMS to 0947315862] Dear Dima Kassab, your dishwasher DW-2025-00418 is ready for pickup. Amount due: 409,375 SYP.", text);
            Assert.Contains("[SMS to 0936508271] Dear Tarek Hijazi, your water heater WH-2024-00077 is ready for pickup. Amount due: 60,000 SYP.", text);
            Assert.Contains($"[ALERT to manager] MW-2023-01290 is 3 day(s) past the date promised to Salma Qudsi ({today.AddDays(-3):dd/MM/yyyy}) and is still AwaitingParts.", text);
            Assert.Contains("Total of all repairs: 865,625 SYP", text);
            Assert.Contains("Late devices reported: 1", text);
            Assert.Contains("Reviewed again: 0 new", text);
            Assert.Contains("DW-2025-00418 handed back to Dima Kassab; paid 409,375 SYP", text);
            Assert.Contains("Under warranty: 1", text);
            Assert.Contains("Urgent jobs: 1", text);
            Assert.Contains("Cost above 200,000 SYP: 2", text);
            Assert.Contains($"MW-2023-01290 is now promised for {today.AddDays(5):dd/MM/yyyy}; late now: 0", text);
        }

        [Fact]
        public void The_tour_leaves_jobs_at_several_stages_for_the_menu()
        {
            ServiceWorkshop workshop = GuidedTour.Play(TextWriter.Null, Make.Today);

            RepairStage[] stages = workshop.Devices.Select(device => device.Stage).ToArray();

            Assert.Equal(new[] { RepairStage.Returned, RepairStage.ReadyForPickup, RepairStage.AwaitingParts, RepairStage.InRepair }, stages);
            Assert.Equal(2, workshop.Technicians.Count);
        }

        [Fact]
        public void The_prompt_asks_again_until_the_answer_is_valid()
        {
            var output = new StringWriter();
            var prompt = new ConsolePrompt(new StringReader("abc\n9\n3\n"), output);

            Assert.Equal(3, prompt.Number("Technician", 1, 5));
            Assert.Equal(2, output.ToString().Split("Type a whole number from 1 to 5.").Length - 1);
        }

        [Fact]
        public void Amounts_take_a_dot_and_refuse_a_comma()
        {
            var output = new StringWriter();
            var prompt = new ConsolePrompt(new StringReader("1,5\n1.5\n"), output);

            Assert.Equal(1.5m, prompt.Amount("Hours", _ => true, "Use a dot."));
            Assert.Contains("Use a dot.", output.ToString());
        }

        [Fact]
        public void Dates_are_typed_day_first_and_must_be_real_days()
        {
            var prompt = new ConsolePrompt(new StringReader("31/02/2026\n2026-11-05\n05/11/2026\n"), TextWriter.Null);

            Assert.Equal(new DateTime(2026, 11, 5), prompt.Date("Promised ready on", _ => true, "Use dd/MM/yyyy."));
        }

        [Theory]
        [InlineData("y\n", true)]
        [InlineData("N\n", false)]
        [InlineData("maybe\nY\n", true)]
        public void Yes_or_no(string typed, bool expected)
        {
            var prompt = new ConsolePrompt(new StringReader(typed), TextWriter.Null);

            Assert.Equal(expected, prompt.YesNo("Inverter model?"));
        }

        [Fact]
        public void A_closed_input_ends_the_questions()
        {
            var prompt = new ConsolePrompt(new StringReader(string.Empty), TextWriter.Null);

            Assert.Throws<EndOfStreamException>(() => prompt.Text("Choose"));
        }

        [Fact]
        public void A_whole_job_can_be_run_from_the_menu()
        {
            DateTime today = Make.Today;
            var workshop = new ServiceWorkshop("Menu Workshop");
            workshop.Hire(new Technician("Fadi Jabri", 40_000m));
            var output = new StringWriter();
            Connect(workshop, output);
            string promised = today.AddDays(3).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            Run(workshop, output, today,
                "2", "3", "MW-2024-00555", "Sharp", "R-342", "0955 000 111", "Nizar Bitar", promised, "n", "n", "900", "n",
                "3", "MW-2024-00555", "no heating",
                "5", "mw-2024-00555", "1",
                "6", "MW-2024-00555", "magnetron", "60000", "1",
                "7", "MW-2024-00555", "1,5", "1.5",
                "8", "MW-2024-00555",
                "9", "MW-2024-00555",
                "8", "MW-2024-00555",
                "42",
                "0");
            string text = output.ToString();

            Assert.Contains("Received MW-2024-00555 - Microwave Oven Sharp R-342 (900 W, transformer) for Nizar Bitar (CL-", text);
            Assert.Contains("[SMS to 0955000111] Dear Nizar Bitar, your microwave oven MW-2024-00555 is ready for pickup. Amount due: 225,000 SYP.", text);
            Assert.Contains("MW-2024-00555 handed back to Nizar Bitar; paid 225,000 SYP.", text);
            Assert.Contains("Not done: MW-2024-00555 cannot move from Returned to ReadyForPickup.", text);
            Assert.Contains("There is no such option.", text);
            HomeDevice device = Assert.Single(workshop.Devices);
            Assert.Equal(RepairStage.Returned, device.Stage);
            Assert.Equal(1.5m, device.HoursWorked);
        }

        [Fact]
        public void A_client_who_comes_back_is_found_by_mobile_number()
        {
            DateTime today = Make.Today;
            var workshop = new ServiceWorkshop("Menu Workshop");
            var output = new StringWriter();
            string promised = today.AddDays(2).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            Run(workshop, output, today,
                "2", "4", "VC-2025-00701", "Dyson", "V8", "0988111222", "Lama Hourani", promised, "n", "y", "y", "n",
                "2", "1", "DW-2025-00702", "Candy", "CDPH2L1049", "+963 988 111 222", promised, "y", today.AddYears(1).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), "n", "12", "n",
                "0");

            Assert.Contains("Known client: Lama Hourani (CL-", output.ToString());
            Assert.Equal(2, workshop.Devices.Count);
            Assert.Same(workshop.Devices[0].Client, workshop.Devices[1].Client);
            Assert.True(workshop.Devices[0].IsUrgent);
            Assert.True(workshop.Devices[1].IsUnderWarranty);
        }

        [Fact]
        public void A_refused_step_is_explained_and_the_menu_goes_on()
        {
            var workshop = new ServiceWorkshop("Menu Workshop");
            Dishwasher dishwasher = Make.Dishwasher();
            workshop.Receive(dishwasher);
            dishwasher.Diagnose("pump blocked");
            var output = new StringWriter();

            Run(workshop, output, Make.Today, "5", dishwasher.SerialNo, "1", "0");

            Assert.Contains("Not done: No technician works here yet.", output.ToString());
            Assert.Equal(RepairStage.Diagnosed, dishwasher.Stage);
        }

        [Fact]
        public void The_ready_made_searches_are_lambdas_over_the_list()
        {
            var workshop = new ServiceWorkshop("Search Workshop");
            Dishwasher covered = Make.Dishwasher(card: Make.Card("DW", warrantyUntil: Make.Today.AddYears(1)));
            VacuumCleaner urgent = Make.Vacuum(card: Make.Card("VC", urgent: true));
            MicrowaveOven late = Make.Microwave(card: Make.Card("MW", daysSinceIntake: 9, promisedInDays: -1));
            foreach (HomeDevice device in new HomeDevice[] { covered, urgent, late })
            {
                workshop.Receive(device);
            }

            var output = new StringWriter();

            Run(workshop, output, Make.Today, "14", "1", "14", "2", "14", "3", "0");
            string text = output.ToString();

            Assert.StartsWith($"    {covered.SerialNo}", LineAfter(text, "Under warranty: 1"));
            Assert.StartsWith($"    {urgent.SerialNo}", LineAfter(text, "Urgent jobs: 1"));
            Assert.StartsWith($"    {late.SerialNo}", LineAfter(text, "Late today: 1"));
        }

        [Fact]
        public void A_search_can_use_a_value_the_user_types()
        {
            ServiceWorkshop workshop = GuidedTour.Play(TextWriter.Null, Make.Today);
            var output = new StringWriter();

            Run(workshop, output, Make.Today, "14", "4", "+963 958 604 117", "14", "5", "200000", "0");
            string text = output.ToString();

            Assert.Contains("Devices of one client: 1", text);
            Assert.Contains("MW-2023-01290 - Microwave Oven Panasonic NN-ST34", text);
            Assert.Contains("Costing more than an amount: 2", text);
        }

        [Fact]
        public void The_menu_stops_when_the_input_ends()
        {
            var workshop = new ServiceWorkshop("Menu Workshop");
            var menu = new MainMenu(workshop, new ConsolePrompt(new StringReader("1\n"), TextWriter.Null), TextWriter.Null, () => Make.Today);

            Assert.Throws<EndOfStreamException>(() => menu.Run());
        }

        private static void Connect(ServiceWorkshop workshop, TextWriter output)
        {
            var alerts = new AlertPrinter(output);
            workshop.RepairFinished += alerts.OnRepairFinished;
            workshop.DeadlineMissed += alerts.OnDeadlineMissed;
        }

        private static void Run(ServiceWorkshop workshop, TextWriter output, DateTime today, params string[] typed)
        {
            var input = new StringReader(string.Join("\n", typed) + "\n");
            new MainMenu(workshop, new ConsolePrompt(input, output), output, () => today).Run();
        }

        // The line printed right after the one that contains `heading`.
        private static string LineAfter(string text, string heading)
        {
            string[] lines = text.Split('\n');
            int at = Array.FindIndex(lines, line => line.Contains(heading, StringComparison.Ordinal));
            Assert.True(at >= 0 && at + 1 < lines.Length, $"'{heading}' was not printed.");
            return lines[at + 1].TrimEnd('\r');
        }
    }
}
