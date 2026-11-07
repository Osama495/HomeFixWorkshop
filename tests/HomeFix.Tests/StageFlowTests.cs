using HomeFix.Core.Devices;
using HomeFix.Core.People;
using HomeFix.Core.Repairs;

namespace HomeFix.Tests
{
    /// <summary>The workflow of a job: the moves allowed, the moves refused, and the promised dates.</summary>
    public class StageFlowTests
    {
        [Fact]
        public void A_new_device_waits_with_nothing_recorded()
        {
            MicrowaveOven oven = Make.Microwave();

            Assert.Equal(RepairStage.Waiting, oven.Stage);
            Assert.Equal(string.Empty, oven.Findings);
            Assert.Null(oven.Technician);
            Assert.Empty(oven.InstalledParts);
            Assert.Equal(0m, oven.HoursWorked);
            Assert.Null(oven.ReturnedOn);
        }

        [Fact]
        public void A_job_goes_from_the_counter_back_to_its_owner()
        {
            Technician technician = Make.Technician();
            Dishwasher dishwasher = Make.OnBench(Make.Dishwasher(), technician, 1.5m, new SparePart("door seal", 40_000m));
            dishwasher.LogLabor(0.5m);
            dishwasher.FinishRepair();
            dishwasher.HandOver(Make.Today);

            Assert.Equal(RepairStage.Returned, dishwasher.Stage);
            Assert.Equal("fault found", dishwasher.Findings);
            Assert.Same(technician, dishwasher.Technician);
            Assert.Equal(2m, dishwasher.HoursWorked);
            Assert.Single(dishwasher.InstalledParts);
            Assert.Equal(Make.Today, dishwasher.ReturnedOn);
            Assert.Equal(1, technician.FinishedJobs);
        }

        [Fact]
        public void A_job_on_hold_for_a_part_goes_on_when_the_repair_starts()
        {
            WaterHeater heater = Make.WaterHeater();
            heater.Diagnose("element burnt");
            heater.WaitForParts("  element   2 kW ");

            Assert.Equal(RepairStage.AwaitingParts, heater.Stage);
            Assert.Equal("element 2 kW", heater.MissingPart);

            heater.BeginRepair(Make.Technician());

            Assert.Equal(RepairStage.InRepair, heater.Stage);
            Assert.Equal(string.Empty, heater.MissingPart);
        }

        [Fact]
        public void A_repair_cannot_start_before_the_diagnosis()
        {
            VacuumCleaner vacuum = Make.Vacuum();

            Assert.Throws<InvalidOperationException>(() => vacuum.BeginRepair(Make.Technician()));
            Assert.Equal(RepairStage.Waiting, vacuum.Stage);
            Assert.Null(vacuum.Technician);
        }

        [Fact]
        public void A_device_is_diagnosed_once()
        {
            Dishwasher dishwasher = Make.Dishwasher();
            dishwasher.Diagnose("pump noisy");

            Assert.Throws<InvalidOperationException>(() => dishwasher.Diagnose("pump blocked"));
            Assert.Equal("pump noisy", dishwasher.Findings);
        }

        [Fact]
        public void A_repair_without_working_hours_cannot_be_finished()
        {
            MicrowaveOven oven = Make.Microwave();
            oven.Diagnose("fuse blown");
            oven.BeginRepair(Make.Technician());

            Assert.Throws<InvalidOperationException>(() => oven.FinishRepair());
            Assert.Equal(RepairStage.InRepair, oven.Stage);
        }

        [Fact]
        public void Parts_and_hours_are_recorded_only_while_the_device_is_on_the_bench()
        {
            Dishwasher dishwasher = Make.Dishwasher();
            dishwasher.Diagnose("pump noisy");

            Assert.Throws<InvalidOperationException>(() => dishwasher.InstallPart(new SparePart("pump", 85_000m)));
            Assert.Throws<InvalidOperationException>(() => dishwasher.LogLabor(1m));
            Assert.Empty(dishwasher.InstalledParts);
            Assert.Equal(0m, dishwasher.HoursWorked);
        }

