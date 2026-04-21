namespace AndroidXR.LanguageModel
{
    public readonly struct WordDecodeResult
    {
        public string Word { get; }
        public float Score { get; }
        public bool IsDecoded { get; }

        public WordDecodeResult(string word, float score, bool isDecoded)
        {
            Word = word;
            Score = score;
            IsDecoded = isDecoded;
        }
    }
}

