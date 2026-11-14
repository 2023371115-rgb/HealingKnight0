using System.Collections.Generic;
using HealingKnight.Core.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealingKnight.Gameplay.Audio
{
    /// <summary>
    /// Reproductor de efectos de sonido sencillo: un grupo de AudioSources que se reparten los sonidos,
    /// con variación de tono al azar y un límite para que el mismo sonido no se apile en el mismo instante.
    /// Uso: Sfx.Play(clip, volumen);
    ///
    /// Si un medicamento no tiene su sonido asignado, se busca solo en Audio/Resources/SFX
    /// por nombre (Disparo_&lt;archivo del medicamento&gt;, Impacto_..., Rebote_Ineficaz), así suena sin regenerar nada.
    /// Si la escena no tiene AudioListener, se pone uno de respaldo (sin oyente Unity no reproduce nada).
    /// </summary>
    public static class Sfx
    {
        private const int Voices = 12;
        private const float SameClipGap = 0.035f;

        private static AudioSource[] sources;
        private static int next;
        private static readonly Dictionary<AudioClip, float> lastPlayed = new Dictionary<AudioClip, float>();
        private static readonly Dictionary<string, AudioClip> byName = new Dictionary<string, AudioClip>();
        private static AudioListener backupListener;

        public const string ResourcesFolder = "SFX";

        /// <summary>Carga un sonido de Audio/Resources/SFX por nombre (sin extensión). Devuelve null si no existe.</summary>
        public static AudioClip Load(string clipName)
        {
            if (string.IsNullOrEmpty(clipName)) return null;
            if (!byName.TryGetValue(clipName, out AudioClip clip))
            {
                clip = Resources.Load<AudioClip>($"{ResourcesFolder}/{clipName}");
                byName[clipName] = clip;
            }
            return clip;
        }

        public static AudioClip FireSound(MedicationData med) =>
            med == null ? null : med.fireSound != null ? med.fireSound : Load("Disparo_" + med.name);

        public static AudioClip HitSound(MedicationData med) =>
            med == null ? null : med.hitSound != null ? med.hitSound : Load("Impacto_" + med.name);

        /// <summary>El primero de la lista que exista en Resources/SFX.</summary>
        public static AudioClip LoadFirst(params string[] names)
        {
            foreach (string n in names)
            {
                AudioClip c = Load(n);
                if (c != null) return c;
            }
            return null;
        }

        /// <summary>
        /// Sonido de un enemigo: "&lt;archivo&gt;_&lt;evento&gt;" (Virus_Rinovirus_Muere), si no "&lt;tipo&gt;_&lt;evento&gt;" (Virus_Muere),
        /// si no "Enemigo_&lt;evento&gt;". Así se puede dar sonido a un enemigo solo con nombrar bien el archivo.
        /// </summary>
        public static AudioClip EnemySound(Object enemyData, string evento)
        {
            string file = enemyData != null ? enemyData.name : "";
            int cut = file.IndexOf('_');
            string type = cut > 0 ? file.Substring(0, cut) : file;
            return file.Length > 0
                ? LoadFirst($"{file}_{evento}", $"{type}_{evento}", $"Enemigo_{evento}")
                : Load($"Enemigo_{evento}");
        }

        /// <summary>Efecto por nombre (archivo en Resources/SFX), con el volumen general de efectos.</summary>
        public static void Effect(string clipName, float volume = 1f) =>
            Play(Load(clipName), volume * CombatAudio.Effects, 0.05f);

        /// <summary>Igual, pero más bajo si pasa lejos de la cámara (y nada si está muy fuera de pantalla).</summary>
        public static void EffectAt(string clipName, Vector2 position, float volume = 1f) =>
            PlayAt(Load(clipName), position, volume * CombatAudio.Effects);

        public static void PlayAt(AudioClip clip, Vector2 position, float volume, float pitchJitter = 0.06f)
        {
            float k = Audibility(position);
            if (k > 0.02f) Play(clip, volume * k, pitchJitter);
        }

        /// <summary>1 dentro de la pantalla; baja hasta 0 a 8 u fuera de ella.</summary>
        public static float Audibility(Vector2 position)
        {
            Camera cam = Camera.main;
            if (cam == null || !cam.orthographic) return 1f;
            Vector2 c = cam.transform.position;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            float dx = Mathf.Max(0f, Mathf.Abs(position.x - c.x) - halfW);
            float dy = Mathf.Max(0f, Mathf.Abs(position.y - c.y) - halfH);
            return Mathf.Clamp01(1f - Mathf.Max(dx, dy) / 8f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            sources = null;
            next = 0;
            lastPlayed.Clear();
            byName.Clear();
            backupListener = null;
        }

        public static void Play(AudioClip clip, float volume = 1f, float pitchJitter = 0.06f)
        {
            if (clip == null) return;
            if (lastPlayed.TryGetValue(clip, out float t) && Time.unscaledTime - t < SameClipGap) return;
            lastPlayed[clip] = Time.unscaledTime;

            AudioSource s = NextSource();
            if (s == null) return;
            s.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            s.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// Siguiente bocina libre. Si alguna se borró (al salir de Play con la recarga de scripts desactivada,
        /// al cambiar de escena, etc.) se vuelven a crear todas, así nunca truena.
        /// </summary>
        private static AudioSource NextSource()
        {
            if (!SourcesAlive()) CreateSources();
            AudioSource s = sources[next];
            next = (next + 1) % sources.Length;
            return s;
        }

        private static bool SourcesAlive()
        {
            if (sources == null) return false;
            foreach (AudioSource s in sources)
                if (s == null) return false;
            return true;
        }

        private static void CreateSources()
        {
            var root = new GameObject("Sfx");
            Object.DontDestroyOnLoad(root);
            sources = new AudioSource[Voices];
            next = 0;
            for (int i = 0; i < Voices; i++)
            {
                var voice = new GameObject($"Voz {i}");
                voice.transform.SetParent(root.transform, false);
                AudioSource s = voice.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f; // 2D
                sources[i] = s;
            }
            backupListener = root.AddComponent<AudioListener>();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureListener();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureListener();

        private static void EnsureListener()
        {
            if (backupListener == null) return;
            // El de respaldo solo se prende si no hay otro (dos oyentes a la vez dan advertencia).
            bool other = false;
            foreach (AudioListener l in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (l != backupListener && l.isActiveAndEnabled) { other = true; break; }
            backupListener.enabled = !other;
        }
    }
}
