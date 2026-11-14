using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.CameraSystem;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// PERSECUCIÓN de la lombriz (Ascaris) en el íleon: la lombriz va escarbando bajo el piso hacia la válvula ileocecal
    /// (la puerta al intestino grueso). Sale adelante de ti en un brinco (como delfín): ahí es cuando le puedes pegar.
    /// Antes de salir se ve el montículo temblando: si estás encima, te pega al salir.
    /// No se muere aquí (le queda vida para la pelea final en el intestino grueso): al llegar a la válvula se escapa.
    /// Si mueres, vuelve a salir adelante de donde reaparezcas.
    /// Este objeto es la CABEZA (tiene la vida y el Hurtbox); el cuerpo y el montículo se sueltan al empezar.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class WormChase : MonoBehaviour
    {
        [SerializeField] private string bossName = "Lombriz";
        [SerializeField] private float startX = 160f;
        [Tooltip("Lugares (X) donde puede salir; el último es la válvula ileocecal, por donde se escapa.")]
        [SerializeField] private float[] emergePoints = { 164f, 169f, 174f, 179f };
        [SerializeField] private float emergeHeight = 3.4f;
        [SerializeField] private float emergeLength = 3.6f;
        [SerializeField] private float emergeTime = 1.9f;
        [SerializeField] private float warnTime = 0.85f;
        [SerializeField] private float buriedTime = 0.7f;
        [SerializeField] private float contactDamage = 14f;
        [Tooltip("No baja de esta vida aquí (0.4 = 40 %): la pelea final es en el intestino grueso.")]
        [SerializeField, Range(0f, 1f)] private float healthFloor = 0.45f;
        [SerializeField, TextArea] private string startMessage = "¡La lombriz huye hacia el intestino grueso! Pégale cada vez que salga del piso.";
        [SerializeField, TextArea] private string escapeMessage = "Se escapó por la válvula ileocecal... Te espera en el intestino grueso.";

        [SerializeField] private Transform bodyRoot;          // los pedazos del cuerpo (se sueltan al empezar)
        [SerializeField] private SpriteRenderer mound;         // montículo de tierra/tejido que tiembla
        [SerializeField] private SpriteRenderer[] headRenderers;

        [Header("Dibujo de la cabeza (opcional)")]
        [SerializeField] private SpriteRenderer headArt;
        [SerializeField] private Sprite headOut;
        [SerializeField] private Sprite headRoar;
        [SerializeField] private Sprite headHurt;
        [Tooltip("Cuánto se inclina la cabeza siguiendo el arco (grados máximos). El dibujo trae el cuerpo doblado.")]
        [SerializeField] private float maxTilt = 35f;

        private enum State { Waiting, Warn, Emerge, Buried, Escaped }
        private State state = State.Waiting;
        private EnemyHealth health;
        private Hurtbox hurtbox;
        private Transform[] segs;
        private SpriteRenderer[] segRenderers;
        private readonly List<Vector2> trail = new List<Vector2>();
        private float floorY, timer, centerX, contactCooldown;
        private bool lastEmergence, started;
        private float hurtTimer;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            hurtbox = GetComponent<Hurtbox>();
        }

        private void OnEnable() => health.Hit += OnHit;
        private void OnDisable() => health.Hit -= OnHit;
        private void OnHit(DamageInfo info, HitResult result) => hurtTimer = 0.3f;

        public void Configure(float fromX, float[] points)
        {
            startX = fromX;
            emergePoints = points;
        }

        private void Start()
        {
            floorY = transform.position.y;
            health.HealthFloor01 = healthFloor;
            if (bodyRoot != null)
            {
                bodyRoot.SetParent(transform.parent, true);
                segs = new Transform[bodyRoot.childCount];
                segRenderers = new SpriteRenderer[segs.Length];
                for (int i = 0; i < segs.Length; i++)
                {
                    segs[i] = bodyRoot.GetChild(i);
                    segRenderers[i] = segs[i].GetComponentInChildren<SpriteRenderer>();
                }
            }
            if (mound != null) mound.transform.SetParent(transform.parent, true);
            Hide();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (contactCooldown > 0f) contactCooldown -= dt;
            PlayerController player = PlayerController.Current;
            bool alive = player != null && player.Health != null && player.Health.IsAlive;

            switch (state)
            {
                case State.Waiting:
                    if (!alive || player.Position.x < startX) return;
                    if (!started)
                    {
                        started = true;
                        GameplayEvents.RaiseBossStarted(bossName, health);
                        GameplayEvents.RaiseMessage(startMessage);
                        Sfx.Effect("Lombriz_Ruge", 1f);
                    }
                    NextEmergence(player);
                    break;

                case State.Warn:
                    timer -= dt;
                    if (mound != null)
                    {
                        mound.enabled = true;
                        mound.transform.position = new Vector3(centerX + Mathf.Sin(Time.time * 45f) * 0.08f, floorY, 0f);
                    }
                    if (timer <= 0f)
                    {
                        state = State.Emerge;
                        timer = 0f;
                        trail.Clear();
                        Sfx.EffectAt("Lombriz_Sale", new Vector2(centerX, floorY), 0.9f);
                        Shake(0.15f, 0.3f);
                    }
                    break;

                case State.Emerge:
                {
                    timer += dt / emergeTime;
                    float u = timer;   // pasa de 1: la cabeza sigue bajando y el cuerpo la sigue bajo el piso
                    // Arco de delfín: sale por la izquierda del montículo y se vuelve a meter por la derecha.
                    float x = centerX - emergeLength * 0.5f + emergeLength * u;
                    float y = Mathf.Max(floorY - 3f, floorY - 0.8f + 4f * (emergeHeight + 0.8f) * u * (1f - u));
                    Vector2 before = transform.position;
                    transform.position = new Vector2(x, y);
                    Vector2 dir = (Vector2)transform.position - before;
                    if (dir.sqrMagnitude > 1e-6f)
                    {
                        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                        if (headArt != null) angle = Mathf.Clamp(angle, -maxTilt, maxTilt);
                        transform.rotation = Quaternion.Euler(0f, 0f, angle);
                    }
                    if (headArt != null)
                    {
                        hurtTimer -= dt;
                        Sprite face = hurtTimer > 0f && headHurt != null ? headHurt : (u < 0.45f && headRoar != null ? headRoar : headOut);
                        if (face != null && headArt.sprite != face) headArt.sprite = face;
                    }
                    if (mound != null && u > 0.15f) mound.enabled = false;
                    trail.Insert(0, transform.position);
                    if (trail.Count > 240) trail.RemoveAt(trail.Count - 1);
                    LayoutBody();
                    bool exposed = transform.position.y > floorY - 0.1f;
                    SetHeadVisible(exposed);
                    if (alive && exposed) TryContact(player);
                    if (u >= 1f && BodyUnderground())
                    {
                        Sfx.EffectAt("Lombriz_Entra", transform.position, 0.8f);
                        if (lastEmergence) Escape();
                        else { state = State.Buried; timer = buriedTime; Hide(); }
                    }
                    break;
                }

                case State.Buried:
                    timer -= dt;
                    if (timer <= 0f && alive) NextEmergence(player);
                    break;
            }
        }

        /// <summary>Sale en el siguiente punto que esté adelante del jugador (así nunca sale detrás).</summary>
        private void NextEmergence(PlayerController player)
        {
            if (emergePoints == null || emergePoints.Length == 0) { Escape(); return; }
            int index = emergePoints.Length - 1;
            for (int i = 0; i < emergePoints.Length; i++)
                if (emergePoints[i] > player.Position.x + 3.5f) { index = i; break; }
            centerX = emergePoints[index];
            lastEmergence = index == emergePoints.Length - 1;
            state = State.Warn;
            timer = warnTime;
            Sfx.EffectAt("Lombriz_Retumbo", new Vector2(centerX, floorY), 0.7f);
        }

        private void LayoutBody()
        {
            if (segs == null) return;
            const float gap = 0.5f;
            int t = 0;
            float travelled = 0f;
            for (int i = 0; i < segs.Length; i++)
            {
                float want = gap * (i + 1);
                // Camina por la estela de la cabeza hasta la distancia de este pedazo.
                while (t + 1 < trail.Count && travelled + Vector2.Distance(trail[t], trail[t + 1]) < want)
                {
                    travelled += Vector2.Distance(trail[t], trail[t + 1]);
                    t++;
                }
                Vector2 p = trail.Count > 0 ? trail[Mathf.Min(t, trail.Count - 1)] : (Vector2)transform.position;
                if (t + 1 >= trail.Count) p = new Vector2(centerX - emergeLength * 0.5f, floorY - 1f);   // aún no sale este pedazo
                Vector2 ahead = i == 0 ? (Vector2)transform.position : (Vector2)segs[i - 1].position;
                Vector2 d = ahead - p;
                segs[i].position = p;
                if (d.sqrMagnitude > 1e-5f) segs[i].rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                if (segRenderers[i] != null) segRenderers[i].enabled = p.y > floorY - 0.15f;
            }
        }

        private bool BodyUnderground()
        {
            if (segs == null || segs.Length == 0) return true;
            return (trail.Count > 30 && segs[segs.Length - 1].position.y < floorY - 0.15f) || timer > 3f;
        }

        private void TryContact(PlayerController player)
        {
            if (contactCooldown > 0f) return;
            Hurtbox box = player.GetComponent<Hurtbox>();
            if (box == null || !box.IsAlive || !box.OverlapsCircle(transform.position, 0.65f)) return;
            DamageInfo info = DamageInfo.Create(contactDamage, DamageKind.Contact, Team.Enemy, transform.position);
            info.knockback = 8f;
            box.Damageable.ApplyDamage(info);
            contactCooldown = 1f;
        }

        private void Escape()
        {
            state = State.Escaped;
            Hide();
            GameplayEvents.RaiseBossEnded();
            if (!string.IsNullOrEmpty(escapeMessage)) GameplayEvents.RaiseMessage(escapeMessage);
            Shake(0.2f, 0.5f);
        }

        private void Hide()
        {
            SetHeadVisible(false);
            if (segRenderers != null) foreach (SpriteRenderer r in segRenderers) if (r != null) r.enabled = false;
            if (mound != null) mound.enabled = false;
            transform.position = new Vector3(transform.position.x, floorY - 2f, 0f);
        }

        private void SetHeadVisible(bool visible)
        {
            if (hurtbox != null && hurtbox.enabled != visible) hurtbox.enabled = visible;
            health.Invulnerable = !visible;
            if (headRenderers == null) return;
            foreach (SpriteRenderer r in headRenderers) if (r != null) r.enabled = visible;
        }

        private static void Shake(float strength, float duration)
        {
            Camera cam = Camera.main;
            if (cam != null && cam.TryGetComponent(out GameCamera gameCamera)) gameCamera.Shake(strength, duration);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.5f, 0.8f);
            if (emergePoints == null) return;
            foreach (float x in emergePoints) Gizmos.DrawWireSphere(new Vector3(x, transform.position.y, 0f), 0.5f);
        }
    }
}
