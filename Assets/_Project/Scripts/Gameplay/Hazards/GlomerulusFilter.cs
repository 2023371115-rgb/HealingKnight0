using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// GLOMÉRULO: un "colador" de vasitos que tapa el paso. Para filtrar hace falta PRESIÓN: con los dos botones
    /// (arteriolas) prendidos a la vez, se abre y la presión te EMPUJA a través. Si los botones se quedan prendidos
    /// (en el Riñón sí), el colador se queda ABIERTO.
    /// </summary>
    public class GlomerulusFilter : MonoBehaviour
    {
        [SerializeField] private ShootSwitch afferent, efferent;
        [SerializeField] private float height = 10f;
        [SerializeField] private float openTime = 4f;

        private BoxCollider2D wall;
        private Transform art;
        private float open, push;
        private bool told, opened;

        public void Configure(ShootSwitch a, ShootSwitch b, float wallHeight)
        {
            afferent = a; efferent = b; height = wallHeight;
        }

        private void Start()
        {
            gameObject.layer = PhysicsLayers.Ground;
            wall = gameObject.AddComponent<BoxCollider2D>();
            wall.size = new Vector2(1.4f, height);
            wall.offset = new Vector2(0f, height * 0.5f);
            art = new GameObject("Colador").transform;
            art.SetParent(transform, false);
            for (float y = 0.5f; y < height; y += 0.9f)
                for (int c = -1; c <= 1; c += 2)
                {
                    CodeSprites.Add(art, CodeSprites.Ring, new Color(0.9f, 0.3f, 0.35f), 10, new Vector2(c * 0.35f, y), new Vector2(0.8f, 0.8f), "Asa");
                }
            CodeSprites.Add(art, CodeSprites.Square, new Color(0.55f, 0.12f, 0.18f, 0.7f), 9, new Vector2(0f, height * 0.5f), new Vector2(1.4f, height), "Membrana");
        }

        private void Update()
        {
            if (wall == null) return;
            float dt = Time.deltaTime;
            bool pressure = afferent != null && efferent != null && afferent.On && efferent.On;
            if (pressure)
            {
                if (!opened)
                {
                    opened = true;
                    push = openTime;
                    Sfx.Effect("Nave_Despegue", 0.6f);
                    GameplayEvents.RaiseMessage("¡PRESIÓN! El glomérulo FILTRA: ¡pasa ya!");
                }
                else if (open <= 0f) push = openTime;
                open = Mathf.Max(open, openTime);   // mientras haya presión, sigue abierto
            }
            if (open > 0f) open -= dt;
            if (open <= 0f) opened = false;
            if (push > 0f)
            {
                push -= dt;
                // Al abrirse, la presión te empuja hacia adelante a través del colador (solo un momento).
                PlayerController player = PlayerController.Current;
                if (player != null && Mathf.Abs(player.Position.x - transform.position.x) < 3f && player.Position.x < transform.position.x + 1f)
                    player.Motor.Body.position += new Vector2(6f * dt, 0f);
            }
            bool isOpen = open > 0f;
            wall.enabled = !isOpen;
            if (art != null) art.localScale = new Vector3(isOpen ? 0.35f : 1f, 1f, 1f);

            if (!told)
            {
                PlayerController p = PlayerController.Current;
                if (p != null && Mathf.Abs(p.Position.x - transform.position.x) < 12f)
                {
                    told = true;
                    GameplayEvents.RaiseMessage("GLOMÉRULO: un colador de vasitos. Para filtrar la sangre hace falta PRESIÓN: dispárale a las DOS " +
                        "arteriolas (los botones) y cuando las dos brillen, el colador se abre.");
                }
            }
        }
    }
}
