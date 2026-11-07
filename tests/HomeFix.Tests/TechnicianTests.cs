using HomeFix.Core.Devices;
using HomeFix.Core.People;
using HomeFix.Core.Repairs;

namespace HomeFix.Tests
{
    /// <summary>Technicians, and the one kind of device that may refuse one (the override of CheckTechnician).</summary>
    public class TechnicianTests
    {
        [Fact]
        public void A_gas_water_heater_is_opened_only_by_a_gas_certified_technician()
        {
            WaterHeater heater = Make.WaterHeater(source: HeatSource.Gas);
            heater.Diagnose("pilot flame goes out");
            Technician fadi = Make.Technician(gas: false);

            var refused = Assert.Throws<InvalidOperationException>(() => heater.BeginRepair(fadi));

            Assert.Equal($"Fadi Jabri is not certified for gas; {heater.SerialNo} needs a gas technician.", refused.Message);
            Assert.Equal(RepairStage.Diagnosed, heater.Stage);
            Assert.Null(heater.Technician);

            Technician ruba = new("Ruba Atassi", 55_000m, isGasCertified: true);
            heater.BeginRepair(ruba);

            Assert.Equal(RepairStage.InRepair, heater.Stage);
            Assert.Same(ruba, heater.Technician);
        }

        [Theory]
        [InlineData(HeatSource.Electric)]
        [InlineData(HeatSource.Solar)]
        public void Other_water_heaters_take_any_technician(HeatSource source)
        {
            WaterHeater heater = Make.WaterHeater(source: source);
            heater.Diagnose("no hot water");

            heater.BeginRepair(Make.Technician(gas: false));

            Assert.Equal(RepairStage.InRepair, heater.Stage);
        }

        [Fact]
        public void The_other_kinds_take_any_technician()
        {
            HomeDevice[] devices = { Make.Dishwasher(), Make.Microwave(), Make.Vacuum() };

            foreach (HomeDevice device in devices)
            {
                device.Diagnose("worn part");
                device.BeginRepair(Make.Technician(gas: false));
                Assert.Equal(RepairStage.InRepair, device.Stage);
            }
        }

        [Fact]
        public void A_technician_counts_each_finished_repair_once()
        {
            Technician technician = Make.Technician();
            Make.OnBench(Make.Dishwasher(), technician, 1m).FinishRepair();
            MicrowaveOven oven = Make.OnBench(Make.Microwave(), technician, 2m);

            Assert.Equal(1, technician.FinishedJobs);

            oven.FinishRepair();
            oven.HandOver(Make.Today);

            Assert.Equal(2, technician.FinishedJobs);
        }

        [Fact]
        public void A_technician_is_hired_with_a_name_and_a_positive_rate()
        {
            var ruba = new Technician("  Ruba   Atassi ", 55_000m, isGasCertified: true);

            Assert.Equal("Ruba Atassi", ruba.Name);
            Assert.Equal(55_000m, ruba.HourlyRate);
            Assert.True(ruba.IsGasCertified);
            Assert.Equal(0, ruba.FinishedJobs);
            Assert.Throws<ArgumentException>(() => new Technician("Ruba", 55_000m));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Technician("Ruba Atassi", 0m));
        }
    }
}
