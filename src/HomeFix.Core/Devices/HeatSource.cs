namespace HomeFix.Core.Devices
{
    /// <summary>The energy a water heater runs on; it sets the labor difficulty and the on-site fee.</summary>
    public enum HeatSource
    {
        /// <summary>An electric heating element inside the tank.</summary>
        Electric,

        /// <summary>A gas burner: only a certified technician may open it.</summary>
        Gas,

        /// <summary>Solar panels on the roof, with the tank installed up there.</summary>
        Solar,
    }
}
