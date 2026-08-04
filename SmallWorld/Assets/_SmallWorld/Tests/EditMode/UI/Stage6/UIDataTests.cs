using NUnit.Framework;
using SmallWorld.Services;
using UnityEngine;

namespace SmallWorld.UI.Tests
{
    public sealed class UIDataTests
    {
        [Test]
        public void SettingsSnapshot_ClampsUnsafeValues()
        {
            var value = new UISettingsSnapshot
            {
                master = float.NaN,
                music = -1f,
                sfx = 2f,
                voice = float.PositiveInfinity,
                width = 100,
                height = 9000
            }.Validated();

            Assert.That(value.master, Is.EqualTo(1f));
            Assert.That(value.music, Is.EqualTo(0f));
            Assert.That(value.sfx, Is.EqualTo(1f));
            Assert.That(value.voice, Is.EqualTo(1f));
            Assert.That(value.width, Is.EqualTo(640));
            Assert.That(value.height, Is.EqualTo(4320));
        }

        [Test]
        public void SafeArea_CalculatesNormalizedAnchors()
        {
            Vector4 anchors = SafeAreaFitter.CalculateAnchors(
                new Rect(100f, 50f, 1720f, 980f), 1920, 1080);

            Assert.That(anchors.x, Is.EqualTo(100f / 1920f).Within(0.0001f));
            Assert.That(anchors.y, Is.EqualTo(50f / 1080f).Within(0.0001f));
            Assert.That(anchors.z, Is.EqualTo(1820f / 1920f).Within(0.0001f));
            Assert.That(anchors.w, Is.EqualTo(1030f / 1080f).Within(0.0001f));
        }

        [Test]
        public void SafeArea_InvalidResolutionUsesFullRect()
        {
            Assert.That(SafeAreaFitter.CalculateAnchors(default, 0, 0),
                Is.EqualTo(new Vector4(0f, 0f, 1f, 1f)));
        }

        [Test]
        public void SettingsBinding_RoundTripsValidatedServiceData()
        {
            SettingsData data = new SettingsData
            {
                masterVolume = 0.25f,
                musicVolume = 0.5f,
                sfxVolume = 0.75f,
                voiceVolume = 1f,
                fullscreen = false,
                width = 1600,
                height = 900
            };

            SettingsData roundTrip = Stage6SettingsBinding.ToData(Stage6SettingsBinding.ToSnapshot(data));

            Assert.That(roundTrip.masterVolume, Is.EqualTo(0.25f));
            Assert.That(roundTrip.musicVolume, Is.EqualTo(0.5f));
            Assert.That(roundTrip.sfxVolume, Is.EqualTo(0.75f));
            Assert.That(roundTrip.voiceVolume, Is.EqualTo(1f));
            Assert.That(roundTrip.fullscreen, Is.False);
            Assert.That(roundTrip.width, Is.EqualTo(1600));
            Assert.That(roundTrip.height, Is.EqualTo(900));
        }

        [TestCase(0f, 0f)]
        [TestCase(-100f, 100f)]
        public void SafeArea_EmptyOrNegativeAreaUsesFullRect(float width, float height)
        {
            Assert.That(SafeAreaFitter.CalculateAnchors(
                    new Rect(0f, 0f, width, height), 1920, 1080),
                Is.EqualTo(new Vector4(0f, 0f, 1f, 1f)));
        }

        [Test]
        public void SafeArea_NonFiniteCoordinatesUseFullRect()
        {
            Assert.That(SafeAreaFitter.CalculateAnchors(
                    new Rect(float.NaN, 0f, 1920f, 1080f), 1920, 1080),
                Is.EqualTo(new Vector4(0f, 0f, 1f, 1f)));
        }
    }
}