        [Fact]
        public void A_device_goes_back_to_its_owner_only_when_it_is_ready()
        {
            WaterHeater heater = Make.WaterHeater();
            heater.Diagnose("thermostat stuck");

            Assert.Throws<InvalidOperationException>(() => heater.HandOver(Make.Today));
            Assert.Null(heater.ReturnedOn);
        }

        [Fact]
        public void A_closed_job_accepts_no_more_work()
        {
            VacuumCleaner vacuum = Make.OnBench(Make.Vacuum(), Make.Technician(), 1m);
            vacuum.FinishRepair();
            vacuum.HandOver(Make.Today);

            Assert.Throws<InvalidOperationException>(() => vacuum.BeginRepair(Make.Technician()));
            Assert.Throws<InvalidOperationException>(() => vacuum.InstallPart(new SparePart("belt", 5_000m)));
            Assert.Throws<InvalidOperationException>(() => vacuum.FinishRepair());
            Assert.Throws<InvalidOperationException>(() => vacuum.HandOver(Make.Today));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(24.5)]
        public void One_entry_of_working_time_is_more_than_zero_and_at_most_a_day(double hours)
        {
            MicrowaveOven oven = Make.Microwave();
            oven.Diagnose("door switch broken");
            oven.BeginRepair(Make.Technician());

            Assert.Throws<ArgumentOutOfRangeException>(() => oven.LogLabor((decimal)hours));
            Assert.Equal(0m, oven.HoursWorked);
        }

        [Fact]
        public void Blank_findings_and_blank_part_names_are_refused()
        {
            Dishwasher dishwasher = Make.Dishwasher();

            Assert.Throws<ArgumentException>(() => dishwasher.Diagnose("   "));
            dishwasher.Diagnose("heater dead");
            Assert.Throws<ArgumentException>(() => dishwasher.WaitForParts(string.Empty));
            Assert.Equal(RepairStage.Diagnosed, dishwasher.Stage);
        }

        [Fact]
        public void A_device_cannot_go_back_before_the_day_it_came_in()
        {
            MicrowaveOven oven = Make.OnBench(Make.Microwave(card: Make.Card("MW", daysSinceIntake: 5)), Make.Technician(), 1m);
            oven.FinishRepair();

            Assert.Throws<ArgumentException>(() => oven.HandOver(Make.Today.AddDays(-6)));
            Assert.Equal(RepairStage.ReadyForPickup, oven.Stage);
        }

        [Fact]
        public void The_list_of_parts_cannot_be_changed_from_outside()
        {
            Dishwasher dishwasher = Make.OnBench(Make.Dishwasher(), Make.Technician(), 1m, new SparePart("pump", 85_000m));
            var parts = (ICollection<SparePart>)dishwasher.InstalledParts;

            Assert.Throws<NotSupportedException>(() => parts.Add(new SparePart("seal", 1_000m)));
            Assert.Throws<NotSupportedException>(() => parts.Clear());
            Assert.Single(dishwasher.InstalledParts);
        }

        [Fact]
        public void A_device_still_not_ready_after_its_promised_day_is_late()
        {
            MicrowaveOven oven = Make.Microwave(card: Make.Card("MW", daysSinceIntake: 10, promisedInDays: -3));

            Assert.True(oven.IsLate(Make.Today));
            Assert.Equal(3, oven.DaysPastPromise(Make.Today));
            Assert.False(oven.IsLate(oven.PromisedOn));
            Assert.Equal(0, oven.DaysPastPromise(oven.PromisedOn));
        }

        [Fact]
        public void A_repaired_device_is_never_late()
        {
            MicrowaveOven oven = Make.OnBench(Make.Microwave(card: Make.Card("MW", daysSinceIntake: 10, promisedInDays: -3)), Make.Technician(), 1m);
            oven.FinishRepair();

            Assert.False(oven.IsLate(Make.Today));
            Assert.Equal(0, oven.DaysPastPromise(Make.Today));
        }

