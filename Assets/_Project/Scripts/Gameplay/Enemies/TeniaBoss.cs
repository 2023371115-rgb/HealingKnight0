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
    /// Minijefe TENIA (solitaria): gusano plano larguísimo hecho de pedacitos (proglótidos).
    /// Biología: se agarra a la pared del intestino con la cabeza (escólex), que tiene ganchos y ventosas;
    /// el cuerpo crece desde el cuello y los pedazos del final, llenos de huevos, se sueltan y salen.
    /// En el juego:
    ///  - La cabeza (este objeto) está colgada arriba, a la derecha de la arena. Es lo que hay que matar.
    ///  - El cuerpo cuelga hacia el piso y se mece. Cada pedazo tiene su propia vida: si lo cortas,
    ///    se suelta y se vuelve un enemigo chiquito que camina (por eso conviene pegarle a la cabeza).
    ///  - Ataques: LATIGAZO (echa el cuerpo atrás y barre el piso: brinca), MORDIDA (la cabeza se lanza hacia ti
    ///    con los ganchos) y cada cierto tiempo SUELTA el último pedazo (huevos).
    ///  - Debajo de la mitad de vida ataca más seguido.
    /// Los pedazos (segments) son hijos de este objeto; los pone PrefabSetup. Se dibuja con figuras hasta tener sprite.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class TeniaBoss : MonoBehaviour
    {
        [SerializeField] private string bossName = "Tenia";
        [SerializeField, TextArea] private string introMessage = "¡TENIA (solitaria)! Su cabeza tiene ganchos. Si le cortas pedazos del cuerpo, se vuelven enemigos: pégale a la CABEZA.";
        [SerializeField, TextArea] private string defeatMessage = "NODO: Tenia eliminada. Se contagia por comer carne de cerdo o res mal cocida.";
        [SerializeField] private Transform head;                 // dibujo de la cabeza (tiembla y se voltea)
        [SerializeField] private EnemyHealth[] segments;
        [SerializeField] private EnemyHealth walkerPrefab;       // proglótido suelto que camina
        [SerializeField] private EnemyData walkerData;
        [SerializeField] private int maxWalkers = 4;

        [Header("Dibujo de la cabeza (opcional)")]
        [SerializeField] private SpriteRenderer headRenderer;
        [SerializeField] private Sprite headIdle;
        [SerializeField] private Sprite headBite;
        [SerializeField] private Sprite headDefeated;
        [Tooltip("Centro de la cabeza respecto al objeto (el objeto está en el cuello, donde empieza el cuerpo).")]
        [SerializeField] private Vector2 headCenterOffset = Vector2.zero;

        [Header("Cuerpo")]
        [SerializeField] private float anchorHeight = 5.2f;
        [SerializeField] private float spacing = 0.72f;
        [SerializeField] private float baseAngle = -105f;        // grados: hacia abajo y un poco a la izquierda

        [Header("Ataques")]
        [SerializeField] private float idleTime = 1.6f;
        [SerializeField] private float contactDamage = 10f;
        [SerializeField] private float whipDamage = 16f;
        [SerializeField] private float biteDamage = 15f;
        [SerializeField] private float shedInterval = 11f;
        [SerializeField] private float phase2At = 0.5f;

        private enum State { Intro, Idle, WindWhip, Whip, Recover, WindBite, Bite, BiteBack, Dead }
        private State state = State.Intro;
        private EnemyHealth health;
        private readonly List<EnemyHealth> body = new List<EnemyHealth>();
        private readonly List<EnemyHealth> walkers = new List<EnemyHealth>();
        private Vector2 anchor, biteTarget;
        private float floorY, timer, whip, shedTimer, contactCooldown, whipCooldown;
        private int attackCount;
        private bool shedWarned;

        private bool Angry => health != null && health.Health01 <= phase2At;

        private void Awake() => health = GetComponent<EnemyHealth>();

        private void OnEnable() => health.Died += OnDied;
        private void OnDisable() => health.Died -= OnDied;

        private void OnDestroy()
        {
            if (state != State.Dead) GameplayEvents.RaiseBossEnded();
        }

        private void Start()
        {
            floorY = transform.position.y;
            float x = Mathf.Min(transform.position.x, WalkableArea.MaxX - 1.6f);
            anchor = new Vector2(x, floorY + anchorHeight);
            transform.position = anchor;
            body.Clear();
            if (segments != null)
                foreach (EnemyHealth s in segments)
                {
                    if (s == null) continue;
                    body.Add(s);
                    EnemyHealth seg = s;
                    seg.Died += _ => OnSegmentCut(seg);
                }
            shedTimer = shedInterval;
            timer = 1.4f;
            GameplayEvents.RaiseBossStarted(bossName, health);
            if (!string.IsNullOrEmpty(introMessage)) GameplayEvents.RaiseMessage(introMessage);
            Sfx.Effect("Lombriz_Ruge", 1f);
            Shake(0.25f, 0.5f);
            LayoutBody(0f);
        }

        private void Update()
        {
            if (state == State.Dead) return;
            float dt = Time.deltaTime;
            float speed = Angry ? 1.35f : 1f;
            timer -= dt * speed;
            if (contactCooldown > 0f) contactCooldown -= dt;
            if (whipCooldown > 0f) whipCooldown -= dt;
            PlayerController player = PlayerController.Current;
            bool alive = player != null && player.Health != null && player.Health.IsAlive;

            switch (state)
            {
                case State.Intro:
                    if (timer <= 0f) { state = State.Idle; timer = idleTime; }
                    break;

                case State.Idle:
                    whip = Mathf.MoveTowards(whip, 0f, dt * 60f);
                    transform.position = Vector2.MoveTowards(transform.position, anchor, dt * 4f);
                    if (timer <= 0f && alive) ChooseAttack(player);
                    break;

                case State.WindWhip:
                    // Echa el cuerpo hacia atrás (a la derecha) y la cabeza tiembla: AVISO.
                    whip = Mathf.MoveTowards(whip, 45f, dt * 90f);
                    if (head != null) head.localPosition = new Vector3(Mathf.Sin(Time.time * 45f) * 0.07f, 0f, 0f);
                    if (timer <= 0f)
                    {
                        state = State.Whip;
                        timer = 0.55f;
                        Sfx.Effect("Lombriz_Retumbo", 0.9f);
                    }
                    break;

                case State.Whip:
                    // Barre hacia la izquierda pegado al piso.
                    whip = Mathf.MoveTowards(whip, -75f, dt * 260f * speed);
                    if (head != null) head.localPosition = Vector3.zero;
                    if (timer <= 0f) { state = State.Recover; timer = 0.9f; Shake(0.15f, 0.25f); }
                    break;

                case State.Recover:
                    whip = Mathf.MoveTowards(whip, 0f, dt * 80f);
                    if (timer <= 0f) { state = State.Idle; timer = idleTime; }
                    break;

                case State.WindBite:
                    if (head != null) head.localPosition = new Vector3(Mathf.Sin(Time.time * 50f) * 0.08f, 0.15f, 0f);
                    if (timer <= 0f)
                    {
                        state = State.Bite;
                        if (head != null) head.localPosition = Vector3.zero;
                        Sfx.Effect("Lombriz_Ruge", 0.7f);
                    }
                    break;

                case State.Bite:
                    transform.position = Vector2.MoveTowards(transform.position, biteTarget, dt * 13f);
                    if (((Vector2)transform.position - biteTarget).sqrMagnitude < 0.01f) { state = State.BiteBack; Shake(0.12f, 0.2f); }
                    break;

                case State.BiteBack:
                    transform.position = Vector2.MoveTowards(transform.position, anchor, dt * 5f);
                    if (((Vector2)transform.position - anchor).sqrMagnitude < 0.01f) { state = State.Idle; timer = idleTime; }
                    break;
            }

            // Suelta el último pedazo (lleno de huevos) cada cierto tiempo.
            shedTimer -= dt * speed;
            if (shedTimer <= 0f && state != State.Intro)
            {
                shedTimer = shedInterval;
                EnemyHealth last = LastSegment();
                if (last != null)
                {
                    if (!shedWarned)
                    {
                        shedWarned = true;
                        GameplayEvents.RaiseMessage("La tenia suelta su último pedazo: va lleno de huevos.");
                    }
                    last.Kill();
                }
            }

            LayoutBody(dt);
            UpdateHeadArt();
            if (alive) TryContact(player);
        }

        private void ChooseAttack(PlayerController player)
        {
            attackCount++;
            float dx = player.Position.x - transform.position.x;
            // Si estás cerca y abajo de la cabeza, muerde; si no, latigazo (alterna para que no sea predecible).
            bool bite = Mathf.Abs(dx) < 6f && attackCount % 3 != 0;
            if (bite)
            {
                state = State.WindBite;
                timer = 0.65f;
                float y = Mathf.Max(floorY + 1.1f, player.Position.y + 0.9f) - headCenterOffset.y;
                y = Mathf.Max(floorY + 0.3f, y);
                biteTarget = new Vector2(Mathf.Clamp(player.Position.x, WalkableArea.MinX + 1f, anchor.x), y);
            }
            else
            {
                state = State.WindWhip;
                timer = 0.85f;
            }
        }

        /// <summary>Acomoda los pedazos: una cadena que cuelga de la cabeza, se mece y se dobla con el latigazo.</summary>
        private void LayoutBody(float dt)
        {
            float t = Time.time;
            Vector2 prev = transform.position;
            int n = body.Count;
            for (int i = 0; i < n; i++)
            {
                EnemyHealth seg = body[i];
                if (seg == null) continue;
                float k = (i + 1f) / Mathf.Max(1, n);
                float a = (baseAngle + Mathf.Sin(t * 1.8f - i * 0.6f) * 10f * k + whip * k) * Mathf.Deg2Rad;
                Vector2 p = prev + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * spacing;
                if (p.y < floorY + 0.35f) p.y = floorY + 0.35f;   // la cola se arrastra por el piso
                Vector2 dir = p - prev;
                seg.transform.position = p;
                seg.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f);
                prev = p;
            }
        }

        private void UpdateHeadArt()
        {
            if (headRenderer == null) return;
            bool biting = state == State.WindBite || state == State.Bite || state == State.WindWhip;
            Sprite s = biting && headBite != null ? headBite : headIdle;
            if (s != null && headRenderer.sprite != s) headRenderer.sprite = s;
        }

        private Vector2 HeadCenter => (Vector2)transform.position + headCenterOffset;

        private EnemyHealth LastSegment()
        {
            for (int i = body.Count - 1; i >= 0; i--)
                if (body[i] != null && body[i].IsAlive) return body[i];
            return null;
        }

        private void TryContact(PlayerController player)
        {
            if (contactCooldown > 0f) return;
            Hurtbox box = player.GetComponent<Hurtbox>();
            if (box == null || !box.IsAlive) return;

            bool biting = state == State.Bite || state == State.BiteBack;
            if (box.OverlapsCircle(HeadCenter, 0.8f))
            {
                Hit(box, biting ? biteDamage : contactDamage, HeadCenter);
                return;
            }
            bool whipping = state == State.Whip;
            foreach (EnemyHealth seg in body)
            {
                if (seg == null || !seg.IsAlive) continue;
                if (!box.OverlapsCircle(seg.transform.position, 0.42f)) continue;
                if (whipping && whipCooldown > 0f) return;
                Hit(box, whipping ? whipDamage : contactDamage, seg.transform.position);
                if (whipping) whipCooldown = 1f;
                return;
            }
        }

        private void Hit(Hurtbox box, float amount, Vector2 from)
        {
            DamageInfo info = DamageInfo.Create(amount, DamageKind.Contact, Team.Enemy, from);
            info.knockback = 8f;
            box.Damageable.ApplyDamage(info);
            contactCooldown = 0.9f;
        }

        /// <summary>Un pedazo cortado (o soltado) se vuelve un proglótido que camina.</summary>
        private void OnSegmentCut(EnemyHealth seg)
        {
            body.Remove(seg);
            if (state == State.Dead || walkerPrefab == null || seg == null) return;
            walkers.RemoveAll(w => w == null || !w.IsAlive);
            if (walkers.Count >= maxWalkers) return;
            Vector3 pos = seg.transform.position;
            pos.y = Mathf.Max(floorY + 0.2f, pos.y - 0.3f);
            EnemyHealth w2 = Instantiate(walkerPrefab, pos, Quaternion.identity);
            if (walkerData != null) w2.Configure(walkerData, null);
            EnemyBrain brain = w2.GetComponent<EnemyBrain>();
            if (brain != null)
            {
                if (walkerData != null) brain.Configure(walkerData);
                brain.MarkOffspring();
            }
            CharacterMotor2D motor = w2.GetComponent<CharacterMotor2D>();
            if (motor != null) motor.SetVelocity(new Vector2(Random.Range(-2f, 1f), 3f));
            ArenaZone.Adopt(w2);
            walkers.Add(w2);
        }

        private void OnDied(EnemyHealth _)
        {
            state = State.Dead;
            if (headRenderer != null && headDefeated != null) headRenderer.sprite = headDefeated;
            // El cuerpo se deshace sin soltar más proglótidos.
            foreach (EnemyHealth seg in body)
                if (seg != null) Destroy(seg.gameObject);
            body.Clear();
            GameplayEvents.RaiseBossEnded();
            if (!string.IsNullOrEmpty(defeatMessage)) GameplayEvents.RaiseMessage(defeatMessage);
            Shake(0.35f, 0.6f);
        }

        private static void Shake(float strength, float duration)
        {
            Camera cam = Camera.main;
            if (cam != null && cam.TryGetComponent(out GameCamera gameCamera)) gameCamera.Shake(strength, duration);
        }
    }
}
