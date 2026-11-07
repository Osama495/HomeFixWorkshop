using System.Globalization;
using HomeFix.Core.Devices;
using HomeFix.Core.People;
using HomeFix.Core.Repairs;
using HomeFix.Core.Rules;
using HomeFix.Core.Workshop;

namespace HomeFix.Tests
{
    /// <summary>
    /// The static members shared by the whole program. Other tests move the same counters, so each
    /// test reads them before and after and checks the difference.
    /// </summary>
    public class StaticMemberTests
    {
        [Fact]
        public void A_device_is_counted_from_the_moment_a_workshop_receives_it()
        {
            int waitingBefore = HomeDevice.CountAt(RepairStage.Waiting);
            int inWorkshopBefore = HomeDevice.InWorkshopCount;
            Dishwasher dishwasher = Make.Dishwasher();

            Assert.Equal(waitingBefore, HomeDevice.CountAt(RepairStage.Waiting));

            new ServiceWorkshop("Counting Workshop").Receive(dishwasher);

            Assert.Equal(waitingBefore + 1, HomeDevice.CountAt(RepairStage.Waiting));
            Assert.Equal(inWorkshopBefore + 1, HomeDevice.InWorkshopCount);
        }

        [Fact]
        public void A_refused_device_is_never_counted()
        {
            int[] before = Counts();

            Assert.Throws<ArgumentOutOfRangeException>(() => new Dishwasher(Make.Card("DW"), placeSettings: 30, isBuiltIn: false));

            Assert.Equal(before, Counts());
        }

        [Fact]
        public void Each_move_takes_the_device_from_one_count_to_the_next()
        {
            MicrowaveOven oven = Make.Microwave();
            new ServiceWorkshop("Counting Workshop").Receive(oven);
            int[] before = Counts();

            oven.Diagnose("fuse blown");
            int[] after = Counts();

            Assert.Equal(before[(int)RepairStage.Waiting] - 1, after[(int)RepairStage.Waiting]);
            Assert.Equal(before[(int)RepairStage.Diagnosed] + 1, after[(int)RepairStage.Diagnosed]);
            Assert.Equal(before.Sum(), after.Sum());
        }

        [Fact]
        public void Handing_a_device_back_adds_its_bill_to_the_money_collected()
        {
            VacuumCleaner vacuum = Make.Vacuum();
            new ServiceWorkshop("Counting Workshop").Receive(vacuum);
            Make.OnBench(vacuum, Make.Technician(40_000m), 1m).FinishRepair();
            decimal collectedBefore = HomeDevice.TotalCollected;
            int readyBefore = HomeDevice.ReadyForPickupCount;
            int inWorkshopBefore = HomeDevice.InWorkshopCount;

            vacuum.HandOver(Make.Today);

            // bench check 25,000 + labor 40,000 + cord test 10,000
            Assert.Equal(collectedBefore + 75_000m, HomeDevice.TotalCollected);
            Assert.Equal(readyBefore - 1, HomeDevice.ReadyForPickupCount);
            Assert.Equal(inWorkshopBefore - 1, HomeDevice.InWorkshopCount);
        }

        [Fact]
        public void A_device_is_received_by_one_workshop_only()
        {
            Dishwasher dishwasher = Make.Dishwasher();
            new ServiceWorkshop("First Workshop").Receive(dishwasher);
            var second = new ServiceWorkshop("Second Workshop");
            int[] before = Counts();

            Assert.Throws<InvalidOperationException>(() => second.Receive(dishwasher));

            Assert.Empty(second.Devices);
            Assert.Equal(before, Counts());
        }

        [Fact]
        public void Clients_get_consecutive_codes_and_a_refused_client_uses_none()
        {
            var first = new Client("Nizar Bitar", "0955000111");
            Assert.Throws<ArgumentException>(() => new Client("Joud Kanaan", "12345"));
            var second = new Client("Joud Kanaan", "0955000222");

            Assert.Matches(@"^CL-\d{4}$", first.Code);
            Assert.Equal(Number(first.Code) + 1, Number(second.Code));
        }

        [Fact]
        public void The_shared_charges_are_constants()
        {
            Assert.Equal(50_000m, Tariff.DiagnosisFee);
            Assert.Equal(0.25m, Tariff.UrgentLaborMarkup);
            Assert.Equal("dd/MM/yyyy", DataRules.DateInputFormat);
        }

        [Fact]
        public void Each_kind_has_its_own_serial_prefix()
        {
            string[] prefixes = { Dishwasher.SerialPrefix, WaterHeater.SerialPrefix, MicrowaveOven.SerialPrefix, VacuumCleaner.SerialPrefix };

            Assert.Equal(new[] { "DW", "WH", "MW", "VC" }, prefixes);
        }

        private static int[] Counts() => Enum.GetValues<RepairStage>().Select(HomeDevice.CountAt).ToArray();

        private static int Number(string clientCode) => int.Parse(clientCode[3..], CultureInfo.InvariantCulture);
    }
}
