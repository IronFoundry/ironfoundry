﻿namespace IronFoundry.Warden.Handlers
{
    using System.Threading.Tasks;
    using IronFoundry.Warden.Protocol;

    public class PingRequestHandler : RequestHandler
    {
        private readonly PingRequest request;

        public PingRequestHandler(Request request)
            : base(request)
        {
            this.request = (PingRequest)request;
        }

        public override Task<Response> HandleAsync()
        {
            return Task.FromResult<Response>(new PingResponse());
        }
    }
}
