using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Bolin
{
    public class ReadingEvaluator
    {
        private readonly bool removeCommonWords;
        private readonly HashSet<string> commonWords = new()
        {
            "a", "al", "cada", "con", "de", "del", "el", "en", "es", "la", "las",
            "lo", "los", "para", "por", "que", "se", "su", "sus", "un", "una",
            "unas", "unos", "y"
        };

        public ReadingEvaluator(bool removeCommonWords)
        {
            this.removeCommonWords = removeCommonWords;
        }

        public ReadingEvaluationResult Evaluate(
            string expected,
            string recognized,
            float threeStarsThreshold,
            float twoStarsThreshold,
            float oneStarThreshold)
        {
            string[] expectedWords = GetWords(expected);
            string[] recognizedWords = GetWords(recognized);

            if (expectedWords.Length == 0 || recognizedWords.Length == 0)
            {
                return new ReadingEvaluationResult(0f, 0, 0, expectedWords.Length, recognizedWords.Length);
            }

            Dictionary<string, int> recognizedCounts = new();
            foreach (string word in recognizedWords)
            {
                recognizedCounts.TryGetValue(word, out int count);
                recognizedCounts[word] = count + 1;
            }

            int matches = 0;
            foreach (string expectedWord in expectedWords)
            {
                if (!recognizedCounts.TryGetValue(expectedWord, out int count) || count <= 0) continue;

                matches++;
                recognizedCounts[expectedWord] = count - 1;
            }

            float similarity = Mathf.Clamp01(matches / (float)expectedWords.Length);
            int stars = CalculateStars(similarity, threeStarsThreshold, twoStarsThreshold, oneStarThreshold);
            return new ReadingEvaluationResult(similarity, stars, matches, expectedWords.Length, recognizedWords.Length);
        }

        public int CountWords(string text)
        {
            return GetWords(text).Length;
        }

        private string[] GetWords(string text)
        {
            string normalized = NormalizeText(text);
            if (string.IsNullOrWhiteSpace(normalized)) return new string[0];

            string[] words = normalized.Split(' ');
            if (!removeCommonWords) return words;

            List<string> filteredWords = new();
            foreach (string word in words)
            {
                if (!commonWords.Contains(word))
                {
                    filteredWords.Add(word);
                }
            }

            return filteredWords.ToArray();
        }

        public static string NormalizeText(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            string lower = input.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            StringBuilder builder = new();

            foreach (char character in lower)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category == UnicodeCategory.NonSpacingMark) continue;

                builder.Append(char.IsLetterOrDigit(character) || char.IsWhiteSpace(character) ? character : ' ');
            }

            return string.Join(" ", builder
                .ToString()
                .Normalize(NormalizationForm.FormC)
                .Split(new[] { ' ', '\n', '\r', '\t' }, System.StringSplitOptions.RemoveEmptyEntries));
        }

        public static string[] GetNormalizedWords(string input)
        {
            string normalized = NormalizeText(input);
            return string.IsNullOrWhiteSpace(normalized)
                ? new string[0]
                : normalized.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        }

        private static int CalculateStars(
            float similarity,
            float threeStarsThreshold,
            float twoStarsThreshold,
            float oneStarThreshold)
        {
            if (similarity >= threeStarsThreshold) return 3;
            if (similarity >= twoStarsThreshold) return 2;
            if (similarity >= oneStarThreshold) return 1;
            return 0;
        }
    }
}
