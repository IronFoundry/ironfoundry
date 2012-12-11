﻿namespace IronFoundry.Ui.Controls.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Extensions;
    using GalaSoft.MvvmLight.Messaging;
    using Properties;
    using Vcap;
    using Models;
    using Utilities;

    public class CloudFoundryProvider : ICloudFoundryProvider
    {
        private readonly PreferencesProvider preferencesProvider;
        private readonly IDictionary<Guid, Cloud> clouds = new Dictionary<Guid, Cloud>();

        public CloudFoundryProvider(PreferencesProvider preferencesProvider)
        {
            this.preferencesProvider = preferencesProvider;
            LoadCloudsFromPreferences();
            Messenger.Default.Register<NotificationMessageAction<ICloudFoundryProvider>>(this, ProcessCloudFoundryProviderMessage);
        }

        public IEnumerable<Cloud> Clouds
        {
            get { return clouds.Values; }
        }

        public event EventHandler<CloudEventArgs> CloudAdded;
        public event EventHandler<CloudEventArgs> CloudRemoved;
        public event EventHandler<CloudEventArgs> CloudChanged;

        public void AddCloud(Cloud cloud)
        {
            clouds.Add(cloud.ID, cloud);
            OnCloudAdded(cloud);
        }

        public void RemoveCloud(Cloud cloud)
        {
            clouds.Remove(cloud.ID);
            if (null != CloudRemoved)
            {
                CloudRemoved(this, new CloudEventArgs(cloud));
            }
        }

        public void RemoveCloud(Guid cloudID)
        {
            Cloud toRemove;
            if (clouds.TryGetValue(cloudID, out toRemove))
            {
                RemoveCloud(toRemove);
            }
        }

        public void SaveOrUpdate(CloudUpdate updateData)
        {
            bool cloudAdded = false;
            Cloud cloud;
            if (false == clouds.TryGetValue(updateData.ID, out cloud))
            {
                cloud = new Cloud(updateData.ID);
                clouds[cloud.ID] = cloud;
                cloudAdded = true;
            }
            cloud.ServerName = updateData.ServerName;
            cloud.Url        = updateData.ServerUrl;
            cloud.Email      = updateData.Email;
            cloud.Password   = updateData.Password;
            if (cloudAdded)
            {
                OnCloudAdded(cloud);
            }
            else
            {
                OnCloudChanged(cloud);
            }
        }

        public void SaveChanges()
        {
            preferencesProvider.Save(new PreferencesV2 { Clouds = this.Clouds.ToArrayOrNull() });
        }

        public ProviderResponse<Cloud> Connect(Cloud cloud)
        {
            var response = new ProviderResponse<Cloud>();

            if (cloud.IsDataComplete)
            {
                Cloud local = cloud.DeepCopy();
                IVcapClient client = new VcapClient(local);
                try
                {
                    client.Login();
                    local.AccessToken = client.CurrentToken;
                    var applications = client.GetApplications();
                    var provisionedServices = client.GetProvisionedServices();
                    var availableServices = client.GetSystemServices();
                    local.Applications.Synchronize(new SafeObservableCollection<Application>(applications), new ApplicationEqualityComparer());
                    local.Services.Synchronize(new SafeObservableCollection<ProvisionedService>(provisionedServices), new ProvisionedServiceEqualityComparer());
                    local.AvailableServices.Synchronize(new SafeObservableCollection<SystemService>(availableServices), new SystemServiceEqualityComparer());
                    foreach (Application app in local.Applications)
                    {
                        var instances = GetInstances(local, app);
                        if (instances.Response != null)
                            app.InstanceCollection.Synchronize(new SafeObservableCollection<Instance>(instances.Response), new InstanceEqualityComparer());
                    }
                    response.Response = local;
                }
                catch (Exception ex)
                {
                    response.Response = null;
                    response.Message = ex.Message;
                }
            }
            else
            {
                response.Message = Resources.CloudFoundryProvider_ConnectIncompleteData_Message;
            }

            return response;
        }

        public Cloud Disconnect(Cloud cloud)
        {
            cloud.AccessToken = String.Empty;
            cloud.Services.Clear();
            cloud.Applications.Clear();
            cloud.AvailableServices.Clear();
            return cloud;
        }

        public ProviderResponse<bool> ValidateAccount(Cloud cloud)
        {
            return ValidateAccount(cloud.Url, cloud.Email, cloud.Password);
        }

        public ProviderResponse<bool> ValidateAccount(string serverUrl, string email, string password)
        {
            var response = new ProviderResponse<bool>();
            try
            {
                IVcapClient client = new VcapClient(serverUrl);
                client.Login(email, password);
                response.Response = true;
            }
            catch (Exception ex)
            {
                response.Response = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public ProviderResponse<IEnumerable<Instance>> GetInstances(Cloud cloud, Application app)
        {
            var response = new ProviderResponse<IEnumerable<Instance>>();
            try
            {
                IVcapClient client = new VcapClient(cloud);
                var stats = client.GetStats(app);
                var instances = new SafeObservableCollection<Instance>();
                if (stats != null)
                {

                    foreach (var stat in stats)
                    {
                        var instance = new Instance()
                                       {
                                           Id = stat.ID,
                                           State = stat.State
                                       };
                        if (stat.Stats != null)
                        {
                            instance.Cores = stat.Stats.Cores;
                            instance.MemoryQuota = stat.Stats.MemQuota/1048576;
                            instance.DiskQuota = stat.Stats.DiskQuota/1048576;
                            instance.Host = stat.Stats.Host;
                            instance.Parent = app;
                            instance.Uptime = TimeSpan.FromSeconds(stat.Stats.Uptime);

                            if (stat.Stats.Usage != null)
                            {
                                instance.Cpu = stat.Stats.Usage.CpuTime/100;
                                instance.Memory = Convert.ToInt32(stat.Stats.Usage.MemoryUsage)/1024;
                                instance.Disk = Convert.ToInt32(stat.Stats.Usage.DiskUsage)/1048576;
                            }
                        }
                        instances.Add(instance);
                    }
                }
                response.Response = instances;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
            }
            return response;
        }

        public ProviderResponse<IEnumerable<StatInfo>> GetStats(Application app, Cloud cloud)
        {
            var response = new ProviderResponse<IEnumerable<StatInfo>>();
            try
            {
                IVcapClient client = new VcapClient(cloud);
                response.Response = client.GetStats(app);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
            }
            return response;
        }

        public ProviderResponse<Application> GetApplication(Application app, Cloud cloud)
        {
            var response = new ProviderResponse<Application>();
            try
            {
                IVcapClient client = new VcapClient(cloud);
                response.Response = client.GetApplication(app.Name);
                var instancesResponse = this.GetInstances(cloud, app);
                if (instancesResponse.Response != null)
                    response.Response.InstanceCollection.Synchronize(new SafeObservableCollection<Instance>(instancesResponse.Response),new InstanceEqualityComparer());
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
            }
            return response;
        }

        public ProviderResponse<SafeObservableCollection<ProvisionedService>> GetProvisionedServices(Cloud cloud)
        {
            var response = new ProviderResponse<SafeObservableCollection<ProvisionedService>>();
            try
            {
                IVcapClient client = new VcapClient(cloud);
                response.Response = new SafeObservableCollection<ProvisionedService>(client.GetProvisionedServices());
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
            }
            return response;
        }

        public ProviderResponse<SafeObservableCollection<StatInfo>> GetStats(Cloud cloud, Application application)
        {
            var response = new ProviderResponse<SafeObservableCollection<StatInfo>>();
            try
            {
                IVcapClient client = new VcapClient(cloud);
                response.Response = new SafeObservableCollection<StatInfo>(client.GetStats(application));
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
            }
            return response;
        }

        public ProviderResponse<bool> UpdateApplication(Application app, Cloud cloud)
        {
            var response = new ProviderResponse<bool>();
            try
            {
                IVcapClient client = new VcapClient(cloud);
                client.UpdateApplication(app);
                response.Response = true;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
            }
            return response;
        }

        public ProviderResponse<bool> Start(Application app, Cloud cloud)
        {
            var response = new ProviderResponse<bool>();
            try
            {
                IVcapClient client = new VcapClient(cloud);
                client.Start(app);
                response.Response = true;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
            }
            return response;
        }

        public ProviderResponse<bool> Stop(Application app, Cloud cloud)
        {
            var response = new ProviderResponse<bool>();
            try
            {
                IVcapClient client = new VcapClient(cloud);
                client.Stop(app);
                response.Response = true;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
            }
            return response;
        }

        public ProviderResponse<bool> Restart(Application app, Cloud cloud)
        {
            var response = new ProviderResponse<bool>();
            try
            {
                IVcapClient client = new VcapClient(cloud);
                client.Restart(app);
                response.Response = true;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
            }
            return response;
        }

        public ProviderResponse<bool> Delete(Application app, Cloud cloud)
        {
            var response = new ProviderResponse<bool>();
            try
            {
                IVcapClient client = new VcapClient(cloud);
                client.Delete(app.Name);
                response.Response = true;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
            }
            return response;
        }

        public ProviderResponse<bool> CreateService(Cloud cloud, string serviceName, string provisionedServiceName)
        {
            var response = new ProviderResponse<bool>();
            try
            {
                IVcapClient client = new VcapClient(cloud);
                client.CreateService(serviceName, provisionedServiceName);
                response.Response = true;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
            }
            return response;
        }

        public ProviderResponse<bool> ChangePassword(Cloud cloud, string newPassword)
        {
            var response = new ProviderResponse<bool>();
            try
            {
                IVcapClient client = new VcapClient(cloud);
                client.ChangePassword(newPassword);
                response.Response = true;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
            }
            return response;
        }

        public ProviderResponse<bool> RegisterAccount(Cloud cloud, string email, string password)
        {
            return RegisterAccount(cloud.Url, email, password);
        }

        public ProviderResponse<bool> RegisterAccount(string serverUrl, string email, string password)
        {
            var response = new ProviderResponse<bool>();
            try
            {
                IVcapClient client = new VcapClient(serverUrl);
                client.AddUser(email, password);
                response.Response = true;
            }
            catch (Exception ex)
            {
                response.Response = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public ProviderResponse<VcapFilesResult> GetFiles(Cloud cloud, Application application, string path, ushort instanceId)
        {
            var response = new ProviderResponse<VcapFilesResult>();
            try
            {
                IVcapClient client = new VcapClient(cloud);
                var result = client.Files(application.Name, path, instanceId);
                response.Response = result;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
            }
            return response;
        }

        public ProviderResponse<bool> Push(Cloud cloud, string name, string url, ushort instances, string directoryToPushFrom, uint memory, string[] services)
        {
            var response = new ProviderResponse<bool>();
            try
            {
                IVcapClient client = new VcapClient(cloud);
                client.Push(name, url, instances, new System.IO.DirectoryInfo(directoryToPushFrom), memory, services);
                response.Response = true;
            }
            catch (Exception ex)
            {
                response.Response = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public ProviderResponse<bool> Update(Cloud cloud, Application app, string directoryToPushFrom)
        {
            var response = new ProviderResponse<bool>();
            try
            {
                IVcapClient client = new VcapClient(cloud);
                client.Update(app.Name, new System.IO.DirectoryInfo(directoryToPushFrom));
                response.Response = true;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
            }
            return response;
        }

        private void ProcessCloudFoundryProviderMessage(NotificationMessageAction<ICloudFoundryProvider> message)
        {
            if (message.Notification.Equals(Messages.GetCloudFoundryProvider))
            {
                message.Execute(this);
            }
        }

        private void OnCloudAdded(Cloud cloud)
        {
            if (null != CloudAdded)
            {
                CloudAdded(this, new CloudEventArgs(cloud));
            }
        }

        private void OnCloudChanged(Cloud cloud)
        {
            if (null != CloudChanged)
            {
                CloudChanged(this, new CloudEventArgs(cloud));
            }
        }

        private void Cloud_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "AccessToken":
                case "Email":
                case "ServerName":
                case "Url":
                case "TimeoutStart":
                case "TimeoutStop":
                case "Id":
                case "Password":
                case "IsConnected":
                case "IsDisconnected":
                    preferencesProvider.Save(new PreferencesV2 { Clouds = this.Clouds.ToArrayOrNull() });
                    break;
                default:
                    break;
            }
        }

        private void LoadCloudsFromPreferences()
        {
            PreferencesV2 preferences = preferencesProvider.Load();
            if (false == preferences.Clouds.IsNullOrEmpty())
            {
                foreach (Cloud cloud in preferences.Clouds)
                {
                    cloud.PropertyChanged -= Cloud_PropertyChanged;
                }

                clouds.Clear();

                foreach (Cloud cloud in preferences.Clouds)
                {
                    var kvp = new KeyValuePair<Guid, Cloud>(cloud.ID, cloud);
                    clouds.Add(kvp);
                    cloud.PropertyChanged += Cloud_PropertyChanged;
                }
            }
        }
    }
}