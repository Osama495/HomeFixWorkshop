using HomeFix.Core.Devices;
using HomeFix.Core.People;
using HomeFix.Core.Repairs;
using HomeFix.Core.Rules;
using HomeFix.Core.Workshop;

namespace HomeFix.Tests
{
    /// <summary>The workshop and its single list of devices of every kind (polymorphism at work).</summary>
    public class WorkshopTests
    {
        [Fact]
        public void The_total_adds_up_what_each_kind_charges_for_itself()
        {
            var workshop = new ServiceWorkshop("Test Workshop");
            HomeDevice[] devices =
            {
                Make.Dishwasher(),              // 50,000 diagnosis + 30,000 leak test
                Make.WaterHeater(liters: 30),   // 50,000 diagnosis + 3 x 8,000 descaling
                Make.Microwave(inverter: true), // 50,000 + 45,000 calibration + 55,000 safety steps
                Make.Vacuum(cordless: true),    // 25,000 bench check + 30,000 battery test
            };
            foreach (HomeDevice device in devices)
            {
                workshop.Receive(device);
            }

            Assert.Equal(80_000m + 74_000m + 150_000m + 55_000m, workshop.TotalRepairCost());
            Assert.Equal(devices, workshop.Devices);
        }

        [Fact]
        public void An_empty_workshop_costs_nothing()
        {
            var workshop = new ServiceWorkshop("Test Workshop");

            Assert.Equal(0m, workshop.TotalRepairCost());
            Assert.Empty(workshop.StatusBoard());
        }

        [Fact]
        public void The_status_board_prints_one_line_per_device_in_its_own_words()
        {
            var workshop = new ServiceWorkshop("Test Workshop");
            Dishwasher dishwasher = Make.Dishwasher(14, builtIn: true);
            VacuumCleaner vacuum = Make.Vacuum(cordless: true, hepa: true);
            workshop.Receive(dishwasher);
            workshop.Receive(vacuum);
            vacuum.Diagnose("battery worn");

            IReadOnlyList<string> board = workshop.StatusBoard();

            Assert.Equal(2, board.Count);
            Assert.StartsWith(dishwasher.SerialNo, board[0]);
            Assert.Contains(dishwasher.Describe(), board[0]);
            Assert.Contains("Waiting", board[0]);
            Assert.EndsWith(Tariff.Format(dishwasher.ComputeRepairCost()), board[0]);
            Assert.Contains("(cordless, HEPA)", board[1]);
            Assert.Contains("Diagnosed", board[1]);
        }

        [Fact]
        public void A_serial_number_is_found_whatever_its_case_and_spaces()
        {
            var workshop = new ServiceWorkshop("Test Workshop");
            MicrowaveOven oven = Make.Microwave();
            workshop.Receive(oven);

            Assert.Same(oven, workshop.Find($"  {oven.SerialNo.ToLowerInvariant()} "));
            Assert.Null(workshop.Find("MW-2024-99999"));
        }

        [Fact]
        public void Two_devices_with_one_serial_number_are_refused()
        {
            var workshop = new ServiceWorkshop("Test Workshop");
            DeviceIntake card = Make.Card("DW");
            workshop.Receive(Make.Dishwasher(card: card));
            int waitingBefore = HomeDevice.CountAt(RepairStage.Waiting);

            Assert.Throws<InvalidOperationException>(() => workshop.Receive(Make.Dishwasher(card: card)));
            Assert.Single(workshop.Devices);
            Assert.Equal(waitingBefore, HomeDevice.CountAt(RepairStage.Waiting));
        }

