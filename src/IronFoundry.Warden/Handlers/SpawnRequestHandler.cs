﻿namespace IronFoundry.Warden.Handlers
{
    using System;
    using IronFoundry.Warden.Containers;
    using IronFoundry.Warden.Jobs;
    using IronFoundry.Warden.Protocol;
    using NLog;

    /// <summary>
    /// This request will spawn the requested script in the background and
    /// create a job ID that can be used to retrieve results later on with a
    /// stream or link request.
    /// </summary>
    public class SpawnRequestHandler : TaskRequestHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly IJobManager jobManager;
        private readonly SpawnRequest request;

        public SpawnRequestHandler(IContainerManager containerManager, IJobManager jobManager, Request request)
            : base(containerManager, request)
        {
            if (jobManager == null)
            {
                throw new ArgumentNullException("jobManager");
            }
            this.jobManager = jobManager;
            this.request = (SpawnRequest)request;
        }

        public override Response Handle()
        {
            log.Trace("Handle: '{0}' Script: '{1}'", request.Handle, request.Script);
            IJobRunnable runnable = base.GetRunnableFor(request);
            uint jobId = jobManager.StartJobFor(runnable); // run async
            return new SpawnResponse { JobId = jobId };
        }
    }
}
