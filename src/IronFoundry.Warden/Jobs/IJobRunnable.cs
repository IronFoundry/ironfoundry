﻿namespace IronFoundry.Warden.Jobs
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IJobRunnable
    {
        void Cancel();

        /// <summary>
        /// Runs job synchronously and all status/results will be in return value.
        /// </summary>
        /// <returns></returns>
        IJobResult Run();


        /// <summary>
        /// Runs job asynchronously and all status/results will made available via the event.
        /// </summary>
        Task<IJobResult> RunAsync();
        event EventHandler<JobStatusEventArgs> JobStatusAvailable;

        /// <summary>
        /// Dequeues and returns current set of un-observed status.
        /// </summary>
        IEnumerable<IJobStatus> Status { get; }
    }
}
