using System;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LocalDataStorage.Tests.EditMode
{
    [System.Serializable]
    [DataVersion(2)]
    internal sealed class MigratedData : LocalData
    {
        public int Coins;
        public int Gems;   // added in v2
    }

    [MigrationFor(typeof(MigratedData), from: 1, to: 2)]
    internal sealed class MigratedData_1_to_2 : ILocalDataMigration
    {
        public JObject Migrate(JObject payload)
        {
            payload["Gems"] = 42;
            return payload;
        }
    }

    public sealed class MigrationTests
    {
        private string _dir;
        private ILocalDataStorage _storage;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "gdf-lds-mig-" + Guid.NewGuid().ToString("N"));
            _storage = new Internal.LocalDataStorage(
                LocalDataStorageConfig.Default()
                    .WithFileBackend()
                    .WithFileAbsoluteRoot(_dir)
                    .WithoutEncryption());
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
        }

        [Test]
        public void Load_WithLowerVersion_RunsMigrationChain()
        {
            // Pre-populate the file as if it was saved by schema v1 (prefix int = 1).
            string json = "{\"Coins\":100}";
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
            byte[] payload = new byte[4 + jsonBytes.Length];
            payload[0] = 1; // version 1
            Buffer.BlockCopy(jsonBytes, 0, payload, 4, jsonBytes.Length);

            Directory.CreateDirectory(_dir);
            File.WriteAllBytes(Path.Combine(_dir, typeof(MigratedData).FullName + ".sav"), payload);

            Assert.IsTrue(_storage.TryLoad(out MigratedData loaded));
            Assert.AreEqual(100, loaded.Coins, "Original value should survive the migration.");
            Assert.AreEqual(42, loaded.Gems, "Migration step must inject the v2 field.");
        }
    }
}
