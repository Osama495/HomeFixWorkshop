using HomeFix.Core.Contracts;
using HomeFix.Core.Notifications;
using HomeFix.Core.People;
using HomeFix.Core.Repairs;
using HomeFix.Core.Rules;

namespace HomeFix.Core.Devices
{
    /// <summary>
    /// What all devices in the workshop have in common: identity and owner, the dates of the job,
    /// the record of parts and working hours, and the rules of the workflow. How a repair is
    /// priced depends on the kind of device, so <see cref="ComputeRepairCost"/> is abstract and
    /// every subclass writes its own price.
    /// </summary>
    public abstract class HomeDevice : IRepairable
    {
        // From each stage, the stages a job may move to; any other move is refused.
        private static readonly Dictionary<RepairStage, RepairStage[]> nextStages = new()
        {
            [RepairStage.Waiting] = new[] { RepairStage.Diagnosed },
            [RepairStage.Diagnosed] = new[] { RepairStage.AwaitingParts, RepairStage.InRepair },
            [RepairStage.AwaitingParts] = new[] { RepairStage.InRepair },
            [RepairStage.InRepair] = new[] { RepairStage.ReadyForPickup },
            [RepairStage.ReadyForPickup] = new[] { RepairStage.Returned },
            [RepairStage.Returned] = Array.Empty<RepairStage>(),
        };

        // Shared by every device of the program: how many received devices are at each stage now,
        // and the money taken for the jobs that are closed.
        private static readonly int[] devicesAtStage = new int[Enum.GetValues<RepairStage>().Length];
        private static decimal amountCollected;

        private readonly string serialNo;
        private readonly string brand;
        private readonly string modelName;
        private readonly Client client;
        private readonly DateTime receivedOn;
        private readonly DateTime? warrantyUntil;
        private readonly bool isUrgent;
        private readonly List<SparePart> installedParts = new();

        private DateTime promisedOn;
        private RepairStage stage = RepairStage.Waiting;
        private string findings = string.Empty;
        private string missingPart = string.Empty;
        private Technician? technician;
        private decimal hoursWorked;
        private DateTime? returnedOn;
        private bool isCounted;

        /// <summary>Checks the intake card and opens a job at <see cref="RepairStage.Waiting"/>.</summary>
        /// <param name="serialPrefix">The two letters every serial number of this kind starts with.</param>
        /// <param name="intake">What was written at the counter.</param>
        protected HomeDevice(string serialPrefix, DeviceIntake intake)
        {
            ArgumentNullException.ThrowIfNull(intake);

            if (!DataRules.IsValidSerial(intake.SerialNo, serialPrefix))
            {
                throw new ArgumentException($"The serial number must look like {serialPrefix}-2025-00042.", nameof(intake));
            }

            if (string.IsNullOrWhiteSpace(intake.Brand) || string.IsNullOrWhiteSpace(intake.ModelName))
            {
                throw new ArgumentException("Both the brand and the model name are needed.", nameof(intake));
            }

            if (intake.Client is null)
            {
                throw new ArgumentException("Every device belongs to a client.", nameof(intake));
            }

            if (!DataRules.IsValidIntakeDate(intake.ReceivedOn, DateTime.Today))
            {
                throw new ArgumentException("A device cannot be received on a future day.", nameof(intake));
            }

            if (!DataRules.IsValidPromise(intake.ReceivedOn, intake.PromisedOn))
            {
                throw new ArgumentException("The promised day must fall within 90 days after intake.", nameof(intake));
            }

            this.serialNo = intake.SerialNo;
            this.brand = DataRules.Tidy(intake.Brand);
            this.modelName = DataRules.Tidy(intake.ModelName);
            this.client = intake.Client;
            this.receivedOn = intake.ReceivedOn.Date;
            this.promisedOn = intake.PromisedOn.Date;
            this.warrantyUntil = intake.WarrantyUntil?.Date;
            this.isUrgent = intake.IsUrgent;

            // The device is not counted here: the constructor of the subclass may still refuse its
            // own values. It enters the statistics when a workshop receives it (EnterStatistics).
        }

        /// <inheritdoc/>
        public event StageChangedHandler? StageChanged;

        /// <summary>The number of received devices, in the whole program, that have not been handed back yet.</summary>
        public static int InWorkshopCount => devicesAtStage.Sum() - devicesAtStage[(int)RepairStage.Returned];

        /// <summary>The number of repaired devices, in the whole program, waiting for their owners.</summary>
        public static int ReadyForPickupCount => devicesAtStage[(int)RepairStage.ReadyForPickup];

        /// <summary>The money taken for every closed job, in Syrian pounds.</summary>
        public static decimal TotalCollected => amountCollected;

        /// <inheritdoc/>
        public string SerialNo => this.serialNo;

