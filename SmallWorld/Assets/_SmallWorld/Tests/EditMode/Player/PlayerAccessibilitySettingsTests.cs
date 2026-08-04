using NUnit.Framework;

namespace SmallWorld.Player.Tests
{
    public sealed class PlayerAccessibilitySettingsTests
    {
        [Test]
        public void Defaults_AreComfortFocused()
        {
            var settings = new PlayerAccessibilitySettings();

            Assert.That(settings.FieldOfView, Is.EqualTo(85f));
            Assert.That(settings.CrosshairVisible, Is.True);
            Assert.That(settings.CameraBobEnabled, Is.True);
        }

        [Test]
        public void FixedComfortDot_ForcesCrosshairAndDisablesBob()
        {
            var settings = new PlayerAccessibilitySettings();
            settings.Configure(0.4f, 90f, true, false, true);

            Assert.That(settings.CrosshairVisible, Is.True);
            Assert.That(settings.CameraBobEnabled, Is.False);
        }

        [Test]
        public void Values_AreClampedToSafeRanges()
        {
            var settings = new PlayerAccessibilitySettings();
            settings.Configure(99f, 200f, false, false, false);

            Assert.That(settings.LookSensitivity, Is.EqualTo(2f));
            Assert.That(settings.FieldOfView, Is.EqualTo(110f));
        }
    }
}
