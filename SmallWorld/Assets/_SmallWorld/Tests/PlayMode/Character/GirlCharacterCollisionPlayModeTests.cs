using System.Collections;
using NUnit.Framework;
using SmallWorld.Character.Stage11;
using UnityEngine;
using UnityEngine.TestTools;

namespace SmallWorld.Character.PlayMode.Tests
{
    public sealed class GirlCharacterCollisionPlayModeTests
    {
        private const float CharacterRadius = 0.3f;
        private const int SimulationFrames = 240;

        private GameObject root;
        private GirlCharacterController girl;
        private CapsuleCollider girlCollider;
        private Transform player;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Girl collision test root");

            var girlObject = new GameObject("Girl");
            girlObject.transform.SetParent(root.transform);
            girl = girlObject.AddComponent<GirlCharacterController>();
            girlCollider = girlObject.AddComponent<CapsuleCollider>();
            girlCollider.radius = CharacterRadius;
            girlCollider.height = 1.7f;
            girlCollider.center = Vector3.up * 0.85f;

            var playerObject = new GameObject("Player view");
            playerObject.transform.SetParent(root.transform);
            player = playerObject.transform;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator Patrol_DoesNotPassThroughWall()
        {
            girl.transform.position = Vector3.zero;
            player.position = new Vector3(0f, 0f, -20f);
            player.forward = Vector3.back;
            Transform waypoint = CreatePoint("Patrol target", new Vector3(3f, 0f, 0f));
            Collider wall = CreateObstacle("Wall", new Vector3(1.25f, 1f, 0f), new Vector3(0.2f, 2f, 4f));
            girl.Configure(null, null, null, new[] { waypoint }, player);

            yield return AssertNeverPenetrates(wall);
        }

        [UnityTest]
        public IEnumerator Approach_DoesNotPassThroughFurniture()
        {
            girl.transform.position = Vector3.zero;
            player.position = new Vector3(3f, 1.35f, 0f);
            player.forward = Vector3.right;
            Collider furniture = CreateObstacle("Furniture", new Vector3(1.25f, 0.6f, 0f), new Vector3(0.6f, 1.2f, 1.8f));
            girl.Configure(null, null, null, System.Array.Empty<Transform>(), player);
            girl.ApplyCharacterState(GirlMood.Warm, GirlBehavior.Approach);

            yield return AssertNeverPenetrates(furniture);
        }

        [UnityTest]
        public IEnumerator Retreat_DoesNotPassThroughWallBehindGirl()
        {
            girl.transform.position = Vector3.zero;
            player.position = new Vector3(0.75f, 1.35f, 0f);
            player.forward = Vector3.left;
            Collider wall = CreateObstacle("Wall behind girl", new Vector3(-1.25f, 1f, 0f), new Vector3(0.2f, 2f, 4f));
            girl.Configure(null, null, null, System.Array.Empty<Transform>(), player);
            girl.ApplyCharacterState(GirlMood.Hurt, GirlBehavior.Withdraw);

            yield return AssertNeverPenetrates(wall);
        }

        private IEnumerator AssertNeverPenetrates(Collider obstacle)
        {
            Physics.SyncTransforms();
            for (int frame = 0; frame < SimulationFrames; frame++)
            {
                yield return null;
                Physics.SyncTransforms();
                bool overlaps = Physics.ComputePenetration(
                    girlCollider, girlCollider.transform.position, girlCollider.transform.rotation,
                    obstacle, obstacle.transform.position, obstacle.transform.rotation,
                    out _, out float penetrationDistance);

                Assert.That(overlaps && penetrationDistance > 0.001f, Is.False,
                    $"Girl penetrated {obstacle.name} on frame {frame} by {penetrationDistance:F4}m at {girl.transform.position}.");
            }
        }

        private Transform CreatePoint(string name, Vector3 position)
        {
            var point = new GameObject(name);
            point.transform.SetParent(root.transform);
            point.transform.position = position;
            return point.transform;
        }

        private Collider CreateObstacle(string name, Vector3 position, Vector3 size)
        {
            var obstacle = new GameObject(name);
            obstacle.transform.SetParent(root.transform);
            obstacle.transform.position = position;
            var collider = obstacle.AddComponent<BoxCollider>();
            collider.size = size;
            return collider;
        }
    }
}
