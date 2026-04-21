using System;

namespace AndroidXR.Protocol
{
    [Serializable]
    public sealed class KeyboardSuggestionsPayload
    {
        public string rawWord;
        public string[] suggestions;
    }
}

