using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Audio
{
    /// <summary>
    /// Sonido en bucle pegado a un objeto (charco de ácido, etc.): suena más fuerte mientras más cerca está el jugador.
    /// Se agrega solo desde código: SfxLoop.Attach(gameObject, "Acido_Burbujeo", volumen, distancia).
    /// </summary>
    public class SfxLoop : MonoBehaviour
    {
        private AudioSource source;
        private float volume;
        private float near;
        private float far;

        public static SfxLoop Attach(GameObject owner, string clipName, float volume, float near = 1.5f, float far = 9f)
        {
            AudioClip clip = Sfx.Load(clipName);
            if (clip == null) return null;
            var loop = owner.AddComponent<SfxLoop>();
            loop.source = owner.AddComponent<AudioSource>();
            loop.source.clip = clip;
            loop.source.loop = true;
            loop.source.playOnAwake = false;
            loop.source.spatialBlend = 0f;
            loop.source.volume = 0f;
            loop.source.time = Random.Range(0f, clip.length); // que dos charcos no suenen igualitos
            loop.source.Play();
            loop.volume = volume;
            loop.near = near;
            loop.far = far;
            return loop;
        }

        private void Update()
        {
            if (source == null) return;
            PlayerController player = PlayerController.Current;
            float k = 0f;
            if (player != null)
            {
                float d = Vector2.Distance(player.Position, transform.position);
                k = 1f - Mathf.Clamp01((d - near) / Mathf.Max(0.01f, far - near));
            }
            source.volume = Mathf.MoveTowards(source.volume, volume * CombatAudio.Effects * k * k, Time.deltaTime * 2f);
        }
    }
}
