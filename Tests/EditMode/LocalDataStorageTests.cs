using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LocalDataStorage.Tests.EditMode
{
    [System.Serializable]
    internal sealed class TestProgressData : LocalData
    {
        public int Level;
        public int Coins;
    }

    [System.Serializable]
    internal sealed class TestSettingsData : LocalData
    {
        public float Volume;
    }

    public sealed class LocalDataStorageTests
    {
        private const string CORRUPTED_PAYLOAD = "not-a-valid-encrypted-payload";

        private ILocalDataStorage _storage;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteAll();
            _storage = new Internal.LocalDataStorage();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteAll();
        }

        [Test]
        public void Save_ThenLoad_ReturnsStoredData()
        {
            TestProgressData data = new()
            {
                Level = 7,
                Coins = 125,
            };

            _storage.Save(data);

            bool loaded = _storage.TryLoad(out TestProgressData loadedData);

            Assert.That(loaded, Is.True);
            Assert.That(loadedData, Is.Not.Null);
            Assert.That(loadedData.Level, Is.EqualTo(7));
            Assert.That(loadedData.Coins, Is.EqualTo(125));
        }

        [Test]
        public void TryLoad_WithoutSavedData_ReturnsFalse()
        {
            bool loaded = _storage.TryLoad(out TestProgressData loadedData);

            Assert.That(loaded, Is.False);
            Assert.That(loadedData, Is.Null);
        }

        [Test]
        public void Delete_RemovesOnlyRequestedType()
        {
            TestProgressData progressData = new()
            {
                Level = 3,
                Coins = 40,
            };
            TestSettingsData settingsData = new()
            {
                Volume = 0.75f,
            };

            _storage.Save(progressData);
            _storage.Save(settingsData);

            _storage.Delete<TestProgressData>();

            Assert.That(_storage.TryLoad(out TestProgressData deletedProgress), Is.False);
            Assert.That(deletedProgress, Is.Null);
            Assert.That(_storage.TryLoad(out TestSettingsData loadedSettings), Is.True);
            Assert.That(loadedSettings, Is.Not.Null);
            Assert.That(loadedSettings.Volume, Is.EqualTo(0.75f));
        }

        [Test]
        public void DeleteAll_ClearsStoredData_AndCache()
        {
            _storage.Save(new TestProgressData
            {
                Level = 5,
                Coins = 10,
            });

            _storage.DeleteAll();

            Assert.That(_storage.TryLoad(out TestProgressData loadedData), Is.False);
            Assert.That(loadedData, Is.Null);
            Assert.That(PlayerPrefs.HasKey(typeof(TestProgressData).ToString()), Is.False);
        }

        [Test]
        public void DeleteAll_DoesNotDeleteForeignPlayerPrefsKeys()
        {
            const string foreignKey = "foreign.key";
            const string foreignValue = "foreign-value";

            PlayerPrefs.SetString(foreignKey, foreignValue);
            _storage.Save(new TestProgressData
            {
                Level = 9,
                Coins = 90,
            });

            _storage.DeleteAll();

            Assert.That(PlayerPrefs.GetString(foreignKey), Is.EqualTo(foreignValue));
            Assert.That(PlayerPrefs.HasKey(typeof(TestProgressData).ToString()), Is.False);
        }

        [Test]
        public void TryLoad_WithCorruptedPayload_ReturnsFalse()
        {
            PlayerPrefs.SetString(typeof(TestProgressData).ToString(), CORRUPTED_PAYLOAD);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("LocalDataStorage Failed to load data for key"));

            bool loaded = _storage.TryLoad(out TestProgressData loadedData);

            Assert.That(loaded, Is.False);
            Assert.That(loadedData, Is.Null);
        }
    }
}
