using HomeFix.Core.Contracts;
using HomeFix.Core.Devices;
using HomeFix.Core.Repairs;
using HomeFix.Core.Rules;

namespace HomeFix.App.Output
{
    /// <summary>
    /// Listens to the workshop's events and prints the messages a real workshop would send: a text
    /// message to the client when a device is ready, an alert to the manager when one is late.
    /// Its two methods have the shapes of the two delegates, so they can be attached to the events.
    /// </summary>
    internal sealed class AlertPrinter
    {
        private readonly TextWriter output;

        public AlertPrinter(TextWriter output)
        {
            this.output = output;
        }

        /// <summary>Matches <c>StageChangedHandler</c>: attached to <c>ServiceWorkshop.RepairFinished</c>.</summary>
        public void OnRepairFinished(IRepairable device, RepairStage previous, RepairStage current)
        {
            this.output.WriteLine(
                $"    [SMS to {device.Client.Mobile}] Dear {device.Client.FullName}, your {KindOf(device)} {device.SerialNo} is ready for pickup. Amount due: {Tariff.Format(device.ComputeRepairCost())}.");
        }

        /// <summary>Matches <c>DeadlineMissedHandler</c>: attached to <c>ServiceWorkshop.DeadlineMissed</c>.</summary>
        public void OnDeadlineMissed(IRepairable device, int daysPast)
        {
            this.output.WriteLine(
                $"    [ALERT to manager] {device.SerialNo} is {daysPast} day(s) past the date promised to {device.Client.FullName} ({device.PromisedOn:dd/MM/yyyy}) and is still {device.Stage}.");
        }

        private static string KindOf(IRepairable device)
        {
            return device is HomeDevice homeDevice ? homeDevice.Kind.ToLowerInvariant() : "device";
        }
    }
}
