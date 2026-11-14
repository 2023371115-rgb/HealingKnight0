using HealingKnight.Gameplay.Audio;
using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// Jefe lombriz en vista lateral (estilo jefe de Metal Slug).
    /// Ciclo: bajo tierra → aviso (montículo que tiembla) → ataque → expuesta → bajo tierra.
    /// Ataques:
    ///  - Embestida: sale de un lado y cruza la arena a ras de piso. Se esquiva SALTANDO (o con la burbuja).
    ///  - Arco: salta del suelo en un arco alto sobre ti y vuelve a entrar. Se esquiva corriendo.
    /// Solo recibe daño mientras la cabeza está fuera. Fases por vida: más rápida y, desde la fase 2, invoca parásitos.
    /// Huida: si 'fleeAtHealth01' > 0, al llegar a ese % de vida se escapa (Intestino delgado) en lugar de morir.
    /// La raíz del objeto ES la cabeza; el cuerpo son segmentos que siguen su rastro.
    /// El suelo es la Y donde se coloca al empezar.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class LombrizBoss : MonoBehaviour
    {
        private enum State { Intro, Underground, Telegraph, Moving, Exposed, Fleeing, Dead }
        private enum Attack { Lunge, Arc }

        [Header("Identidad")]
        [SerializeField] private string bossName = "LOMBRIZ";
        [SerializeField, TextArea] private string introMessage = "¡Un foco de infección! Elimínalo con Antiparasitario.";
        [SerializeField, TextArea] private string defeatMessage = "Foco eliminado.";

        [Header("Huida (0 = pelea hasta morir)")]
        [Range(0f, 0.95f)] [SerializeField] private float fleeAtHealth01 = 0f;
        [SerializeField, TextArea] private string fleeMessage = "NODO: ¡Escapa! Síguela.";

        [Header("Cuerpo")]
        [Tooltip("Sprites de la cabeza (se voltean y giran con el movimiento).")]
        [SerializeField] private Transform visual;
        [SerializeField] private Transform segmentsRoot;
        [SerializeField] private Sprite segmentSprite;
        [SerializeField] private Color segmentColor = new Color(0.95f, 0.6f, 0.6f);
        [SerializeField] private int segmentCount = 12;
        [SerializeField] private float segmentSpacing = 0.5f;
        [SerializeField] private float headSize = 1.4f;
        [SerializeField] private float tailSize = 0.45f;
        [SerializeField] private SpriteRenderer moundRenderer;

        [Header("Ritmo")]
        [SerializeField] private float undergroundTime = 1.4f;
        [SerializeField] private float telegraphTime = 1.0f;
        [SerializeField] private float exposedTime = 1.6f;

        [Header("Embestida (se salta)")]
        [SerializeField] private float lungeSpeed = 9f;
        [Tooltip("Altura del centro de la cabeza sobre el piso durante la embestida.")]
        [SerializeField] private float lungeHeight = 0.5f;
        [Tooltip("En la embestida solo dañan la cabeza y estos primeros segmentos (la cola va pegada al piso).")]
        [SerializeField] private int lungeHarmfulSegments = 4;

        [Header("Arco (se esquiva corriendo)")]
        [SerializeField] private float arcWidth = 7f;
        [SerializeField] private float arcHeight = 4f;
        [SerializeField] private float arcDuration = 1.3f;

        [Header("Daño")]
        [SerializeField] private float contactDamage = 20f;
        [SerializeField] private float contactKnockback = 7f;

        [Header("Fases (por % de vida)")]
        [SerializeField] private float phase2At = 0.6f;
        [SerializeField] private float phase3At = 0.3f;

        [Header("Invocar (fase 2 en adelante)")]
        [SerializeField] private EnemyHealth minionPrefab;
        [SerializeField] private EnemyData minionData;
        [SerializeField] private int minionsPerSummon = 2;
        [SerializeField] private int maxMinionsAlive = 3;

        private const float BelowGround = -1.4f;

        private EnemyHealth health;
        private Hurtbox hurtbox;

        private State state = State.Intro;
        private Attack attack;
        private float timer;
        private float floorY;
        private float height;            // altura de la cabeza respecto al piso
        private float startX, endX;
        private int direction = 1;
        private float progress;          // 0..1 del recorrido; sigue un poco más para enterrar la cola
        private float pathDuration;
        private bool hitThisAttack;
        private int chainedLunges;
        private Vector2 lastHeadPosition;

        private readonly List<Vector2> trail = new List<Vector2>();
        private SpriteRenderer[] headRenderers;
        private Transform[] segments;
        private SpriteRenderer[] segmentRenderers;
        private readonly List<EnemyHealth> minions = new List<EnemyHealth>();
        private Vector3 visualBaseScale = Vector3.one;

        private int Phase
        {
            get
            {
                float h = health.Health01;
                if (h <= phase3At) return 3;
                if (h <= phase2At) return 2;
                return 1;
            }
        }

        private float SpeedMultiplier => Phase == 3 ? 1.5f : Phase == 2 ? 1.25f : 1f;
        private Vector2 Head => transform.position;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            hurtbox = GetComponent<Hurtbox>();
            if (visual != null)
            {
                headRenderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
                visualBaseScale = new Vector3(Mathf.Abs(visual.localScale.x), visual.localScale.y, visual.localScale.z);
            }
            else headRenderers = new SpriteRenderer[0];
            BuildSegments();
        }

        private void OnEnable() => health.Died += OnDied;
        private void OnDisable() => health.Died -= OnDied;

        private void OnDestroy()
        {
            if (state != State.Dead) GameplayEvents.RaiseBossEnded();
            foreach (EnemyHealth m in minions)
                if (m != null) Destroy(m.gameObject);
        }

        private void Start()
        {
            floorY = transform.position.y;
            health.HealthFloor01 = fleeAtHealth01;
            SetExposed(false);
            SetHeadPosition(transform.position.x, BelowGround);
            if (moundRenderer != null) moundRenderer.enabled = false;

            GameplayEvents.RaiseBossStarted(bossName, health);
            if (!string.IsNullOrEmpty(introMessage)) GameplayEvents.RaiseMessage(introMessage);

            state = State.Intro;
            timer = 1.2f;
        }

        // ---------------------------------------------------------------- ciclo

        private void Update()
        {
            if (state == State.Dead) return;
            float dt = Time.deltaTime;

            if (fleeAtHealth01 > 0f && state != State.Fleeing && health.Health01 <= fleeAtHealth01 + 0.0001f)
                BeginFlee();

            switch (state)
            {
                case State.Intro:
                case State.Underground:
                    timer -= dt;
                    if (timer <= 0f) BeginTelegraph();
                    break;

                case State.Telegraph:
                    timer -= dt;
                    UpdateMound(true);
                    if (timer <= 0f) BeginMove();
                    break;

                case State.Moving:
                case State.Fleeing:
                    UpdateMove(dt);
                    break;

                case State.Exposed:
                    timer -= dt;
                    // Asomada y "aturdida": se balancea, momento para castigarla.
                    SetHeadPosition(transform.position.x, 0.9f + Mathf.Sin(Time.time * 6f) * 0.12f);
                    RecordTrail();
                    if (timer <= 0f) Burrow();
                    break;
            }

            UpdateSegments();
        }

        private void BeginTelegraph()
        {
            PlayerController player = PlayerController.Current;
            float playerX = player != null ? player.Position.x : (WalkableArea.MinX + WalkableArea.MaxX) * 0.5f;

            float minX = WalkableArea.MinX + 0.6f;
            float maxX = WalkableArea.MaxX - 0.6f;

            if (chainedLunges > 0)
            {
                attack = Attack.Lunge;
                chainedLunges--;
            }
            else
            {
                attack = Random.value < (Phase == 1 ? 0.6f : 0.5f) ? Attack.Lunge : Attack.Arc;
                if (attack == Attack.Lunge && Phase == 3) chainedLunges = 1; // en fase 3 encadena dos embestidas
            }

            if (attack == Attack.Lunge)
            {
                // Sale del lado contrario al jugador y cruza toda la arena.
                bool fromRight = playerX < (minX + maxX) * 0.5f;
                startX = fromRight ? maxX : minX;
                endX = fromRight ? minX : maxX;
                pathDuration = Mathf.Abs(endX - startX) / (lungeSpeed * SpeedMultiplier);
            }
            else
            {
                // Salta en arco pasando justo por encima del jugador.
                int side = Random.value < 0.5f ? 1 : -1;
                startX = Mathf.Clamp(playerX - side * arcWidth * 0.5f, minX, maxX);
                endX = Mathf.Clamp(playerX + side * arcWidth * 0.5f, minX, maxX);
                if (Mathf.Abs(endX - startX) < 2f)
                    endX = Mathf.Clamp(startX + (startX < (minX + maxX) * 0.5f ? arcWidth : -arcWidth), minX, maxX);
                pathDuration = arcDuration / SpeedMultiplier;
            }

            direction = endX >= startX ? 1 : -1;
            SetHeadPosition(startX, BelowGround);
            Face(direction);
            trail.Clear();

            state = State.Telegraph;
            Sfx.EffectAt("Lombriz_Retumbo", new Vector2(startX, transform.position.y));
            timer = telegraphTime / Mathf.Lerp(1f, SpeedMultiplier, 0.5f);
        }

        private void BeginMove()
        {
            UpdateMound(false);
            state = State.Moving;
            Sfx.EffectAt("Lombriz_Sale", new Vector2(startX, transform.position.y));
            progress = 0f;
            hitThisAttack = false;
        }

        private void UpdateMove(float dt)
        {
            bool flat = attack == Attack.Lunge || state == State.Fleeing;

            // Avanza más allá de 1 para que la cola termine de entrar al suelo.
            float speed = Mathf.Max(1f, Mathf.Abs(endX - startX) / Mathf.Max(0.01f, pathDuration));
            float extra = segmentCount * segmentSpacing / speed / Mathf.Max(0.01f, pathDuration);
            progress += dt / Mathf.Max(0.01f, pathDuration);

            float t = progress;
            float x = Mathf.LerpUnclamped(startX, endX, t);
            float peak = flat ? lungeHeight : arcHeight;
            float h;
            if (t > 1f) h = BelowGround - (t - 1f) * 3f;
            else if (flat)
            {
                // Sale rápido, cruza a ras de piso y se vuelve a meter al final.
                float rise = Mathf.Clamp01(Mathf.Min(t, 1f - t) / 0.1f);
                h = Mathf.Lerp(BelowGround, peak, Mathf.Sin(rise * Mathf.PI * 0.5f)) + Mathf.Sin(Time.time * 10f) * 0.06f;
            }
            else h = (peak - BelowGround) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t)) + BelowGround;

            if (state != State.Fleeing) x = Mathf.Clamp(x, WalkableArea.MinX - 3f, WalkableArea.MaxX + 3f);

            SetHeadPosition(x, h);
            RecordTrail();

            SetExposed(h > -0.2f && state == State.Moving);
            if (state == State.Moving && !hitThisAttack) TryHitPlayer(flat);

            if (progress >= 1f + extra)
            {
                if (state == State.Fleeing)
                {
                    FinishFlee();
                    return;
                }

                if (attack == Attack.Lunge && chainedLunges == 0) EmergeExposed();
                else
                {
                    if (attack == Attack.Arc && Phase >= 2) SummonMinions();
                    Burrow();
                }
            }
        }

        private void EmergeExposed()
        {
            state = State.Exposed;
            timer = exposedTime;
            Sfx.Effect("Lombriz_Ruge", 0.8f);
            SetHeadPosition(Mathf.Clamp(endX, WalkableArea.MinX + 0.6f, WalkableArea.MaxX - 0.6f), 0.9f);
            Face(-direction);
            if (visual != null) visual.localRotation = Quaternion.identity;
            trail.Clear();
            SetExposed(true);
        }

        private void Burrow()
        {
            SetExposed(false);
            Sfx.EffectAt("Lombriz_Entra", transform.position, 0.8f);
            SetHeadPosition(transform.position.x, BelowGround);
            state = State.Underground;
            timer = undergroundTime / SpeedMultiplier;
        }

        private void SetHeadPosition(float x, float h)
        {
            height = h;
            Vector2 previous = lastHeadPosition;
            transform.position = new Vector3(x, floorY + h, transform.position.z);
            lastHeadPosition = transform.position;

            // La cabeza mira hacia donde se mueve (sube y baja en el arco).
            if (visual != null && (state == State.Moving || state == State.Fleeing))
            {
                Vector2 delta = lastHeadPosition - previous;
                if (delta.sqrMagnitude > 0.0001f)
                {
                    float angle = Mathf.Atan2(delta.y, Mathf.Abs(delta.x)) * Mathf.Rad2Deg;
                    visual.localRotation = Quaternion.Euler(0f, 0f, angle * direction);
                }
            }
        }

        private void Face(int dir)
        {
            if (visual == null || dir == 0) return;
            visual.localScale = new Vector3(visualBaseScale.x * dir, visualBaseScale.y, visualBaseScale.z);
        }

        // ---------------------------------------------------------------- daño

        private void TryHitPlayer(bool flat)
        {
            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;

            Hurtbox target = player.GetComponent<Hurtbox>();
            if (target == null) return;

            if (height > -0.3f && CheckHit(target, Head, headSize * 0.45f)) return;

            int harmful = flat ? Mathf.Min(lungeHarmfulSegments, segments.Length) : segments.Length;
            for (int i = 0; i < harmful; i++)
            {
                if (!segmentRenderers[i].enabled) continue;
                Vector2 p = segments[i].position;
                if (p.y < floorY - 0.3f) continue;
                if (CheckHit(target, p, segments[i].localScale.x * 0.45f)) return;
            }
        }

        private bool CheckHit(Hurtbox target, Vector2 center, float radius)
        {
            if (!target.OverlapsCircle(center, radius)) return false;

            DamageInfo info = DamageInfo.Create(contactDamage, DamageKind.Contact, Team.Enemy, center);
            info.knockback = contactKnockback;
            target.Damageable.ApplyDamage(info);
            hitThisAttack = true;
            return true;
        }

        private void SetExposed(bool exposed)
        {
            health.Invulnerable = !exposed;
            if (hurtbox != null && hurtbox.enabled != exposed) hurtbox.enabled = exposed;
        }

        // ---------------------------------------------------------------- huida y muerte

        private void BeginFlee()
        {
            state = State.Fleeing;
            Sfx.Effect("Lombriz_Ruge");
            SetExposed(false);
            UpdateMound(false);
            if (!string.IsNullOrEmpty(fleeMessage)) GameplayEvents.RaiseMessage(fleeMessage);

            // Se escapa por la derecha (hacia el siguiente tramo del intestino).
            attack = Attack.Lunge;
            startX = transform.position.x;
            endX = WalkableArea.MaxX + 6f;
            direction = 1;
            Face(1);
            pathDuration = Mathf.Abs(endX - startX) / (lungeSpeed * 1.4f);
            progress = 0.15f;
        }

        private void FinishFlee()
        {
            state = State.Dead;
            foreach (EnemyHealth m in minions)
                if (m != null) Destroy(m.gameObject);
            minions.Clear();
            health.Retreat();
            GameplayEvents.RaiseBossEnded();
            Destroy(gameObject, 0.1f);
        }

        private void OnDied(EnemyHealth _)
        {
            state = State.Dead;
            UpdateMound(false);
            foreach (EnemyHealth m in minions)
                if (m != null) Destroy(m.gameObject);
            minions.Clear();
            GameplayEvents.RaiseBossEnded();
            if (!string.IsNullOrEmpty(defeatMessage)) GameplayEvents.RaiseMessage(defeatMessage);
        }

        // ---------------------------------------------------------------- invocar

        private void SummonMinions()
        {
            if (minionPrefab == null) return;
            minions.RemoveAll(m => m == null || !m.IsAlive);

            int toSpawn = Mathf.Min(minionsPerSummon, maxMinionsAlive - minions.Count);
            for (int i = 0; i < toSpawn; i++)
            {
                float x = Mathf.Clamp(transform.position.x + Random.Range(-1.5f, 1.5f), WalkableArea.MinX + 0.5f, WalkableArea.MaxX - 0.5f);
                EnemyHealth m = Instantiate(minionPrefab, new Vector3(x, floorY + 0.3f, 0f), Quaternion.identity);
                if (minionData != null)
                {
                    m.Configure(minionData, null);
                    EnemyBrain brain = m.GetComponent<EnemyBrain>();
                    if (brain != null) brain.Configure(minionData);
                }
                minions.Add(m);
            }
        }

        // ---------------------------------------------------------------- cuerpo y aviso

        private void BuildSegments()
        {
            if (segmentsRoot == null)
            {
                segmentsRoot = new GameObject("Segments").transform;
                segmentsRoot.SetParent(transform, false);
            }

            segments = new Transform[segmentCount];
            segmentRenderers = new SpriteRenderer[segmentCount];
            for (int i = 0; i < segmentCount; i++)
            {
                var go = new GameObject("Segment_" + i);
                go.transform.SetParent(segmentsRoot, false);
                float k = segmentCount > 1 ? (float)i / (segmentCount - 1) : 0f;
                float size = Mathf.Lerp(headSize * 0.85f, tailSize, k);
                go.transform.localScale = new Vector3(size, size, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = segmentSprite;
                // Anillos alternados para que se lea como lombriz.
                sr.color = i % 2 == 0 ? segmentColor : Color.Lerp(segmentColor, Color.black, 0.12f);
                sr.sortingOrder = -1 - i;
                sr.enabled = false;

                segments[i] = go.transform;
                segmentRenderers[i] = sr;
            }
        }

        private void RecordTrail()
        {
            Vector2 head = Head;
            if (trail.Count == 0 || (head - trail[0]).sqrMagnitude > 0.0004f) trail.Insert(0, head);

            int max = segmentCount * 12 + 20;
            if (trail.Count > max) trail.RemoveRange(max, trail.Count - max);
        }

        private void UpdateSegments()
        {
            if (segments == null) return;

            bool hidden = state == State.Underground || state == State.Telegraph || state == State.Intro;

            // La cabeza solo se ve cuando está fuera del suelo.
            bool headVisible = height > -0.25f && !hidden;
            for (int i = 0; i < headRenderers.Length; i++)
                if (headRenderers[i] != null) headRenderers[i].enabled = headVisible;

            int index = 0;
            float walked = 0f;
            for (int i = 0; i < segments.Length; i++)
            {
                Vector2 point;
                bool found = TrailPointAt((i + 1) * segmentSpacing, ref index, ref walked, out point);

                bool visible = found && point.y > floorY - 0.25f && !hidden;
                segmentRenderers[i].enabled = visible;
                if (!found) continue;

                // Ondulación ligera para que se vea orgánica.
                float wave = Mathf.Sin(Time.time * 8f - i * 0.7f) * 0.06f;
                segments[i].position = new Vector3(point.x, point.y + wave, transform.position.z);
            }
        }

        private bool TrailPointAt(float distance, ref int index, ref float walked, out Vector2 point)
        {
            point = Vector2.zero;
            if (trail.Count < 2) return false;

            while (index < trail.Count - 1)
            {
                Vector2 a = trail[index];
                Vector2 b = trail[index + 1];
                float seg = Vector2.Distance(a, b);
                if (walked + seg >= distance)
                {
                    float k = seg > 0.0001f ? (distance - walked) / seg : 0f;
                    point = Vector2.Lerp(a, b, k);
                    return true;
                }
                walked += seg;
                index++;
            }
            return false;
        }

        private void UpdateMound(bool show)
        {
            if (moundRenderer == null) return;
            moundRenderer.enabled = show;
            if (!show) return;

            // Montículo que tiembla en el piso: el aviso de por dónde va a salir.
            float shake = Mathf.Sin(Time.time * 40f) * 0.05f;
            moundRenderer.transform.position = new Vector3(transform.position.x + shake, floorY, moundRenderer.transform.position.z);
        }
    }
}
