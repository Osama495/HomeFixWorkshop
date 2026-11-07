using HomeFix.Core.Contracts;

namespace HomeFix.Core.Notifications
{
    /// <summary>
    /// The signature of a listener told about a device whose promised day went by before its
    /// repair was done.
    /// </summary>
    /// <param name="device">The late device.</param>
    /// <param name="daysPast">Whole days between the promised day and the day of the review.</param>
    public delegate void DeadlineMissedHandler(IRepairable device, int daysPast);
}
