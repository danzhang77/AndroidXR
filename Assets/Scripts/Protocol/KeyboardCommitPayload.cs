using System;

namespace AndroidXR.Protocol
{
    [Serializable]
    public sealed class KeyboardCommitPayload
    {
        public string keyId;
        public string label;
        public string text;
        public string kind;
        public float confidence;
        public NormalizedPointPayload touch;
    }
}

