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
    /// Biología: su cuerpo crece desde el cuello y los pedazos del final, llenos de huevos, se sueltan y salen.
    /// En el juego:
    ///  - El cuerpo sale de un PLIEGUE del intestino (el resto del gusano sigue más adelante, escondido) y se para
    ///    como cobra: la base está ANCLADA al piso y la cabeza (escólex con ganchos) arriba, siempre MIRÁNDOTE.
    ///  - Cada pedazo tiene su propia vida: si lo cortas se suelta y camina (por eso conviene pegarle a la cabeza).
    ///  - Ataques:
    ///      DISCOS: varios pedazos se inflan (aviso) y escupen proglótidos que vuelan RECTO, girando como discos,
    ///              cada uno a la altura de su pedazo: abajo = brinca, a media altura = agáchate, abajo y arriba =
    ///              brinca al hueco. Los discos se pueden reventar a balazos.
    ///      MORDIDA: el cuerpo se dobla hacia ti y la cabeza muerde.
    ///      AZOTE:   se echa para atrás, temblando, y se deja caer hacia ti de cuerpo entero; se queda tirado un
    ///               momento (la cabeza queda a tu alcance).
    ///    Cada cierto tiempo suelta el pedazo de la base (huevos) y le crece otro en el cuello.
    ///  - Debajo de la mitad de vida es más rápido y combina discos.
    /// Los pedazos (segments) son hijos de este objeto; los pone PrefabSetup. La raíz es la cabeza.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class TeniaBoss : MonoBehaviour
    {
        [SerializeField] private string bossName = "Tenia";
        [SerializeField, TextArea] private string introMessage = "¡TENIA (solitaria)! Escupe pedazos de su cuerpo como DISCOS: brinca los de abajo, agáchate de los de en medio. Pégale a la CABEZA.";
        [SerializeField, TextArea] private string defeatMessage = "NODO: Tenia eliminada. Se contagia por comer carne de cerdo o res mal cocida.";
        [SerializeField] private Transform head;                 // dibujo de la cabeza
        [SerializeField] private EnemyHealth[] segments;
        [SerializeField] private EnemyHealth walkerPrefab;       // proglótido suelto que camina
        [SerializeField] private EnemyData walkerData;
        [SerializeField] private int maxWalkers = 4;
        [Tooltip("El cuerpo vuelve a crecer desde el cuello (así crece la tenia de verdad): un pedazo cada tantos segundos.")]
        [SerializeField] private float regrowInterval = 4.5f;
        [SerializeField] private int maxSegments = 9;
        [Tooltip("Solo suelta pedazos con huevos si le quedan más de estos.")]
        [SerializeField] private int minSegmentsToShed = 5;

        [Header("Dibujo de la cabeza (opcional)")]
        [SerializeField] private SpriteRenderer headRenderer;
        [SerializeField] private Sprite headIdle;
        [SerializeField] private Sprite headBite;
        [SerializeField] private Sprite headDefeated;
        [Tooltip("Centro de la cabeza respecto al objeto (el objeto está en el cuello, donde empieza el cuerpo).")]
        [SerializeField] private Vector2 headCenterOffset = Vector2.zero;
        [Tooltip("El dibujo de la cabeza mira a la derecha: se voltea para mirarte.")]
        [SerializeField] private bool artFacesRight = true;

        [Header("Cuerpo")]
        [SerializeField] private float spacing = 0.55f;
        [Tooltip("Color del pliegue de intestino del que sale el cuerpo.")]
        [SerializeField] private Color foldColor = new Color(0.72f, 0.3f, 0.36f);

        [Header("Ataques")]
        [SerializeField] private float idleTime = 1.5f;
        [SerializeField] private float contactDamage = 10f;
        [SerializeField] private float slamDamage = 16f;
        [SerializeField] private float biteDamage = 15f;
        [SerializeField] private float discDamage = 9f;
        [SerializeField] private float discSpeed = 7f;
        [SerializeField] private float shedInterval = 11f;
        [SerializeField] private float phase2At = 0.5f;

        private enum State { Intro, Idle, WindDiscs, WindBite, Bite, BiteBack, WindSlam, Slam, Down, Rise, Dead }
        private State state = State.Intro;
        private EnemyHealth health;
        private readonly List<EnemyHealth> body = new List<EnemyHealth>();
        private readonly List<EnemyHealth> walkers = new List<EnemyHealth>();
        private Vector2 basePoint;
        private float floorY, timer, lean, leanTarget, leanSpeed, shedTimer, contactCooldown, slamCooldown, regrowTimer;
        private int attackCount, side = -1, volleysLeft;
        private bool shedWarned, discsExplained;
        private GameObject segmentTemplate;
        private Sprite discSprite;
        private Transform fold;

        // Discos en el aire.
        private class Disc { public Transform t; public float vx; public float spin; }
        private readonly List<Disc> discs = new List<Disc>();
        private readonly List<EnemyHealth> charging = new List<EnemyHealth>();
        private readonly List<float> chargeDelay = new List<float>();

        private bool Angry => health != null && health.Health01 <= phase2At;

        private void Awake() => health = GetComponent<EnemyHealth>();

        private void OnEnable() => health.Died += OnDied;
        private void OnDisable() => health.Died -= OnDied;

        private void OnDestroy()
        {
            ClearDiscs();
            if (fold != null) Destroy(fold.gameObject);
            if (state != State.Dead) GameplayEvents.RaiseBossEnded();
        }

        private void Start()
        {
            floorY = transform.position.y;
            float x = Mathf.Min(transform.position.x, WalkableArea.MaxX - 1.8f);
            basePoint = new Vector2(x, floorY);
            body.Clear();
            if (segments != null)
                foreach (EnemyHealth s in segments)
                {
                    if (s == null) continue;
                    body.Add(s);
                    EnemyHealth seg = s;
                    seg.Died += _ => OnSegmentCut(seg);
                }
            // Molde (apagado) para que el cuerpo vuelva a crecer: copia de un pedazo de en medio.
            if (body.Count > 0)
            {
                GameObject source = body[Mathf.Min(2, body.Count - 1)].gameObject;
                source.SetActive(false);
                segmentTemplate = Instantiate(source, transform);
                segmentTemplate.name = "MoldeProglotido";
                source.SetActive(true);
                SpriteRenderer art = segmentTemplate.GetComponentInChildren<SpriteRenderer>(true);
                if (art != null) discSprite = art.sprite;
            }
            BuildFold();
            shedTimer = shedInterval;
            regrowTimer = regrowInterval;
            timer = 1.4f;
            GameplayEvents.RaiseBossStarted(bossName, health);
            if (!string.IsNullOrEmpty(introMessage)) GameplayEvents.RaiseMessage(introMessage);
            Sfx.Effect("Lombriz_Ruge", 1f);
            Shake(0.25f, 0.5f);
            LayoutBody();
        }

        /// <summary>Pliegue del intestino que tapa la base: de ahí sale el resto del gusano.</summary>
        private void BuildFold()
        {
            fold = new GameObject("PliegueTenia").transform;
            fold.position = basePoint;
            Color line = new Color(foldColor.r * 0.4f, foldColor.g * 0.35f, foldColor.b * 0.4f);
            CodeSprites.Add(fold, CodeSprites.Circle, line, 20, new Vector2(0f, 0.05f), new Vector2(2.9f, 1.3f), "Contorno");
            CodeSprites.Add(fold, CodeSprites.Circle, foldColor, 21, new Vector2(0f, 0.02f), new Vector2(2.7f, 1.12f), "Pliegue");
            CodeSprites.Add(fold, CodeSprites.Circle, new Color(1f, 1f, 1f, 0.25f), 22, new Vector2(-0.5f, 0.3f), new Vector2(0.9f, 0.22f), "Brillo");
            // Hueco oscuro de donde sale el cuerpo.
            CodeSprites.Add(fold, CodeSprites.Circle, new Color(0.2f, 0.05f, 0.08f), 19, new Vector2(0f, 0.45f), new Vector2(1.2f, 0.35f), "Hueco");
        }

        private void Update()
        {
            if (state == State.Dead) return;
            float dt = Time.deltaTime;
            float speed = Angry ? 1.3f : 1f;
            timer -= dt * speed;
            if (contactCooldown > 0f) contactCooldown -= dt;
            if (slamCooldown > 0f) slamCooldown -= dt;
            PlayerController player = PlayerController.Current;
            bool alive = player != null && player.Health != null && player.Health.IsAlive;
            if (alive) side = player.Position.x >= basePoint.x ? 1 : -1;
            float shake = 0f;

            switch (state)
            {
                case State.Intro:
                    leanTarget = 0f;
                    if (timer <= 0f) { state = State.Idle; timer = idleTime; }
                    break;

                case State.Idle:
                    leanTarget = side * 6f;   // se inclina un poquito hacia ti
                    leanSpeed = 40f;
                    if (timer <= 0f && alive) ChooseAttack(player);
                    break;

                case State.WindDiscs:
                    leanTarget = -side * 8f;  // se echa para atrás para escupir
                    leanSpeed = 60f;
                    UpdateCharging(dt * speed);
                    if (timer <= 0f && charging.Count == 0)
                    {
                        if (--volleysLeft > 0) StartVolley(player, true);
                        else { state = State.Idle; timer = idleTime; }
                    }
                    break;

                case State.WindBite:
                    shake = 0.05f;
                    leanTarget = -side * 12f;
                    leanSpeed = 80f;
                    if (timer <= 0f)
                    {
                        state = State.Bite;
                        float dx = alive ? Mathf.Abs(player.Position.x - basePoint.x) : 4f;
                        leanTarget = side * Mathf.Clamp(dx / Mathf.Max(1f, BodyLength) * 75f, 25f, 62f);
                        leanSpeed = 320f * speed;
                        timer = 0.45f;
                        Sfx.Effect("Lombriz_Ruge", 0.7f);
                    }
                    break;

                case State.Bite:
                    if (timer <= 0f) { state = State.BiteBack; timer = 0.5f; Shake(0.12f, 0.2f); }
                    break;

                case State.BiteBack:
                    leanTarget = side * 6f;
                    leanSpeed = 90f;
                    if (timer <= 0f) { state = State.Idle; timer = idleTime; }
                    break;

                case State.WindSlam:
                    // AVISO: se echa para atrás temblando.
                    shake = 0.09f;
                    leanTarget = -side * 22f;
                    leanSpeed = 50f;
                    if (timer <= 0f)
                    {
                        state = State.Slam;
                        leanTarget = side * 84f;
                        leanSpeed = 300f * speed;
                        timer = 0.6f;
                        Sfx.Effect("Lombriz_Retumbo", 0.9f);
                    }
                    break;

                case State.Slam:
                    if (Mathf.Abs(lean - leanTarget) < 4f)
                    {
                        state = State.Down;
                        timer = Angry ? 0.9f : 1.3f;   // tirada: la cabeza queda a tu alcance
                        Shake(0.3f, 0.3f);
                        Sfx.Effect("Muela_Mastica", 0.6f);
                    }
                    break;

                case State.Down:
                    if (timer <= 0f) { state = State.Rise; timer = 0.7f; }
                    break;

                case State.Rise:
                    leanTarget = side * 6f;
                    leanSpeed = 120f;
                    if (timer <= 0f) { state = State.Idle; timer = idleTime; }
                    break;
            }
            lean = Mathf.MoveTowards(lean, leanTarget, Mathf.Max(1f, leanSpeed) * dt);

            // Suelta el pedazo de la base (lleno de huevos) cada cierto tiempo.
            shedTimer -= dt * speed;
            if (shedTimer <= 0f && state != State.Intro)
            {
                shedTimer = shedInterval;
                EnemyHealth last = body.Count > minSegmentsToShed ? LastSegment() : null;
                if (last != null)
                {
                    if (!shedWarned)
                    {
                        shedWarned = true;
                        GameplayEvents.RaiseMessage("La tenia suelta el pedazo de la base: va lleno de huevos.");
                    }
                    last.Kill();
                }
            }

            // Crece desde el cuello.
            regrowTimer -= dt * speed;
            if (regrowTimer <= 0f)
            {
                regrowTimer = regrowInterval;
                if (body.Count < maxSegments) Regrow();
            }

            LayoutBody(shake);
            UpdateDiscs(dt, player);
            UpdateHeadArt(player);
            if (alive) TryContact(player);
        }

        // ================================================================ ataques

        private void ChooseAttack(PlayerController player)
        {
            attackCount++;
            float dx = Mathf.Abs(player.Position.x - basePoint.x);
            if (dx < BodyLength * 0.75f && attackCount % 3 != 0)
            {
                state = State.WindBite;
                timer = 0.6f;
                return;
            }
            if (attackCount % 4 == 0 && slamCooldown <= 0f && dx < BodyLength + 1.5f)
            {
                state = State.WindSlam;
                timer = 0.9f;
                slamCooldown = 6f;
                return;
            }
            volleysLeft = Angry ? 2 : 1;
            StartVolley(player, false);
        }

        /// <summary>Escoge qué pedazos escupen (contando desde la base: 0 = el de hasta abajo).</summary>
        private void StartVolley(PlayerController player, bool followUp)
        {
            state = State.WindDiscs;
            charging.Clear();
            chargeDelay.Clear();
            int n = body.Count;
            int pattern = (attackCount + (followUp ? 1 : 0)) % 3;
            int[] fromBase;
            if (pattern == 2 && n >= 9) fromBase = new[] { 0, 1, 7, 8 };   // abajo y arriba: brinca al hueco
            else if (pattern == 1 && n >= 5) fromBase = new[] { 3, 4 };    // a media altura: agáchate
            else fromBase = new[] { 0, 1 };                                 // abajo: brinca
            foreach (int k in fromBase)
            {
                int i = n - 1 - k;
                if (i < 0 || i >= n || body[i] == null || !body[i].IsAlive) continue;
                charging.Add(body[i]);
                chargeDelay.Add(Angry ? 0.5f : 0.75f);
                body[i].SetTelegraph(true);
            }
            timer = 0.2f;
            if (!discsExplained)
            {
                discsExplained = true;
                GameplayEvents.RaiseMessage("Los pedazos que se inflan van a salir volando: abajo = brinca, en medio = agáchate.");
            }
            Sfx.Effect("Parasito_Prepara", 0.8f);
        }

        /// <summary>Los pedazos avisados se inflan y, al terminar el aviso, escupen su disco.</summary>
        private void UpdateCharging(float dt)
        {
            for (int i = charging.Count - 1; i >= 0; i--)
            {
                EnemyHealth seg = charging[i];
                if (seg == null || !seg.IsAlive) { charging.RemoveAt(i); chargeDelay.RemoveAt(i); continue; }
                chargeDelay[i] -= dt;
                float pulse = 1f + 0.22f * Mathf.Abs(Mathf.Sin(Time.time * 18f));
                seg.transform.localScale = new Vector3(pulse, pulse, 1f);
                if (chargeDelay[i] > 0f) continue;
                seg.transform.localScale = Vector3.one;
                seg.SetTelegraph(false);
                LaunchDisc(seg.transform.position);
                charging.RemoveAt(i);
                chargeDelay.RemoveAt(i);
            }
        }

        private void LaunchDisc(Vector2 from)
        {
            var go = new GameObject("DiscoProglotido");
            go.transform.position = new Vector3(from.x + side * 0.4f, from.y, 0f);
            go.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            if (discSprite != null)
            {
                SpriteRenderer r = go.AddComponent<SpriteRenderer>();
                r.sprite = discSprite;
                r.sortingOrder = 30;
            }
            else
            {
                CodeSprites.Add(go.transform, CodeSprites.Circle, new Color(0.35f, 0.25f, 0.1f), 30, Vector2.zero, new Vector2(1.1f, 0.6f), "Contorno");
                CodeSprites.Add(go.transform, CodeSprites.Circle, new Color(0.95f, 0.88f, 0.7f), 31, Vector2.zero, new Vector2(1f, 0.5f), "Pedazo");
            }
            discs.Add(new Disc { t = go.transform, vx = side * discSpeed * (Angry ? 1.2f : 1f), spin = -side * 720f });
            Sfx.EffectAt("Parasito_Disparo", from, 0.9f);
        }

        private void UpdateDiscs(float dt, PlayerController player)
        {
            Hurtbox box = player != null && player.Health != null && player.Health.IsAlive ? player.GetComponent<Hurtbox>() : null;
            for (int i = discs.Count - 1; i >= 0; i--)
            {
                Disc d = discs[i];
                if (d.t == null) { discs.RemoveAt(i); continue; }
                d.t.position += new Vector3(d.vx * dt, 0f, 0f);
                d.t.Rotate(0f, 0f, d.spin * dt);
                Vector2 p = d.t.position;
                bool gone = p.x < WalkableArea.MinX - 2f || p.x > WalkableArea.MaxX + 2f;
                if (!gone && box != null && box.IsAlive && box.OverlapsCircle(p, 0.36f))
                {
                    DamageInfo info = DamageInfo.Create(discDamage, DamageKind.Contact, Team.Enemy, p);
                    info.knockback = 6f;
                    box.Damageable.ApplyDamage(info);
                    gone = true;
                }
                if (!gone)
                {
                    // Se revientan a balazos.
                    foreach (Projectile s in Projectile.Flying)
                    {
                        if (s == null || s.SourceTeam != Team.Player || Vector2.Distance(s.Position, p) > 0.5f) continue;
                        s.Consume();
                        Sfx.EffectAt("Parasito_Muere", p, 0.6f);
                        gone = true;
                        break;
                    }
                }
                if (gone) { Destroy(d.t.gameObject); discs.RemoveAt(i); }
            }
        }

        private void ClearDiscs()
        {
            foreach (Disc d in discs) if (d.t != null) Destroy(d.t.gameObject);
            discs.Clear();
        }

        // ================================================================ cuerpo

        private float BodyLength => Mathf.Max(1, body.Count) * spacing + 0.6f;

        /// <summary>
        /// Acomoda el cuerpo como una columna que sale del pliegue: la base fija en el piso y cada pedazo un poco más
        /// doblado hacia donde se inclina (lean). La cabeza (este objeto) queda arriba del cuello, girada igual.
        /// </summary>
        private void LayoutBody(float shake = 0f)
        {
            float t = Time.time;
            int n = body.Count;
            Vector2 p = basePoint + new Vector2(0f, 0.3f);
            float angle = 0f;
            for (int k = 0; k < n; k++)
            {
                EnemyHealth seg = body[n - 1 - k];   // de la base (cola) hacia el cuello
                float f = (k + 1f) / Mathf.Max(1, n);
                angle = lean * Mathf.Pow(f, 1.3f) + Mathf.Sin(t * 1.7f - k * 0.55f) * 6f * f;
                if (seg != null)
                {
                    seg.transform.position = p + new Vector2(Mathf.Sin(t * 50f) * shake * f, 0f);
                    seg.transform.rotation = Quaternion.Euler(0f, 0f, -angle);
                }
                float a = (90f - angle) * Mathf.Deg2Rad;
                p += new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * spacing;
                if (p.y < floorY + 0.35f) p.y = floorY + 0.35f;   // al azotar se acuesta en el piso, no lo atraviesa
            }
            float ha = (90f - angle) * Mathf.Deg2Rad;
            Vector2 headPos = p + new Vector2(Mathf.Cos(ha), Mathf.Sin(ha)) * (spacing * 0.2f);
            if (headPos.y < floorY + 0.4f) headPos.y = floorY + 0.4f;
            transform.position = headPos + new Vector2(Mathf.Sin(t * 50f) * shake, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, -angle);
        }

        private void Regrow()
        {
            if (segmentTemplate == null) return;
            GameObject clone = Instantiate(segmentTemplate, transform);
            clone.name = "ProglotidoNuevo";
            clone.transform.position = transform.position;
            clone.SetActive(true);
            EnemyHealth seg = clone.GetComponent<EnemyHealth>();
            if (seg == null) { Destroy(clone); return; }
            seg.Died += _ => OnSegmentCut(seg);
            body.Insert(0, seg);
        }

        /// <summary>La cabeza siempre te mira (el dibujo se voltea) y abre la boca al atacar.</summary>
        private void UpdateHeadArt(PlayerController player)
        {
            if (headRenderer == null) return;
            bool biting = state == State.WindBite || state == State.Bite || state == State.WindSlam || state == State.Slam || state == State.WindDiscs;
            Sprite s = biting && headBite != null ? headBite : headIdle;
            if (s != null && headRenderer.sprite != s) headRenderer.sprite = s;
            if (player != null)
            {
                bool playerRight = player.Position.x >= transform.position.x;
                headRenderer.flipX = artFacesRight ? !playerRight : playerRight;
            }
        }

        private Vector2 HeadCenter => (Vector2)transform.TransformPoint(headCenterOffset);

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

            bool biting = state == State.Bite;
            bool slamming = state == State.Slam;
            if (box.OverlapsCircle(HeadCenter, 0.8f))
            {
                Hit(box, biting ? biteDamage : slamming ? slamDamage : contactDamage, HeadCenter);
                return;
            }
            foreach (EnemyHealth seg in body)
            {
                if (seg == null || !seg.IsAlive) continue;
                if (!box.OverlapsCircle(seg.transform.position, 0.42f)) continue;
                Hit(box, slamming ? slamDamage : contactDamage, seg.transform.position);
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
            int idx = charging.IndexOf(seg);
            if (idx >= 0) { charging.RemoveAt(idx); chargeDelay.RemoveAt(idx); }
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
            if (motor != null) motor.SetVelocity(new Vector2(side * Random.Range(1f, 3f), 3f));
            ArenaZone.Adopt(w2);
            walkers.Add(w2);
        }

        private void OnDied(EnemyHealth _)
        {
            state = State.Dead;
            ClearDiscs();
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
