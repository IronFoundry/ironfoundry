﻿namespace IronFoundry.Warden.Protocol
{
    public partial class SpawnResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Message.Type.Spawn; }
        }
    }
}
