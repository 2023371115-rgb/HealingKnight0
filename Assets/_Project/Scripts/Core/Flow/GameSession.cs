using HealingKnight.Core.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealingKnight.Core.Flow
{
    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
        public const string BodyMap = "BodyMap";
    }

    /// <summary>
    /// Estado global de la partida: catálogo, guardado y nivel actual.
    /// Es estático para sobrevivir a los cambios de escena sin objetos DontDestroyOnLoad.
    /// </summary>
    public static class GameSession
    {
        private static GameCatalog catalog;
        private static SaveData save;
        private static ProgressService progress;

        public static LevelData CurrentLevel { get; private set; }

        public static GameCatalog Catalog
        {
            get
            {
                if (catalog == null)
                {
                    catalog = Resources.Load<GameCatalog>(GameCatalog.ResourcePath);
                    if (catalog == null)
                        Debug.LogError("[GameSession] No se encontró Resources/GameCatalog. Ejecuta 'Healing Knight > Crear proyecto base'.");
                }
                return catalog;
            }
        }

        public static SaveData Save
        {
            get
            {
                if (save == null) save = SaveSystem.Load();
                return save;
            }
        }

        public static ProgressService Progress
        {
            get
            {
                if (progress == null && Catalog != null) progress = new ProgressService(Catalog, Save);
                return progress;
            }
        }

        public static SystemData CurrentSystem => Catalog != null ? Catalog.FindSystemOf(CurrentLevel) : null;

        // Con "Enter Play Mode Options" (sin recarga de dominio) los estáticos sobreviven entre ejecuciones.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            catalog = null;
            save = null;
            progress = null;
            CurrentLevel = null;
        }

        /// <summary>Lo llama cada escena de nivel al cargar, para saber en qué nivel estamos aunque se abra directo desde el Editor.</summary>
        public static void RegisterActiveLevel(string sceneName)
        {
            if (Catalog == null) return;
            LevelData level = Catalog.FindLevelByScene(sceneName);
            if (level != null) CurrentLevel = level;
        }

        public static void StartLevel(LevelData level)
        {
            if (level == null || !level.HasScene) return;
            CurrentLevel = level;
            Time.timeScale = 1f;
            SceneManager.LoadScene(level.sceneName);
        }

        /// <summary>Marca el nivel actual como completado y guarda.</summary>
        public static void CompleteCurrentLevel()
        {
            if (CurrentLevel == null || Progress == null) return;
            Progress.CompleteLevel(CurrentLevel);
            SaveSystem.Write(Save);
        }

        /// <summary>Registra un secreto encontrado y guarda. Devuelve true si es nuevo.</summary>
        public static bool FindSecret(string secretId)
        {
            bool isNew = Save.AddSecret(secretId);
            if (isNew) SaveSystem.Write(Save);
            return isNew;
        }

        public static void MarkPrologueSeen()
        {
            Save.prologueSeen = true;
            SaveSystem.Write(Save);
        }

        public static void ResetProgress()
        {
            SaveSystem.Delete();
            save = new SaveData();
            progress = null;
        }

        public static void GoToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        public static void GoToBodyMap()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.BodyMap);
        }

        public static void ReloadCurrentScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
