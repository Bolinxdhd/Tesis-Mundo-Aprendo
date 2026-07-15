using System;
using UnityEngine;

namespace Bolin
{
    public static class WorldProgressRepository
    {
        public const int WorldCount = 4;
        private const string CompletedKeyFormat = "MundoAprendo_World_{0}_Completed";
        private const string StarsKeyFormat = "MundoAprendo_World_{0}_Stars";

        /// <summary>
        /// Raised after a persisted world-progress change.  Selection UIs subscribe to
        /// this rather than relying on a scene reload to redraw locks and stars.
        /// </summary>
        public static event Action ProgressChanged;

        public static int GetStars(int worldIndex)
        {
            ValidateWorldIndex(worldIndex);
            return Mathf.Clamp(PlayerPrefs.GetInt(GetStarsKey(worldIndex), 0), 0, 3);
        }

        public static bool IsCompleted(int worldIndex)
        {
            ValidateWorldIndex(worldIndex);
            return PlayerPrefs.GetInt(GetCompletedKey(worldIndex), 0) == 1;
        }

        public static bool IsUnlocked(int worldIndex)
        {
            ValidateWorldIndex(worldIndex);
            return worldIndex == 0 || IsCompleted(worldIndex - 1);
        }

        public static int SaveBestResult(int worldIndex, int stars, bool markCompleted = true)
        {
            ValidateWorldIndex(worldIndex);
            int clampedStars = Mathf.Clamp(stars, 0, 3);
            int bestStars = Mathf.Max(GetStars(worldIndex), clampedStars);

            if (markCompleted) PlayerPrefs.SetInt(GetCompletedKey(worldIndex), 1);
            PlayerPrefs.SetInt(GetStarsKey(worldIndex), bestStars);
            PlayerPrefs.Save();
            NotifyProgressChanged();
            return bestStars;
        }

        public static void ResetWorld(int worldIndex, bool save = true)
        {
            ValidateWorldIndex(worldIndex);
            PlayerPrefs.DeleteKey(GetCompletedKey(worldIndex));
            PlayerPrefs.DeleteKey(GetStarsKey(worldIndex));
            if (!save) return;

            PlayerPrefs.Save();
            NotifyProgressChanged();
        }

        public static void ResetAll()
        {
            for (int i = 0; i < WorldCount; i++) ResetWorld(i, false);
            StoryProgressRepository.ResetAllStories(false);
            StoryProgressRepository.ResetTutorialState(false);
            MusicalTutorialController.ResetTutorialState(false);
            SizeWorldTutorialController.ResetTutorialState(false);
            PlayerPrefs.Save();
            NotifyProgressChanged();
        }

        public static string GetCompletedKey(int worldIndex)
        {
            ValidateWorldIndex(worldIndex);
            return string.Format(CompletedKeyFormat, worldIndex);
        }

        public static string GetStarsKey(int worldIndex)
        {
            ValidateWorldIndex(worldIndex);
            return string.Format(StarsKeyFormat, worldIndex);
        }

        private static void ValidateWorldIndex(int worldIndex)
        {
            if (worldIndex < 0 || worldIndex >= WorldCount)
            {
                throw new ArgumentOutOfRangeException(nameof(worldIndex), worldIndex, $"El indice debe estar entre 0 y {WorldCount - 1}.");
            }
        }

        private static void NotifyProgressChanged()
        {
            ProgressChanged?.Invoke();
        }
    }
}
