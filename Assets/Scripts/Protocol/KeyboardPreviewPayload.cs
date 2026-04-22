using System;

namespace AndroidXR.Protocol
{
    [Serializable]
    public sealed class KeyboardPreviewPayload
    {
        public string keyId;
        public string label;
        public bool isActive;
        public NormalizedPointPayload touch;
    }
}

