using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using TMPro;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// SANGRE ESPESA: un tramo con tanta grasa y proteínas que la sangre se pone espesa (hiperviscosidad). Mientras la
    /// pantalla va por aquí, todo avanza más lento y la nave se mueve con trabajo (sin dash).
    /// </summary>
    public class ThickBloodZone : MonoBehaviour
    {
        [SerializeField] private float fromX, toX;
        [SerializeField] private float halfHeight = 3.6f;
        [Tooltip("Cuánto se alenta la pantalla (1 = nada).")]
        [SerializeField] private float screenFactor = 0.6f;
        [Tooltip("Cuánto se alenta la nave (1 = nada).")]
        [SerializeField] private float shipFactor = 0.55f;
        [SerializeField] private int branch;
        [SerializeField, TextArea] private string message;

        private bool told, inside;
        private float t;
        private Transform blobs;
        private SpriteRenderer[] drops = new SpriteRenderer[0];
        private Vector2[] dropBase = new Vector2[0];

        public void Configure(float startX, float endX, float vesselHalfHeight, string firstMessage, int routeBranch = 0)
        {
            fromX = startX;
            toX = endX;
            halfHeight = vesselHalfHeight;
            message = firstMessage;
            branch = routeBranch;
        }

        private void Start()
        {
            // Una niebla amarillenta y gotitas de grasa flotando (de adorno: no estorban).
            blobs = new GameObject("Dibujo").transform;
            blobs.SetParent(transform, false);
            float w = toX - fromX;
            var rnd = new System.Random(Mathf.RoundToInt(fromX * 3f));
            for (float x = 0.8f; x < w; x += 2.4f)
                CodeSprites.Add(blobs, CodeSprites.Glow, new Color(0.95f, 0.75f, 0.3f, 0.22f), -60, new Vector2(x - w * 0.5f, ((float)rnd.NextDouble() - 0.5f) * halfHeight * 0.5f),
                    new Vector2(4.5f, halfHeight * 1.5f), "Niebla");
            int n = Mathf.RoundToInt(w * 1.4f);
            drops = new SpriteRenderer[n];
            dropBase = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                dropBase[i] = new Vector2((float)rnd.NextDouble() * w - w * 0.5f, ((float)rnd.NextDouble() * 2f - 1f) * halfHeight * 0.8f);
                float s = 0.15f + (float)rnd.NextDouble() * 0.25f;
                drops[i] = CodeSprites.Add(blobs, CodeSprites.Circle, new Color(1f, 0.82f, 0.35f, 0.75f), -59, dropBase[i], Vector2.one * s, "Grasa");
            }
        }

        private void Update()
        {
            FlightRoute route = FlightRoute.Current;
            if (route == null) return;
            t += Time.deltaTime;
            for (int i = 0; i < drops.Length; i++)
                if (drops[i] != null)
                    drops[i].transform.localPosition = dropBase[i] + new Vector2(Mathf.Sin(t * 0.7f + i) * 0.25f, Mathf.Cos(t * 0.5f + i * 1.3f) * 0.2f);
            if (!route.InBranch(branch)) return;

            float cx = route.CameraPosition.x;
            bool nowInside = cx > fromX && cx < toX;
            if (nowInside)
            {
                route.SlowZone(screenFactor);
                PlayerController player = PlayerController.Current;
                if (player != null && player.Flight != null && player.Position.x > fromX - 1f && player.Position.x < toX + 1f)
                    player.Flight.Slow(shipFactor);
            }
            if (nowInside && !inside)
            {
                SpeedBanner.Show("SANGRE ESPESA", new Color(1f, 0.82f, 0.4f));
                if (!told && !string.IsNullOrEmpty(message)) GameplayEvents.RaiseMessage(message);
                told = true;
            }
            inside = nowInside;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.8f);
            Gizmos.DrawWireCube(new Vector3((fromX + toX) * 0.5f, transform.position.y, 0f), new Vector3(toX - fromX, halfHeight * 2f, 0f));
        }
    }
}
