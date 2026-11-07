using HomeFix.Core.Contracts;
using HomeFix.Core.Repairs;

namespace HomeFix.Core.Notifications
{
    /// <summary>
    /// The shape of a method that wants to hear about a device moving from one repair stage to
    /// another. A device raises its <c>StageChanged</c> event with it, and the workshop reuses it
    /// for <c>RepairFinished</c>.
    /// </summary>
    /// <param name="device">The device that moved.</param>
    /// <param name="previous">The stage it left.</param>
    /// <param name="current">The stage it is in now.</param>
    public delegate void StageChangedHandler(IRepairable device, RepairStage previous, RepairStage current);
}