        [Fact]
        public void A_search_takes_a_lambda_or_a_named_method()
        {
            var workshop = new ServiceWorkshop("Test Workshop");
            VacuumCleaner urgent = Make.Vacuum(card: Make.Card("VC", urgent: true));
            Dishwasher covered = Make.Dishwasher(card: Make.Card("DW", warrantyUntil: Make.Today.AddMonths(6)));
            MicrowaveOven plain = Make.Microwave();
            foreach (HomeDevice device in new HomeDevice[] { urgent, covered, plain })
            {
                workshop.Receive(device);
            }

            DeviceFilter isDishwasher = IsDishwasher;

            Assert.Equal(new HomeDevice[] { urgent }, workshop.FindAll(device => device.IsUrgent));
            Assert.Equal(new HomeDevice[] { covered }, workshop.FindAll(device => device.IsUnderWarranty));
            Assert.Equal(new HomeDevice[] { covered }, workshop.FindAll(isDishwasher));
            Assert.Equal(new HomeDevice[] { urgent, covered, plain }, workshop.FindAll(_ => true));
            Assert.Throws<ArgumentNullException>(() => workshop.FindAll(null!));
        }

        [Fact]
        public void Repaired_and_still_in_service_split_the_list()
        {
            var workshop = new ServiceWorkshop("Test Workshop");
            Dishwasher ready = Make.OnBench(Make.Dishwasher(), Make.Technician(), 1m);
            MicrowaveOven returned = Make.OnBench(Make.Microwave(), Make.Technician(), 1m);
            WaterHeater waiting = Make.WaterHeater();
            foreach (HomeDevice device in new HomeDevice[] { ready, returned, waiting })
            {
                workshop.Receive(device);
            }

            ready.FinishRepair();
            returned.FinishRepair();
            returned.HandOver(Make.Today);

            Assert.Equal(new HomeDevice[] { ready, returned }, workshop.Repaired());
            Assert.Equal(new HomeDevice[] { waiting }, workshop.StillInService());
        }

        [Fact]
        public void The_lists_of_the_workshop_cannot_be_changed_from_outside()
        {
            var workshop = new ServiceWorkshop("Test Workshop");
            workshop.Hire(Make.Technician());

            Assert.Throws<NotSupportedException>(() => ((ICollection<HomeDevice>)workshop.Devices).Add(Make.Dishwasher()));
            Assert.Throws<NotSupportedException>(() => ((ICollection<Technician>)workshop.Technicians).Clear());
            Assert.Empty(workshop.Devices);
            Assert.Single(workshop.Technicians);
        }

        [Fact]
        public void A_technician_is_hired_once()
        {
            var workshop = new ServiceWorkshop("Test Workshop");
            Technician fadi = Make.Technician();
            workshop.Hire(fadi);

            Assert.Throws<InvalidOperationException>(() => workshop.Hire(fadi));
            Assert.Single(workshop.Technicians);
        }
      
        [Fact]
        public void Delivering_a_device_removes_it_from_the_workshop()
        {
            var workshop = new ServiceWorkshop("Test Workshop");
            var technician = Make.Technician();
            
            // نضع الجهاز على طاولة العمل أولاً
            var device = Make.OnBench(Make.Dishwasher(), technician, 1.5m, new SparePart("door seal", 50000m));
            
            workshop.Receive(device);

            // ننهي إصلاحه ونسلمه
            device.FinishRepair();
            device.HandOver(Make.Today);

            // نختبر دالتنا الجديدة
            workshop.DeliverDevice(device.SerialNo);

            Assert.DoesNotContain(device, workshop.Devices);
        }

        [Fact]
        public void Delivering_a_device_that_is_not_returned_throws_exception()
        {
            var workshop = new ServiceWorkshop("Test Workshop");
            var device = Make.Dishwasher();
            
            workshop.Receive(device);

            Assert.Throws<InvalidOperationException>(() => workshop.DeliverDevice(device.SerialNo));
        }

        [Fact]
        public void Delivering_a_non_existent_device_throws_exception()
        {
            var workshop = new ServiceWorkshop("Test Workshop");
            
            Assert.Throws<KeyNotFoundException>(() => workshop.DeliverDevice("NON-EXISTENT-123"));
        }

        private static bool IsDishwasher(HomeDevice device) => device is Dishwasher;
    }
}
