using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// AMÍGDALAS: cuando muere un enemigo de la lista, suelta un ANTÍGENO (una "muestra" del germen).
    /// El jugador lo carga (va lento y no dispara) y lo entrega en el CENTRO GERMINAL, donde salen linfocitos B que
    /// disparan anticuerpos. Si te pegan mientras lo cargas, se te cae.
    /// Biología: las amígdalas atrapan gérmenes y presentan sus antígenos para que se fabriquen anticuerpos.
    /// </summary>
    public class AntigenSystem : MonoBehaviour
    {
        [SerializeField] private EnemyData[] sources;
        [SerializeField] private int maxOnGround = 3;
        [SerializeField, TextArea] private string firstDrop = "¡Un ANTÍGENO! Es una muestra del germen. Llévalo al CENTRO GERMINAL para fabricar anticuerpos (cargándolo no puedes disparar).";

        private readonly List<Antigen> active = new List<Antigen>();
        private bool told;

        public static AntigenSystem Current { get; private set; }
        public Antigen Carried { get; private set; }

        public void Configure(EnemyData[] enemies) => sources = enemies;

        private void OnEnable()
        {
            Current = this;
            EnemyHealth.AnyDied += OnEnemyDied;
            PlayerController.Spawned += Hook;
            if (PlayerController.Current != null) Hook(PlayerController.Current);
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
            EnemyHealth.AnyDied -= OnEnemyDied;
            PlayerController.Spawned -= Hook;
            if (hooked != null) hooked.Health.Damaged -= OnPlayerHurt;
        }

        private PlayerController hooked;
        private void Hook(PlayerController p)
        {
            if (hooked != null) hooked.Health.Damaged -= OnPlayerHurt;
            hooked = p;
            if (p != null) p.Health.Damaged += OnPlayerHurt;
        }

        private void OnEnemyDied(EnemyHealth e)
        {
            if (e == null || e.Data == null || sources == null || System.Array.IndexOf(sources, e.Data) < 0) return;
            active.RemoveAll(x => x == null);
            if (active.Count >= maxOnGround) return;
            var go = new GameObject("Antigeno");
            go.transform.position = e.transform.position + Vector3.up * 0.6f;
            Antigen a = go.AddComponent<Antigen>();
            active.Add(a);
            if (!told && !string.IsNullOrEmpty(firstDrop)) { told = true; GameplayEvents.RaiseMessage(firstDrop); }
        }

        public bool TryCarry(Antigen a)
        {
            if (Carried != null) return false;
            Carried = a;
            if (PlayerController.Current != null) PlayerController.Current.Carrying = true;
            Sfx.Effect("Bomba_Ganada", 0.4f);
            return true;
        }

        /// <summary>Lo entrega el centro germinal: desaparece el antígeno.</summary>
        public Antigen Deliver()
        {
            Antigen a = Carried;
            Carried = null;
            if (PlayerController.Current != null) PlayerController.Current.Carrying = false;
            active.Remove(a);
            if (a != null) Destroy(a.gameObject);
            return a;
        }

        private void OnPlayerHurt()
        {
            if (Carried == null) return;
            Antigen a = Carried;
            Carried = null;
            if (PlayerController.Current != null) PlayerController.Current.Carrying = false;
            a.Drop();
            GameplayEvents.RaiseMessage("¡Se te cayó el antígeno! Recógelo otra vez.");
        }
    }

    /// <summary>El antígeno en el piso o cargado sobre la cabeza del jugador.</summary>
    public class Antigen : MonoBehaviour
    {
        private Transform visual;
        private float vy, cooldown, life = 25f;
        private bool grounded;

        private void Start()
        {
            visual = new GameObject("Dibujo").transform;
            visual.SetParent(transform, false);
            Sprite c = CodeSprites.Circle, sq = CodeSprites.Square;
            CodeSprites.Add(visual, c, new Color(1f, 0.9f, 0.4f, 0.35f), 25, Vector2.zero, new Vector2(1.2f, 1.2f));
            CodeSprites.Add(visual, c, new Color(0.3f, 0.08f, 0.15f), 26, Vector2.zero, new Vector2(0.66f, 0.66f));
            CodeSprites.Add(visual, c, new Color(1f, 0.55f, 0.35f), 27, Vector2.zero, new Vector2(0.54f, 0.54f));
            // "Picos" del antígeno (lo que reconocen los anticuerpos).
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f;
                SpriteRenderer spike = CodeSprites.Add(visual, sq, new Color(1f, 0.85f, 0.3f), 26, Vector2.zero, new Vector2(0.12f, 0.32f));
                spike.transform.localRotation = Quaternion.Euler(0f, 0f, a);
                spike.transform.localPosition = Quaternion.Euler(0f, 0f, a) * new Vector3(0f, 0.36f, 0f);
            }
        }

        public void Drop()
        {
            grounded = false;
            vy = 3f;
            cooldown = 0.8f;
            life = 20f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            AntigenSystem sys = AntigenSystem.Current;
            PlayerController p = PlayerController.Current;
            if (visual != null) visual.Rotate(0f, 0f, 60f * dt);
            if (sys != null && sys.Carried == this && p != null)
            {
                transform.position = Vector3.Lerp(transform.position, p.Position + Vector2.up * 2.6f, 1f - Mathf.Exp(-14f * dt));
                return;
            }

            if (!grounded)
            {
                vy -= 14f * dt;
                Vector3 pos = transform.position + Vector3.up * vy * dt;
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, Mathf.Max(0.05f, -vy * dt) + 0.35f, 1 << Movement.PhysicsLayers.Ground);
                if (vy < 0f && hit.collider != null) { pos.y = hit.point.y + 0.35f; grounded = true; }
                transform.position = pos;
            }
            life -= dt;
            if (life <= 0f) { Destroy(gameObject); return; }
            if (cooldown > 0f) { cooldown -= dt; return; }
            if (p != null && p.Health.IsAlive && sys != null && Vector2.Distance(p.Position + Vector2.up * 0.9f, transform.position) < 1f)
                sys.TryCarry(this);
        }
    }
}
