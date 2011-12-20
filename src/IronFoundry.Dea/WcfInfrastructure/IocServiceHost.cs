﻿namespace IronFoundry.Dea.WcfInfrastructure
{
    using System;

    public class IocServiceHost : PerCallServiceHost
    {
        public IocServiceHost(Type serviceType) : base(serviceType) { }

        protected override void OnOpening()
        {
            IocServiceBehaviorUtil.AddIoCServiceBehaviorTo(this);
            base.OnOpening();
        }
    }
}