﻿namespace IronFoundry.Warden
{
    using System;
    using IronFoundry.Warden.Handlers;
    using IronFoundry.Warden.Protocol;

    public class RequestHandlerFactory
    {
        private readonly Message.Type requestType;
        private readonly Request request;

        public RequestHandlerFactory(Message.Type requestType, Request request)
        {
            if (requestType == default(Message.Type))
            {
                throw new ArgumentNullException("requestType");
            }
            if (request == null)
            {
                throw new ArgumentNullException("message");
            }
            this.requestType = requestType;
            this.request = request;
        }

        public RequestHandler GetHandler()
        {
            RequestHandler handler = null;

            switch (requestType)
            {
                case Message.Type.CopyIn:
                    handler = new CopyInRequestHandler(request);
                    break;
                case Message.Type.CopyOut:
                    handler = new CopyOutRequestHandler(request);
                    break;
                case Message.Type.Create:
                    handler = new CreateRequestHandler(request);
                    break;
                case Message.Type.Destroy:
                    handler = new DestroyRequestHandler(request);
                    break;
                case Message.Type.Echo:
                    handler = new EchoRequestHandler(request);
                    break;
                case Message.Type.Info:
                    handler = new InfoRequestHandler(request);
                    break;
                case Message.Type.LimitBandwidth:
                    handler = new LimitBandwidthRequestHandler(request);
                    break;
                case Message.Type.LimitDisk:
                    handler = new LimitDiskRequestHandler(request);
                    break;
                case Message.Type.LimitMemory:
                    handler = new LimitMemoryRequestHandler(request);
                    break;
                case Message.Type.Link:
                    handler = new LinkRequestHandler(request);
                    break;
                case Message.Type.List:
                    handler = new ListRequestHandler(request);
                    break;
                case Message.Type.NetIn:
                    handler = new NetInRequestHandler(request);
                    break;
                case Message.Type.NetOut:
                    handler = new NetOutRequestHandler(request);
                    break;
                case Message.Type.Ping:
                    handler = new PingRequestHandler(request);
                    break;
                case Message.Type.Run:
                    handler = new RunRequestHandler(request);
                    break;
                case Message.Type.Spawn:
                    handler = new SpawnRequestHandler(request);
                    break;
                case Message.Type.Stop:
                    handler = new StopRequestHandler(request);
                    break;
                case Message.Type.Stream:
                    handler = new StreamRequestHandler(request);
                    break;
                default:
                    throw new WardenException("Unknown request type '{0}' passed to handler factory.", requestType);
            }

            return handler;
        }
    }
}
