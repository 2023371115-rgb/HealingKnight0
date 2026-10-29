using System.Collections.Generic;
using HealingKnight.Core.Combat2D;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// IA de enemigo en vista lateral (estilo Metal Slug). La forma de moverse y pelear sale de EnemyData.behavior
    /// (cada una basada en la biología del microorganismo; ver EnemyBehavior).
    ///
    /// Navegación de los de suelo: si estás más abajo, BAJAN de la plataforma (si abajo hay piso, no un hueco);
    /// si estás más arriba y cerca, SALTAN para subir (las plataformas se atraviesan desde abajo).
    /// Si no te alcanzan, patrullan en vez de quedarse quietos.
    /// Usa "fichas de ataque" (máx. 2 atacando a la vez) para que los grupos sean justos.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyBrain : MonoBehaviour
    {
        private enum State { Idle, Approach, Windup, Attack, Recover, Hurt, Special, Dead }

        [SerializeField] private EnemyData data;
        [SerializeField] private Transform visual;

        [Tooltip("Para los enemigos que disparan o lanzan.")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private LayerMask groundMask = 1 << 6;

        [Tooltip("Distancia a la que esperan su turno los que no tienen ficha de ataque.")]
        [SerializeField] private float waitDistance = 3.5f;

        private const float ArcGravity = 14f;
        private static readonly List<EnemyBrain> active = new List<EnemyBrain>();

        private CharacterMotor2D motor;   // null en voladores
        private EnemyHealth health;
        private ComponentPool<Projectile> projectilePool;
        private SpriteRenderer[] renderers;
        private float[] rendererAlpha;

        private State state = State.Idle;
        private float timer;
        private float attackCooldown;
        private float specialCooldown;
        private float jumpCooldown;
        private float contactCooldown;
        private float knockbackVelocity;
        private int facing = -1;
        private int attackDirection;
        private bool attackLanded;
        private float bobPhase;
        private Vector3 visualBaseScale = Vector3.one;
        private float age;
        private int specialPhase;
        private int burstLeft;
        private float burstTimer;
        private Vector2 lockedTarget;
        private float wanderDirection = 1f;
        private float fade = 1f;
        private float fadeTarget = 1f;
        private Vector2 shape = Vector2.one;   // estirar/aplastar el dibujo (se aplica en LateUpdate)
        private float spin;                    // rotación del dibujo (rotavirus rodando)
        private bool rangedAttack;             // el ataque en curso es disparo/lanzamiento

        // Movimiento con inercia y "vida" en el dibujo.
        private Vector2 vel;                   // velocidad actual (acelera y frena, no cambia de golpe)
        private Vector2 lastPosition;
        private Vector2 frameVelocity;         // lo que de verdad se movió este cuadro (para inclinarse)
        private float walkPhase;
        private float landSquash;
        private bool wasGrounded = true;
        private float spinSpeed;               // rueda del rotavirus: grados por segundo
        private Vector2 launchVelocity;
        private int bounces;

        private const float GroundAcceleration = 38f;
        private const float AirAcceleration = 16f;

        // Giardia pegada al jugador.
        private PlayerController latchedTo;
        private float latchTimer;
        private float latchTick;

        /// <summary>Las crías (cocos, oxiuros que salen del huevo) ya no se reproducen.</summary>
        private bool isOffspring;

        public EnemyData Data => data;
        private bool IsFlying => motor == null;
        private Vector2 Position => transform.position;
        private EnemyBehavior Behavior => data.behavior;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => active.Clear();

        private void Awake()
        {
            motor = GetComponent<CharacterMotor2D>();
            health = GetComponent<EnemyHealth>();
            attackCooldown = Random.Range(0.4f, 1.2f);
            bobPhase = Random.value * 10f;
            lastPosition = transform.position;
            if (visual == null && motor != null) visual = motor.Visual;
            if (visual != null) visualBaseScale = new Vector3(Mathf.Abs(visual.localScale.x), visual.localScale.y, visual.localScale.z);

            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            rendererAlpha = new float[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) rendererAlpha[i] = renderers[i].color.a;
        }

        private void OnEnable()
        {
            active.Add(this);
            health.Hit += OnHit;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            active.Remove(this);
            health.Hit -= OnHit;
            health.Died -= OnDied;
            AttackTokens.Release(this);
            health.SetTelegraph(false);
        }

        private void OnDestroy() => projectilePool?.Clear();

        public void Configure(EnemyData enemyData) => data = enemyData;

        /// <summary>Lo llama quien la hace nacer (huevo, racimo): las crías no vuelven a reproducirse.</summary>
        public void MarkOffspring() => isOffspring = true;

        private void Start()
        {
            if (data == null) return;
            if (projectilePrefab != null) projectilePool = new ComponentPool<Projectile>(projectilePrefab, 2, 8);
            specialCooldown = data.specialCooldown * Random.Range(0.5f, 1f);
            if (Behavior == EnemyBehavior.Egg)
            {
                state = State.Special;
                timer = data.specialDuration;
            }
        }

        private void Update()
        {
            if (data == null || state == State.Dead) return;

            // Cayó a un hueco: cuenta como eliminado.
            if (Position.y < WalkableArea.KillY)
            {
                Destroy(gameObject);
                return;
            }

            float dt = Time.deltaTime;
            age += dt;
            if (attackCooldown > 0f) attackCooldown -= dt;
            if (specialCooldown > 0f) specialCooldown -= dt;
            if (jumpCooldown > 0f) jumpCooldown -= dt;
            if (contactCooldown > 0f) contactCooldown -= dt;
            shape = Vector2.one;

            // Infección que se cura sola (resfriado): se desvanece.
            if (data.lifetime > 0f && age >= data.lifetime)
            {
                health.Kill();
                return;
            }

            PlayerController player = PlayerController.Current;
            bool playerAvailable = player != null && player.Health != null && player.Health.IsAlive;

            if (latchedTo != null)
            {
                UpdateLatched(dt);
                return;
            }

            switch (state)
            {
                case State.Idle:
                    Move(Vector2.zero);
                    if (playerAvailable && Vector2.Distance(Position, player.Position) <= data.aggroRange) state = State.Approach;
                    break;

                case State.Approach:
                    if (!playerAvailable) { GoIdle(); break; }
                    if (TryStartSpecial(player)) break;
                    UpdateApproach(player, dt);
                    break;

                case State.Windup:
                    UpdateWindup(player, dt);
                    break;

                case State.Attack:
                    UpdateAttack(player, dt);
                    break;

                case State.Recover:
                    UpdateRecover(player, dt);
                    break;

                case State.Hurt:
                    timer -= dt;
                    knockbackVelocity = Mathf.MoveTowards(knockbackVelocity, 0f, 20f * dt);
                    Move(new Vector2(knockbackVelocity, 0f), true);
                    if (timer <= 0f) state = State.Approach;
                    break;

                case State.Special:
                    UpdateSpecial(player, dt);
                    break;
            }
        }

        private void LateUpdate()
        {
            // Desvanecer (latencia, enterrado) y deformar el dibujo (inflarse para toser, estirar el seudópodo...).
            if (fade < 0.999f || !Mathf.Approximately(fade, fadeTarget))
            {
                fade = Mathf.MoveTowards(fade, fadeTarget, Time.deltaTime * 3f);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null) continue;
                    Color c = renderers[i].color;
                    c.a = rendererAlpha[i] * fade;
                    renderers[i].color = c;
                }
            }

            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            frameVelocity = ((Vector2)transform.position - lastPosition) / dt;
            lastPosition = transform.position;
            if (visual == null || state == State.Dead) return;

            Vector2 juice = Vector2.one;
            float lean;
            if (motor != null)
            {
                // Pasos: rebota al caminar; se estira al subir y se aplasta al caer al piso.
                bool grounded = motor.IsGrounded;
                float speedX = Mathf.Abs(frameVelocity.x);
                if (grounded && speedX > 0.3f)
                {
                    walkPhase += speedX * 5.5f * dt;
                    float step = Mathf.Abs(Mathf.Sin(walkPhase));
                    juice = new Vector2(1f + step * 0.06f, 1f - step * 0.08f);
                }
                else if (grounded)
                {
                    float breathe = Mathf.Sin(age * 3f + bobPhase) * 0.035f; // respira quieto
                    juice = new Vector2(1f - breathe, 1f + breathe);
                }
                else
                {
                    float vy = Mathf.Clamp(motor.Velocity.y / 12f, -1f, 1f);
                    juice = new Vector2(1f - Mathf.Abs(vy) * 0.12f, 1f + Mathf.Abs(vy) * 0.16f);
                }
                if (grounded && !wasGrounded) landSquash = 1f;
                wasGrounded = grounded;
                lean = Mathf.Clamp(-frameVelocity.x * 1.6f, -9f, 9f);
            }
            else
            {
                // Voladores: laten y se inclinan hacia donde van.
                float pulse = Mathf.Sin(age * 3.4f + bobPhase) * 0.045f;
                juice = new Vector2(1f + pulse, 1f - pulse);
                lean = Mathf.Clamp(-frameVelocity.x * 3.2f, -22f, 22f);
            }

            if (landSquash > 0f)
            {
                landSquash = Mathf.MoveTowards(landSquash, 0f, dt * 6f);
                juice = new Vector2(juice.x * (1f + landSquash * 0.25f), juice.y * (1f - landSquash * 0.22f));
            }

            float sign = visual.localScale.x < 0f ? -1f : 1f;
            Vector2 final = new Vector2(shape.x * juice.x, shape.y * juice.y);
            visual.localScale = new Vector3(visualBaseScale.x * sign * final.x, visualBaseScale.y * final.y, visualBaseScale.z);

            float currentZ = visual.localEulerAngles.z;
            float z = spin != 0f ? spin : Mathf.LerpAngle(currentZ, lean, dt * 10f);
            visual.localRotation = Quaternion.Euler(0f, 0f, z);
        }

        // ================================================================ moverse

        /// <summary>
        /// Mueve con inercia: acelera y frena en vez de arrancar y parar en seco (se siente más vivo).
        /// 'instant' = sin inercia (empujones al recibir un golpe).
        /// </summary>
        private void Move(Vector2 velocity, bool instant = false)
        {
            float dt = Time.deltaTime;
            if (motor != null)
            {
                float accel = motor.IsGrounded ? GroundAcceleration : AirAcceleration;
                vel.x = instant ? velocity.x : Mathf.MoveTowards(motor.Velocity.x, velocity.x, accel * dt);
                motor.SetHorizontalVelocity(vel.x);
            }
            else
            {
                // Volador: gira suave hacia donde quiere ir, con un vaivén.
                vel = instant ? velocity : Vector2.MoveTowards(vel, velocity, AirAcceleration * 1.4f * dt);
                bobPhase += dt * 2.5f;
                Vector2 bob = new Vector2(0f, Mathf.Sin(bobPhase) * 0.6f);
                transform.position += (Vector3)((vel + bob) * dt);
            }
        }

        private void Face(int direction)
        {
            if (direction == 0) return;
            facing = direction;
            if (motor != null) motor.SetFacing(direction);
            else if (visual != null) visual.localScale = new Vector3(visualBaseScale.x * direction, visual.localScale.y, visualBaseScale.z);
        }

        private bool Grounded => motor != null && motor.IsGrounded;

        /// <summary>¿Hay piso un paso adelante en esa dirección?</summary>
        private bool GroundAhead(int dir, float distance = 0.5f)
        {
            Bounds b = motor.Collider.bounds;
            Vector2 origin = new Vector2(b.center.x + dir * (b.extents.x + distance), b.min.y + 0.1f);
            return Physics2D.Raycast(origin, Vector2.down, 1.0f, groundMask).collider != null;
        }

        /// <summary>Si se deja caer por esa orilla, ¿cae en piso (y no en un hueco)?</summary>
        private bool SafeDrop(int dir)
        {
            Bounds b = motor.Collider.bounds;
            Vector2 origin = new Vector2(b.center.x + dir * (b.extents.x + 0.6f), b.min.y - 0.2f);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 14f, groundMask);
            return hit.collider != null && hit.point.y > WalkableArea.KillY + 1f;
        }

        /// <summary>
        /// Lleva a un enemigo de suelo hacia una X. Baja de las plataformas si el jugador está abajo,
        /// salta si está arriba y cerca, y patrulla si no puede llegar. Devuelve la velocidad horizontal usada.
        /// </summary>
        private float NavigateGround(PlayerController player, float desiredX, float speed, bool changeLevels = true)
        {
            Vector2 me = Position;
            float dy = player.Position.y - me.y;
            bool playerAbove = changeLevels && dy > 1.6f;
            bool playerBelow = changeLevels && dy < -1.6f;
            bool canClimb = playerAbove && data.jumpHeight > 0f && dy <= data.jumpHeight + 0.5f;
            if (playerAbove || playerBelow) desiredX = player.Position.x; // ir justo abajo/arriba para cambiar de piso

            float dx = desiredX - me.x;
            float vx = Mathf.Abs(dx) > 0.15f ? Mathf.Clamp(dx * 4f, -speed, speed) : 0f; // llega frenando, no en seco

            if (playerBelow && Mathf.Abs(dx) < 1.5f) vx = wanderDirection * speed;              // busca una orilla para bajar
            else if (playerAbove && !canClimb && Mathf.Abs(dx) < 1.5f) vx = wanderDirection * speed * 0.4f; // no alcanza: ronda

            if (vx != 0f && Grounded)
            {
                int dir = vx > 0f ? 1 : -1;
                if (!GroundAhead(dir))
                {
                    bool drop = playerBelow && SafeDrop(dir);
                    if (!drop)
                    {
                        // No puede seguir: da la vuelta y patrulla su plataforma en vez de quedarse quieto.
                        wanderDirection = -dir;
                        vx = GroundAhead(-dir) ? -dir * speed * 0.45f : 0f;
                    }
                }
            }

            // Saltar para subir a la plataforma donde estás.
            if (canClimb && Grounded && jumpCooldown <= 0f && Mathf.Abs(player.Position.x - me.x) < 2.6f)
            {
                motor.Jump(Mathf.Clamp(dy + 1f, 1.5f, data.jumpHeight + 0.6f), 0.42f);
                jumpCooldown = 1.1f;
            }

            vx += Separation() * 1.5f;
            Move(new Vector2(vx, 0f));
            return vx;
        }

        private float Separation()
        {
            float push = 0f;
            for (int i = 0; i < active.Count; i++)
            {
                EnemyBrain other = active[i];
                if (other == this || other.IsFlying != IsFlying) continue;
                float d = Position.x - other.Position.x;
                if (Mathf.Abs(d) < 0.7f && Mathf.Abs(Position.y - other.Position.y) < 1f)
                    push += (d >= 0f ? 1f : -1f) * (1f - Mathf.Abs(d) / 0.7f);
            }
            return push;
        }

        // ================================================================ acercarse

        private void UpdateApproach(PlayerController player, float dt)
        {
            Vector2 me = Position;
            Vector2 target = player.Position;
            int side = me.x >= target.x ? 1 : -1; // de qué lado del jugador estoy

            // Los que no esperan turno (saltan, ruedan, flotan con el aire) no ocupan ficha de ataque.
            switch (Behavior)
            {
                case EnemyBehavior.Hopper: ApproachHopper(player); return;
                case EnemyBehavior.Drifter: ApproachDrifter(player, dt); return;
            }

            bool hasToken = AttackTokens.TryAcquire(this);
            if (Behavior == EnemyBehavior.Roller) { ApproachWheel(player, hasToken); return; }
            if (Behavior == EnemyBehavior.Diver) { ApproachFlying(player, hasToken, data.flyHeight, 1.5f); return; }
            if (Behavior == EnemyBehavior.Frenzy) { ApproachFlying(player, hasToken, 0.8f, data.attackDistance); return; }
            if (IsFlying)
            {
                ApproachFlying(player, hasToken, data.flyHeight, data.attackDistance);
                return;
            }

            // ---- De suelo ----
            Face(-side);
            float distance = Mathf.Abs(target.x - me.x);
            bool lobber = Behavior == EnemyBehavior.Lobber;
            bool tankLob = Behavior == EnemyBehavior.Tank && distance > 4.5f; // tuberculosis: de lejos escupe gotitas
            float desired = hasToken ? data.attackDistance * (lobber ? 1f : 0.85f) : waitDistance;
            if (lobber) desired = Mathf.Max(desired, data.attackDistance);

            float speed = data.moveSpeed;
            if (Behavior == EnemyBehavior.Breeder) speed *= 1f + Mathf.Sin(age * 12f) * 0.5f; // culebrea

            // El que lanza en arco no necesita cambiar de piso: te alcanza desde donde está.
            NavigateGround(player, target.x + side * desired, speed, !lobber);
            Face(-side);

            // ¿Listo para atacar?
            bool ready = hasToken && attackCooldown <= 0f && Grounded;
            if (lobber) ready &= distance <= desired + 1.5f && distance <= 9f;
            else if (tankLob) ready &= distance <= 9f;
            else ready &= distance <= desired + 0.4f && Mathf.Abs(target.y - me.y) < 1.2f;
            if (ready) StartWindup(-side, lobber || tankLob);
        }

        private void ApproachFlying(PlayerController player, bool hasToken, float height, float distance)
        {
            Vector2 me = Position;
            Vector2 target = player.Position;
            int side = me.x >= target.x ? 1 : -1;
            Face(-side);

            float desired = hasToken ? distance : Mathf.Max(distance, waitDistance);
            Vector2 goal = new Vector2(target.x + side * desired, target.y + height);
            // Llega frenando y rodea un poco (no se queda clavado en un punto).
            goal += new Vector2(Mathf.Sin(age * 0.9f + bobPhase) * 1.2f, Mathf.Sin(age * 1.7f + bobPhase) * 0.5f);
            Vector2 velocity = Vector2.ClampMagnitude((goal - me) * 2.6f, data.moveSpeed);

            if (Behavior == EnemyBehavior.Spreader)
            {
                // Vuelo errático de partícula pequeña.
                velocity += new Vector2(Mathf.PerlinNoise(age * 1.3f, bobPhase) - 0.5f, Mathf.PerlinNoise(bobPhase, age * 1.3f) - 0.5f) * 5f;
            }

            velocity.x += Separation() * 1.5f;
            Move(velocity);

            bool inRange = Mathf.Abs(target.x - me.x) <= desired + 0.5f && Mathf.Abs(goal.y - me.y) < 1.5f;
            bool shooter = Behavior == EnemyBehavior.Shooter || Behavior == EnemyBehavior.Spreader
                           || Behavior == EnemyBehavior.Paralyzer || Behavior == EnemyBehavior.Latent;
            if (hasToken && inRange && attackCooldown <= 0f) StartWindup(-side, shooter);
        }

        /// <summary>Neumococo: solo avanza saltando; te cae encima.</summary>
        private void ApproachHopper(PlayerController player)
        {
            Vector2 me = Position;
            int dir = player.Position.x >= me.x ? 1 : -1;
            Face(dir);
            ContactDamage(player, 0.55f);

            if (!Grounded)
            {
                Move(new Vector2(dir * data.moveSpeed * 1.4f, 0f));
                return;
            }

            Move(Vector2.zero);
            shape = new Vector2(1.15f, 0.8f); // agazapado antes del salto
            if (jumpCooldown > 0f) return;

            float dy = player.Position.y - me.y;
            float height = dy > 1.2f ? Mathf.Clamp(dy + 1f, 1.5f, data.jumpHeight + 0.6f) : Random.Range(1.1f, 2f);
            if (!GroundAhead(dir) && !(dy < -1.2f && SafeDrop(dir))) dir = -dir;
            motor.Jump(height, 0.36f);
            Move(new Vector2(dir * data.moveSpeed * 1.4f, 0f), true); // el salto sale disparado
            jumpCooldown = Random.Range(0.45f, 0.75f);
        }

        /// <summary>
        /// Rotavirus: rueda voladora. Flota girando a tu alrededor; luego acelera el giro, se echa para atrás
        /// y se lanza en línea recta como sierra. Rebota en el piso, las paredes y tu escudo.
        /// </summary>
        private void ApproachWheel(PlayerController player, bool hasToken)
        {
            Vector2 me = Position;
            Vector2 target = player.Position + Vector2.up * 0.8f;
            int side = me.x >= target.x ? 1 : -1;
            Face(-side);

            float desired = hasToken ? data.attackDistance : Mathf.Max(data.attackDistance, waitDistance);
            float orbit = age * 1.1f + bobPhase;
            Vector2 goal = new Vector2(target.x + side * desired + Mathf.Sin(orbit) * 1.2f, target.y + data.flyHeight + Mathf.Cos(orbit * 1.3f) * 0.7f);
            Move(Vector2.ClampMagnitude((goal - me) * 2.6f, data.moveSpeed));

            spinSpeed = Mathf.MoveTowards(spinSpeed, 320f + Mathf.Abs(vel.x) * 70f, 900f * Time.deltaTime);
            spin -= side * -spinSpeed * Time.deltaTime;

            bool inRange = Vector2.Distance(me, goal) < 1.6f;
            if (hasToken && inRange && attackCooldown <= 0f) StartWindup(-side);
        }

        /// <summary>Movimiento de la rueda lanzada: avanza recto, rebota en el escenario y en los bordes.</summary>
        private void UpdateWheelLaunch(PlayerController player, float dt)
        {
            spinSpeed = Mathf.MoveTowards(spinSpeed, 1500f, 3000f * dt);
            spin -= Mathf.Sign(launchVelocity.x == 0f ? 1f : launchVelocity.x) * spinSpeed * dt;

            Vector2 me = Position;
            Vector2 step = launchVelocity * dt;
            RaycastHit2D hit = Physics2D.Raycast(me, launchVelocity.normalized, step.magnitude + 0.45f, groundMask);
            bool outLeft = me.x <= WalkableArea.MinX + 0.5f && launchVelocity.x < 0f;
            bool outRight = me.x >= WalkableArea.MaxX - 0.5f && launchVelocity.x > 0f;
            if (hit.collider != null && bounces < 2)
            {
                launchVelocity = Vector2.Reflect(launchVelocity, hit.normal) * 0.75f;
                bounces++;
                landSquash = 1f;
            }
            else if ((outLeft || outRight) && bounces < 2)
            {
                launchVelocity.x = -launchVelocity.x * 0.75f;
                bounces++;
            }
            else if (hit.collider != null)
            {
                launchVelocity = Vector2.zero; // ya rebotó bastante: se queda
            }

            transform.position += (Vector3)(launchVelocity * dt);
            vel = launchVelocity;
            if (!attackLanded) attackLanded = ContactDamage(player, 0.6f);

            if (timer <= 0f || launchVelocity.sqrMagnitude < 1f)
            {
                vel = launchVelocity * 0.5f; // frena con inercia al regresar
                EndAttack(0.7f);
            }
        }

        /// <summary>Polen: lo lleva el aire en ondas, sin perseguirte de verdad; cerca de ti revienta.</summary>
        private void ApproachDrifter(PlayerController player, float dt)
        {
            Vector2 me = Position;
            Vector2 target = player.Position + Vector2.up * 1f;
            Vector2 toward = (target - me);
            Vector2 drift = new Vector2(Mathf.Sign(toward.x) * data.moveSpeed, Mathf.Clamp(toward.y, -1f, 1f) * 0.6f);
            drift.y += Mathf.Sin(age * 2.2f + bobPhase) * 1.4f;     // corriente de aire
            drift.x += Mathf.Sin(age * 0.9f + bobPhase) * 0.8f;
            transform.position += (Vector3)(drift * dt);
            spin += 40f * dt;

            if (toward.magnitude < 1.3f)
            {
                state = State.Windup;
                timer = data.attackWindup;
                health.SetTelegraph(true);
            }
        }

        private void StartWindup(int direction, bool ranged = false)
        {
            rangedAttack = ranged;
            state = State.Windup;
            timer = data.attackWindup;
            attackDirection = direction;
            health.SetTelegraph(true);
            PlayerController p = PlayerController.Current;
            lockedTarget = p != null ? p.Position + Vector2.up * 0.8f : Position;
        }

        // ================================================================ atacar

        private void UpdateWindup(PlayerController player, float dt)
        {
            timer -= dt;
            float k = 1f - Mathf.Clamp01(timer / Mathf.Max(0.01f, data.attackWindup));

            switch (Behavior)
            {
                case EnemyBehavior.Cougher: shape = new Vector2(1f + k * 0.25f, 1f + k * 0.25f); break;     // toma aire
                case EnemyBehavior.Engulfer: shape = new Vector2(1f - k * 0.2f, 1f + k * 0.15f); break;     // se recoge
                case EnemyBehavior.Charger: shape = new Vector2(1f - k * 0.15f, 1f - k * 0.1f); break;      // se agazapa
                case EnemyBehavior.Drifter: shape = Vector2.one * (1f + k * 0.4f); break;                   // se hincha
            }

            if (Behavior == EnemyBehavior.Roller)
            {
                // Se echa para atrás y acelera el giro (aviso claro antes de lanzarse).
                Vector2 away = (Position - lockedTarget).normalized;
                Move(away * 2.2f);
                spinSpeed = Mathf.MoveTowards(spinSpeed, 1500f, 2400f * dt);
                spin -= attackDirection * spinSpeed * dt;
                shape = new Vector2(1f - k * 0.1f, 1f - k * 0.1f);
            }
            else if (IsFlying && Behavior != EnemyBehavior.Drifter) Move(Vector2.zero);
            else if (!IsFlying) Move(Vector2.zero);

            if (timer <= 0f) BeginAttack(player);
        }

        private void BeginAttack(PlayerController player)
        {
            health.SetTelegraph(false);
            attackLanded = false;

            if (rangedAttack)
            {
                burstLeft = Mathf.Max(1, data.burstCount);
                burstTimer = 0f;
                state = State.Attack;
                timer = 0f;
                return;
            }

            switch (Behavior)
            {
                case EnemyBehavior.Drifter:
                    Burst(player);
                    return;

                case EnemyBehavior.Cougher:
                    burstLeft = 3; // ataque de tos: tres golpes seguidos
                    burstTimer = 0f;
                    state = State.Attack;
                    return;

                case EnemyBehavior.Latcher:
                    // Salta hacia ti para pegarse.
                    if (Grounded) motor.Jump(1.6f, 0.3f);
                    state = State.Attack;
                    timer = 0.7f;
                    return;

                case EnemyBehavior.Diver:
                    state = State.Attack;
                    timer = 1f;
                    return;

                case EnemyBehavior.Roller:
                    launchVelocity = (lockedTarget - Position).normalized * 13f;
                    bounces = 0;
                    state = State.Attack;
                    timer = 1.2f;
                    return;

                case EnemyBehavior.Frenzy:
                    burstLeft = 3; // tres mordidas
                    state = State.Attack;
                    timer = 0.22f;
                    lockedTarget = player != null ? player.Position + Vector2.up * 0.8f : Position;
                    return;

                case EnemyBehavior.Charger:
                    state = State.Attack;
                    timer = 0.6f;
                    return;

                case EnemyBehavior.Engulfer:
                    state = State.Attack;
                    timer = 0.35f;
                    return;

                default:
                    state = State.Attack;
                    timer = 0.22f;
                    return;
            }
        }

        private void UpdateAttack(PlayerController player, float dt)
        {
            timer -= dt;

            if (rangedAttack)
            {
                Move(Vector2.zero);
                burstTimer -= dt;
                if (burstTimer <= 0f && burstLeft > 0)
                {
                    if (player != null) FireVolley(player);
                    burstLeft--;
                    burstTimer = 0.16f;
                }
                if (burstLeft <= 0) EndAttack(0.4f);
                return;
            }

            switch (Behavior)
            {

                case EnemyBehavior.Cougher:
                    UpdateCough(player, dt);
                    return;

                case EnemyBehavior.Latcher:
                    Move(new Vector2(attackDirection * data.moveSpeed * 2.2f, 0f), true);
                    if (player != null && TryLatch(player)) return;
                    if (timer <= 0f && Grounded) EndAttack(0.6f);
                    return;

                case EnemyBehavior.Roller:
                    UpdateWheelLaunch(player, dt);
                    return;

                case EnemyBehavior.Diver:
                {
                    // Picada en línea recta hacia donde estabas.
                    Vector2 to = lockedTarget - Position;
                    if (to.magnitude > 0.3f)
                    {
                        vel = to.normalized * 11f;
                        transform.position += (Vector3)(vel * dt);
                    }
                    ContactDamage(player, 0.6f);
                    if (to.magnitude <= 0.3f || timer <= 0f) EndAttack(0.7f);
                    return;
                }

                case EnemyBehavior.Frenzy:
                {
                    Vector2 to = lockedTarget - Position;
                    if (to.magnitude > 0.2f)
                    {
                        vel = to.normalized * 9f;
                        transform.position += (Vector3)(vel * dt);
                    }
                    if (!attackLanded) attackLanded = ContactDamage(player, 0.55f);
                    if (timer <= 0f)
                    {
                        burstLeft--;
                        if (burstLeft > 0 && player != null)
                        {
                            // Otra mordida.
                            timer = 0.22f;
                            attackLanded = false;
                            lockedTarget = player.Position + Vector2.up * 0.8f + Random.insideUnitCircle * 0.4f;
                        }
                        else EndAttack(0.6f);
                    }
                    return;
                }

                case EnemyBehavior.Charger:
                {
                    bool blocked = Grounded && !GroundAhead(attackDirection);
                    Move(blocked ? Vector2.zero : new Vector2(attackDirection * data.moveSpeed * 3.4f, 0f), true);
                    shape = new Vector2(1.25f, 0.85f);
                    if (!attackLanded && player != null)
                        attackLanded = HitRect(player, Position + new Vector2(attackDirection * 0.4f, 0.5f), new Vector2(1.1f, 1f), data.attackDamage, data.attackKnockback);
                    if (timer <= 0f || blocked) EndAttack(0.7f);
                    return;
                }

                case EnemyBehavior.Engulfer:
                {
                    // Seudópodo: se estira hasta 2.8 u y te jala hacia el cuerpo.
                    Move(Vector2.zero);
                    float k = 1f - Mathf.Clamp01(timer / 0.35f);
                    float reach = Mathf.Lerp(0.6f, 2.8f, Mathf.Sin(k * Mathf.PI));
                    shape = new Vector2(1f + reach * 0.5f, 0.8f);
                    if (!attackLanded && player != null)
                    {
                        Vector2 center = Position + new Vector2(attackDirection * reach * 0.5f, 0.5f);
                        if (HitRect(player, center, new Vector2(reach + 0.4f, 0.9f), data.attackDamage, 0f))
                        {
                            attackLanded = true;
                            if (player.Motor != null) player.Motor.SetVelocity(new Vector2(-attackDirection * 8f, 2.5f));
                        }
                    }
                    if (timer <= 0f) EndAttack(0.7f);
                    return;
                }

                default:
                {
                    // Embestida corta (caminantes, racimo, oxiuro, tuberculosis cuerpo a cuerpo).
                    bool blocked = Grounded && !GroundAhead(attackDirection);
                    Move(blocked ? Vector2.zero : new Vector2(attackDirection * data.moveSpeed * 3.5f, 0f), true);
                    if (!attackLanded && player != null)
                    {
                        Vector2 size = Behavior == EnemyBehavior.Tank ? new Vector2(1.4f, 1.6f) : new Vector2(0.9f, 1.2f);
                        attackLanded = HitRect(player, Position + new Vector2(attackDirection * 0.5f, 0.6f), size, data.attackDamage, data.attackKnockback);
                    }
                    if (timer <= 0f) EndAttack(0.5f);
                    return;
                }
            }
        }

        /// <summary>Tos ferina: tres golpes de tos en cono hacia adelante; empujan lejos.</summary>
        private void UpdateCough(PlayerController player, float dt)
        {
            Move(Vector2.zero);
            burstTimer -= dt;
            shape = new Vector2(1.1f, 0.9f);
            if (burstTimer > 0f) return;

            if (burstLeft <= 0)
            {
                EndAttack(0.9f);
                return;
            }

            burstLeft--;
            burstTimer = 0.28f;
            shape = new Vector2(0.8f, 1.2f);
            Sfx.PlayAt(Sfx.EnemySound(data, "Disparo"), Position, CombatAudio.Enemies);
            if (player != null)
                HitRect(player, Position + new Vector2(attackDirection * 1.5f, 0.6f), new Vector2(2.6f, 1.6f),
                    data.attackDamage * 0.5f, data.attackKnockback);
        }

        private void UpdateRecover(PlayerController player, float dt)
        {
            timer -= dt;
            if (Behavior == EnemyBehavior.Roller)
            {
                spinSpeed = Mathf.MoveTowards(spinSpeed, 320f, 1200f * dt);
                spin -= Mathf.Sign(vel.x == 0f ? 1f : vel.x) * spinSpeed * dt;
            }
            if (IsFlying && player != null && (Behavior == EnemyBehavior.Diver || Behavior == EnemyBehavior.Frenzy || Behavior == EnemyBehavior.Roller))
            {
                // Regresa a su altura después de la picada.
                Vector2 home = new Vector2(Position.x, player.Position.y + data.flyHeight);
                Move((home - Position).normalized * data.moveSpeed);
            }
            else Move(Vector2.zero);
            if (timer <= 0f) state = State.Approach;
        }

        private void EndAttack(float recover)
        {
            state = State.Recover;
            timer = recover * 0.7f;
            attackCooldown = data.attackCooldown;
            AttackTokens.Release(this);
        }

        /// <summary>Daño por contacto (rodar, saltar encima, picada). Devuelve true si pegó.</summary>
        private bool ContactDamage(PlayerController player, float radius)
        {
            if (player == null || contactCooldown > 0f) return false;
            Hurtbox box = player.GetComponent<Hurtbox>();
            if (box == null || !box.IsAlive) return false;
            Vector2 center = IsFlying ? Position : Position + Vector2.up * 0.45f;
            if (!box.OverlapsCircle(center, radius)) return false;

            DamageInfo info = DamageInfo.Create(data.attackDamage, DamageKind.Contact, Team.Enemy, Position);
            info.knockback = data.attackKnockback;
            HitResult r = box.Damageable.ApplyDamage(info);
            contactCooldown = 0.8f;
            if (r.outcome == HitOutcome.Blocked && Behavior == EnemyBehavior.Roller)
            {
                launchVelocity = -launchVelocity * 0.6f + Vector2.up * 3f; // rebota en tu escudo
                bounces++;
            }
            return true;
        }

        private bool HitRect(PlayerController player, Vector2 center, Vector2 size, float damage, float knockback)
        {
            Hurtbox box = player.GetComponent<Hurtbox>();
            if (box == null || !box.IsAlive || !box.OverlapsRect(HitMath.FromCenter(center, size))) return false;

            DamageInfo info = DamageInfo.Create(damage, DamageKind.Contact, Team.Enemy, Position);
            info.knockback = knockback;
            HitResult result = box.Damageable.ApplyDamage(info);
            if (result.outcome == HitOutcome.Blocked && motor != null)
            {
                // Rebota contra el escudo.
                knockbackVelocity = -attackDirection * 6f;
                state = State.Hurt;
                timer = 0.35f;
                attackCooldown = data.attackCooldown;
                AttackTokens.Release(this);
            }
            return true;
        }

        /// <summary>Polen: revienta y suelta una nube que te da alergia (daño + te frena).</summary>
        private void Burst(PlayerController player)
        {
            if (player != null)
            {
                Hurtbox box = player.GetComponent<Hurtbox>();
                if (box != null && box.IsAlive && box.OverlapsCircle(Position, 1.6f))
                {
                    DamageInfo info = DamageInfo.Create(data.attackDamage, DamageKind.Contact, Team.Enemy, Position);
                    info.knockback = data.attackKnockback;
                    HitResult r = box.Damageable.ApplyDamage(info);
                    if (r.Landed && data.slowOnHit > 0f) player.ApplySlow(data.slowOnHit, data.slowDuration);
                }
            }
            health.Kill();
        }

        // ================================================================ disparar

        private void FireVolley(PlayerController player)
        {
            if (projectilePool == null) return;
            Vector2 origin = Position + new Vector2(facing * 0.4f, IsFlying ? 0f : 0.8f);
            Vector2 targetPoint = player.Position + new Vector2(0f, player.IsCrouching ? 0.5f : 1f);
            bool arc = data.projectileGravity > 0f;

            Vector2 baseDir;
            float speed = data.projectileSpeed;
            if (arc)
            {
                // Tiro parabólico que cae donde estás (alcanza plataformas).
                Vector2 d = targetPoint - origin;
                float t = Mathf.Clamp(Mathf.Abs(d.x) / 6f, 0.55f, 1.2f);
                Vector2 v = new Vector2(d.x / t, d.y / t + 0.5f * data.projectileGravity * t);
                baseDir = v.normalized;
                speed = v.magnitude;
            }
            else baseDir = IsFlying || Behavior == EnemyBehavior.Lobber ? (targetPoint - origin).normalized : new Vector2(facing, 0f);

            Sfx.PlayAt(Sfx.EnemySound(data, "Disparo"), origin, CombatAudio.Enemies);
            int shots = Mathf.Max(1, data.spreadShots);
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
            for (int i = 0; i < shots; i++)
            {
                float offset = shots > 1 ? -data.spreadDegrees * 0.5f + data.spreadDegrees * i / (shots - 1) : 0f;
                float a = (baseAngle + offset) * Mathf.Deg2Rad;
                Projectile p = projectilePool.Get();
                p.Launch(new ProjectileShot
                {
                    origin = origin,
                    direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a)),
                    speed = speed,
                    range = arc ? 30f : data.projectileRange,
                    damage = data.attackDamage,
                    knockback = data.attackKnockback,
                    radius = 0.22f,
                    sourceTeam = Team.Enemy,
                    targetTeam = Team.Player,
                    medication = null,
                    color = data.projectileColor,
                    groundMask = groundMask,
                    gravity = data.projectileGravity,
                    slowMultiplier = data.slowOnHit,
                    slowDuration = data.slowDuration
                }, finished => projectilePool.Release(finished));
            }
        }

        // ================================================================ Giardia: pegarse

        private bool TryLatch(PlayerController player)
        {
            Hurtbox box = player.GetComponent<Hurtbox>();
            if (box == null || !box.IsAlive || !box.OverlapsCircle(Position + Vector2.up * 0.5f, 0.6f)) return false;

            // El escudo la rechaza.
            DamageInfo info = DamageInfo.Create(data.attackDamage * 0.5f, DamageKind.Contact, Team.Enemy, Position);
            info.knockback = 1f;
            HitResult r = box.Damageable.ApplyDamage(info);
            if (!r.Landed)
            {
                EndAttack(0.6f);
                return true;
            }

            latchedTo = player;
            latchTimer = 1.8f;
            latchTick = 0.5f;
            if (motor != null) motor.Body.simulated = false;
            return true;
        }

        private void UpdateLatched(float dt)
        {
            PlayerController p = latchedTo;
            if (p == null || p.Health == null || !p.Health.IsAlive)
            {
                Unlatch();
                return;
            }

            // Pegada con su disco ventral a la espalda del jugador.
            transform.position = p.Position + new Vector2(-p.Motor.Facing * 0.25f, 0.9f);
            Face(p.Motor.Facing);
            shape = new Vector2(1f + Mathf.Sin(age * 25f) * 0.08f, 1f);
            p.ApplySlow(0.6f, 0.2f);

            latchTick -= dt;
            if (latchTick <= 0f)
            {
                latchTick = 0.5f;
                DamageInfo info = DamageInfo.Create(3f, DamageKind.Environment, Team.Enemy, p.Position);
                info.blockable = false;
                info.knockback = 0f;
                p.Health.ApplyDamage(info);
            }

            latchTimer -= dt;
            if (latchTimer <= 0f) Unlatch();
        }

        private void Unlatch()
        {
            latchedTo = null;
            if (motor != null)
            {
                motor.Body.simulated = true;
                motor.SetVelocity(new Vector2(-facing * 3f, 4f));
            }
            EndAttack(0.8f);
        }

        // ================================================================ habilidades especiales

        private bool TryStartSpecial(PlayerController player)
        {
            if (specialCooldown > 0f) return false;
            switch (Behavior)
            {
                case EnemyBehavior.Burrower:
                    if (!Grounded || Mathf.Abs(player.Position.y - Position.y) > 1.5f) return false;
                    BeginSpecial(0.3f);
                    return true;

                case EnemyBehavior.Sporer:
                    if (health.Health01 > 0.65f) return false;
                    BeginSpecial(data.specialDuration);
                    health.Shelled = true;
                    health.SetTint(new Color(0.92f, 0.9f, 0.8f), 0.7f);
                    return true;

                case EnemyBehavior.Tank:
                    if (health.Health01 > 0.85f) return false;
                    BeginSpecial(data.specialDuration);
                    health.Shelled = true;
                    health.SetTint(new Color(0.35f, 0.3f, 0.28f), 0.6f);
                    return true;

                case EnemyBehavior.Latent:
                    BeginSpecial(data.specialDuration);
                    health.Invulnerable = true;
                    fadeTarget = 0.12f;
                    PlayerController pc = PlayerController.Current;
                    lockedTarget = (pc != null ? pc.Position : Position)
                                   + new Vector2(Random.value < 0.5f ? -3.5f : 3.5f, data.flyHeight + Random.Range(-0.5f, 0.8f));
                    return true;
            }
            return false;
        }

        private void BeginSpecial(float duration)
        {
            state = State.Special;
            specialPhase = 0;
            timer = duration;
            health.SetTelegraph(false);
            AttackTokens.Release(this);
        }

        private void EndSpecial()
        {
            specialCooldown = data.specialCooldown;
            health.Shelled = false;
            health.Invulnerable = false;
            fadeTarget = 1f;
            state = State.Approach;
        }

        private void UpdateSpecial(PlayerController player, float dt)
        {
            timer -= dt;
            switch (Behavior)
            {
                case EnemyBehavior.Egg:
                    // Late cada vez más rápido antes de eclosionar.
                    Move(Vector2.zero);
                    float k = 1f - Mathf.Clamp01(timer / Mathf.Max(0.01f, data.specialDuration));
                    float beat = Mathf.Sin(age * Mathf.Lerp(4f, 22f, k)) * Mathf.Lerp(0.04f, 0.14f, k);
                    shape = new Vector2(1f - beat, 1f + beat);
                    if (timer <= 0f) Hatch();
                    return;

                case EnemyBehavior.Sporer:
                    Move(Vector2.zero);
                    shape = new Vector2(0.85f, 0.75f);
                    if (timer <= 0f)
                    {
                        health.SetTint(Color.white, 0f);
                        health.Heal(data.maxHealth * 0.15f); // la espora germina un poco más fuerte
                        EndSpecial();
                    }
                    return;

                case EnemyBehavior.Tank:
                    Move(Vector2.zero);
                    shape = new Vector2(1.1f, 0.92f);
                    if (timer <= 0f)
                    {
                        health.SetTint(Color.white, 0f);
                        EndSpecial();
                    }
                    return;

                case EnemyBehavior.Latent:
                {
                    // Latente: invisible e inmune, se acomoda en otra posición y reaparece disparando.
                    Vector2 to = lockedTarget - Position;
                    if (to.magnitude > 0.2f) transform.position += (Vector3)(to.normalized * data.moveSpeed * 1.6f * dt);
                    if (timer <= 0f)
                    {
                        EndSpecial();
                        if (player != null)
                        {
                            Face(player.Position.x >= Position.x ? 1 : -1);
                            StartWindup(facing, true);
                            timer = 0.35f;
                        }
                    }
                    return;
                }

                case EnemyBehavior.Burrower:
                    UpdateBurrow(player, dt);
                    return;
            }
            EndSpecial();
        }

        /// <summary>H. pylori: se mete bajo la mucosa, avanza escondido y sale debajo de ti.</summary>
        private void UpdateBurrow(PlayerController player, float dt)
        {
            switch (specialPhase)
            {
                case 0: // se mete
                    Move(Vector2.zero);
                    shape = new Vector2(1.2f, 0.6f);
                    if (timer <= 0f)
                    {
                        specialPhase = 1;
                        timer = 2.5f;
                        health.Invulnerable = true;
                        fadeTarget = 0.18f;
                    }
                    break;

                case 1: // avanza bajo la mucosa (solo se ve una sombra)
                {
                    shape = new Vector2(1.4f, 0.35f);
                    float dx = player != null ? player.Position.x - Position.x : 0f;
                    int dir = dx >= 0f ? 1 : -1;
                    bool canGo = Grounded && GroundAhead(dir, 0.2f);
                    Move(new Vector2(canGo && Mathf.Abs(dx) > 0.3f ? dir * data.moveSpeed * 1.7f : 0f, 0f));
                    if (Mathf.Abs(dx) <= 0.3f || timer <= 0f || !canGo)
                    {
                        specialPhase = 2;
                        timer = 0.55f;
                        fadeTarget = 0.6f;
                        health.SetTelegraph(true);
                    }
                    break;
                }

                case 2: // aviso: tiembla
                    Move(Vector2.zero);
                    shape = new Vector2(1.3f + Mathf.Sin(age * 40f) * 0.1f, 0.5f);
                    if (timer <= 0f)
                    {
                        // ¡Sale disparado hacia arriba!
                        health.SetTelegraph(false);
                        health.Invulnerable = false;
                        fadeTarget = 1f;
                        if (motor != null) motor.Jump(1.4f, 0.25f);
                        if (player != null)
                            HitRect(player, Position + new Vector2(0f, 0.8f), new Vector2(1.3f, 1.8f), data.attackDamage, data.attackKnockback);
                        specialPhase = 3;
                        timer = 0.6f;
                    }
                    break;

                default:
                    shape = new Vector2(0.85f, 1.25f);
                    Move(Vector2.zero);
                    if (timer <= 0f) EndSpecial();
                    break;
            }
        }

        // ================================================================ nacer / morir

        private void Hatch()
        {
            SpawnChildren(1);
            state = State.Dead;
            Destroy(gameObject); // el cascarón se rompe (no cuenta como "derrotado")
        }

        private void SpawnChildren(int count)
        {
            if (data.childPrefab == null) return;
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = new Vector2((i - (count - 1) * 0.5f) * 0.6f, 0.3f);
                GameObject go = Instantiate(data.childPrefab, (Vector3)(Position + offset), Quaternion.identity);
                EnemyHealth child = go.GetComponent<EnemyHealth>();
                EnemyBrain brain = go.GetComponent<EnemyBrain>();
                if (child != null && data.childData != null) child.Configure(data.childData, null);
                if (brain != null)
                {
                    if (data.childData != null) brain.Configure(data.childData);
                    brain.MarkOffspring();
                }
                CharacterMotor2D m = go.GetComponent<CharacterMotor2D>();
                if (m != null) m.SetVelocity(new Vector2(offset.x * 5f + Random.Range(-1f, 1f), 4f));
                ArenaZone.Adopt(child);
            }
        }

        private void OnHit(DamageInfo info, HitResult result)
        {
            if (!result.Landed || state == State.Dead) return;

            if (latchedTo != null)
            {
                Unlatch(); // un golpe la despega
                return;
            }
            if (state == State.Special || Behavior == EnemyBehavior.Egg) return;

            health.SetTelegraph(false);
            AttackTokens.Release(this);

            float dir = Position.x >= info.sourcePosition.x ? 1f : -1f;
            knockbackVelocity = dir * info.knockback * (1f - data.knockbackResistance);

            // Los pesados (y la rueda) no se interrumpen con cada golpe.
            if (Behavior == EnemyBehavior.Roller || Behavior == EnemyBehavior.Drifter) return;
            if (data.knockbackResistance < 0.8f || state != State.Windup)
            {
                state = State.Hurt;
                timer = data.hurtStunTime;
            }
        }

        private void OnDied(EnemyHealth _)
        {
            if (latchedTo != null) Unlatch();
            state = State.Dead;
            Move(Vector2.zero);
            AttackTokens.Release(this);
            enabled = false;

            // Racimo que se separa / gusano que deja huevo.
            if (!isOffspring && (Behavior == EnemyBehavior.Splitter || Behavior == EnemyBehavior.Breeder))
                SpawnChildren(Behavior == EnemyBehavior.Breeder ? 1 : data.childCount);
        }

        private void GoIdle()
        {
            state = State.Idle;
            health.SetTelegraph(false);
            AttackTokens.Release(this);
            Move(Vector2.zero);
        }
    }
}
