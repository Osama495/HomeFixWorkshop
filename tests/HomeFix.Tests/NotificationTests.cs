using System.Reflection;
using HomeFix.Core.Contracts;
using HomeFix.Core.Devices;
using HomeFix.Core.Notifications;
using HomeFix.Core.Repairs;
using HomeFix.Core.Workshop;

namespace HomeFix.Tests
{
    /// <summary>The delegates and events: StageChanged on a device, RepairFinished and DeadlineMissed on the workshop.</summary>
    public class NotificationTests
    {
        [Fact]
        public void Every_move_raises_StageChanged_with_the_stage_left_and_the_stage_reached()
        {
            var moves = new List<(IRepairable Device, RepairStage From, RepairStage To)>();
            MicrowaveOven oven = Make.Microwave();
            oven.StageChanged += (device, previous, current) => moves.Add((device, previous, current));

            oven.Diagnose("magnetron burnt");
            oven.WaitForParts("magnetron");
            oven.BeginRepair(Make.Technician());
            oven.LogLabor(1m);
            oven.FinishRepair();
            oven.HandOver(Make.Today);

            var expected = new[]
            {
                (RepairStage.Waiting, RepairStage.Diagnosed),
                (RepairStage.Diagnosed, RepairStage.AwaitingParts),
                (RepairStage.AwaitingParts, RepairStage.InRepair),
                (RepairStage.InRepair, RepairStage.ReadyForPickup),
                (RepairStage.ReadyForPickup, RepairStage.Returned),
            };
            Assert.Equal(expected, moves.Select(move => (move.From, move.To)));
            Assert.All(moves, move => Assert.Same(oven, move.Device));
        }

        [Fact]
        public void Work_that_does_not_change_the_stage_raises_nothing()
        {
            int heard = 0;
            Dishwasher dishwasher = Make.Dishwasher();
            dishwasher.Diagnose("pump blocked");
            dishwasher.BeginRepair(Make.Technician());
            dishwasher.StageChanged += (_, _, _) => heard++;

            dishwasher.InstallPart(new SparePart("pump", 85_000m));
            dishwasher.LogLabor(1m);
            Assert.Throws<InvalidOperationException>(() => dishwasher.Diagnose("again"));

            Assert.Equal(0, heard);
        }

        [Fact]
        public void A_device_with_no_listener_still_moves()
        {
            VacuumCleaner vacuum = Make.Vacuum();

            vacuum.Diagnose("brush stuck");

            Assert.Equal(RepairStage.Diagnosed, vacuum.Stage);
        }

        [Fact]
        public void A_listener_that_unsubscribes_hears_nothing_more()
        {
            int heard = 0;
            StageChangedHandler listener = (_, _, _) => heard++;
            MicrowaveOven oven = Make.Microwave();

            oven.StageChanged += listener;
            oven.Diagnose("fuse blown");
            oven.StageChanged -= listener;
            oven.BeginRepair(Make.Technician());

            Assert.Equal(1, heard);
        }

        [Fact]
        public void The_workshop_announces_each_finished_repair_once()
        {
            var workshop = new ServiceWorkshop("Event Workshop");
            var announced = new List<string>();
            workshop.RepairFinished += (device, previous, current) => announced.Add($"{device.SerialNo}: {previous} -> {current}");
            Dishwasher dishwasher = Make.Dishwasher();
            workshop.Receive(dishwasher);

            Make.OnBench(dishwasher, Make.Technician(), 1m);
            Assert.Empty(announced);

            dishwasher.FinishRepair();
            dishwasher.HandOver(Make.Today);

            Assert.Equal(new[] { $"{dishwasher.SerialNo}: InRepair -> ReadyForPickup" }, announced);
        }

        [Fact]
        public void The_workshop_stops_listening_to_a_device_once_its_job_is_closed()
        {
            var workshop = new ServiceWorkshop("Event Workshop");
            VacuumCleaner vacuum = Make.Vacuum();
            workshop.Receive(vacuum);

            Assert.Equal(1, ListenerCount(vacuum));

            Make.OnBench(vacuum, Make.Technician(), 1m).FinishRepair();
            vacuum.HandOver(Make.Today);

            Assert.Equal(0, ListenerCount(vacuum));
        }

        [Fact]
        public void The_deadline_review_reports_each_late_device_once_with_its_days_late()
        {
            var workshop = new ServiceWorkshop("Event Workshop");
            var alerts = new List<(string SerialNo, int DaysPastPromise)>();
            workshop.DeadlineMissed += (device, daysPast) => alerts.Add((device.SerialNo, daysPast));

            MicrowaveOven late = Make.Microwave(card: Make.Card("MW", daysSinceIntake: 10, promisedInDays: -4));
            Dishwasher withinPromise = Make.Dishwasher(card: Make.Card("DW", daysSinceIntake: 2, promisedInDays: 3));
            WaterHeater lateThenFixed = Make.WaterHeater(card: Make.Card("WH", daysSinceIntake: 10, promisedInDays: -1));
            workshop.Receive(late);
            workshop.Receive(withinPromise);
            workshop.Receive(lateThenFixed);
            Make.OnBench(lateThenFixed, Make.Technician(), 1m).FinishRepair();

            Assert.Equal(1, workshop.ReviewDeadlines(Make.Today));
            Assert.Equal(0, workshop.ReviewDeadlines(Make.Today));
            Assert.Equal(new[] { (late.SerialNo, 4) }, alerts);
        }

        [Fact]
        public void A_postponed_device_is_reported_again_if_it_misses_the_new_day()
        {
            var workshop = new ServiceWorkshop("Event Workshop");
            var alerts = new List<int>();
            workshop.DeadlineMissed += (_, daysPast) => alerts.Add(daysPast);
            MicrowaveOven oven = Make.Microwave(card: Make.Card("MW", daysSinceIntake: 10, promisedInDays: -4));
            workshop.Receive(oven);
            workshop.ReviewDeadlines(Make.Today);

            workshop.Postpone(oven.SerialNo, Make.Today.AddDays(2));

            Assert.Equal(0, workshop.ReviewDeadlines(Make.Today));
            Assert.Equal(1, workshop.ReviewDeadlines(Make.Today.AddDays(5)));
            Assert.Equal(new[] { 4, 3 }, alerts);
        }

        [Fact]
        public void Without_listeners_the_review_still_counts_late_devices()
        {
            var workshop = new ServiceWorkshop("Event Workshop");
            workshop.Receive(Make.Vacuum(card: Make.Card("VC", daysSinceIntake: 6, promisedInDays: -1)));

            Assert.Equal(1, workshop.ReviewDeadlines(Make.Today));
        }

        // The number of methods attached to a device's StageChanged event, read from its private field.
        private static int ListenerCount(HomeDevice device)
        {
            FieldInfo field = typeof(HomeDevice).GetField(nameof(HomeDevice.StageChanged), BindingFlags.Instance | BindingFlags.NonPublic)!;
            return (field.GetValue(device) as Delegate)?.GetInvocationList().Length ?? 0;
        }
    }
}
