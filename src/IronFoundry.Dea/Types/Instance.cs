﻿namespace IronFoundry.Dea.Types
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using IronFoundry.Dea.WindowsJobObjects;
    using JsonConverters;
    using Newtonsoft.Json;

    /*
     * TODO: should probably separate out this "on the wire" class from one used to track data
     */
    public class Instance : EntityBase, IDisposable
    {
        private const int MaxUsageSamples = 30;
        private readonly LinkedList<Usage> usageHistory = new LinkedList<Usage>();

        private readonly DateTime instanceStartDate;

        private readonly JobObject jobObject = new JobObject();

        private readonly string logID;

        private DateTime? workerProcessStartDate = null;
        private bool isEvacuated = false;

        public Instance()
        {
            jobObject.DieOnUnhandledException = true;
            jobObject.ActiveProcessesLimit = 10;
        }

        public Instance(string appDir, Droplet droplet) : this()
        {
            if (null != droplet)
            {
                DropletID      = droplet.ID;
                InstanceID     = Guid.NewGuid();
                InstanceIndex  = droplet.InstanceIndex;
                Name           = droplet.Name;
                Uris           = droplet.Uris;
                Users          = droplet.Users;
                Version        = droplet.Version;
                MemQuotaBytes  = droplet.Limits.MemoryMB * (1024*1024);
                DiskQuotaBytes = droplet.Limits.DiskMB * (1024*1024);
                FdsQuota       = droplet.Limits.FDs;
                Runtime        = droplet.Runtime;
                Framework      = droplet.Framework;
                Staged         = droplet.Name;
                Sha1           = droplet.Sha1;
                logID          = String.Format("(name={0} app_id={1} instance={2:N} index={3})", Name, DropletID, InstanceID, InstanceIndex);
                Staged         = String.Format("{0}-{1}-{2:N}", Name, InstanceIndex, InstanceID);
                Dir            = Path.Combine(appDir, Staged);
            }

            State             = VcapStates.STARTING;
            instanceStartDate = DateTime.Now;
            Start             = instanceStartDate.ToJsonString();
            StateTimestamp    = Utility.GetEpochTimestamp();

            if (MemQuotaBytes > 0)
            {
                /*
                 * TODO: if the user pushes an ASP.NET app with 64MB allocation, this will almost
                 * always kill it due to the fact that the default working set size is 64MB or maybe a bit more
                 * when a worker process is spun up.
                jobObject.JobMemoryLimit = MemQuotaBytes;
                 */
            }
        }

        [JsonIgnore]
        public DateTime StartDate
        {
            get { return instanceStartDate; }
        }

        [JsonProperty(PropertyName = "droplet_id")]
        public uint DropletID { get; set; }

        [JsonProperty(PropertyName = "instance_id"), JsonConverter(typeof(VcapGuidConverter))]
        public Guid InstanceID { get; set; }

        [JsonProperty(PropertyName = "instance_index")]
        public uint InstanceIndex { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "dir")]
        public string Dir { get; set; }

        [JsonProperty(PropertyName = "uris")]
        public string[] Uris { get; set; }

        [JsonProperty(PropertyName = "users")]
        public string[] Users { get; set; }

        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }

        [JsonProperty(PropertyName = "mem_quota")]
        public uint MemQuotaBytes { get; set; }

        [JsonProperty(PropertyName = "disk_quota")]
        public uint DiskQuotaBytes { get; set; }

        [JsonProperty(PropertyName = "fds_quota")]
        public uint FdsQuota { get; set; }

        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }

        [JsonProperty(PropertyName = "runtime")]
        public string Runtime { get; set; }

        [JsonProperty(PropertyName = "framework")]
        public string Framework { get; set; }

        [JsonProperty(PropertyName = "start")]
        public string Start { get; private set; }

        [JsonProperty(PropertyName = "state_timestamp")]
        public int StateTimestamp { get; set; }

        [JsonProperty(PropertyName = "port")]
        public ushort Port { get; set; }

        [JsonProperty(PropertyName = "staged")]
        public string Staged { get; set; }

        [JsonProperty(PropertyName = "exit_reason")]
        public string ExitReason { get; set; }

        [JsonIgnore]
        public string LogID
        {
            get { return logID; }
        }

        [JsonIgnore]
        public bool HasExitReason
        {
            get { return false == String.IsNullOrWhiteSpace(ExitReason); }
        }

        [JsonProperty(PropertyName = "sha1")]
        public string Sha1 { get; set; }

        [JsonProperty(PropertyName = "host")]
        public string Host { get; set; }

        [JsonIgnore]
        public bool IsStarting
        {
            get { return null != State && State == VcapStates.STARTING; }
        }

        [JsonIgnore]
        public bool IsRunning
        {
            get { return null != State && State == VcapStates.RUNNING; }
        }

        [JsonIgnore]
        public bool IsStartingOrRunning
        {
            get { return null != State && (State == VcapStates.RUNNING || State == VcapStates.STARTING); }
        }

        [JsonIgnore]
        public bool IsCrashed
        {
            get { return null != State && State == VcapStates.CRASHED; }
        }

        [JsonIgnore]
        public bool IsEvacuated
        {
            get { return isEvacuated; }
        }

        [JsonIgnore]
        public bool StopProcessed { get; set; }

        [JsonIgnore]
        public bool IsNotified { get; set; }

        public void Crashed()
        {
            ExitReason = State = VcapStates.CRASHED;
            StateTimestamp = Utility.GetEpochTimestamp();
        }

        public void DeaEvacuation()
        {
            ExitReason = "DEA_EVACUATION";
        }

        public void DeaShutdown()
        {
            if (VcapStates.CRASHED != State)
            {
                ExitReason = "DEA_SHUTDOWN";
            }
        }

        public void Evacuated()
        {
            isEvacuated = true;
        }

        public void OnDeaStop()
        {
            if (State == VcapStates.STARTING || State == VcapStates.RUNNING)
            {
                ExitReason = VcapStates.STOPPED;
            }

            if (State == VcapStates.CRASHED)
            {
                State = VcapStates.DELETED;
                StopProcessed = false;
            }
        }

        public void DeaStopComplete()
        {
            StopProcessed = true;
        }

        public void OnDeaStart()
        {
            StateTimestamp = Utility.GetEpochTimestamp();
            State = VcapStates.RUNNING;
        }

        public void UpdateState(string argNewState)
        {
            if (VcapStates.IsValid(argNewState))
            {
                State = argNewState;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void AddWorkerProcess(Process newInstanceWorkerProcess)
        {
            if (false == newInstanceWorkerProcess.HasExited &&
                false == jobObject.HasProcess(newInstanceWorkerProcess))
            {
                jobObject.AddProcess(newInstanceWorkerProcess);
                if (false == workerProcessStartDate.HasValue)
                {
                    workerProcessStartDate = DateTime.Now;
                }
            }
        }

        [JsonIgnore]
        public bool CanGatherStats
        {
            get { return this.IsStarting || this.IsRunning; }
        }

        [JsonIgnore]
        public Usage MostRecentUsage
        {
            get
            {
                Usage rv = null;
                if (usageHistory.Count > 0)
                {
                    rv = usageHistory.First.Value;
                }
                return rv;
            }
        }

        [JsonIgnore]
        private long MostRecentCpuTicks
        {
            get
            {
                long rv = 0;
                Usage mostRecent = MostRecentUsage;
                if (null != mostRecent)
                {
                    rv = mostRecent.TotalCpuTicks;
                }
                return rv;
            }
        }

        public void CalculateUsage()
        {
            /*
             * NB: some of this code is from this file, MonitorApps() method
             * https://raw.github.com/UhuruSoftware/vcap-dotnet/master/src/Uhuru.CloudFoundry.DEA/Agent.cs
             * Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
             */
            var newUsage = new Usage();

            newUsage.DiskUsageBytes = GetDiskUsage(this.Dir);

            newUsage.MemoryUsageKB = jobObject.WorkingSetMemory / 1024;

            long currentTicks = newUsage.TotalCpuTicks = jobObject.TotalProcessorTime.Ticks;
            DateTime currentTicksTimestamp = newUsage.Time = DateTime.Now;

            long lastTicks = MostRecentCpuTicks;
            
            long ticksDelta = currentTicks - lastTicks;

            DateTime startDate = workerProcessStartDate ?? instanceStartDate;
            long tickTimespan = (currentTicksTimestamp - startDate).Ticks;

            float cpu = 0;
            if (tickTimespan > 0)
            {
                cpu = (((float)ticksDelta / tickTimespan) * 100).Truncate(1);
            }

            newUsage.Cpu = cpu;

            usageHistory.AddFirst(newUsage);

            if (usageHistory.Count > MaxUsageSamples)
            {
                usageHistory.RemoveLast();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (jobObject != null)
                {
                    jobObject.Dispose();
                }
            }
        }

        private static long GetDiskUsage(string path)
        {
            long rv = 0;

            try
            {
                var di = new DirectoryInfo(path);
                if (di.Exists)
                {
                    rv = di.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
                }
            }
            catch { }

            return rv;
        }
    }
}