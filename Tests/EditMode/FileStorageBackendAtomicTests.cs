using System;
using System.IO;
using LocalDataStorage.Internal;
using NUnit.Framework;

namespace LocalDataStorage.Tests.EditMode
{
    /// <summary>
    /// Exercises <see cref="FileStorageBackend"/> atomic write + .bak fallback. The backend promises:
    /// <list type="bullet">
    ///   <item>Write produces or replaces <c>&lt;key&gt;.sav</c> atomically.</item>
    ///   <item>The previous <c>.sav</c> content survives in <c>&lt;key&gt;.sav.bak</c>.</item>
    ///   <item>If <c>.sav</c> is missing or corrupt and <c>.sav.bak</c> exists, Read returns the backup.</item>
    /// </list>
    /// Simulated crashes delete intermediate files to prove recovery.
    /// </summary>
    public sealed class FileStorageBackendAtomicTests
    {
        private string _dir;
        private FileStorageBackend _backend;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "gdf-lds-atomic-" + Guid.NewGuid().ToString("N"));
            _backend = new FileStorageBackend(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
        }

        [Test]
        public void Write_SecondTime_PromotesPreviousToBak()
        {
            _backend.Write("key", new byte[] { 1 });
            _backend.Write("key", new byte[] { 2 });

            Assert.IsTrue(File.Exists(_backend.PathFor("key")));
            Assert.IsTrue(File.Exists(_backend.BackupPathFor("key")));

            CollectionAssert.AreEqual(new byte[] { 2 }, File.ReadAllBytes(_backend.PathFor("key")));
            CollectionAssert.AreEqual(new byte[] { 1 }, File.ReadAllBytes(_backend.BackupPathFor("key")));
        }

        [Test]
        public void Read_MissingPrimary_FallsBackToBackup()
        {
            _backend.Write("key", new byte[] { 1 });
            _backend.Write("key", new byte[] { 2 });

            // Simulate crash: delete the primary file.
            File.Delete(_backend.PathFor("key"));

            byte[] loaded = _backend.Read("key");

            CollectionAssert.AreEqual(new byte[] { 1 }, loaded);
            Assert.IsTrue(File.Exists(_backend.PathFor("key")), "Read must restore the primary file from the backup.");
        }

        [Test]
        public void Delete_RemovesAllArtifacts()
        {
            _backend.Write("key", new byte[] { 1 });
            _backend.Write("key", new byte[] { 2 });

            _backend.Delete("key");

            Assert.IsFalse(File.Exists(_backend.PathFor("key")));
            Assert.IsFalse(File.Exists(_backend.BackupPathFor("key")));
        }

        [Test]
        public void EnumerateKeys_YieldsOnlyPrimarySaves()
        {
            _backend.Write("a", new byte[] { 1 });
            _backend.Write("a", new byte[] { 2 }); // creates .bak
            _backend.Write("b", new byte[] { 1 });

            System.Collections.Generic.HashSet<string> keys = new(_backend.EnumerateKeys());

            Assert.AreEqual(2, keys.Count);
            Assert.IsTrue(keys.Contains("a"), "a");
            Assert.IsTrue(keys.Contains("b"), "b");
        }
    }
}
