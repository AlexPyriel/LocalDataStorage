using System;
using System.Text;
using LocalDataStorage.Internal;
using NUnit.Framework;
using UnityEngine;

namespace LocalDataStorage.Tests.EditMode
{
    /// <summary>
    /// Confirms that payloads written by com.gdf.local-data-storage v1.0.0 (base64 encrypted JSON
    /// under <c>PlayerPrefs[typeof(T).ToString()]</c>) are still readable through v2.
    /// </summary>
    public sealed class BackwardCompatTests
    {
        private const string LEGACY_PASS_PHRASE = LocalDataStorageConfig.DEFAULT_PASS_PHRASE;

        [SetUp]
        public void SetUp() => PlayerPrefs.DeleteAll();

        [TearDown]
        public void TearDown() => PlayerPrefs.DeleteAll();

        [Test]
        public void Load_V1EncryptedPayload_ReadsSuccessfully()
        {
            // Simulate a v1.0.0 save: JSON encoded directly, encrypted, base64 string in PlayerPrefs.
            TestProgressData expected = new() { Level = 9, Coins = 999 };
            string json = JsonUtility.ToJson(expected);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            EncryptionService encryption = new(LEGACY_PASS_PHRASE);
            byte[] cipher = encryption.Encrypt(jsonBytes);
            string base64 = Convert.ToBase64String(cipher);

            string key = typeof(TestProgressData).ToString();
            PlayerPrefs.SetString(key, base64);
            // v1 kept a key registry under the same name; v2's PlayerPrefsStorageBackend reads the registry
            // on DeleteAll but Read works even without a registry entry.
            PlayerPrefs.Save();

            ILocalDataStorage storage = new LocalDataStorage.Internal.LocalDataStorage();
            Assert.IsTrue(storage.TryLoad(out TestProgressData loaded));
            Assert.AreEqual(9, loaded.Level);
            Assert.AreEqual(999, loaded.Coins);
        }
    }
}
