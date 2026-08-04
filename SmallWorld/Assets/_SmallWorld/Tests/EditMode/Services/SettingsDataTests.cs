using NUnit.Framework;

namespace SmallWorld.Services.Tests
{
    public sealed class SettingsDataTests
    {
        [Test]
        public void ValidatedCopy_ClampsInvalidValues()
        {
            var source = new SettingsData
            {
                masterVolume = -1f,
                musicVolume = 2f,
                sfxVolume = float.NaN,
                voiceVolume = float.PositiveInfinity,
                width = 100,
                height = 9000
            };

            var result = source.ValidatedCopy();

            Assert.That(result.masterVolume, Is.EqualTo(0f));
            Assert.That(result.musicVolume, Is.EqualTo(1f));
            Assert.That(result.sfxVolume, Is.EqualTo(1f));
            Assert.That(result.voiceVolume, Is.EqualTo(1f));
            Assert.That(result.width, Is.EqualTo(640));
            Assert.That(result.height, Is.EqualTo(4320));
        }

        [Test]
        public void ValidatedCopy_UnknownSchemaRestoresDefaults()
        {
            var source = new SettingsData
            {
                schemaVersion = SettingsData.CurrentSchemaVersion + 1,
                masterVolume = 0.25f,
                width = 1280
            };

            var result = source.ValidatedCopy();

            Assert.That(result.schemaVersion, Is.EqualTo(SettingsData.CurrentSchemaVersion));
            Assert.That(result.masterVolume, Is.EqualTo(1f));
            Assert.That(result.width, Is.EqualTo(SettingsData.DefaultWidth));
        }
    }
}

