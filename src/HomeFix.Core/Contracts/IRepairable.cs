using HomeFix.Core.Notifications;
using HomeFix.Core.People;
using HomeFix.Core.Repairs;

namespace HomeFix.Core.Contracts
{
    /// <summary>
    /// What every device in the workshop can do, whatever its kind: be inspected, wait for a part,
    /// be repaired and handed back, tell whether it is late, and price its repair.
    /// Code that holds an <see cref="IRepairable"/> needs nothing else to run a job from drop-off
    /// to pickup.
    /// </summary>
    public interface IRepairable
    {
        /// <summary>Raised each time the device moves to another <see cref="RepairStage"/>.</summary>
        event StageChangedHandler? StageChanged;

        /// <summary>The manufacturer's serial number, fixed when the device is received.</summary>
        string SerialNo { get; }

        /// <summary>The person the device belongs to.</summary>
        Client Client { get; }

        /// <summary>Where the device is in the workflow.</summary>
        RepairStage Stage { get; }

        /// <summary>The day the workshop promised to have the device ready.</summary>
        DateTime PromisedOn { get; }

        /// <summary>Writes down what the inspection found.</summary>
        void Diagnose(string findings);

        /// <summary>Puts the job on hold until a missing part arrives.</summary>
        void WaitForParts(string missingPart);

        /// <summary>Puts the device on a technician's bench.</summary>
        void BeginRepair(Technician technician);

        /// <summary>Records a spare part fitted during the repair.</summary>
        void InstallPart(SparePart part);

        /// <summary>Adds hours of work spent on the repair.</summary>
        void LogLabor(decimal hours);

        /// <summary>Declares the repair done: the device waits for its owner.</summary>
        void FinishRepair();

        /// <summary>Gives the device back to its owner and closes the job.</summary>
        void HandOver(DateTime handedOverOn);

        /// <summary>Tells whether the device is still not ready although the promised day has passed.</summary>
        bool IsLate(DateTime today);

        /// <summary>The amount owed for the work done so far.</summary>
        decimal ComputeRepairCost();
    }
}
