using HealingKnight.Gameplay.Audio;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// Tormenta eléctrica (GDD 5.12, Cerebro): cada 6 a 10 s cae un rayo vertical de 2 u de ancho
    /// cerca del jugador. Un destello avisa durante 1 s dónde va a caer. Hay que salir de la columna.
    /// El escudo no protege. Solo funciona mientras el jugador está dentro de [minX, maxX].
    /// </summary>
    public class LightningStorm : MonoBehaviour
    {
        [SerializeField] private float minX = -10f;
        [SerializeField] private float maxX = 10f;
        [SerializeField] private float floorY = 0f;
        [SerializeField] private float columnHeight = 14f;

        [Header("Ritmo")]
        [SerializeField] private float minInterval = 6f;
        [SerializeField] private float maxInterval = 10f;
        [SerializeField] private float warningTime = 1f;
        [SerializeField] private float strikeTime = 0.3f;

        [Header("Rayo")]
        [SerializeField] private float width = 2f;
        [SerializeField] private float damage = 20f;
        [Tooltip("El rayo cae cerca del jugador, con este margen al azar.")]
        [SerializeField] private float aimJitter = 2.5f;

        [Header("Partes")]
        [SerializeField] private SpriteRenderer warningRenderer;
        [SerializeField] private SpriteRenderer boltRenderer;
        [SerializeField] private SpriteRenderer groundGlow;

        private enum State { Waiting, Warning, Striking }
        private State state = State.Waiting;
        private float timer;
        private float strikeX;
        private bool hitThisStrike;

        /// <summary>false = solo cae cuando alguien lo pide (el jefe rabia controla sus rayos).</summary>
        public bool AutoStrikes { get; set; } = true;
        public bool IsBusy => state != State.Waiting;

        public void Configure(float from, float to, float floor)
        {
            minX = from;
            maxX = to;
            floorY = floor;
        }

        /// <summary>Hace caer un rayo (con su aviso de 1 s) en la X indicada. Devuelve false si ya hay uno en camino.</summary>
        public bool ForceStrike(float x)
        {
            if (state != State.Waiting) return false;
            strikeX = Mathf.Clamp(x, minX + width * 0.5f, maxX - width * 0.5f);
            BeginWarning();
            return true;
        }

        private void Start()
        {
            timer = Random.Range(2f, minInterval);
            Show(false, false);
        }

        private void Update()
        {
            PlayerController player = PlayerController.Current;
            bool playerInside = player != null && player.Position.x >= minX && player.Position.x <= maxX;

            timer -= Time.deltaTime;
            switch (state)
            {
                case State.Waiting:
                    if (AutoStrikes && timer <= 0f && playerInside)
                    {
                        strikeX = Mathf.Clamp(player.Position.x + Random.Range(-aimJitter, aimJitter), minX + width * 0.5f, maxX - width * 0.5f);
                        BeginWarning();
                    }
                    break;

                case State.Warning:
                    if (warningRenderer != null)
                    {
                        // Parpadea cada vez más rápido.
                        float speed = Mathf.Lerp(8f, 30f, 1f - timer / warningTime);
                        Color c = warningRenderer.color;
                        c.a = 0.15f + Mathf.Abs(Mathf.Sin(Time.time * speed)) * 0.3f;
                        warningRenderer.color = c;
                    }
                    if (timer <= 0f)
                    {
                        state = State.Striking;
                        timer = strikeTime;
                        hitThisStrike = false;
                        Show(false, true);
                        Sfx.EffectAt("Rayo_Cae", new Vector2(strikeX, floorY + 2f));
                    }
                    break;

                case State.Striking:
                    if (!hitThisStrike && player != null) TryHit(player);
                    if (boltRenderer != null) boltRenderer.transform.localScale = new Vector3(width * Random.Range(0.6f, 1f), columnHeight, 1f);
                    if (timer <= 0f)
                    {
                        state = State.Waiting;
                        timer = Random.Range(minInterval, maxInterval);
                        Show(false, false);
                    }
                    break;
            }
        }

        private void BeginWarning()
        {
            state = State.Warning;
            timer = warningTime;
            Place();
            Show(true, false);
            Sfx.EffectAt("Rayo_Aviso", new Vector2(strikeX, floorY + 2f));
        }

        private void TryHit(PlayerController player)
        {
            Hurtbox box = player.GetComponent<Hurtbox>();
            if (box == null || !box.IsAlive) return;
            Rect column = new Rect(strikeX - width * 0.5f, floorY - 1f, width, columnHeight + 1f);
            if (!box.OverlapsRect(column)) return;

            DamageInfo info = DamageInfo.Create(damage, DamageKind.Contact, Team.Neutral, new Vector2(strikeX, player.Position.y));
            info.blockable = false; // el escudo no protege
            info.knockback = 5f;
            box.Damageable.ApplyDamage(info);
            hitThisStrike = true;
        }

        private void Place()
        {
            if (warningRenderer != null)
            {
                warningRenderer.transform.position = new Vector3(strikeX, floorY, 0f);
                warningRenderer.transform.localScale = new Vector3(width, columnHeight, 1f);
            }
            if (boltRenderer != null) boltRenderer.transform.position = new Vector3(strikeX, floorY, 0f);
            if (groundGlow != null) groundGlow.transform.position = new Vector3(strikeX, floorY, 0f);
        }

        private void Show(bool warning, bool bolt)
        {
            if (warningRenderer != null) warningRenderer.enabled = warning;
            if (boltRenderer != null) boltRenderer.enabled = bolt;
            if (groundGlow != null) groundGlow.enabled = warning || bolt;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.8f, 0.4f, 1f, 0.8f);
            Gizmos.DrawLine(new Vector3(minX, floorY, 0f), new Vector3(maxX, floorY, 0f));
            Gizmos.DrawLine(new Vector3(minX, floorY, 0f), new Vector3(minX, floorY + 6f, 0f));
            Gizmos.DrawLine(new Vector3(maxX, floorY, 0f), new Vector3(maxX, floorY + 6f, 0f));
        }
    }
}
