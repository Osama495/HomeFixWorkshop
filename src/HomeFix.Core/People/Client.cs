using System.Globalization;
using HomeFix.Core.Rules;

namespace HomeFix.Core.People
{
    /// <summary>
    /// A person who leaves devices at the workshop. The workshop calls or texts this person,
    /// so the name and the mobile number are checked once here and never edited afterwards.
    /// </summary>
    public sealed class Client
    {
        // One counter for the whole program: each new client gets the next number.
        private static int lastIssuedNumber;

        private readonly string code;
        private readonly string fullName;
        private readonly string mobile;

        /// <summary>Registers a client and gives them a code such as CL-0007.</summary>
        /// <param name="fullName">First and last name at least, letters only.</param>
        /// <param name="mobile">A Syrian mobile number: 09xxxxxxxx or +9639xxxxxxxx.</param>
        public Client(string fullName, string mobile)
        {
            if (!DataRules.IsValidFullName(fullName))
            {
                throw new ArgumentException("Write at least a first and a last name, in letters.", nameof(fullName));
            }

            if (!DataRules.IsValidMobile(mobile))
            {
                throw new ArgumentException("A mobile number is 09 followed by eight digits.", nameof(mobile));
            }

            this.fullName = DataRules.Tidy(fullName);
            this.mobile = DataRules.NormalizeMobile(mobile);
            int number = Interlocked.Increment(ref lastIssuedNumber);
            this.code = "CL-" + number.ToString("D4", CultureInfo.InvariantCulture);
        }

        /// <summary>The code printed on the client's receipts.</summary>
        public string Code => this.code;

        /// <summary>The client's name, with extra spaces removed.</summary>
        public string FullName => this.fullName;

        /// <summary>The mobile number, always stored as 09xxxxxxxx.</summary>
        public string Mobile => this.mobile;

        /// <inheritdoc/>
        public override string ToString() => $"{this.fullName} ({this.code})";
    }
}
