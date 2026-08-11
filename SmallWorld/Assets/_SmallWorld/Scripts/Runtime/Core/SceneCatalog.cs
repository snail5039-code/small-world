using System;
using System.Collections.Generic;

namespace SmallWorld.Core
{
    /// <summary>Central mapping between stable scene IDs, Unity names, and asset paths.</summary>
    public static class SceneCatalog
    {
        private const string SceneRoot = "Assets/_SmallWorld/Scenes/";

        private static readonly IReadOnlyDictionary<SceneId, string> Names =
            new Dictionary<SceneId, string>
            {
                { SceneId.Boot, "00_Boot" },
                { SceneId.MainMenu, "01_MainMenu" },
                { SceneId.RealityRoom, "02_RealityRoom" }
                ,{ SceneId.FirstMemory, "03_FirstMemory" }
            };

        public static string GetName(SceneId sceneId)
        {
            if (!Names.TryGetValue(sceneId, out string sceneName))
            {
                throw new ArgumentOutOfRangeException(nameof(sceneId), sceneId, "Unknown scene ID.");
            }

            return sceneName;
        }

        public static string GetPath(SceneId sceneId)
        {
            return SceneRoot + GetName(sceneId) + ".unity";
        }

        public static bool TryGetId(string sceneNameOrPath, out SceneId sceneId)
        {
            foreach (KeyValuePair<SceneId, string> entry in Names)
            {
                if (string.Equals(sceneNameOrPath, entry.Value, StringComparison.Ordinal) ||
                    string.Equals(sceneNameOrPath, GetPath(entry.Key), StringComparison.Ordinal))
                {
                    sceneId = entry.Key;
                    return true;
                }
            }

            sceneId = default;
            return false;
        }
    }
}
