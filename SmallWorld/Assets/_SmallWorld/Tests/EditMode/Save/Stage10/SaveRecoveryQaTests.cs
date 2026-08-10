using System;
using System.IO;
using NUnit.Framework;

namespace SmallWorld.Save.Stage10.Tests
{
    public sealed class SaveRecoveryQaTests
    {
        private string directory;
        private AtomicFileSaveStore store;
        private SaveSlot slot;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "SmallWorld-SaveRecoveryQa-" + Guid.NewGuid().ToString("N"));
            store = new AtomicFileSaveStore(directory, new BinarySaveDataSerializer());
            slot = new SaveSlot(SaveSlotKind.Manual, 0);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void Read_CorruptPrimaryTwice_PreservesARecoverableGeneration()
        {
            Assert.That(store.Write(slot, Data("recoverable")), Is.True);
            Assert.That(store.Write(slot, Data("latest")), Is.True);

            string primaryPath = Path.Combine(directory, "manual-0.sav");
            File.WriteAllText(primaryPath, "first corruption");
            Assert.That(store.Read(slot).IsSuccess, Is.True, "The valid backup should repair the first corruption.");

            File.WriteAllText(primaryPath, "second corruption");
            SaveReadResult secondRecovery = store.Read(slot);

            Assert.That(secondRecovery.IsSuccess, Is.True,
                "Self-healing must not replace the only valid backup with the corrupt primary it repaired.");
            Assert.That(secondRecovery.Data.CheckpointId, Is.EqualTo("recoverable"));
        }

        [Test]
        public void Delete_RemovesPrimaryBackupAndTemporaryFiles()
        {
            Assert.That(store.Write(slot, Data("first")), Is.True);
            Assert.That(store.Write(slot, Data("second")), Is.True);
            File.WriteAllText(Path.Combine(directory, "manual-0.sav.tmp"), "interrupted write");

            Assert.That(store.Delete(slot), Is.True);

            Assert.That(Directory.GetFiles(directory, "manual-0.sav*"), Is.Empty);
        }

        private static SaveData Data(string checkpoint)
        {
            SaveData data = SaveData.CreateNew();
            data.CheckpointId = checkpoint;
            data.ActiveSceneId = "reality-room";
            return data;
        }
    }
}
