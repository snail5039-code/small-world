using NUnit.Framework;

namespace SmallWorld.Services.Tests
{
    public sealed class AudioServiceTests
    {
        [TestCase(1f, 0f)]
        [TestCase(0f, AudioService.MinimumDecibels)]
        [TestCase(-1f, AudioService.MinimumDecibels)]
        public void LinearToDecibels_HandlesBoundaryValues(float linear, float expected)
        {
            Assert.That(AudioService.LinearToDecibels(linear), Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void LinearToDecibels_ConvertsHalfVolume()
        {
            Assert.That(AudioService.LinearToDecibels(0.5f), Is.EqualTo(-6.0206f).Within(0.001f));
        }
    }
}

