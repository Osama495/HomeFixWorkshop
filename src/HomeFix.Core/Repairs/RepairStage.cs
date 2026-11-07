namespace HomeFix.Core.Repairs
{
    /// <summary>
    /// The steps a device goes through in the workshop, in the order they normally happen.
    /// The order matters: a stage written later in this list is further along the job.
    /// </summary>
    public enum RepairStage
    {
        /// <summary>Dropped off at the counter; nobody has inspected it yet.</summary>
        Waiting,

        /// <summary>Inspected: the fault is known and written down.</summary>
        Diagnosed,

        /// <summary>On hold until a missing spare part arrives.</summary>
        AwaitingParts,

        /// <summary>On a technician's bench.</summary>
        InRepair,

        /// <summary>Repaired and tested; waiting for its owner.</summary>
        ReadyForPickup,

        /// <summary>Given back to its owner; nothing more happens to this job.</summary>
        Returned,
    }
}
