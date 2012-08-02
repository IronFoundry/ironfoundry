﻿namespace IronFoundry.Nats.Client
{
    using Newtonsoft.Json;

    public abstract class NatsMessage : INatsMessage
    {
        public virtual bool CanPublishWithSubject(string subject)
        {
            return false;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public override string ToString()
        {
            return ToJson();
        }

        [JsonIgnore]
        public abstract string PublishSubject { get; }

        [JsonIgnore]
        public bool IsReceiveOnly
        {
            get { return false; }
        }
    }
}