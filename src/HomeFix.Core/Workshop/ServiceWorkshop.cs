using System.Globalization;
using HomeFix.Core.Contracts;
using HomeFix.Core.Devices;
using HomeFix.Core.Notifications;
using HomeFix.Core.People;
using HomeFix.Core.Repairs;
using HomeFix.Core.Rules;

namespace HomeFix.Core.Workshop
{
    /// <summary>
    /// The workshop: it keeps one list of every device it has received, whatever the kind, and
    /// runs every job through the methods that all devices share. Totals, the status board and
    /// the deadline check call <see cref="HomeDevice.ComputeRepairCost"/> and
    /// <see cref="HomeDevice.Describe"/> on each device, and each kind answers in its own way.
    /// </summary>
    public sealed class ServiceWorkshop
    {
        private readonly string name;
        private readonly List<HomeDevice> devices = new();
        private readonly List<Technician> technicians = new();

        // Serial numbers already reported late, so each late device is reported only once.
        private readonly HashSet<string> reportedLate = new(StringComparer.Ordinal);

        /// <summary>Opens a workshop.</summary>
        /// <param name="name">The name written on its receipts.</param>
        public ServiceWorkshop(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A workshop needs a name.", nameof(name));
            }

            this.name = DataRules.Tidy(name);
        }

        /// <summary>Raised when one of the workshop's devices is repaired and can be collected.</summary>
        public event StageChangedHandler? RepairFinished;

        /// <summary>Raised by <see cref="ReviewDeadlines"/>, once for each device found late.</summary>
        public event DeadlineMissedHandler? DeadlineMissed;

        /// <summary>The name of the workshop.</summary>
        public string Name => this.name;

        /// <summary>All devices received, in the order they arrived; read-only from outside.</summary>
        public IReadOnlyList<HomeDevice> Devices => this.devices.AsReadOnly();

        /// <summary>The technicians who work here; read-only from outside.</summary>
        public IReadOnlyList<Technician> Technicians => this.technicians.AsReadOnly();

        /// <summary>Adds a technician to the staff.</summary>
        public void Hire(Technician technician)
        {
            ArgumentNullException.ThrowIfNull(technician);

            if (this.technicians.Contains(technician))
            {
                throw new InvalidOperationException($"{technician.Name} already works here.");
            }

            this.technicians.Add(technician);
        }

        /// <summary>Takes a device in, counts it in the statistics and starts listening to its stage changes.</summary>
        /// <exception cref="InvalidOperationException">A device with the same serial number is already here, or another workshop holds this one.</exception>
        public void Receive(HomeDevice device)
        {
            ArgumentNullException.ThrowIfNull(device);

            if (this.Find(device.SerialNo) is not null)
            {
                throw new InvalidOperationException($"{device.SerialNo} has already been received.");
            }

            device.EnterStatistics();
            this.devices.Add(device);
            device.StageChanged += this.OnDeviceStageChanged;
        }

        /// <summary>Looks a serial number up, ignoring case and outer spaces; null if no device carries it.</summary>
        public HomeDevice? Find(string serialNo)
        {
            string wanted = (serialNo ?? string.Empty).Trim();
            return this.devices.Find(device => string.Equals(device.SerialNo, wanted, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The devices for which <paramref name="filter"/> returns true, in arrival order.</summary>
        public IReadOnlyList<HomeDevice> FindAll(DeviceFilter filter)
        {
            ArgumentNullException.ThrowIfNull(filter);

            var matches = new List<HomeDevice>();
            foreach (HomeDevice device in this.devices)
            {
                if (filter(device))
                {
                    matches.Add(device);
                }
            }

            return matches;
        }

        /// <summary>The devices whose repair is done, whether collected or not.</summary>
        public IReadOnlyList<HomeDevice> Repaired()
        {
            return this.FindAll(device => device.Stage >= RepairStage.ReadyForPickup);
        }

        /// <summary>The devices still being worked on, or waiting for an inspection or a part.</summary>
        public IReadOnlyList<HomeDevice> StillInService()
        {
            return this.FindAll(device => device.Stage < RepairStage.ReadyForPickup);
        }

        /// <summary>The cost of every repair in the workshop; each device prices itself.</summary>
        public decimal TotalRepairCost()
        {
            decimal total = 0m;
            foreach (HomeDevice device in this.devices)
            {
                total += device.ComputeRepairCost();
            }

            return total;
        }

        /// <summary>
        /// One line per device: serial number, its own description, its stage and its cost. The
        /// same loop prints every kind, because each one describes and prices itself.
        /// </summary>
        public IReadOnlyList<string> StatusBoard()
        {
            var lines = new List<string>(this.devices.Count);
            foreach (HomeDevice device in this.devices)
            {
                string cost = Tariff.Format(device.ComputeRepairCost());
                lines.Add(string.Format(CultureInfo.InvariantCulture, "{0,-14} {1,-58} {2,-15} {3,13}", device.SerialNo, device.Describe(), device.Stage, cost));
            }

            return lines;
        }

        /// <summary>
        /// Looks for devices that are still not ready after their promised day and raises
        /// <see cref="DeadlineMissed"/> for each one not reported before. Returns how many were reported now.
        /// </summary>
        public int ReviewDeadlines(DateTime today)
        {
            int reported = 0;
            foreach (HomeDevice device in this.devices)
            {
                if (device.IsLate(today) && this.reportedLate.Add(device.SerialNo))
                {
                    this.DeadlineMissed?.Invoke(device, device.DaysPastPromise(today));
                    reported++;
                }
            }

            return reported;
        }

        /// <summary>Moves a device's promised day later; if it was reported late, it can be reported again.</summary>
        public void Postpone(string serialNo, DateTime newPromisedOn)
        {
            HomeDevice device = this.Find(serialNo) ?? throw new KeyNotFoundException($"No device {serialNo} in this workshop.");
            device.PostponePromise(newPromisedOn);
            this.reportedLate.Remove(device.SerialNo);
        }

        // Runs whenever one of the workshop's devices changes stage.
        private void OnDeviceStageChanged(IRepairable device, RepairStage previous, RepairStage current)
        {
            if (current == RepairStage.ReadyForPickup)
            {
                this.reportedLate.Remove(device.SerialNo);
                this.RepairFinished?.Invoke(device, previous, current);
            }
            else if (current == RepairStage.Returned)
            {
                // The job is closed: nothing more will happen to this device.
                device.StageChanged -= this.OnDeviceStageChanged;
            }
        }
    }
}
