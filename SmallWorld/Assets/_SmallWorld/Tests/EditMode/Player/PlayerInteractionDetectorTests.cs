using NUnit.Framework;
using UnityEngine;

namespace SmallWorld.Player.Tests
{
    public sealed class PlayerInteractionDetectorTests
    {
        private GameObject detectorObject;

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (gameObject.scene.IsValid() &&
                    (gameObject == detectorObject || gameObject.name.StartsWith("Interaction Detector Test")))
                    Object.DestroyImmediate(gameObject);
            }

            detectorObject = null;
        }

        [Test]
        public void Configure_UsesTwoMeterDefaultRange()
        {
            PlayerInteractionDetector detector = CreateDetector();
            detector.Configure(detectorObject.transform);

            Assert.That(detector.Range, Is.EqualTo(2f));
        }

        [TestCase(1.5f, 0.5f, TestName = "RefreshDetection_NormalDistance_KeepsFrontInteractable")]
        [TestCase(0.3f, 0.2f, TestName = "RefreshDetection_VeryClose_KeepsFrontInteractable")]
        public void RefreshDetection_InFrontAndWithinRange_KeepsTarget(float centerZ, float depth)
        {
            PlayerInteractionDetector detector = CreateDetector();
            InspectableInteractable target = CreateTarget(centerZ, depth);

            detector.RefreshDetection();

            Assert.That(detector.HasTarget, Is.True);
            Assert.That(detector.CurrentInteractable, Is.SameAs(target));
            Assert.That(detector.TryInteract(), Is.True);
        }

        [Test]
        public void RefreshDetection_OnColliderSurface_KeepsFrontInteractable()
        {
            PlayerInteractionDetector detector = CreateDetector();
            InspectableInteractable target = CreateTarget(0.5f, 1f);

            detector.RefreshDetection();

            Assert.That(detector.HasTarget, Is.True);
            Assert.That(detector.CurrentInteractable, Is.SameAs(target));
            Assert.That(detector.TryInteract(), Is.True);
        }

        [Test]
        public void RefreshDetection_InsideCollider_KeepsFrontInteractable()
        {
            PlayerInteractionDetector detector = CreateDetector();
            InspectableInteractable target = CreateTarget(0.25f, 1f);

            detector.RefreshDetection();

            Assert.That(detector.HasTarget, Is.True);
            Assert.That(detector.CurrentInteractable, Is.SameAs(target));
            Assert.That(detector.TryInteract(), Is.True);
        }

        [Test]
        public void TryInteract_BeyondInteractionRange_IsRejected()
        {
            PlayerInteractionDetector detector = CreateDetector();
            InspectableInteractable target = CreateTarget(2.5f, 0.5f);

            detector.RefreshDetection();

            Assert.That(detector.CurrentInteractable, Is.SameAs(target),
                "The target should remain focused inside the detector's focus margin.");
            Assert.That(detector.TryInteract(), Is.False);
            Assert.That(target.InteractionCount, Is.Zero);
        }

        [Test]
        public void RefreshDetection_WallInFrontOfInteractable_BlocksTarget()
        {
            PlayerInteractionDetector detector = CreateDetector();
            CreateTarget(1.5f, 0.5f);
            CreateCollider("Interaction Detector Test Wall", 0.75f, 0.1f);

            detector.RefreshDetection();

            Assert.That(detector.HasTarget, Is.False);
            Assert.That(detector.CurrentInteractable, Is.Null);
        }

        [Test]
        public void RefreshDetection_InteractableBehindView_DoesNotBecomeTarget()
        {
            PlayerInteractionDetector detector = CreateDetector();
            CreateTarget(-1f, 0.5f);

            detector.RefreshDetection();

            Assert.That(detector.HasTarget, Is.False);
            Assert.That(detector.CurrentInteractable, Is.Null);
        }

        private PlayerInteractionDetector CreateDetector()
        {
            detectorObject = new GameObject("Interaction Detector Test Detector");
            detectorObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var detector = detectorObject.AddComponent<PlayerInteractionDetector>();
            detector.Configure(detectorObject.transform, 2f);
            return detector;
        }

        private static InspectableInteractable CreateTarget(float centerZ, float depth)
        {
            GameObject targetObject = CreateCollider("Interaction Detector Test Target", centerZ, depth);
            var target = targetObject.AddComponent<InspectableInteractable>();
            target.ConfigureInspection("Inspect", "Inspected");
            return target;
        }

        private static GameObject CreateCollider(string name, float centerZ, float depth)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.position = new Vector3(0f, 0f, centerZ);
            var collider = gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(1f, 1f, depth);
            Physics.SyncTransforms();
            return gameObject;
        }
    }
}
