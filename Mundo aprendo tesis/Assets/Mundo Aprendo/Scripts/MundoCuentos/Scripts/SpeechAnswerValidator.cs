using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bolin
{
    public readonly struct SpeechValidationResult
    {
        public SpeechValidationResult(bool isCorrect, string recognizedAlternative, string acceptedAnswer)
        {
            IsCorrect = isCorrect;
            RecognizedAlternative = recognizedAlternative ?? string.Empty;
            AcceptedAnswer = acceptedAnswer ?? string.Empty;
        }

        public bool IsCorrect { get; }
        public string RecognizedAlternative { get; }
        public string AcceptedAnswer { get; }
    }

    public sealed class SpeechAnswerValidator
    {
        public SpeechValidationResult Validate(PictogramaData pictograma, IReadOnlyList<string> alternatives)
        {
            List<string> acceptedAnswers = BuildAcceptedAnswers(pictograma);
            if (pictograma == null || alternatives == null || alternatives.Count == 0)
            {
                LogResult(pictograma, acceptedAnswers, false, string.Empty, string.Empty);
                return new SpeechValidationResult(false, string.Empty, string.Empty);
            }

            for (int alternativeIndex = 0; alternativeIndex < alternatives.Count; alternativeIndex++)
            {
                string recognized = alternatives[alternativeIndex] ?? string.Empty;
                string normalizedRecognized = NormalizeText(recognized);
                if (string.IsNullOrEmpty(normalizedRecognized)) continue;

                string[] recognizedTokens = normalizedRecognized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (string accepted in acceptedAnswers)
                {
                    string normalizedAccepted = NormalizeText(accepted);
                    if (string.IsNullOrEmpty(normalizedAccepted)) continue;

                    string[] acceptedTokens = normalizedAccepted.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (!ContainsWholePhrase(recognizedTokens, acceptedTokens)) continue;

                    LogResult(pictograma, acceptedAnswers, true, recognized, accepted);
                    return new SpeechValidationResult(true, recognized, accepted);
                }
            }

            string firstAlternative = alternatives.Count > 0 ? alternatives[0] ?? string.Empty : string.Empty;
            LogResult(pictograma, acceptedAnswers, false, firstAlternative, string.Empty);
            return new SpeechValidationResult(false, firstAlternative, string.Empty);
        }

        public static string NormalizeText(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            string lowered = input.Trim().ToLowerInvariant();
            StringBuilder builder = new(lowered.Length);
            bool previousWasSpace = true;

            foreach (char sourceCharacter in lowered)
            {
                if (sourceCharacter == 'ñ')
                {
                    builder.Append(sourceCharacter);
                    previousWasSpace = false;
                    continue;
                }

                string decomposedCharacter = sourceCharacter.ToString().Normalize(NormalizationForm.FormD);
                bool appendedCharacter = false;
                foreach (char character in decomposedCharacter)
                {
                    UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
                    if (category == UnicodeCategory.NonSpacingMark) continue;
                    if (!char.IsLetterOrDigit(character)) continue;

                    builder.Append(character);
                    previousWasSpace = false;
                    appendedCharacter = true;
                }

                if (appendedCharacter || previousWasSpace) continue;
                builder.Append(' ');
                previousWasSpace = true;
            }

            return builder.ToString().Trim().Normalize(NormalizationForm.FormC);
        }

        private static List<string> BuildAcceptedAnswers(PictogramaData pictograma)
        {
            List<string> answers = new();
            if (pictograma == null) return answers;

            HashSet<string> normalizedAnswers = new(StringComparer.Ordinal);
            AddUniqueAnswer(answers, normalizedAnswers, pictograma.Concepto);

            IReadOnlyList<string> configured = pictograma.RespuestasAceptadas;
            if (configured == null) return answers;

            for (int i = 0; i < configured.Count; i++)
            {
                AddUniqueAnswer(answers, normalizedAnswers, configured[i]);
            }

            return answers;
        }

        private static void AddUniqueAnswer(ICollection<string> answers, ISet<string> normalizedAnswers, string answer)
        {
            if (string.IsNullOrWhiteSpace(answer)) return;
            string normalized = NormalizeText(answer);
            if (string.IsNullOrEmpty(normalized) || !normalizedAnswers.Add(normalized)) return;
            answers.Add(answer.Trim());
        }

        private static bool ContainsWholePhrase(IReadOnlyList<string> source, IReadOnlyList<string> phrase)
        {
            if (source == null || phrase == null || phrase.Count == 0 || phrase.Count > source.Count) return false;

            int lastStart = source.Count - phrase.Count;
            for (int start = 0; start <= lastStart; start++)
            {
                bool matches = true;
                for (int offset = 0; offset < phrase.Count; offset++)
                {
                    if (string.Equals(source[start + offset], phrase[offset], StringComparison.Ordinal)) continue;
                    matches = false;
                    break;
                }

                if (matches) return true;
            }

            return false;
        }

        private static void LogResult(
            PictogramaData pictograma,
            IReadOnlyList<string> acceptedAnswers,
            bool accepted,
            string recognized,
            string answer)
        {
            string concept = pictograma != null ? pictograma.Concepto : "<sin configurar>";
            string receivedText = string.IsNullOrWhiteSpace(recognized) ? "<vacío>" : recognized;
            string configuredAnswers = acceptedAnswers != null && acceptedAnswers.Count > 0
                ? string.Join(", ", acceptedAnswers)
                : "<ninguna>";
            UnityEngine.Debug.Log($"[MundoCuentos/Validator] Texto recibido: \"{receivedText}\"");
            UnityEngine.Debug.Log($"[MundoCuentos/Validator] Concepto esperado: \"{concept}\"");
            UnityEngine.Debug.Log($"[MundoCuentos/Validator] Respuestas aceptadas: [{configuredAnswers}]");
            string suffix = accepted && !string.IsNullOrWhiteSpace(answer) ? $" ({answer})" : string.Empty;
            UnityEngine.Debug.Log($"[MundoCuentos/Validator] Resultado: {(accepted ? "CORRECTO" : "RECHAZADO")}{suffix}");
        }
    }
}
