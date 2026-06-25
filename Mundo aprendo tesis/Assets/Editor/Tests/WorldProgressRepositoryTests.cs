#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Bolin.Editor.Tests
{
    public class WorldProgressRepositoryTests
    {
        private readonly Dictionary<string, int?> originalValues = new();

        [SetUp]
        public void SetUp()
        {
            originalValues.Clear();
            for (int i = 0; i < WorldProgressRepository.WorldCount; i++)
            {
                Backup(WorldProgressRepository.GetCompletedKey(i));
                Backup(WorldProgressRepository.GetStarsKey(i));
                WorldProgressRepository.ResetWorld(i, false);
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (KeyValuePair<string, int?> entry in originalValues)
            {
                if (entry.Value.HasValue) PlayerPrefs.SetInt(entry.Key, entry.Value.Value);
                else PlayerPrefs.DeleteKey(entry.Key);
            }

            PlayerPrefs.Save();
        }

        [TestCase(-5, 0)]
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(3, 3)]
        [TestCase(8, 3)]
        public void SaveBestResult_ClampsStars(int requestedStars, int expectedStars)
        {
            WorldProgressRepository.SaveBestResult(0, requestedStars);
            Assert.AreEqual(expectedStars, WorldProgressRepository.GetStars(0));
        }

        [Test]
        public void SaveBestResult_DoesNotReplaceHigherScore()
        {
            WorldProgressRepository.SaveBestResult(1, 3);
            WorldProgressRepository.SaveBestResult(1, 1);
            Assert.AreEqual(3, WorldProgressRepository.GetStars(1));
        }

        [Test]
        public void SaveBestResult_MarksWorldCompleted()
        {
            WorldProgressRepository.SaveBestResult(2, 0);
            Assert.IsTrue(WorldProgressRepository.IsCompleted(2));
        }

        [Test]
        public void Unlocking_IsLinear()
        {
            Assert.IsTrue(WorldProgressRepository.IsUnlocked(0));
            Assert.IsFalse(WorldProgressRepository.IsUnlocked(1));
            WorldProgressRepository.SaveBestResult(0, 1);
            Assert.IsTrue(WorldProgressRepository.IsUnlocked(1));
            Assert.IsFalse(WorldProgressRepository.IsUnlocked(2));
        }

        [Test]
        public void ResetAll_RemovesStarsAndCompletedState()
        {
            for (int i = 0; i < WorldProgressRepository.WorldCount; i++) WorldProgressRepository.SaveBestResult(i, 3);
            WorldProgressRepository.ResetAll();

            for (int i = 0; i < WorldProgressRepository.WorldCount; i++)
            {
                Assert.AreEqual(0, WorldProgressRepository.GetStars(i));
                Assert.IsFalse(WorldProgressRepository.IsCompleted(i));
            }
        }

        [TestCase(0, 3)]
        [TestCase(1, 3)]
        [TestCase(2, 2)]
        [TestCase(3, 2)]
        [TestCase(4, 1)]
        [TestCase(5, 1)]
        [TestCase(6, 0)]
        public void StarRatingCalculator_UsesConfiguredThresholds(int mistakes, int expectedStars)
        {
            Assert.AreEqual(expectedStars, StarRatingCalculator.FromMistakes(mistakes, 1, 3, 5));
        }

        private void Backup(string key)
        {
            originalValues[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : null;
        }
    }
}
#endif
