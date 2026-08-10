using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace SmallWorld.Save.Stage10
{
    public sealed class AtomicFileSaveStore : ISaveGameStore
    {
        private const uint Magic = 0x53575356; // SWSV
        private readonly string directory;
        private readonly ISaveDataSerializer serializer;

        public AtomicFileSaveStore(string directory, ISaveDataSerializer serializer)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Save directory cannot be empty.", nameof(directory));
            this.directory = Path.GetFullPath(directory);
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public bool Write(SaveSlot slot, SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var path = PathFor(slot);
            var temporaryPath = path + ".tmp";
            var backupPath = path + ".bak";
            try
            {
                Directory.CreateDirectory(directory);
                var payload = serializer.Serialize(data);
                var envelope = CreateEnvelope(data.Version, payload);
                WriteThrough(temporaryPath, envelope);
                if (File.Exists(path))
                    File.Replace(temporaryPath, path, backupPath);
                else
                    File.Move(temporaryPath, path);
                return true;
            }
            catch
            {
                TryDelete(temporaryPath);
                return false;
            }
        }

        public SaveReadResult Read(SaveSlot slot)
        {
            var path = PathFor(slot);
            var primary = ReadFile(path);
            if (primary.IsSuccess || primary.Status == SaveReadStatus.UnsupportedVersion) return primary;

            var backupPath = path + ".bak";
            var backup = ReadFile(backupPath);
            if (!backup.IsSuccess) return primary.Status == SaveReadStatus.Missing ? backup : primary;

            // Best-effort self-healing. Never rotate the corrupt primary over the
            // only valid backup; repeated corruption must remain recoverable.
            RepairPrimaryFromBackup(path, backupPath);
            return SaveReadResult.Success(backup.Data, backupPath);
        }

        public bool Delete(SaveSlot slot)
        {
            try
            {
                DeleteFamily(PathFor(slot));
                return true;
            }
            catch { return false; }
        }

        public void DeleteAllProgress()
        {
            for (var i = 0; i < 2; i++) Delete(new SaveSlot(SaveSlotKind.Auto, i));
            for (var i = 0; i < 3; i++) Delete(new SaveSlot(SaveSlotKind.Manual, i));
        }

        private SaveReadResult ReadFile(string path)
        {
            if (!File.Exists(path)) return SaveReadResult.Failure(SaveReadStatus.Missing, path);
            try
            {
                int version;
                byte[] payload;
                if (!TryReadEnvelope(File.ReadAllBytes(path), out version, out payload))
                    return SaveReadResult.Failure(SaveReadStatus.Corrupt, path);
                if (version > SaveData.CurrentVersion || version < 1)
                    return SaveReadResult.Failure(SaveReadStatus.UnsupportedVersion, path);
                SaveData data;
                if (!serializer.TryDeserialize(payload, out data) || data == null || data.Version != version)
                    return SaveReadResult.Failure(SaveReadStatus.Corrupt, path);
                return SaveReadResult.Success(data, path);
            }
            catch (IOException) { return SaveReadResult.Failure(SaveReadStatus.IoFailure, path); }
            catch (UnauthorizedAccessException) { return SaveReadResult.Failure(SaveReadStatus.IoFailure, path); }
            catch { return SaveReadResult.Failure(SaveReadStatus.Corrupt, path); }
        }

        private string PathFor(SaveSlot slot) => Path.Combine(directory, slot.FileStem + ".sav");

        private static byte[] CreateEnvelope(int version, byte[] payload)
        {
            if (payload == null) throw new InvalidOperationException("Serializer returned null.");
            using (var output = new MemoryStream())
            using (var writer = new BinaryWriter(output))
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(payload);
                writer.Write(Magic);
                writer.Write(version);
                writer.Write(payload.Length);
                writer.Write(hash.Length);
                writer.Write(hash);
                writer.Write(payload);
                writer.Flush();
                return output.ToArray();
            }
        }

        private static bool TryReadEnvelope(byte[] bytes, out int version, out byte[] payload)
        {
            version = 0;
            payload = null;
            if (bytes == null) return false;
            try
            {
                using (var input = new MemoryStream(bytes, false))
                using (var reader = new BinaryReader(input))
                using (var sha = SHA256.Create())
                {
                    if (reader.ReadUInt32() != Magic) return false;
                    version = reader.ReadInt32();
                    var payloadLength = reader.ReadInt32();
                    var hashLength = reader.ReadInt32();
                    if (payloadLength < 0 || hashLength != 32 || input.Length - input.Position != hashLength + payloadLength) return false;
                    var expected = reader.ReadBytes(hashLength);
                    payload = reader.ReadBytes(payloadLength);
                    var actual = sha.ComputeHash(payload);
                    return FixedTimeEquals(expected, actual);
                }
            }
            catch { payload = null; return false; }
        }

        private static bool FixedTimeEquals(IList<byte> left, IList<byte> right)
        {
            if (left.Count != right.Count) return false;
            var difference = 0;
            for (var i = 0; i < left.Count; i++) difference |= left[i] ^ right[i];
            return difference == 0;
        }

        private static void WriteThrough(string path, byte[] bytes)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
        }

        private static void RepairPrimaryFromBackup(string primaryPath, string backupPath)
        {
            var temporaryPath = primaryPath + ".repair.tmp";
            try
            {
                WriteThrough(temporaryPath, File.ReadAllBytes(backupPath));
                File.Replace(temporaryPath, primaryPath, null);
            }
            catch
            {
                TryDelete(temporaryPath);
            }
        }

        private static void DeleteFamily(string path)
        {
            TryDelete(path);
            TryDelete(path + ".bak");
            TryDelete(path + ".tmp");
            TryDelete(path + ".repair.tmp");
        }

        private static void TryDelete(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
