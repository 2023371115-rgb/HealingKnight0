using UnityEngine;

namespace HealingKnight.Core.Combat2D
{
    /// <summary>Choques simples en 2D (vista lateral) para disparos, golpes y explosiones.</summary>
    public static class HitMath
    {
        /// <summary>¿Un círculo toca un rectángulo?</summary>
        public static bool CircleOverlapsRect(Vector2 center, float radius, Rect rect)
        {
            float x = Mathf.Clamp(center.x, rect.xMin, rect.xMax);
            float y = Mathf.Clamp(center.y, rect.yMin, rect.yMax);
            float dx = center.x - x;
            float dy = center.y - y;
            return dx * dx + dy * dy <= radius * radius;
        }

        public static bool RectsOverlap(Rect a, Rect b)
        {
            return a.xMin <= b.xMax && a.xMax >= b.xMin && a.yMin <= b.yMax && a.yMax >= b.yMin;
        }

        /// <summary>Rectángulo a partir de un centro y un tamaño.</summary>
        public static Rect FromCenter(Vector2 center, Vector2 size)
        {
            return new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
        }

        /// <summary>¿'source' está delante de quien mira hacia 'facing' (-1 o 1)?</summary>
        public static bool IsInFront(float ownerX, int facing, float sourceX, float tolerance = 0.05f)
        {
            float dx = sourceX - ownerX;
            if (Mathf.Abs(dx) <= tolerance) return true;
            return Mathf.Sign(dx) == Mathf.Sign(facing);
        }
    }

    /// <summary>Salto con altura y tiempo al punto más alto fijos (la altura siempre es la misma).</summary>
    public static class JumpMath
    {
        public static float Gravity(float height, float timeToApex)
        {
            return 2f * height / (timeToApex * timeToApex);
        }

        public static float InitialVelocity(float height, float timeToApex)
        {
            return 2f * height / timeToApex;
        }
    }

    /// <summary>
    /// Dirección de disparo estilo Metal Slug a partir de los controles.
    /// Arriba = apuntar arriba; arriba + lado = diagonal; abajo solo en el aire (en el suelo es agacharse).
    /// </summary>
    public static class AimMath
    {
        public static Vector2 Resolve(Vector2 input, int facing, bool grounded, float deadZone = 0.5f)
        {
            bool up = input.y > deadZone;
            bool down = input.y < -deadZone;
            bool side = Mathf.Abs(input.x) > deadZone;
            int dirX = side ? (input.x > 0f ? 1 : -1) : facing;

            if (up && side) return new Vector2(dirX, 1f).normalized;
            if (up) return Vector2.up;
            if (down && !grounded && side) return new Vector2(dirX, -1f).normalized;
            if (down && !grounded) return Vector2.down;
            return new Vector2(dirX, 0f);
        }

        /// <summary>Apuntado libre (mouse o stick derecho): dirección del hombro al punto. Si está encima, hacia donde mira.</summary>
        public static Vector2 Toward(Vector2 from, Vector2 to, int facing)
        {
            Vector2 d = to - from;
            return d.sqrMagnitude < 0.0001f ? new Vector2(facing, 0f) : d.normalized;
        }

        /// <summary>
        /// Con el botón de fijar (como en Cuphead): no te mueves y apuntas libre en 8 direcciones,
        /// incluso abajo en diagonal estando en el suelo. Recto abajo solo en el aire.
        /// </summary>
        public static Vector2 ResolveLocked(Vector2 input, int facing, bool grounded, float deadZone = 0.5f)
        {
            bool up = input.y > deadZone;
            bool down = input.y < -deadZone;
            bool side = Mathf.Abs(input.x) > deadZone;
            int dirX = side ? (input.x > 0f ? 1 : -1) : facing;

            if (up && side) return new Vector2(dirX, 1f).normalized;
            if (up) return Vector2.up;
            if (down && (side || grounded)) return new Vector2(dirX, -1f).normalized;
            if (down) return Vector2.down;
            return new Vector2(dirX, 0f);
        }
    }
}
