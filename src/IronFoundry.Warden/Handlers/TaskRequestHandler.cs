﻿namespace IronFoundry.Warden.Handlers
{
    using System;
    using Containers;
    using Jobs;
    using Protocol;
    using Tasks;

    public abstract class TaskRequestHandler : ContainerRequestHandler
    {
        protected readonly IContainerManager containerManager;

        public TaskRequestHandler(IContainerManager containerManager, Request request)
            : base(containerManager, request)
        {
        }

        protected IJobRunnable GetRunnableFor(ITaskRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            Container container = GetContainer();
            return new TaskRunner(container, request);
        }
    }
}
