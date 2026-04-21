using System;

namespace AndroidXR.Protocol
{
    [Serializable]
    public sealed class ClientHelloPayload
    {
        public string role;
        public string clientName;
        public string[] capabilities;
    }
}

