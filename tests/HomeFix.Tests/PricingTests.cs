using HomeFix.Core.Devices;
using HomeFix.Core.Repairs;

namespace HomeFix.Tests
{
    /// <summary>Each kind of device prices its repair by its own rules (the overrides of ComputeRepairCost).</summary>
    public class PricingTests
    {
        [Fact]
        public void A_diagnosed_freestanding_dishwasher_costs_the_diagnosis_and_the_leak_test()
        {
            Dishwasher dishwasher = Make.Dishwasher(placeSettings: 12, builtIn: false);
            dishwasher.Diagnose("spray arm blocked");

            Assert.Equal(50_000m + 30_000m, dishwasher.ComputeRepairCost());
        }

        [Fact]
        public void A_large_built_in_dishwasher_pays_heavier_labor_and_the_cabinet_removal()
        {
            Dishwasher dishwasher = Make.OnBench(Make.Dishwasher(placeSettings: 14, builtIn: true), Make.Technician(45_000m), 2.5m,
                new SparePart("drain pump", 85_000m), new SparePart("door seal", 40_000m));

            // labor 2.5 h x 45,000 x 1.15 = 129,375; repair 50,000 + 125,000 + 129,375; fees 30,000 + 75,000
            Assert.Equal(409_375m, dishwasher.ComputeRepairCost());
        }

        [Fact]
        public void Under_warranty_the_repair_is_free_but_the_fees_of_the_kind_are_paid()
        {
            Dishwasher dishwasher = Make.OnBench(
                Make.Dishwasher(placeSettings: 10, builtIn: true, card: Make.Card("DW", warrantyUntil: Make.Today.AddYears(1))),
                Make.Technician(), 2m, new SparePart("heater", 90_000m));

            Assert.True(dishwasher.IsUnderWarranty);
            Assert.Equal(30_000m + 75_000m, dishwasher.ComputeRepairCost());
        }

        [Fact]
        public void A_warranty_that_ended_before_intake_covers_nothing()
        {
            Dishwasher dishwasher = Make.Dishwasher(card: Make.Card("DW", daysSinceIntake: 5, warrantyUntil: Make.Today.AddDays(-10)));

            Assert.False(dishwasher.IsUnderWarranty);
            Assert.Equal(80_000m, dishwasher.ComputeRepairCost());
        }

        [Fact]
        public void An_electric_water_heater_is_descaled_by_the_size_of_its_tank()
        {
            WaterHeater heater = Make.OnBench(Make.WaterHeater(liters: 50), Make.Technician(40_000m), 2m, new SparePart("element", 20_000m));

            // 50,000 + 20,000 + 80,000 labor + 5 x 8,000 descaling
            Assert.Equal(190_000m, heater.ComputeRepairCost());
        }

        [Fact]
        public void A_gas_water_heater_pays_gas_labor_and_the_safety_check()
        {
            WaterHeater heater = Make.OnBench(Make.WaterHeater(liters: 80, source: HeatSource.Gas), Make.Technician(55_000m, gas: true), 1.5m,
                new SparePart("thermocouple", 35_000m));

            // labor 1.5 x 55,000 x 1.4 = 115,500; descaling 8 x 8,000 = 64,000; safety check 60,000
            Assert.Equal(50_000m + 35_000m + 115_500m + 64_000m + 60_000m, heater.ComputeRepairCost());
        }

        [Fact]
        public void A_solar_water_heater_pays_the_roof_visit()
        {
            WaterHeater heater = Make.WaterHeater(liters: 150, source: HeatSource.Solar);

            Assert.Equal(50_000m + (15 * 8_000m) + 90_000m, heater.ComputeRepairCost());
        }

        [Theory]
        [InlineData(80, 8)]
        [InlineData(85, 9)]
        [InlineData(10, 1)]
        public void Descaling_is_charged_for_every_started_ten_liters(int liters, int blocks)
        {
            Assert.Equal(50_000m + (blocks * 8_000m), Make.WaterHeater(liters).ComputeRepairCost());
        }

        [Theory]
        [InlineData(false, 145_000)]
        [InlineData(true, 190_000)]
        public void A_microwave_always_pays_its_two_safety_steps(bool inverter, int expected)
        {
            MicrowaveOven oven = Make.OnBench(Make.Microwave(inverter: inverter), Make.Technician(40_000m), 1m);

            Assert.Equal(expected, oven.ComputeRepairCost());
        }

        [Fact]
        public void Labor_is_rounded_to_whole_pounds()
        {
            MicrowaveOven oven = Make.OnBench(Make.Microwave(), Make.Technician(45_001m), 0.3m);

            // 0.3 x 45,001 = 13,500.3 -> 13,500
            Assert.Equal(50_000m + 13_500m + 55_000m, oven.ComputeRepairCost());
        }

        [Fact]
        public void A_corded_vacuum_cleaner_pays_the_bench_check_instead_of_the_diagnosis()
        {
            VacuumCleaner vacuum = Make.OnBench(Make.Vacuum(cordless: false, hepa: false), Make.Technician(40_000m), 0.5m);

            Assert.Equal(25_000m + 20_000m + 10_000m, vacuum.ComputeRepairCost());
        }

        [Fact]
        public void An_urgent_cordless_vacuum_cleaner_with_a_hepa_filter()
        {
            VacuumCleaner vacuum = Make.OnBench(Make.Vacuum(true, true, Make.Card("VC", urgent: true)), Make.Technician(45_000m), 1m,
                new SparePart("battery pack", 120_000m));

            // 25,000 + 120,000 + 45,000 x 1.25 + 15,000 filter; battery test 30,000
            Assert.Equal(246_250m, vacuum.ComputeRepairCost());
        }

        [Fact]
        public void An_urgent_job_adds_a_quarter_to_the_labor_only()
        {
            Dishwasher normal = Make.OnBench(Make.Dishwasher(placeSettings: 8), Make.Technician(40_000m), 1m);
            Dishwasher urgent = Make.OnBench(Make.Dishwasher(placeSettings: 8, card: Make.Card("DW", urgent: true)), Make.Technician(40_000m), 1m);

            Assert.Equal(120_000m, normal.ComputeRepairCost());
            Assert.Equal(130_000m, urgent.ComputeRepairCost());
        }

        [Fact]
        public void Parts_count_their_quantity()
        {
            MicrowaveOven oven = Make.OnBench(Make.Microwave(), Make.Technician(40_000m), 1m, new SparePart("fuse", 2_500m, quantity: 4));

            Assert.Equal(145_000m + 10_000m, oven.ComputeRepairCost());
        }
    }
}
