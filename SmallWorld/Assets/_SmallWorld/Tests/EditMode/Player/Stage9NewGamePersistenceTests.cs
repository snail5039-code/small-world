using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using NUnit.Framework;
using SmallWorld.Puzzle.Stage9.Persistence;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage9NewGamePersistenceTests
    {
        [Test]
        public void StartNewGame_ClearsPhotoPuzzleBeforeRealityRoomTransition()
        {
            var storage = new MemoryStorage();
            storage.Write(PhotoPuzzleSaveContract.Key, "completed");
            var host = new GameObject("Title New Game Reset Test");
            Type controllerType = Type.GetType("SmallWorld.Flow.TitleScreenController, Assembly-CSharp");
            Assert.That(controllerType, Is.Not.Null);
            Component controller = host.AddComponent(controllerType);
            controllerType.GetMethod("ConfigureNewGamePersistence")?.Invoke(controller, new object[] { storage });
            string loadedScene = null;
            bool clearedBeforeTransition = false;
            Func<string, Task> loader = sceneId =>
            {
                clearedBeforeTransition = !storage.Contains(PhotoPuzzleSaveContract.Key);
                loadedScene = sceneId;
                return Task.CompletedTask;
            };
            controllerType.GetMethod("ConfigureNewGameSceneLoader")?.Invoke(controller, new object[] { loader });

            try
            {
                controllerType.GetMethod("StartNewGame")?.Invoke(controller, null);
                Assert.That(clearedBeforeTransition, Is.True);
                Assert.That(loadedScene, Is.EqualTo("RealityRoom"));
                Assert.That(storage.Contains(PhotoPuzzleSaveContract.Key), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private sealed class MemoryStorage : IPhotoPuzzleStorage
        {
            private readonly Dictionary<string, string> values = new Dictionary<string, string>();
            public bool Contains(string key) => values.ContainsKey(key);
            public bool TryRead(string key, out string value) => values.TryGetValue(key, out value);
            public void Write(string key, string value) => values[key] = value;
            public void Delete(string key) => values.Remove(key);
            public void Quarantine(string key, string value) => values[key + ".corrupt"] = value;
        }
    }
}
