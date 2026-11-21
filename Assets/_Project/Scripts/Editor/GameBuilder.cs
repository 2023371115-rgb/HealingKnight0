using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
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
    /// "Construir juego (APK para Android)": deja listo lo de Android (horizontal, IL2CPP + ARM64, .apk y no .aab) y
    /// arma Builds/Android/HealingKnight.apk. OJO: el juego aún no tiene controles táctiles (hace falta un control).
    /// </summary>
    public static class GameBuilder
    {
        private const string ExeName = "HealingKnight.exe";

        [MenuItem("Healing Knight/Construir juego (APK para Android)", priority = 4)]
        public static void BuildAndroid()
        {
            if (!ReadyToBuild()) return;
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                EditorUtility.DisplayDialog("Healing Knight",
                    "A tu Unity le falta el módulo de Android.\n\nAbre Unity Hub → Installs → tu versión → engrane → Add modules → " +
                    "marca \"Android Build Support\" (con OpenJDK y Android SDK & NDK Tools) → Install. Luego vuelve a intentarlo.", "OK");
                return;
            }
            List<string> scenes = BuildScenes();
            if (scenes == null) return;
            if (!EditorUtility.DisplayDialog("Healing Knight",
                    "Ojo: el juego todavía NO tiene controles táctiles; en el teléfono hace falta un control (Bluetooth) para jugar.\n\n" +
                    "La primera vez Unity cambia el proyecto a Android y puede tardar bastante. ¿Seguimos?", "Sí, armar la APK", "Cancelar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // Lo de Android: nombre del paquete, horizontal, IL2CPP + ARM64 y un .apk (no el .aab de Google Play).
            NameGame();
            string id = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            if (string.IsNullOrEmpty(id) || id.StartsWith("com.DefaultCompany") || id.StartsWith("com.Company"))
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.healingknight.juego");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;

            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Android"));
            Directory.CreateDirectory(folder);
            string apk = Path.Combine(folder, "HealingKnight.apk");
            Build(scenes, apk, BuildTarget.Android, BuildTargetGroup.Android,
                "Para instalarla, pasa HealingKnight.apk al teléfono (cable, WhatsApp, Drive) y ábrela; el teléfono pide permitir " +
                "\"instalar apps de fuentes desconocidas\".\n\nPara volver a armar el .exe, el botón de Windows regresa solo la plataforma.");
        }

        [MenuItem("Healing Knight/Construir juego (.exe para Windows)", priority = 3)]
        public static void BuildWindows()
        {
            if (!ReadyToBuild()) return;
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            {
                EditorUtility.DisplayDialog("Healing Knight",
                    "A tu Unity le falta el módulo para Windows.\n\nAbre Unity Hub → Installs → tu versión → engrane → Add modules → " +
                    "marca \"Windows Build Support (Mono)\" → Install. Luego vuelve a intentarlo.", "OK");
                return;
            }

            List<string> scenes = BuildScenes();
            if (scenes == null) return;
            // Guarda lo que tengas abierto (pregunta) para no perder cambios.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            NameGame();

            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Windows"));
            Directory.CreateDirectory(folder);
            string exe = Path.Combine(folder, ExeName);
            Build(scenes, exe, BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone,
                "Para compartirlo, comprime la carpeta \"Windows\" completa en un .zip (clic derecho → Enviar a → Carpeta " +
                "comprimida). Quien lo reciba la descomprime y abre HealingKnight.exe.");
        }

        private static bool ReadyToBuild()
        {
            if (!EditorApplication.isPlaying) return true;
            EditorUtility.DisplayDialog("Healing Knight", "Primero sal del modo Play (botón ▶ de arriba) y vuelve a intentarlo.", "OK");
            return false;
        }

        /// <summary>Las escenas de la lista del build (las pone "Regenerar proyecto base"); null si no hay.</summary>
        private static List<string> BuildScenes()
        {
            var scenes = new List<string>();
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
                if (s.enabled && File.Exists(s.path)) scenes.Add(s.path);
            if (scenes.Count > 0) return scenes;
            EditorUtility.DisplayDialog("Healing Knight", "No hay escenas en la lista del build. Corre primero \"Healing Knight → Regenerar proyecto base\".", "OK");
            return null;
        }

        private static void NameGame()
        {
            if (PlayerSettings.productName == "New Unity Project" || string.IsNullOrEmpty(PlayerSettings.productName))
                PlayerSettings.productName = "Healing Knight";
        }

        private static void Build(List<string> scenes, string output, BuildTarget target, BuildTargetGroup group, string howToShare)
        {
            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = output,
                target = target,
                targetGroup = group,
                options = BuildOptions.None
            };
            string folder = Path.GetDirectoryName(output);
            Debug.Log($"[Build] Armando el juego ({target}) con {scenes.Count} escenas en {folder} ...");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                // Carpetas solo para depurar: no hace falta compartirlas.
                foreach (string dir in Directory.GetDirectories(folder, "*DoNotShip"))
                    Directory.Delete(dir, true);
                double mb = summary.totalSize / (1024.0 * 1024.0);
                Debug.Log($"[Build] ¡Listo! {mb:0} MB en {summary.totalTime.TotalSeconds:0} s: {output}");
                EditorUtility.RevealInFinder(output);
                EditorUtility.DisplayDialog("Healing Knight", $"¡Juego listo! ({mb:0} MB)\n\nEstá en:\n{folder}\n\n{howToShare}", "OK");
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
