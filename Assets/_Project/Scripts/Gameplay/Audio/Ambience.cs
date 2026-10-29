using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealingKnight.Gameplay.Audio
{
    /// <summary>
    /// Sonido de fondo de cada sistema. Se elige solo por el nombre de la escena:
    /// "Digestivo_Estomago" → Ambiente_Digestivo, "Nervioso_Cerebro" → Ambiente_Nervioso, etc.
    /// Para un nivel con fondo propio, crea un archivo con el nombre completo de la escena:
    /// "Ambiente_Nervioso_Hipocampo" gana sobre "Ambiente_Nervioso".
    /// Si no hay archivo (menús), se apaga con un fundido.
    /// </summary>
    public static class Ambience
    {
        private static AudioSource source;
        private static AudioClip target;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            source = null;
            target = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;
            string name = scene.name ?? "";
            int cut = name.IndexOf('_');
            string system = cut > 0 ? name.Substring(0, cut) : name;
            target = Sfx.LoadFirst("Ambiente_" + name, "Ambiente_" + system);
            EnsureSource();
            if (target != null && (source.clip != target || !source.isPlaying))
            {
                source.clip = target;
                source.volume = 0f;
                source.Play();
            }
        }

        private static void EnsureSource()
        {
            if (source != null) return;
            var go = new GameObject("Ambiente");
            Object.DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            go.AddComponent<AmbienceFader>();
        }

        /// <summary>Lo llama AmbienceFader cada cuadro: sube o baja el volumen suavemente y apaga cuando no hay ambiente.</summary>
        internal static void Tick()
        {
            if (source == null) return;
            float goal = target != null && source.clip == target ? CombatAudio.Ambience : 0f;
            source.volume = Mathf.MoveTowards(source.volume, goal, Time.unscaledDeltaTime * 0.5f);
            if (goal == 0f && source.volume <= 0f && source.isPlaying) source.Stop();
        }
    }
}
