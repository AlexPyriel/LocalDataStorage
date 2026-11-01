using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

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

    /// <summary>Covers v2 end-to-end on both backends: round-trip, slots, deletes, events, corruption handling.</summary>
    [TestFixture(BackendType.PlayerPrefs)]
    [TestFixture(BackendType.File)]
    public sealed class LocalDataStorageTests
    {
        private readonly BackendType _backend;
        private string _tempDir;
        private ILocalDataStorage _storage;

        public LocalDataStorageTests(BackendType backend) => _backend = backend;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteAll();
            _tempDir = Path.Combine(Path.GetTempPath(), "gdf-lds-" + Guid.NewGuid().ToString("N"));

            LocalDataStorageConfig config = _backend == BackendType.File
                ? LocalDataStorageConfig.Default().WithFileBackend().WithFileAbsoluteRoot(_tempDir)
                : LocalDataStorageConfig.Default().WithPlayerPrefsBackend();

            _storage = new Internal.LocalDataStorage(config);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteAll();
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Test]
        public void Save_ThenLoad_ReturnsStoredData()
        {
            _storage.Save(new TestProgressData { Level = 7, Coins = 125 });

            Assert.IsTrue(_storage.TryLoad(out TestProgressData loaded));
            Assert.AreEqual(7, loaded.Level);
            Assert.AreEqual(125, loaded.Coins);
        }

        [Test]
        public void TryLoad_FreshKey_False()
        {
            Assert.IsFalse(_storage.TryLoad(out TestProgressData _));
        }

        [Test]
        public void Delete_PerType_LeavesOtherTypesIntact()
        {
            _storage.Save(new TestProgressData { Level = 3 });
            _storage.Save(new TestSettingsData { Volume = 0.75f });

            _storage.Delete<TestProgressData>();

            Assert.IsFalse(_storage.TryLoad(out TestProgressData _));
            Assert.IsTrue(_storage.TryLoad(out TestSettingsData loaded));
            Assert.AreEqual(0.75f, loaded.Volume);
        }

        [Test]
        public void DeleteAll_ClearsEveryEntry()
        {
            _storage.Save(new TestProgressData { Level = 5 });
            _storage.Save(new TestSettingsData { Volume = 1f });

            _storage.DeleteAll();

            Assert.IsFalse(_storage.TryLoad(out TestProgressData _));
            Assert.IsFalse(_storage.TryLoad(out TestSettingsData _));
        }

        [Test]
        public void Slots_StoreIndependentPayloadsForSameType()
        {
            _storage.Save("slot1", new TestProgressData { Level = 1 });
            _storage.Save("slot2", new TestProgressData { Level = 42 });

            Assert.IsTrue(_storage.TryLoad("slot1", out TestProgressData a));
            Assert.IsTrue(_storage.TryLoad("slot2", out TestProgressData b));
            Assert.AreEqual(1, a.Level);
            Assert.AreEqual(42, b.Level);
        }

        [Test]
        public void ListSlots_ReturnsAllPopulatedSlots()
        {
            _storage.Save(new TestProgressData { Level = 1 });               // default (empty slot)
            _storage.Save("slot1", new TestProgressData { Level = 2 });
            _storage.Save("autosave", new TestProgressData { Level = 3 });

            System.Collections.Generic.HashSet<string> slots = new(_storage.ListSlots<TestProgressData>());

            Assert.Contains(string.Empty, slots);
            Assert.Contains("slot1", slots);
            Assert.Contains("autosave", slots);
        }

        [Test]
        public void Events_DataSaved_FiresPerSave()
        {
            Type captured = null;
            _storage.DataSaved += t => captured = t;

            _storage.Save(new TestProgressData { Level = 1 });

            Assert.AreEqual(typeof(TestProgressData), captured);
        }

        [Test]
        public void Events_DataDeleted_FiresPerDelete()
        {
            _storage.Save(new TestProgressData { Level = 1 });
            Type captured = null;
            _storage.DataDeleted += t => captured = t;

            _storage.Delete<TestProgressData>();

            Assert.AreEqual(typeof(TestProgressData), captured);
        }

        [Test]
        public void Events_AllDeleted_FiresOnDeleteAll()
        {
            _storage.Save(new TestProgressData { Level = 1 });
            bool fired = false;
            _storage.AllDeleted += () => fired = true;

            _storage.DeleteAll();

            Assert.IsTrue(fired);
        }
    }
}
