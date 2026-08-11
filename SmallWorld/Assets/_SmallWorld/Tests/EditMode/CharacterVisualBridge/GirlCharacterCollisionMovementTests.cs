using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SmallWorld.Character.VisualBridge.Tests
{
    public sealed class GirlCharacterCollisionMovementTests
    {
        [Test]
        public void RealityRoomGirl_UsesCharacterControllerInsteadOfPassiveCollider()
        {
            EditorSceneManager.OpenScene("Assets/_SmallWorld/Scenes/02_RealityRoom.unity");
            GirlCharacterController girl = Object.FindFirstObjectByType<GirlCharacterController>();

            Assert.That(girl, Is.Not.Null);
            Assert.That(girl.CollisionController, Is.Not.Null);
            Assert.That(girl.GetComponent<CapsuleCollider>(), Is.Null);
        }

        [Test]
        public void CharacterMovement_StopsAtSolidCollider()
        {
            GameObject girlObject = new GameObject("Girl");
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                girlObject.transform.position = Vector3.zero;
                CharacterController collisionController = girlObject.AddComponent<CharacterController>();
                collisionController.height = 1.72f;
                collisionController.radius = 0.32f;
                collisionController.center = new Vector3(0f, 0.86f, 0f);
                GirlCharacterController girl = girlObject.AddComponent<GirlCharacterController>();

                wall.transform.position = new Vector3(1f, 0.86f, 0f);
                wall.transform.localScale = new Vector3(0.2f, 1.72f, 2f);
                Physics.SyncTransforms();

                MethodInfo move = typeof(GirlCharacterController).GetMethod(
                    "MoveRespectingCollisions", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(move, Is.Not.Null);
                move.Invoke(girl, new object[] { Vector3.right * 2f });

                Assert.That(girlObject.transform.position.x, Is.LessThan(0.59f));
            }
            finally
            {
                Object.DestroyImmediate(wall);
                Object.DestroyImmediate(girlObject);
            }
        }
    }
}
