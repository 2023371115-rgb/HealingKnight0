using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HealingKnight.EditorTools
{
    /// <summary>
    /// "Healing Knight → Construir juego (.exe para Windows)": arma el juego listo para compartir en
    /// &lt;proyecto&gt;/Builds/Windows/ con TODAS las escenas de la lista del build (las pone "Regenerar proyecto base"; la
    /// primera, el menú principal, es la que abre el juego). Al terminar abre la carpeta.
    /// Para compartirlo hay que mandar la carpeta COMPLETA (el .exe solo no funciona): comprímela en .zip.
    /// La carpeta Builds/ no se sube a git (.gitignore).
    /// </summary>
    public static class GameBuilder
    {
        private const string ExeName = "HealingKnight.exe";

        [MenuItem("Healing Knight/Construir juego (.exe para Windows)", priority = 3)]
        public static void BuildWindows()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Healing Knight", "Primero sal del modo Play (botón ▶ de arriba) y vuelve a intentarlo.", "OK");
                return;
            }
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            {
                EditorUtility.DisplayDialog("Healing Knight",
                    "A tu Unity le falta el módulo para Windows.\n\nAbre Unity Hub → Installs → tu versión → engrane → Add modules → " +
                    "marca \"Windows Build Support (Mono)\" → Install. Luego vuelve a intentarlo.", "OK");
                return;
            }

            var scenes = new List<string>();
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
                if (s.enabled && File.Exists(s.path)) scenes.Add(s.path);
            if (scenes.Count == 0)
            {
                EditorUtility.DisplayDialog("Healing Knight", "No hay escenas en la lista del build. Corre primero \"Healing Knight → Regenerar proyecto base\".", "OK");
                return;
            }

            // Guarda lo que tengas abierto (pregunta) para no perder cambios.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (PlayerSettings.productName == "New Unity Project" || string.IsNullOrEmpty(PlayerSettings.productName))
                PlayerSettings.productName = "Healing Knight";

            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Windows"));
            Directory.CreateDirectory(folder);
            string exe = Path.Combine(folder, ExeName);

            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };
            Debug.Log($"[Build] Armando el juego para Windows con {scenes.Count} escenas en {folder} ...");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                // Esta carpeta es solo para depurar; no hace falta compartirla.
                string debugInfo = Path.Combine(folder, "HealingKnight_BurstDebugInformation_DoNotShip");
                if (Directory.Exists(debugInfo)) Directory.Delete(debugInfo, true);

                double mb = summary.totalSize / (1024.0 * 1024.0);
                Debug.Log($"[Build] ¡Listo! {mb:0} MB en {summary.totalTime.TotalSeconds:0} s: {exe}");
                EditorUtility.RevealInFinder(exe);
                EditorUtility.DisplayDialog("Healing Knight",
                    $"¡Juego listo! ({mb:0} MB)\n\nEstá en:\n{folder}\n\nPara compartirlo, comprime la carpeta \"Windows\" completa " +
                    "en un .zip (clic derecho → Enviar a → Carpeta comprimida). Quien lo reciba la descomprime y abre HealingKnight.exe.", "OK");
            }
            else
            {
                Debug.LogError($"[Build] No se pudo armar el juego: {summary.result}, {summary.totalErrors} error(es). Revisa la consola.");
                EditorUtility.DisplayDialog("Healing Knight",
                    $"No se pudo armar el juego ({summary.totalErrors} error(es)). Mira los mensajes rojos de la consola y mándamelos.", "OK");
            }
        }
    }
}
