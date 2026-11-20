"""
Quita el fondo VERDE (#00FF00) —o MAGENTA (#FF00FF), para los personajes verdes— de los dibujos hechos con IA y los
deja listos para el juego. El color del fondo se adivina solo con las esquinas de la imagen.

    python Tools/Art/quitar_fondo_verde.py dibujo.png                 -> dibujo.png (lo reemplaza)
    python Tools/Art/quitar_fondo_verde.py dibujo.png salida.png      -> salida.png
    python Tools/Art/quitar_fondo_verde.py carpeta/                   -> todos los .png de la carpeta
    python Tools/Art/quitar_fondo_verde.py dibujo.png --max 512       -> además lo achica a 512 px como máximo

Qué hace:
  1. El fondo se vuelve transparente, con orillas suaves (no quedan "escalones").
  2. Quita el reflejo del fondo en las orillas (el "halo" verde o rosa).
  3. Recorta lo que sobra alrededor (deja 6 px de margen).
  4. Si el dibujo es muy grande, lo achica (--max, por omisión 640 px; 0 = no achicar).
Las imágenes que ya tienen transparencia y no tienen fondo verde se quedan igual (solo se recortan).
Necesita: pip install pillow numpy
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image


def canales_del_fondo(x):
    """Qué canales tiene el color del fondo (verde: G; magenta: R y B), viendo las cuatro esquinas."""
    h, w = x.shape[:2]
    k = max(2, min(h, w) // 40)
    esquinas = np.concatenate([x[:k, :k, :3].reshape(-1, 3), x[:k, -k:, :3].reshape(-1, 3),
                               x[-k:, :k, :3].reshape(-1, 3), x[-k:, -k:, :3].reshape(-1, 3)])
    color = np.median(esquinas, axis=0)
    llave = color > 128
    if not llave.any() or llave.all():
        return None   # el fondo no es un color de "pantalla" (ya es transparente, o es blanco/negro)
    return llave


def quitar_verde(im, maximo=640):
    """Quita el fondo, recorta y achica (lo que hace el script con cada archivo)."""
    return recortar(quitar_fondo(im), maximo)


def quitar_fondo(im):
    """Solo quita el fondo (verde o magenta) y su halo; no recorta ni cambia el tamaño."""
    x = np.asarray(im.convert("RGBA")).astype(np.float32)
    llave = canales_del_fondo(x)
    if llave is not None and x[..., 3].min() > 250:
        rgb = x[..., :3]
        dentro = rgb[..., llave].min(axis=-1)       # lo que tiene del color del fondo
        fuera = rgb[..., ~llave].max(axis=-1)       # lo que tiene de los otros colores
        fondo = dentro - fuera                      # 255 = fondo puro; cerca de 0 = dibujo
        alfa = np.clip(1.0 - (fondo - 100.0) / 70.0, 0.0, 1.0)
        x[..., 3] = np.minimum(x[..., 3], alfa * 255.0)
        # Sin reflejo del fondo: SOLO en la orilla (a 3 px o menos de lo transparente), el canal del fondo baja al
        # nivel de los otros. Adentro del dibujo los colores no se tocan (un personaje verde sigue verde).
        cerca = alfa < 0.5
        for _ in range(3):
            c2 = cerca.copy()
            c2[1:, :] |= cerca[:-1, :]
            c2[:-1, :] |= cerca[1:, :]
            c2[:, 1:] |= cerca[:, :-1]
            c2[:, :-1] |= cerca[:, 1:]
            cerca = c2
        tope = fuera + 8.0
        for c in np.where(llave)[0]:
            x[..., c] = np.where(cerca & (fondo > 30.0), np.minimum(x[..., c], tope), x[..., c])
    return Image.fromarray(np.clip(x, 0, 255).astype(np.uint8), "RGBA")


def recortar(out, maximo=640, m=6):
    """Recorta lo transparente de alrededor (deja 'm' px de margen) y achica si pasa de 'maximo'."""
    caja = out.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
    if caja:
        caja = (max(0, caja[0] - m), max(0, caja[1] - m), min(out.width, caja[2] + m), min(out.height, caja[3] + m))
        out = out.crop(caja)
    if maximo and max(out.size) > maximo:
        k = maximo / max(out.size)
        out = out.resize((max(1, round(out.width * k)), max(1, round(out.height * k))), Image.LANCZOS)
    return out


def main():
    args = [a for a in sys.argv[1:]]
    maximo = 640
    if "--max" in args:
        i = args.index("--max")
        maximo = int(args[i + 1])
        del args[i:i + 2]
    if not args:
        print(__doc__)
        return
    entrada = Path(args[0])
    if entrada.is_dir():
        archivos = sorted(entrada.glob("*.png"))
        for f in archivos:
            quitar_verde(Image.open(f), maximo).save(f)
            print("listo:", f)
        return
    salida = Path(args[1]) if len(args) > 1 else entrada
    quitar_verde(Image.open(entrada), maximo).save(salida)
    print("listo:", salida)


if __name__ == "__main__":
    main()
