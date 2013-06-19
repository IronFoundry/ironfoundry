﻿namespace IronFoundry.Warden.Handlers
{
    using System.Threading.Tasks;
    using Containers;
    using NLog;
    using Protocol;

    public class StopRequestHandler : ContainerRequestHandler
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly StopRequest request;

        public StopRequestHandler(IContainerManager containerManager, Request request)
            : base(containerManager, request)
        {
            this.request = (StopRequest)request;
        }

        public override Task<Response> HandleAsync()
        {
            return Task.Factory.StartNew<Response>(() =>
                {
                    // before
                    log.Trace("Handle: '{0}' Background: '{1}' Kill: '{2}'", request.Handle, request.Background, request.Kill);
                    Container c = GetContainer();

                    // do
                    c.Stop();

                    // after
                    c.AfterStop();
                    return new StopResponse();
                });
        }
    }
}
