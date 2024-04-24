using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace JanSharp
{
    public static class SelectParticleSystems
    {
        [MenuItem("Tools/Select Particle Systems/Automatic")]
        public static void SelectAutomatic() => SelectMode(ParticleSystemCullingMode.Automatic);
        [MenuItem("Tools/Select Particle Systems/Pause And Catch-up")]
        public static void SelectPauseAndCatchup() => SelectMode(ParticleSystemCullingMode.PauseAndCatchup);
        [MenuItem("Tools/Select Particle Systems/Pause")]
        public static void SelectPause() => SelectMode(ParticleSystemCullingMode.Pause);
        [MenuItem("Tools/Select Particle Systems/Always Simulate")]
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
