﻿using System.Collections.Generic;
namespace IronFoundry.Dea.Providers
{
    public enum ApplicationInstanceStatus
    {
        Deleted,
        Started,
        Starting,
        Stopped,
        Stopping,
        Unknown
    }

    public class WebServerAdministrationBinding
    {
        public string Host;
        public ushort Port;
    }

    public interface IWebServerAdministrationProvider
    {
        WebServerAdministrationBinding InstallWebApp(string localDirectory, string applicationInstanceName);
        void UninstallWebApp(string applicationInstanceName);
        ApplicationInstanceStatus GetApplicationStatus(string applicationInstanceName);
        IDictionary<string, IList<int>> GetIIsWorkerProcesses();
    }
}