﻿namespace IronFoundry.Warden.Protocol
{
    using System;

    public partial class StreamResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Message.Type.Stream; }
        }

        public static StreamResponse Create(int? exitStatus, string dataSource, string data)
        {
            var streamResponse = new StreamResponse { Name = dataSource, Data = data };
            if (exitStatus.HasValue)
            {
                unchecked
                {
                    streamResponse.ExitStatus = (uint)exitStatus.Value;
                }
            }
            return streamResponse;
        }

        public static StreamResponse Create(int exitCode, string stdout, string stderr)
        {
            var response = new StreamResponse
                {
                    Name       = "stdout", // NB: required!
                    Data       = String.Empty,
                };

            if (!stdout.IsNullOrEmpty())
            {
                response.Name = "stdout";
                response.Data = stdout;
            }
            else if (!stderr.IsNullOrEmpty())
            {
                response.Name = "stderr";
                response.Data = stderr;
            }

            unchecked
            {
                response.ExitStatus = (uint)exitCode;
            }

            return response;
        }

        public static StreamResponse Create(int exitCode)
        {
            unchecked
            {
                return new StreamResponse
                    {
                        Name       = "stdout", // NB: required!
                        Data       = String.Empty,
                        ExitStatus = (uint)exitCode
                    };
            }
        }
    }
}
