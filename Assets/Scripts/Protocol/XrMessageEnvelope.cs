using System;
using UnityEngine;

namespace AndroidXR.Protocol
{
    [Serializable]
    public sealed class XrMessageEnvelope<TPayload>
    {
        public string protocol;
        public string type;
        public string source;
        public string target;
        public string sessionId;
        public int seq;
        public long timestampMs;
        public TPayload payload;
    }

    public static class XrMessageFactory
    {
        private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static string ToJson<TPayload>(string type, string target, int sequence, TPayload payload)
        {
            var envelope = new XrMessageEnvelope<TPayload>
            {
                protocol = XrProtocolConstants.Protocol,
                type = type,
                source = XrProtocolConstants.SourceAndroid,
                target = target,
                sessionId = XrProtocolConstants.SessionId,
                seq = sequence,
                timestampMs = CurrentTimeMs(),
                payload = payload,
            };

            return JsonUtility.ToJson(envelope);
        }

        private static long CurrentTimeMs()
        {
            return (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;
        }
    }
}