        /// <summary>The maker of the device.</summary>
        public string Brand => this.brand;

        /// <summary>The maker's model name.</summary>
        public string ModelName => this.modelName;

        /// <inheritdoc/>
        public Client Client => this.client;

        /// <summary>The day the device was dropped off; fixed at intake.</summary>
        public DateTime ReceivedOn => this.receivedOn;

        /// <inheritdoc/>
        public DateTime PromisedOn => this.promisedOn;

        /// <summary>The last day of the manufacturer's warranty, or null without one; fixed at intake.</summary>
        public DateTime? WarrantyUntil => this.warrantyUntil;

        /// <summary>True when the client paid for the job to go first; fixed at intake.</summary>
        public bool IsUrgent => this.isUrgent;

        /// <summary>True when the device arrived while its warranty was still running.</summary>
        public bool IsUnderWarranty => this.warrantyUntil is DateTime lastDay && this.receivedOn <= lastDay;

        /// <inheritdoc/>
        public RepairStage Stage => this.stage;

        /// <summary>What the inspection found; empty before the device is diagnosed.</summary>
        public string Findings => this.findings;

        /// <summary>The part the job is waiting for; empty when it waits for nothing.</summary>
        public string MissingPart => this.missingPart;

        /// <summary>The technician working on the device, or null before the repair starts.</summary>
        public Technician? Technician => this.technician;

        /// <summary>The hours of work logged so far.</summary>
        public decimal HoursWorked => this.hoursWorked;

        /// <summary>The parts fitted so far; the list cannot be changed from outside.</summary>
        public IReadOnlyList<SparePart> InstalledParts => this.installedParts.AsReadOnly();

        /// <summary>The day the owner collected the device, or null while it is still here.</summary>
        public DateTime? ReturnedOn => this.returnedOn;

        /// <summary>The kind of device as people say it, for example "Dishwasher".</summary>
        public abstract string Kind { get; }

        /// <summary>The prices of parts fitted so far, added up.</summary>
        protected decimal PartsTotal => this.installedParts.Sum(part => part.LineTotal);

        /// <summary>The number of received devices, in the whole program, that are at one stage.</summary>
        public static int CountAt(RepairStage stage) => devicesAtStage[(int)stage];

        /// <inheritdoc/>
        public void Diagnose(string findings)
        {
            if (string.IsNullOrWhiteSpace(findings))
            {
                throw new ArgumentException("Write what the inspection found.", nameof(findings));
            }

            this.CheckMove(RepairStage.Diagnosed);
            this.findings = DataRules.Tidy(findings);
            this.EnterStage(RepairStage.Diagnosed);
        }

        /// <inheritdoc/>
        public void WaitForParts(string missingPart)
        {
            if (string.IsNullOrWhiteSpace(missingPart))
            {
                throw new ArgumentException("Name the part the job is waiting for.", nameof(missingPart));
            }

            this.CheckMove(RepairStage.AwaitingParts);
            this.missingPart = DataRules.Tidy(missingPart);
            this.EnterStage(RepairStage.AwaitingParts);
        }

        /// <inheritdoc/>
        public void BeginRepair(Technician technician)
        {
            ArgumentNullException.ThrowIfNull(technician);

            this.CheckMove(RepairStage.InRepair);
            this.CheckTechnician(technician);
            this.technician = technician;
            this.missingPart = string.Empty;
            this.EnterStage(RepairStage.InRepair);
        }

        /// <inheritdoc/>
        public void InstallPart(SparePart part)
        {
            ArgumentNullException.ThrowIfNull(part);

            this.CheckStage(RepairStage.InRepair, "fit a part");
            this.installedParts.Add(part);
        }

        /// <inheritdoc/>
        public void LogLabor(decimal hours)
        {
            if (!DataRules.IsValidHours(hours))
            {
                throw new ArgumentOutOfRangeException(nameof(hours), "One entry is more than 0 and at most 24 hours.");
            }

            this.CheckStage(RepairStage.InRepair, "log working hours");
            this.hoursWorked += hours;
        }

        /// <inheritdoc/>
        public void FinishRepair()
        {
            this.CheckMove(RepairStage.ReadyForPickup);

            if (this.hoursWorked == 0m)
            {
                throw new InvalidOperationException($"No working hours are logged for {this.serialNo}; log them first.");
            }

            this.technician?.CountFinishedJob();
            this.EnterStage(RepairStage.ReadyForPickup);
        }

        /// <inheritdoc/>
        public void HandOver(DateTime handedOverOn)
        {
            this.CheckMove(RepairStage.Returned);

            if (handedOverOn.Date < this.receivedOn)
            {
                throw new ArgumentException("A device cannot be handed back before it was received.", nameof(handedOverOn));
            }

            this.returnedOn = handedOverOn.Date;
            this.EnterStage(RepairStage.Returned);
        }

