using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// HILO DENTAL (mecánica de la Boca): con la herramienta (R / clic central / select) lanzas hilo al gancho más
    /// cercano a tu alcance. Te cuelgas como péndulo: A/D te columpian, W recoge hilo y S lo suelta. Vuelve a apretar
    /// la herramienta o salta para soltarte con impulso. Si el gancho se puede arrancar (placa), mantenerte colgado
    /// lo jala hasta arrancarlo. Biología: el hilo dental limpia entre los dientes, donde el cepillo no llega.
    /// Va un solo componente por escena; lee el botón del jugador.
    /// </summary>
    public class FlossGrapple : MonoBehaviour
    {
        [SerializeField] private float reach = 8.5f;
        [SerializeField] private float swingForce = 16f;
        [SerializeField] private float reelSpeed = 3f;
        [SerializeField] private float minLength = 1.8f;
        [SerializeField] private float maxLength = 8.5f;
        [SerializeField] private float releaseBoost = 4.5f;
        [SerializeField, TextArea] private string tutorial = "HILO DENTAL: acércate a un gancho y aprieta {herramienta}. {mover} para columpiarte, {arriba}/{abajo} para subir o bajar, {saltar} para soltarte.";

        private FlossHook hook;
        private float length;
        private LineRenderer rope;
        private Transform hint;
        private bool told;

        private void Start()
        {
            var go = new GameObject("Hilo");
            go.transform.SetParent(transform, false);
            rope = go.AddComponent<LineRenderer>();
            rope.positionCount = 2;
            rope.startWidth = rope.endWidth = 0.08f;
            rope.material = new Material(Shader.Find("Sprites/Default"));
            rope.startColor = rope.endColor = new Color(0.92f, 1f, 1f, 1f);
            rope.sortingOrder = 40;
            rope.enabled = false;

            // Flechita que marca el gancho al que te puedes enganchar.
            hint = new GameObject("Alcance").transform;
            hint.SetParent(transform, false);
            CodeSprites.Add(hint, CodeSprites.Circle, new Color(1f, 1f, 0.6f, 0.9f), 41, Vector2.zero, new Vector2(1.3f, 1.3f)).color = new Color(1f, 1f, 0.6f, 0.35f);
            hint.gameObject.SetActive(false);
        }

        private Vector2 Hand(PlayerController p) => p.Position + Vector2.up * 1f;

        private FlossHook FindTarget(PlayerController p)
        {
            FlossHook best = null;
            float bestScore = float.MaxValue;
            Vector2 hand = Hand(p);
            foreach (FlossHook h in FlossHook.All)
            {
                if (h == null || !h.isActiveAndEnabled) continue;
                Vector2 d = (Vector2)h.transform.position - hand;
                float dist = d.magnitude;
                if (dist > reach || d.y < -1.5f) continue;
                // Prefiere los que están enfrente y arriba.
                float score = dist - (Mathf.Sign(d.x) == p.Motor.Facing ? 1.5f : 0f) - Mathf.Clamp(d.y, 0f, 3f) * 0.3f;
                if (score < bestScore) { bestScore = score; best = h; }
            }
            return best;
        }

        private void Update()
        {
            PlayerController p = PlayerController.Current;
            if (p == null || p.Input == null) return;

            if (hook == null)
            {
                FlossHook target = p.Health.IsAlive ? FindTarget(p) : null;
                hint.gameObject.SetActive(target != null);
                if (target != null)
                {
                    hint.position = target.transform.position;
                    hint.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 6f) * 0.08f);
                    if (!told && !string.IsNullOrEmpty(tutorial)) { told = true; GameplayEvents.RaiseMessage(tutorial); }
                }
                if (target != null && p.Input.GadgetPressed) Attach(p, target);
                rope.enabled = false;
                return;
            }

            hint.gameObject.SetActive(false);
            if (!p.Health.IsAlive || hook == null || !hook.isActiveAndEnabled) { Release(p, false); return; }
            if (p.Input.GadgetPressed || p.Input.JumpPressed) { Release(p, true); return; }

            rope.enabled = true;
            rope.SetPosition(0, Hand(p));
            rope.SetPosition(1, hook.transform.position);

            if (hook.Rippable && hook.Pull(Time.deltaTime))
            {
                Sfx.EffectAt("Escudo_Parry", hook.transform.position, 0.8f);
                // Jalón: sales disparado hacia atrás.
                Vector2 away = (Hand(p) - (Vector2)hook.transform.position).normalized;
                Release(p, false);
                p.Motor.SetVelocity(new Vector2(away.x * 7f, 6f));
            }
        }

        private void Attach(PlayerController p, FlossHook target)
        {
            hook = target;
            hook.SetAttached(true);
            length = Mathf.Clamp(Vector2.Distance(Hand(p), hook.transform.position), minLength, maxLength);
            p.Swinging = true;
            Sfx.Effect("Jugador_Salto", 0.6f);
        }

        private void Release(PlayerController p, bool boost)
        {
            if (hook != null) hook.SetAttached(false);
            hook = null;
            rope.enabled = false;
            p.Swinging = false;
            if (boost)
            {
                Vector2 v = p.Motor.Velocity;
                p.Motor.SetVelocity(new Vector2(v.x * 1.1f, Mathf.Max(v.y, 0f) + releaseBoost));
            }
        }

        private void FixedUpdate()
        {
            PlayerController p = PlayerController.Current;
            if (hook == null || p == null || p.Motor == null) return;
            float dt = Time.fixedDeltaTime;
            Vector2 anchor = hook.transform.position;
            Vector2 hand = Hand(p);
            Vector2 offset = hand - anchor;
            float dist = offset.magnitude;
            if (dist < 0.01f) return;
            Vector2 dir = offset / dist;
            Vector2 tangent = new Vector2(-dir.y, dir.x);
            Vector2 v = p.Motor.Velocity;

            // Columpiarse con A/D; recoger/soltar hilo con W/S.
            float input = p.Input.Move.x;
            if (Mathf.Abs(input) > 0.2f) v += tangent * Mathf.Sign(Vector2.Dot(tangent, Vector2.right)) * input * swingForce * dt;
            if (p.Input.Move.y > 0.5f) length = Mathf.Max(minLength, length - reelSpeed * dt);
            else if (p.Input.Move.y < -0.5f) length = Mathf.Min(maxLength, length + reelSpeed * dt);
            else length = Mathf.MoveTowards(length, Mathf.Max(minLength, hook.RopeLength), reelSpeed * 1.5f * dt);   // el hilo te sube solo

            if (dist > length)
            {
                // Hilo tenso: regresa al círculo y quita la velocidad que lo estira.
                Vector2 fixedHand = anchor + dir * length;
                p.Motor.Body.position += fixedHand - hand;
                float radial = Vector2.Dot(v, dir);
                if (radial > 0f) v -= dir * radial;
            }
            v *= 0.998f;
            p.Motor.SetVelocity(v);
        }
    }
}
