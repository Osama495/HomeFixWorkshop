using HomeFix.Core.Rules;

namespace HomeFix.Core.People
{
    /// <summary>
    /// A member of the workshop staff who repairs devices. The hourly rate is agreed when the
    /// technician is hired and cannot be changed by the program afterwards.
    /// </summary>
    public sealed class Technician
    {
        private readonly string name;
        private readonly decimal hourlyRate;
        private readonly bool isGasCertified;
        private int finishedJobs;

        /// <summary>Hires a technician.</summary>
        /// <param name="name">The technician's full name.</param>
        /// <param name="hourlyRate">What one hour of their work costs the client, in Syrian pounds.</param>
        /// <param name="isGasCertified">Whether they hold the certificate for gas appliances.</param>
        public Technician(string name, decimal hourlyRate, bool isGasCertified = false)
        {
            if (!DataRules.IsValidFullName(name))
            {
                throw new ArgumentException("Write the technician's first and last name.", nameof(name));
            }

            if (!DataRules.IsValidPrice(hourlyRate))
            {
                throw new ArgumentOutOfRangeException(nameof(hourlyRate), "The hourly rate must be a positive amount.");
            }

            this.name = DataRules.Tidy(name);
            this.hourlyRate = hourlyRate;
            this.isGasCertified = isGasCertified;
        }

        /// <summary>The technician's name.</summary>
        public string Name => this.name;

        /// <summary>The price of one hour of work.</summary>
        public decimal HourlyRate => this.hourlyRate;

        /// <summary>True when the technician may work on gas appliances.</summary>
        public bool IsGasCertified => this.isGasCertified;

        /// <summary>How many repairs this technician has finished.</summary>
        public int FinishedJobs => this.finishedJobs;

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Counts one more finished repair. Internal: only a device of this library can report
        /// that its repair is done, the console cannot raise the number by itself.
        /// </summary>
        internal void CountFinishedJob() => this.finishedJobs++;
    }
}
