using HealingKnight.Core.Combat2D;
using NUnit.Framework;
using UnityEngine;

namespace HealingKnight.Tests
{
    /// <summary>Apuntado estilo Metal Slug, salto y choques 2D.</summary>
    public class Combat2DTests
    {
        private const float Eps = 0.001f;
        private static readonly float Diag = Mathf.Sqrt(0.5f);

        private static void AreEqual(Vector2 expected, Vector2 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Eps);
            Assert.AreEqual(expected.y, actual.y, Eps);
        }

        // ---------------------------------------------------------------- apuntado

        [Test]
        public void SinTeclas_ApuntaHaciaDondeMira()
        {
            AreEqual(new Vector2(1f, 0f), AimMath.Resolve(Vector2.zero, 1, true));
            AreEqual(new Vector2(-1f, 0f), AimMath.Resolve(Vector2.zero, -1, true));
        }

        [Test]
        public void SoloArriba_ApuntaArriba()
        {
            AreEqual(Vector2.up, AimMath.Resolve(new Vector2(0f, 1f), 1, true));
        }

        [Test]
        public void ArribaMasLado_EsDiagonal()
        {
            AreEqual(new Vector2(Diag, Diag), AimMath.Resolve(new Vector2(1f, 1f), -1, true));
            AreEqual(new Vector2(-Diag, Diag), AimMath.Resolve(new Vector2(-1f, 1f), 1, false));
        }

        [Test]
        public void AbajoEnElSuelo_NoApuntaAbajo()
        {
            // En el suelo, abajo es agacharse: el disparo sale recto.
            AreEqual(new Vector2(1f, 0f), AimMath.Resolve(new Vector2(0f, -1f), 1, true));
            AreEqual(new Vector2(-1f, 0f), AimMath.Resolve(new Vector2(-1f, -1f), 1, true));
        }

        [Test]
        public void AbajoEnElAire_ApuntaAbajoODiagonal()
        {
            AreEqual(Vector2.down, AimMath.Resolve(new Vector2(0f, -1f), 1, false));
            AreEqual(new Vector2(Diag, -Diag), AimMath.Resolve(new Vector2(1f, -1f), -1, false));
        }

        [Test]
        public void StickPocoInclinado_NoCuenta()
        {
            AreEqual(new Vector2(1f, 0f), AimMath.Resolve(new Vector2(0.2f, 0.3f), 1, true));
        }

        [Test]
        public void Fijado_EnElSueloPuedeApuntarDiagonalAbajo()
        {
            AreEqual(new Vector2(Diag, -Diag), AimMath.ResolveLocked(new Vector2(1f, -1f), -1, true));
            // Solo abajo en el suelo: diagonal hacia donde mira (recto abajo pegaría en el piso).
            AreEqual(new Vector2(-Diag, -Diag), AimMath.ResolveLocked(new Vector2(0f, -1f), -1, true));
            AreEqual(Vector2.down, AimMath.ResolveLocked(new Vector2(0f, -1f), 1, false));
            AreEqual(new Vector2(Diag, Diag), AimMath.ResolveLocked(new Vector2(1f, 1f), 1, true));
        }

        [Test]
        public void ApuntadoLibre_HaciaElPunto()
        {
            AreEqual(new Vector2(0.6f, 0.8f), AimMath.Toward(new Vector2(1f, 1f), new Vector2(4f, 5f), 1));
            AreEqual(new Vector2(-1f, 0f), AimMath.Toward(Vector2.zero, Vector2.zero, -1));
        }

        // ---------------------------------------------------------------- salto

        [Test]
        public void Salto_LlegaALaAlturaPedida()
        {
            float height = 2.6f, apex = 0.38f;
            float g = JumpMath.Gravity(height, apex);
            float v = JumpMath.InitialVelocity(height, apex);
            // h = v² / 2g
            Assert.AreEqual(height, v * v / (2f * g), Eps);
            // t = v / g
            Assert.AreEqual(apex, v / g, Eps);
        }

        // ---------------------------------------------------------------- choques

        [Test]
        public void Circulo_ToquesYFallos()
        {
            Rect r = HitMath.FromCenter(new Vector2(0f, 1f), new Vector2(1f, 2f)); // x -0.5..0.5, y 0..2
            Assert.IsTrue(HitMath.CircleOverlapsRect(new Vector2(0f, 1f), 0.1f, r), "dentro");
            Assert.IsTrue(HitMath.CircleOverlapsRect(new Vector2(0.7f, 1f), 0.25f, r), "roza el lado");
            Assert.IsFalse(HitMath.CircleOverlapsRect(new Vector2(0.9f, 1f), 0.25f, r), "lejos");
            Assert.IsFalse(HitMath.CircleOverlapsRect(new Vector2(0f, 2.5f), 0.3f, r), "pasa por encima (agachado)");
        }

        [Test]
        public void Rectangulos_SeTocan()
        {
            Rect a = HitMath.FromCenter(Vector2.zero, new Vector2(2f, 2f));
            Assert.IsTrue(HitMath.RectsOverlap(a, HitMath.FromCenter(new Vector2(1.5f, 0f), new Vector2(1.2f, 1f))));
            Assert.IsFalse(HitMath.RectsOverlap(a, HitMath.FromCenter(new Vector2(3f, 0f), new Vector2(1f, 1f))));
        }

        [Test]
        public void Delante_SegunHaciaDondeMira()
        {
            Assert.IsTrue(HitMath.IsInFront(0f, 1, 2f));
            Assert.IsFalse(HitMath.IsInFront(0f, 1, -2f));
            Assert.IsTrue(HitMath.IsInFront(0f, -1, -2f));
            Assert.IsTrue(HitMath.IsInFront(0f, -1, 0.01f), "encima cuenta como delante");
        }
    }
}
