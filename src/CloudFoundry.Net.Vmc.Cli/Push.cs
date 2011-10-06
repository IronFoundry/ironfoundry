﻿namespace CloudFoundry.Net.Vmc.Cli
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    static partial class Program
    {
        static bool push(IList<string> unparsed)
        {
            // TODO match ruby argument parsing
            if (unparsed.Count < 3 || unparsed.Count > 5)
            {
                Console.Error.WriteLine("Usage: vmc push appname path url <service> --instances N"); // TODO usage statement standardization
                return false;
            }

            string appname = unparsed[0];
            string path    = unparsed[1];
            string url     = unparsed[2];

            string[] serviceNames = null;
            if (unparsed.Count == 4)
            {
                serviceNames = new[] { unparsed[3] };
            }

            DirectoryInfo di = null;
            if (Directory.Exists(path))
            {
                di = new DirectoryInfo(path);
            }
            else
            {
                Console.Error.WriteLine(String.Format("Directory '{0}' does not exist."));
                return false;
            }

            var vc = new VcapClient();
            VcapClientResult rv = vc.Push(appname, url, instances, di, 65536, serviceNames);
            if (false == rv.Success)
            {
                Console.Error.WriteLine(rv.Message);
            }
            return rv.Success;
        }

    }
}