        [Fact]
        public void A_promise_can_only_move_later_and_within_ninety_days_of_intake()
        {
            MicrowaveOven oven = Make.Microwave(card: Make.Card("MW", daysSinceIntake: 10, promisedInDays: -3));
            DateTime promised = oven.PromisedOn;

            Assert.Throws<ArgumentException>(() => oven.PostponePromise(promised));
            Assert.Throws<ArgumentException>(() => oven.PostponePromise(promised.AddDays(-1)));
            Assert.Throws<ArgumentException>(() => oven.PostponePromise(oven.ReceivedOn.AddDays(91)));
            Assert.Equal(promised, oven.PromisedOn);

            oven.PostponePromise(Make.Today.AddDays(4));

            Assert.Equal(Make.Today.AddDays(4), oven.PromisedOn);
            Assert.False(oven.IsLate(Make.Today));
        }

        [Fact]
        public void A_repaired_device_has_no_promise_to_postpone()
        {
            Dishwasher dishwasher = Make.OnBench(Make.Dishwasher(), Make.Technician(), 1m);
            dishwasher.FinishRepair();

            Assert.Throws<InvalidOperationException>(() => dishwasher.PostponePromise(Make.Today.AddDays(20)));
        }

        [Fact]
        public void The_intake_card_is_checked_before_a_job_opens()
        {
            Assert.Throws<ArgumentException>(() => new Dishwasher(Make.Card("WH"), 12, false));
            Assert.Throws<ArgumentException>(() => Make.Dishwasher(card: Make.Card("DW", daysSinceIntake: -1)));
            Assert.Throws<ArgumentException>(() => Make.Dishwasher(card: Make.Card("DW", daysSinceIntake: 2, promisedInDays: -3)));
            Assert.Throws<ArgumentException>(() => Make.Dishwasher(card: Make.Card("DW", daysSinceIntake: 0, promisedInDays: 91)));
            Assert.Throws<ArgumentException>(() => Make.Dishwasher(card: Make.Card("DW") with { Brand = " " }));
            Assert.Throws<ArgumentNullException>(() => new VacuumCleaner(null!, false, false));
        }

        [Fact]
        public void The_intake_card_values_are_tidied_and_kept()
        {
            DeviceIntake card = Make.Card("WH", daysSinceIntake: 3, promisedInDays: 4, warrantyUntil: Make.Today.AddDays(30), urgent: true)
                with { Brand = "  Ariston ", ModelName = "Pro1   Eco 80" };
            WaterHeater heater = Make.WaterHeater(card: card);

            Assert.Equal(card.SerialNo, heater.SerialNo);
            Assert.Equal("Ariston", heater.Brand);
            Assert.Equal("Pro1 Eco 80", heater.ModelName);
            Assert.Equal(Make.Today.AddDays(-3), heater.ReceivedOn);
            Assert.Equal(Make.Today.AddDays(4), heater.PromisedOn);
            Assert.True(heater.IsUnderWarranty);
            Assert.True(heater.IsUrgent);
            Assert.Equal($"{card.SerialNo} - Water Heater Ariston Pro1 Eco 80 (50 L, electric)", heater.ToString());
        }

        [Theory]
        [InlineData(3)]
        [InlineData(19)]
        public void A_dishwasher_holds_four_to_eighteen_place_settings(int placeSettings)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Make.Dishwasher(placeSettings));
        }

        [Theory]
        [InlineData(9)]
        [InlineData(301)]
        public void A_water_heater_tank_holds_ten_to_three_hundred_liters(int liters)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Make.WaterHeater(liters));
        }

        [Fact]
        public void A_water_heater_needs_a_known_heat_source()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Make.WaterHeater(source: (HeatSource)7));
        }

        [Theory]
        [InlineData(499)]
        [InlineData(3001)]
        public void A_microwave_oven_has_five_hundred_to_three_thousand_watts(int watts)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Make.Microwave(watts));
        }

        [Fact]
        public void Each_kind_describes_what_matters_about_it()
        {
            Assert.EndsWith("(14 place settings, built-in)", Make.Dishwasher(14, builtIn: true).Describe());
            Assert.EndsWith("(80 L, gas)", Make.WaterHeater(80, HeatSource.Gas).Describe());
            Assert.EndsWith("(1000 W, inverter)", Make.Microwave(1000, inverter: true).Describe());
            Assert.EndsWith("(cordless, HEPA)", Make.Vacuum(cordless: true, hepa: true).Describe());
            Assert.EndsWith("(corded)", Make.Vacuum().Describe());
        }
    }
}
