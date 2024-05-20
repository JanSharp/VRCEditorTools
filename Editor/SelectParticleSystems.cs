using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace JanSharp
{
    public static class SelectParticleSystems
    {
        [MenuItem("Tools/JanSharp/Select Particle Systems/Automatic", false, 1000)]
        public static void SelectAutomatic() => SelectMode(ParticleSystemCullingMode.Automatic);
        [MenuItem("Tools/JanSharp/Select Particle Systems/Pause And Catch-up", false, 1000)]
        public static void SelectPauseAndCatchup() => SelectMode(ParticleSystemCullingMode.PauseAndCatchup);
        [MenuItem("Tools/JanSharp/Select Particle Systems/Pause", false, 1000)]
        public static void SelectPause() => SelectMode(ParticleSystemCullingMode.Pause);
        [MenuItem("Tools/JanSharp/Select Particle Systems/Always Simulate", false, 1000)]
        public static void SelectAlwaysSimulate() => SelectMode(ParticleSystemCullingMode.AlwaysSimulate);

        private static void SelectMode(ParticleSystemCullingMode mode)
        {
            List<ParticleSystem> pss = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<ParticleSystem>())
                .Where(ps => ps.main.cullingMode == mode)
                .ToList();
            Debug.Log($"Selecting {pss.Count} Particle Systems with mode: {mode.ToString()}");
            Selection.objects = pss.Select(ps => ps.gameObject).Distinct().ToArray();
        }
    }
}
