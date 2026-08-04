using NUnit.Framework;
using UnityEngine;

namespace SmallWorld.Player.Tests
{
    public sealed class PlayerFootstepEmitterTests
    {
        [Test]
        public void Tick_EmitsWithoutAnAudioClip()
        {
            var gameObject = new GameObject("Footsteps");
            try
            {
                var emitter = gameObject.AddComponent<PlayerFootstepEmitter>();
                int count = 0;
                emitter.Step += _ => count++;

                emitter.Tick(0.1f, true, false);

                Assert.That(count, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Tick_DoesNotEmitWhileAirborne()
        {
            var gameObject = new GameObject("Footsteps");
            try
            {
                var emitter = gameObject.AddComponent<PlayerFootstepEmitter>();
                int count = 0;
                emitter.Step += _ => count++;

                emitter.Tick(10f, false, true);

                Assert.That(count, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
