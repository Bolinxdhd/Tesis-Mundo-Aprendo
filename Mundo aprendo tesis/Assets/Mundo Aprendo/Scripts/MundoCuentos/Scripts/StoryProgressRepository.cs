using System.Collections.Generic;
using UnityEngine;

namespace Bolin
{
    // Guarda progreso por cuento y sincroniza el mundo de Cuentos con el progreso global.
    public class StoryProgressRepository
    {
        public const int WorldIndex = 1;
        public const string DefaultStoryId = "luna";
        public const string CompletedKey = "MundoAprendo_World_1_Completed";
        public const string StarsKey = "MundoAprendo_World_1_Stars";
        public const string TutorialSeenKey = "MundoCuentos_TutorialVisto";

        private const string StoryCompletedKeyFormat = "MundoCuentos_Completado_{0}";
        private const string StoryScoreKeyFormat = "MundoCuentos_Puntaje_{0}";
        private const string StoryStarsKeyFormat = "MundoCuentos_Estrellas_{0}";
        private const string KnownStoryIdsKey = "MundoCuentos_CuentosRegistrados";

        public void Save(int stars, bool completeOnlyWithAtLeastOneStar)
        {
            // Guarda el cuento por defecto usando las estrellas como puntaje aproximado.
            SaveStoryResult(DefaultStoryId, Mathf.RoundToInt(Mathf.Clamp01(stars / 3f) * 100f), stars, completeOnlyWithAtLeastOneStar);
        }

        public void SaveStoryResult(string cuentoId, int score, int stars, bool completeOnlyWithAtLeastOneStar)
        {
            // Guarda mejores valores del cuento y actualiza WorldProgressRepository si corresponde.
            string safeId = NormalizeStoryId(cuentoId);
            int clampedStars = Mathf.Clamp(stars, 0, 3);
            int clampedScore = Mathf.Clamp(score, 0, 100);
            bool markCompleted = !completeOnlyWithAtLeastOneStar || clampedStars > 0;

            int bestStars = Mathf.Max(GetBestStoryStars(safeId), clampedStars);
            int bestScore = Mathf.Max(GetBestStoryScore(safeId), clampedScore);

            PlayerPrefs.SetInt(GetStoryStarsKey(safeId), bestStars);
            PlayerPrefs.SetInt(GetStoryScoreKey(safeId), bestScore);
            if (markCompleted)
            {
                PlayerPrefs.SetInt(GetStoryCompletedKey(safeId), 1);
                RegisterKnownStoryId(safeId);
            }

            if (CountCompletedStoryIds(GetKnownStoryIds()) >= 2)
            {
                WorldProgressRepository.SaveBestResult(WorldIndex, bestStars, true);
            }
            else
            {
                WorldProgressRepository.SaveBestResult(WorldIndex, bestStars, false);
            }

            PlayerPrefs.Save();
            Debug.Log($"[MundoCuentos/Progress] Cuento {safeId} guardado. Puntaje {clampedScore}, estrellas {clampedStars}, mejores {bestScore}/{bestStars}.");
        }

        public static bool IsStoryCompleted(string cuentoId)
        {
            return PlayerPrefs.GetInt(GetStoryCompletedKey(cuentoId), 0) == 1;
        }

        public static int GetBestStoryScore(string cuentoId)
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(GetStoryScoreKey(cuentoId), 0), 0, 100);
        }

        public static int GetBestStoryStars(string cuentoId)
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(GetStoryStarsKey(cuentoId), 0), 0, 3);
        }

        public static int CountCompletedStories(IEnumerable<CuentoData> cuentos)
        {
            // Cuenta cuentos unicos completados segun los ids configurados en la escena.
            HashSet<string> counted = new();
            if (cuentos == null) return 0;

            foreach (CuentoData cuento in cuentos)
            {
                if (cuento == null || string.IsNullOrWhiteSpace(cuento.Id)) continue;

                string safeId = NormalizeStoryId(cuento.Id);
                if (counted.Contains(safeId)) continue;
                if (!IsStoryCompleted(safeId)) continue;

                counted.Add(safeId);
            }

            return counted.Count;
        }

        public static bool HasUnlockedOtherWorlds(IEnumerable<CuentoData> cuentos, int requiredCompletedStories)
        {
            // Regla de desbloqueo usada por el boton de otros mundos.
            return CountCompletedStories(cuentos) >= Mathf.Max(1, requiredCompletedStories);
        }

        private static int CountCompletedStoryIds(IEnumerable<string> cuentoIds)
        {
            HashSet<string> counted = new();
            if (cuentoIds == null) return 0;

            foreach (string cuentoId in cuentoIds)
            {
                string safeId = NormalizeStoryId(cuentoId);
                if (counted.Contains(safeId)) continue;
                if (!IsStoryCompleted(safeId)) continue;

                counted.Add(safeId);
            }

            return counted.Count;
        }

        public static string GetStoryCompletedKey(string cuentoId)
        {
            return string.Format(StoryCompletedKeyFormat, NormalizeStoryId(cuentoId));
        }

        public static string GetStoryScoreKey(string cuentoId)
        {
            return string.Format(StoryScoreKeyFormat, NormalizeStoryId(cuentoId));
        }

        public static string GetStoryStarsKey(string cuentoId)
        {
            return string.Format(StoryStarsKeyFormat, NormalizeStoryId(cuentoId));
        }

        public static void ResetAllStories(bool save = true)
        {
            // Borra progreso individual de cuentos y limpia el registro de ids conocidos.
            foreach (string cuentoId in GetKnownStoryIds())
            {
                ResetStory(cuentoId, false);
            }

            ResetStory(DefaultStoryId, false);
            ResetStory("tres_cerditos", false);
            PlayerPrefs.DeleteKey(KnownStoryIdsKey);
            if (save) PlayerPrefs.Save();
        }

        public static bool IsTutorialCompleted()
        {
            return PlayerPrefs.GetInt(TutorialSeenKey, 0) == 1;
        }

        public static void MarkTutorialCompleted(bool save = true)
        {
            PlayerPrefs.SetInt(TutorialSeenKey, 1);
            if (save) PlayerPrefs.Save();
        }

        public static void ResetTutorialState(bool save = true)
        {
            PlayerPrefs.DeleteKey(TutorialSeenKey);
            if (save) PlayerPrefs.Save();
        }

        public static void ResetStory(string cuentoId, bool save = true)
        {
            PlayerPrefs.DeleteKey(GetStoryCompletedKey(cuentoId));
            PlayerPrefs.DeleteKey(GetStoryScoreKey(cuentoId));
            PlayerPrefs.DeleteKey(GetStoryStarsKey(cuentoId));
            if (save) PlayerPrefs.Save();
        }

        private static void RegisterKnownStoryId(string cuentoId)
        {
            HashSet<string> ids = new(GetKnownStoryIds());
            ids.Add(NormalizeStoryId(cuentoId));
            PlayerPrefs.SetString(KnownStoryIdsKey, string.Join("|", ids));
        }

        private static IEnumerable<string> GetKnownStoryIds()
        {
            string stored = PlayerPrefs.GetString(KnownStoryIdsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(stored)) yield break;

            string[] ids = stored.Split('|');
            foreach (string id in ids)
            {
                string safeId = NormalizeStoryId(id);
                if (!string.IsNullOrWhiteSpace(safeId)) yield return safeId;
            }
        }

        private static string NormalizeStoryId(string cuentoId)
        {
            return string.IsNullOrWhiteSpace(cuentoId) ? DefaultStoryId : SpeechAnswerValidator.NormalizeText(cuentoId).Replace(' ', '_');
        }
    }
}
