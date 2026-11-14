using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.CameraSystem;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// PERSECUCIÓN de la lombriz (Ascaris) en el íleon: la lombriz va escarbando bajo el piso hacia la válvula ileocecal
    /// (la puerta al intestino grueso). Sale del piso en un brinco (como delfín): ahí es cuando le puedes pegar.
    /// Antes de salir se ve el montículo temblando: si estás encima, te pega al salir.
    /// La pantalla se CIERRA alrededor del punto donde está (no puedes adelantarte). Sale una y otra vez en ese punto
    /// hasta que le bajas un pedazo de vida; entonces huye al siguiente punto y la pantalla avanza con ella.
    /// En el último (la válvula) se escapa. No se muere aquí: la pelea final es en el intestino grueso.
    /// Si mueres, lo que le bajaste se queda: retomas desde el punto en que ibas.
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
        [SerializeField] private float maxTilt = 90f;
        [Tooltip("Distancia de la cara al primer anillo (el dibujo de la cabeza ya trae un pedazo de cuerpo).")]
        [SerializeField] private float bodyStart = 0.75f;
        [SerializeField] private float ringGap = 0.55f;

        [Header("Pantalla cerrada")]
        [SerializeField] private float lockBehind = 15f;
        [SerializeField] private float lockAhead = 5f;
        [SerializeField, TextArea] private string fleeMessage = "¡Se mete más adelante! Síguela.";

        private enum State { Waiting, Warn, Emerge, Buried, Escaped }
        private State state = State.Waiting;
        private EnemyHealth health;
        private Hurtbox hurtbox;
        private Transform[] segs;
        private SpriteRenderer[] segRenderers;
        private readonly List<Vector2> trail = new List<Vector2>();
        private float floorY, timer, centerX, contactCooldown;
        private bool lastEmergence, started;
        private int pointIndex;
        private float hurtTimer;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            hurtbox = GetComponent<Hurtbox>();
        }

        private void OnEnable()
        {
            health.Hit += OnHit;
            GameplayEvents.PlayerDefeated += OnPlayerDefeated;
        }

        private void OnDestroy() => LevelManager.Unblock(this);

        private void OnDisable()
        {
            health.Hit -= OnHit;
            GameplayEvents.PlayerDefeated -= OnPlayerDefeated;
        }

        /// <summary>Moriste: se esconde y suelta la pantalla; al regresar empieza otra vez desde el punto en que iban.</summary>
        private void OnPlayerDefeated()
        {
            if (state == State.Escaped || state == State.Waiting) return;
            state = State.Waiting;
            Hide();
            WalkableArea.Unlock();
        }
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
            // El nivel no se termina hasta que la lombriz huya (aunque llegues a la salida).
            LevelManager.Block(this);
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
                    pointIndex = ProgressIndex();
                    LockScreen();
                    StartWarn();
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
                        if (lastEmergence && ReachedTarget()) Escape();
                        else { state = State.Buried; timer = buriedTime; Hide(); }
                    }
                    break;
                }

                case State.Buried:
                    timer -= dt;
                    if (timer <= 0f && alive) NextEmergence();
                    break;
            }
        }

        private int Points => emergePoints != null ? emergePoints.Length : 0;

        /// <summary>Vida a la que tiene que bajar para huir del punto 'index' (el último es el límite de vida).</summary>
        private float TargetHealth(int index) => 1f - (1f - healthFloor) * (index + 1) / Mathf.Max(1, Points);

        private bool ReachedTarget() => health.Health01 <= TargetHealth(pointIndex) + 0.001f;

        /// <summary>En qué punto va, según cuánta vida le has bajado (por si moriste y regresas).</summary>
        private int ProgressIndex()
        {
            for (int i = 0; i < Points; i++)
                if (health.Health01 > TargetHealth(i) + 0.001f) return i;
            return Mathf.Max(0, Points - 1);
        }

        private void LockScreen()
        {
            if (Points == 0) return;
            float x = emergePoints[pointIndex];
            WalkableArea.Lock(Mathf.Max(startX - 2f, x - lockBehind), x + lockAhead);
        }

        /// <summary>Si ya le bajaste suficiente, huye al siguiente punto; si no, vuelve a salir en el mismo.</summary>
        private void NextEmergence()
        {
            if (Points == 0) { Escape(); return; }
            if (ReachedTarget() && pointIndex < Points - 1)
            {
                pointIndex++;
                LockScreen();
                if (!string.IsNullOrEmpty(fleeMessage)) GameplayEvents.RaiseMessage(fleeMessage);
                Sfx.Effect("Lombriz_Ruge", 0.8f);
            }
            StartWarn();
        }

        private void StartWarn()
        {
            if (Points == 0) { Escape(); return; }
            centerX = emergePoints[pointIndex];
            lastEmergence = pointIndex == Points - 1;
            state = State.Warn;
            timer = warnTime;
            Sfx.EffectAt("Lombriz_Retumbo", new Vector2(centerX, floorY), 0.7f);
        }

        private void LayoutBody()
        {
            if (segs == null) return;
            float gap = ringGap;
            int t = 0;
            float travelled = 0f;
            for (int i = 0; i < segs.Length; i++)
            {
                float want = bodyStart + gap * i;
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
            WalkableArea.Unlock();
            LevelManager.Unblock(this);
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
