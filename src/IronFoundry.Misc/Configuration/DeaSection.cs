﻿namespace IronFoundry.Misc.Configuration
{
    using System;
    using System.Configuration;

    public class DeaSection : ConfigurationSection
    {
        public const string SectionName = "ironFoundryDea";

        [ConfigurationProperty("natsHost", DefaultValue = "api.vcap.me", IsRequired = true)]
        public string NatsHost
        {
            get
            {
                return (string)this["natsHost"];
            }
            set
            {
                this["natsHost"] = value;
            }
        }

        [ConfigurationProperty("natsPort", DefaultValue = "4222", IsRequired = true)]
        public ushort NatsPort
        {
            get
            {
                return Convert.ToUInt16(this["natsPort"]);
            }
            set
            {
                this["natsPort"] = value;
            }
        }

        [ConfigurationProperty("natsUser", IsRequired = false)]
        public string NatsUser
        {
            get
            {
                return (string)this["natsUser"];
            }
            set
            {
                this["natsUser"] = value;
            }
        }

        [ConfigurationProperty("natsPassword", IsRequired = false)]
        public string NatsPassword
        {
            get
            {
                return (string)this["natsPassword"];
            }
            set
            {
                this["natsPassword"] = value;
            }
        }

        [ConfigurationProperty("localRoute", DefaultValue = "127.0.0.1", IsRequired = false)]
        public string LocalRoute
        {
            get
            {
                return (string)this["localRoute"];
            }
            set
            {
                this["localRoute"] = value;
            }
        }


        [ConfigurationProperty("appDir", DefaultValue = @"C:\IronFoundry\apps", IsRequired = false)]
        public string AppDir
        {
            get
            {
                return (string)this["appDir"];
            }
            set
            {
                this["appDir"] = value;
            }
        }

        [ConfigurationProperty("dropletDir", DefaultValue = @"C:\IronFoundry\droplets", IsRequired = false)]
        public string DropletDir
        {
            get
            {
                return (string)this["dropletDir"];
            }
            set
            {
                this["dropletDir"] = value;
            }
        }

        [ConfigurationProperty("disableDirCleanup", DefaultValue = false, IsRequired = false)]
        public bool DisableDirCleanup
        {
            get
            {
                return Convert.ToBoolean(this["disableDirCleanup"]);
            }
            set
            {
                this["disableDirCleanup"] = value;
            }
        }

        [ConfigurationProperty("filesServicePort", DefaultValue = "12345", IsRequired = false)]
        public ushort FilesServicePort
        {
            get
            {
                return Convert.ToUInt16(this["filesServicePort"]);
            }
            set
            {
                this["filesServicePort"] = value;
            }
        }

        [ConfigurationProperty("maxMemoryMB", DefaultValue = "4096", IsRequired = false)]
        public ushort MaxMemoryMB
        {
            get
            {
                return Convert.ToUInt16(this["maxMemoryMB"]);
            }
            set
            {
                this["maxMemoryMB"] = value;
            }
        }
    }
}