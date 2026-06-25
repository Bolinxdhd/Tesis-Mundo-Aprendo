namespace Bolin
{
    public readonly struct ReadingEvaluationResult
    {
        public ReadingEvaluationResult(float similarity, int stars, int matchedWords, int expectedWords, int recognizedWords)
        {
            Similarity = similarity;
            Stars = stars;
            MatchedWords = matchedWords;
            ExpectedWords = expectedWords;
            RecognizedWords = recognizedWords;
        }

        public float Similarity { get; }
        public int Stars { get; }
        public int MatchedWords { get; }
        public int ExpectedWords { get; }
        public int RecognizedWords { get; }
    }
}
