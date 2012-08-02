﻿namespace IronFoundry.Misc.Configuration
{
    using System;
    using System.Configuration;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using Microsoft.Win32;

    public class Config : IConfig
    {
        private readonly DeaSection deaSection;
        private readonly IPAddress localIPAddress;

        private readonly Uri filesServiceUri;
        private readonly Uri monitoringServiceUri;

        private readonly ServiceCredential filesCredentials;
        private readonly ServiceCredential monitoringCredentials;

        private readonly ushort monitoringServicePort;
        private readonly string monitoringServiceHostStr;

        private readonly string appCmdPath = null;
        private readonly bool hasAppCmd = false;

        public Config()
        {
            this.deaSection = (DeaSection)ConfigurationManager.GetSection(DeaSection.SectionName);
            this.localIPAddress = GetLocalIPAddress();

            this.filesServiceUri = new Uri(String.Format("http://localhost:{0}", FilesServicePort));

            this.monitoringServicePort = Utility.RandomFreePort();
            this.monitoringServiceUri = new Uri(String.Format("http://localhost:{0}", MonitoringServicePort));
            this.monitoringServiceHostStr = String.Format(CultureInfo.InvariantCulture,
                "{0}:{1}", localIPAddress, monitoringServicePort);

            this.filesCredentials = new ServiceCredential();
            this.monitoringCredentials = new ServiceCredential();

            try
            {
                RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
                RegistryKey subKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\InetStp");
                string iisInstallPath = subKey.GetValue("InstallPath").ToString();
                appCmdPath = Path.Combine(iisInstallPath, "appcmd.exe");
                if (File.Exists(appCmdPath))
                {
                    hasAppCmd = true;
                }
                else
                {
                    appCmdPath = null;
                    hasAppCmd = false;
                }
            }
            catch
            {
                appCmdPath = null;
                hasAppCmd = false;
            }
        }

        public ushort MaxMemoryMB
        {
            get { return deaSection.MaxMemoryMB; }
        }

        public bool DisableDirCleanup
        {
            get { return deaSection.DisableDirCleanup; }
        }

        public string DropletDir
        {
            get { return deaSection.DropletDir; }
        }

        public string AppDir
        {
            get { return deaSection.AppDir; }
        }

        public string NatsHost
        {
            get { return deaSection.NatsHost; }
        }

        public ushort NatsPort
        {
            get { return deaSection.NatsPort; }
        }

        public string NatsUser
        {
            get { return deaSection.NatsUser; }
        }

        public string NatsPassword
        {
            get { return deaSection.NatsPassword; }
        }

        public ushort FilesServicePort
        {
            get { return deaSection.FilesServicePort; }
        }

        public ushort MonitoringServicePort
        {
            get { return monitoringServicePort; }
        }

        public ServiceCredential FilesCredentials
        {
            get { return filesCredentials; }
        }

        public ServiceCredential MonitoringCredentials
        {
            get { return monitoringCredentials; }
        }

        public IPAddress LocalIPAddress
        {
            get { return localIPAddress; }
        }

        public Uri FilesServiceUri
        {
            get { return filesServiceUri; }
        }

        public Uri MonitoringServiceUri
        {
            get { return monitoringServiceUri; }
        }

        public string MonitoringServiceHostStr
        {
            get { return monitoringServiceHostStr; }
        }

        public string AppCmdPath
        {
            get { return appCmdPath; }
        }

        public bool HasAppCmd
        {
            get { return hasAppCmd; }
        }

        private IPAddress GetLocalIPAddress()
        {
            string localRoute = deaSection.LocalRoute;
            if (Utility.IsLocalIpAddress(localRoute))
            {
                localRoute = deaSection.NatsHost;
            }
            using (var udpClient = new UdpClient())
            {
                udpClient.Connect(localRoute, 1);
                IPEndPoint ep = (IPEndPoint)udpClient.Client.LocalEndPoint;
                return ep.Address;
            }
        }
    }
}