using System;

namespace AndroidXR.Protocol
{
    [Serializable]
    public sealed class TrackpadTouchPayload
    {
        public int pointerId;
        public string phase;
        public float x;
        public float y;
        public float pressure;
    }
}