        /// <summary>
        /// Moves the promised day later, for example when a part is slow to arrive. The day can
        /// never be moved earlier: the client has already been told it.
        /// </summary>
        public void PostponePromise(DateTime newPromisedOn)
        {
            if (this.stage >= RepairStage.ReadyForPickup)
            {
                throw new InvalidOperationException($"{this.serialNo} is already repaired; there is nothing to postpone.");
            }

            if (newPromisedOn.Date <= this.promisedOn)
            {
                throw new ArgumentException("A promise can only be moved to a later day.", nameof(newPromisedOn));
            }

            if (!DataRules.IsValidPromise(this.receivedOn, newPromisedOn))
            {
                throw new ArgumentException("The new day must fall within 90 days after intake.", nameof(newPromisedOn));
            }

            this.promisedOn = newPromisedOn.Date;
        }

        /// <inheritdoc/>
        public bool IsLate(DateTime today)
        {
            return this.stage < RepairStage.ReadyForPickup && today.Date > this.promisedOn;
        }

        /// <summary>Days since the promised day while the device is late on <paramref name="today"/>; otherwise 0.</summary>
        public int DaysPastPromise(DateTime today)
        {
            return this.IsLate(today) ? (today.Date - this.promisedOn).Days : 0;
        }

        /// <summary>What this job costs its owner; each kind of device works it out its own way.</summary>
        public abstract decimal ComputeRepairCost();

        /// <summary>A one-line description; each kind adds what matters about it.</summary>
        public virtual string Describe()
        {
            return $"{this.Kind} {this.brand} {this.modelName}";
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.serialNo} - {this.Describe()}";
        }

        /// <summary>
        /// The labor of the repair: the hours logged times the technician's rate, made heavier by
        /// <paramref name="difficulty"/> (1 = a normal job) and by the urgent markup. Zero while no
        /// technician has the device.
        /// </summary>
        protected decimal LaborCharge(decimal difficulty)
        {
            if (this.technician is null)
            {
                return 0m;
            }

            decimal markup = this.isUrgent ? 1m + Tariff.UrgentLaborMarkup : 1m;
            decimal charge = this.hoursWorked * this.technician.HourlyRate * difficulty * markup;
            return Math.Round(charge, 0, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Puts a bill together: the warranty pays the <paramref name="coveredByWarranty"/> part
        /// when the device is under warranty, and the client always pays <paramref name="alwaysCharged"/>.
        /// </summary>
        protected decimal Bill(decimal coveredByWarranty, decimal alwaysCharged)
        {
            return (this.IsUnderWarranty ? 0m : coveredByWarranty) + alwaysCharged;
        }

        /// <summary>
        /// Lets a kind of device refuse a technician who may not work on it. Every technician is
        /// accepted unless a subclass says otherwise.
        /// </summary>
        protected virtual void CheckTechnician(Technician technician)
        {
        }

        /// <summary>Raises <see cref="StageChanged"/>; nothing happens when nobody listens.</summary>
        protected virtual void OnStageChanged(RepairStage previous, RepairStage current)
        {
            this.StageChanged?.Invoke(this, previous, current);
        }

        /// <summary>
        /// Adds the device to the static statistics, at the stage it is in. Internal: only
        /// <c>ServiceWorkshop.Receive</c> calls it, so a device is counted once, by the one
        /// workshop that holds it.
        /// </summary>
        /// <exception cref="InvalidOperationException">A workshop has already received this device.</exception>
        internal void EnterStatistics()
        {
            if (this.isCounted)
            {
                throw new InvalidOperationException($"{this.serialNo} has already been received by a workshop.");
            }

            this.isCounted = true;
            devicesAtStage[(int)this.stage]++;
        }

        private void CheckStage(RepairStage required, string action)
        {
            if (this.stage != required)
            {
                throw new InvalidOperationException($"Cannot {action} on {this.serialNo}: it is {this.stage}, not {required}.");
            }
        }

        private void CheckMove(RepairStage next)
        {
            if (!nextStages[this.stage].Contains(next))
            {
                throw new InvalidOperationException($"{this.serialNo} cannot move from {this.stage} to {next}.");
            }
        }

        // The single place where the stage changes: the statistics follow, then listeners are told.
        private void EnterStage(RepairStage next)
        {
            RepairStage previous = this.stage;
            this.stage = next;

            if (this.isCounted)
            {
                devicesAtStage[(int)previous]--;
                devicesAtStage[(int)next]++;
                if (next == RepairStage.Returned)
                {
                    amountCollected += this.ComputeRepairCost();
                }
            }

            this.OnStageChanged(previous, next);
        }
    }
}
