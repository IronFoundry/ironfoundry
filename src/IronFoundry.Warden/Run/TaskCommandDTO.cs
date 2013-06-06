﻿namespace IronFoundry.Warden.Run
{
    using Newtonsoft.Json;

    public class TaskCommandDTO
    {
        [JsonProperty(PropertyName="cmd")]
        public string Command { get; set; }

        [JsonProperty(PropertyName="args")]
        public string[] Args { get; set; }
    }
}
