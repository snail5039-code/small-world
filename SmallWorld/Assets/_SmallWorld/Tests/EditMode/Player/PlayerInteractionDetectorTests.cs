using NUnit.Framework;
using UnityEngine;

namespace SmallWorld.Player.Tests
{
    public sealed class PlayerInteractionDetectorTests
    {
        [Test]
        public void Configure_UsesTwoMeterDefaultRange()
        {
            var gameObject = new GameObject("Detector");
            try
            {
                var detector = gameObject.AddComponent<PlayerInteractionDetector>();
                detector.Configure(gameObject.transform);

                Assert.That(detector.Range, Is.EqualTo(2f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
