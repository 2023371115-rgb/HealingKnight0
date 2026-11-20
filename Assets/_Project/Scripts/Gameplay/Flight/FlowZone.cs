using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// Corriente de sangre dentro de un área: empuja la nave mientras estés adentro (más fuerte al latir).
    /// Ejemplo: debajo de las ramas del arco de la aorta, parte de la sangre se va hacia arriba y te jala.
    /// Se ve como rayitas que corren en esa dirección.
    /// </summary>
    public class FlowZone : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(3f, 4f);
        [SerializeField] private Vector2 force = new Vector2(0f, 8f);
        [SerializeField] private Color streakColor = new Color(1f, 0.75f, 0.75f, 0.35f);

        private Transform[] streaks;
        private float[] offsets;

        public void Configure(Vector2 area, Vector2 push)
        {
            size = area;
            force = push;
        }

        private Rect Area => new Rect((Vector2)transform.position - size * 0.5f, size);

        private void Start()
        {
            int n = Mathf.Clamp(Mathf.RoundToInt(size.x * size.y * 0.6f), 4, 18);
            streaks = new Transform[n];
            offsets = new float[n];
            float angle = Mathf.Atan2(force.y, force.x) * Mathf.Rad2Deg;
            for (int i = 0; i < n; i++)
            {
                SpriteRenderer r = CodeSprites.Add(transform, CodeSprites.Square, streakColor, -5, Vector2.zero, new Vector2(0.9f, 0.05f), "Corriente");
                r.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                streaks[i] = r.transform;
                offsets[i] = Random.value;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float surge = Heartbeat.SurgeNow;
            Vector2 dir = force.sqrMagnitude > 0f ? force.normalized : Vector2.up;
            Vector2 side = new Vector2(-dir.y, dir.x);
            // Rayitas que corren con la corriente.
            for (int i = 0; i < streaks.Length; i++)
            {
                offsets[i] = Mathf.Repeat(offsets[i] + dt * (0.5f + surge * 0.8f), 1f);
                float lane = ((i * 0.618f) % 1f) - 0.5f;
                Vector2 extent = new Vector2(Mathf.Abs(dir.x) * size.x + Mathf.Abs(dir.y) * size.y, Mathf.Abs(dir.x) * size.y + Mathf.Abs(dir.y) * size.x);
                Vector2 p = dir * (offsets[i] - 0.5f) * extent.x + side * lane * extent.y;
                streaks[i].localPosition = new Vector3(p.x, p.y, 0f);
            }

            PlayerController player = PlayerController.Current;
            if (player == null || player.Flight == null || !player.Health.IsAlive) return;
            if (!Area.Contains(player.Position + new Vector2(0f, 0.5f))) return;
            player.Flight.AddPush(force * (0.6f + surge * 0.8f));
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.6f, 0.6f);
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(force.normalized * 1.5f));
        }
    }
}
