﻿namespace IronFoundry.Warden.Jobs
{
    using System;

    public class JobStatusEventArgs : EventArgs
    {
        private readonly IJobStatus jobStatus;

        public JobStatusEventArgs(IJobStatus jobStatus)
        {
            if (jobStatus == null)
            {
                throw new ArgumentNullException("jobStatus");
            }
            this.jobStatus = jobStatus;
        }

        public IJobStatus JobStatus
        {
            get { return jobStatus; }
        }
    }
}
