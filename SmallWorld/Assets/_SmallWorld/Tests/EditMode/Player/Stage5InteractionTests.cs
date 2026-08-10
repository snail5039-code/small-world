using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage5InteractionTests
    {
        [Test]
        public void Inspectable_RotatesAndReportsExactlyOncePerCall()
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var inspectable = gameObject.AddComponent<InspectableInteractable>();
                inspectable.ConfigureInspection("조사", "테스트", gameObject.transform, 30f);
                Quaternion before = gameObject.transform.rotation;

                bool result = inspectable.TryInteract(default);

                Assert.That(result, Is.True);
                Assert.That(inspectable.InteractionCount, Is.EqualTo(1));
                Assert.That(gameObject.transform.rotation, Is.Not.EqualTo(before));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void MidnightClock_InspectionDoesNotRotateClock()
        {
            var clock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var inspectable = clock.AddComponent<InspectableInteractable>();
                inspectable.ConfigureInspection("시계 조사", "시곗바늘은 00:00에서 멈춰 있다.", null, 0f);
                Quaternion before = clock.transform.rotation;

                Assert.That(inspectable.TryInteract(default), Is.True);
                Assert.That(clock.transform.rotation, Is.EqualTo(before));
            }
            finally
            {
                Object.DestroyImmediate(clock);
            }
        }

        [Test]
        public void RealityRoom_MidnightClockHasNoRotationTarget()
        {
            EditorSceneManager.OpenScene("Assets/_SmallWorld/Scenes/02_RealityRoom.unity");
            GameObject clock = GameObject.Find("Midnight Clock");

            Assert.That(clock, Is.Not.Null);
            var inspectable = clock.GetComponent<InspectableInteractable>();
            Assert.That(inspectable, Is.Not.Null);
            var serialized = new SerializedObject(inspectable);
            Assert.That(serialized.FindProperty("rotateTarget").objectReferenceValue, Is.Null);
            Assert.That(serialized.FindProperty("rotationStep").floatValue, Is.Zero);
        }

        [Test]
        public void Pickup_CannotBeCollectedTwice()
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var pickup = gameObject.AddComponent<PickupInteractable>();
                pickup.ConfigurePickup("줍기", "test.item", "획득");

                Assert.That(pickup.TryInteract(default), Is.True);
                Assert.That(pickup.Collected, Is.True);
                Assert.That(pickup.TryInteract(default), Is.False);
                Assert.That(pickup.InteractionCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Pickup_RaisesCompletionBeforeDisabledColliderCanDropDetection()
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var pickup = gameObject.AddComponent<PickupInteractable>();
            pickup.ConfigurePickup("줍기", "test.atomic", "획득");
            int completed = 0;
            pickup.InteractionCompleted += _ => completed++;

            try
            {
                Assert.That(pickup.TryInteract(new InteractionContext(null, null)), Is.True);
                Assert.That(gameObject.GetComponent<Collider>().enabled, Is.False);
                Assert.That(completed, Is.EqualTo(1),
                    "Completion must be delivered atomically even when pickup disables its collider.");
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Detector_ExecutesOnlyInsideTwoMetres()
        {
            var detectorObject = new GameObject("Detector");
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var detector = detectorObject.AddComponent<PlayerInteractionDetector>();
                detector.Configure(detectorObject.transform, 2f);
                var inspectable = target.AddComponent<InspectableInteractable>();
                inspectable.ConfigureInspection("조사", "거리 테스트");

                target.transform.position = new Vector3(0f, 0f, 2.51f);
                Physics.SyncTransforms();
                detector.RefreshDetection();
                Assert.That(detector.HasTarget, Is.True);
                Assert.That(detector.TryInteract(), Is.False);

                target.transform.position = new Vector3(0f, 0f, 2.49f);
                Physics.SyncTransforms();
                detector.RefreshDetection();
                Assert.That(detector.TryInteract(), Is.True);
                Assert.That(inspectable.InteractionCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(detectorObject);
            }
        }

        [Test]
        public void Door_ChangesStateWithoutDuplicateSideEffects()
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var door = gameObject.AddComponent<DoorInteractable>();
                door.ConfigureDoor("열기", gameObject.transform, 90f, 0f);

                Assert.That(door.TryInteract(default), Is.True);
                Assert.That(door.IsOpen, Is.True);
                Assert.That(door.InteractionCount, Is.EqualTo(1));
                Assert.That(gameObject.transform.localEulerAngles.y, Is.EqualTo(90f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
