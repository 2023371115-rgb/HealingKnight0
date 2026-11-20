"""
Corta una HOJA con varias poses del mismo personaje en un PNG por pose, todas a la MISMA escala.

    python Tools/Art/cortar_hoja.py hoja.png <ruta/Prefijo> <Pose1> <Pose2> ...

Ejemplos:
    python Tools/Art/cortar_hoja.py tcruzi.png Assets/_Project/Art/Jefes/TCruzi/TCruzi Normal Ataca Enojado Derrotado --max 900
    python Tools/Art/cortar_hoja.py ldl.png Assets/_Project/Art/Enemigos/Circulatorio/LDL Normal Ataca Dano Muere

Qué hace:
  1. Quita el fondo verde o magenta (igual que quitar_fondo_verde.py).
  2. Busca los dibujos separados de la hoja y los ordena como se lee: de izquierda a derecha y de arriba abajo
     (la primera pose de la lista es la de arriba a la izquierda). Los pedacitos sueltos (gotas, estrellitas,
     humito) se quedan con el dibujo más cercano.
  3. Si ya existe <Prefijo>_Normal.png, NO lo reemplaza: escala las demás poses para que el personaje mida lo mismo
     que en esa Normal (con --reemplazar sí guarda la Normal de la hoja).
  4. --max N: lo más grande que puede medir una pose, en px (por omisión 512; para jefes usa 900).
  5. Una pose llamada "-" se ignora (sirve para no reemplazar una que ya te gusta, por ejemplo:
     ... Ateroma Normal Ataca - Derrotado deja el Ateroma_Enojado.png que ya tienes).
Si encuentra más o menos dibujos que poses, avisa: pide la hoja con más espacio entre las poses.
Necesita: pip install pillow numpy
"""
import sys
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
from quitar_fondo_verde import quitar_fondo, recortar  # noqa: E402


def manchas(alfa, celda, juntar=2):
    """Grupos de pixeles visibles (en una rejilla de 'celda' px). Junta pedacitos que están cerca (gotas, flagelos)."""
    h, w = alfa.shape
    gh, gw = (h + celda - 1) // celda, (w + celda - 1) // celda
    rej = np.zeros((gh, gw), bool)
    ys, xs = np.nonzero(alfa > 100)
    rej[ys // celda, xs // celda] = True
    for _ in range(juntar):
        r2 = rej.copy()
        r2[1:, :] |= rej[:-1, :]
        r2[:-1, :] |= rej[1:, :]
        r2[:, 1:] |= rej[:, :-1]
        r2[:, :-1] |= rej[:, 1:]
        rej = r2
    marca = np.zeros((gh, gw), int)
    grupos = []
    for y0 in range(gh):
        for x0 in range(gw):
            if not rej[y0, x0] or marca[y0, x0]:
                continue
            n = len(grupos) + 1
            cola, celdas = deque([(y0, x0)]), []
            marca[y0, x0] = n
            while cola:
                y, x = cola.popleft()
                celdas.append((y, x))
                for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    yy, xx = y + dy, x + dx
                    if 0 <= yy < gh and 0 <= xx < gw and rej[yy, xx] and not marca[yy, xx]:
                        marca[yy, xx] = n
                        cola.append((yy, xx))
            grupos.append(np.array(celdas))
    return grupos, marca


def main():
    args = sys.argv[1:]
    maximo, reemplazar = 512, "--reemplazar" in args
    if reemplazar:
        args.remove("--reemplazar")
    if "--max" in args:
        i = args.index("--max")
        maximo = int(args[i + 1])
        del args[i:i + 2]
    if len(args) < 3:
        print(__doc__)
        return 1
    hoja, prefijo, poses = Path(args[0]), Path(args[1]), args[2:]

    limpia = quitar_fondo(Image.open(hoja))
    alfa = np.asarray(limpia.getchannel("A"))
    celda = max(2, max(alfa.shape) // 300)
    grupos, marca = manchas(alfa, celda)
    grupos.sort(key=len, reverse=True)
    if not grupos:
        print("No encontré ningún dibujo en la hoja.")
        return 1
    grandes = [g for g in grupos if len(g) >= len(grupos[0]) * 0.08]
    # Los pedacitos sueltos (gotas, estrellitas, anticuerpos, humo) van con el dibujo grande más cercano.
    cajas_g = [(g.min(0), g.max(0)) for g in grandes]
    for g in grupos[len(grandes):]:
        cy, cx = g.mean(0)
        def lejos(c):
            (y0, x0), (y1, x1) = c
            return max(y0 - cy, 0, cy - y1) ** 2 + max(x0 - cx, 0, cx - x1) ** 2
        cerca = min(range(len(grandes)), key=lambda i: lejos(cajas_g[i]))
        destino = marca[grandes[cerca][0][0], grandes[cerca][0][1]]
        marca[g[:, 0], g[:, 1]] = destino
    if len(grandes) != len(poses):
        print(f"Encontré {len(grandes)} dibujos y esperaba {len(poses)} ({', '.join(poses)}). "
              "Pide la hoja con más espacio entre las poses (o revisa que no se toquen).")
        return 1

    # Orden de lectura: renglones de arriba abajo, y en cada renglón de izquierda a derecha.
    cajas = []
    for g in grandes:
        y0, x0 = g.min(0)
        y1, x1 = g.max(0) + 1
        cajas.append((g, y0, x0, y1, x1))
    cajas.sort(key=lambda c: c[1])
    renglones = []
    for c in cajas:
        centro = (c[1] + c[3]) / 2
        if renglones and renglones[-1][0][1] <= centro <= renglones[-1][0][3]:
            renglones[-1].append(c)
        else:
            renglones.append([c])
    orden = [c for r in renglones for c in sorted(r, key=lambda c: c[2])]

    # Cada pose sola (lo de las demás poses se vuelve transparente) y recortada.
    full = np.asarray(limpia).copy()
    recortes = []
    for g, y0, x0, y1, x1 in orden:
        n = marca[g[0][0], g[0][1]]
        propia = np.kron(marca == n, np.ones((celda, celda), bool))[:full.shape[0], :full.shape[1]]
        pedazo = full.copy()
        pedazo[~propia, 3] = 0
        recortes.append(recortar(Image.fromarray(pedazo, "RGBA"), 0))

    # Una sola escala para todas: igual a la Normal que ya existe, o la que quepa en --max.
    existente = prefijo.parent / f"{prefijo.name}_Normal.png"
    k = None
    if "Normal" in poses and existente.exists() and not reemplazar:
        k = Image.open(existente).width / recortes[poses.index("Normal")].width
        print(f"Ya hay {existente.name}: las demás poses se escalan para medir lo mismo (x{k:.2f}).")
    if k is None:
        k = min(1.0, maximo / max(max(r.size) for r in recortes))
    prefijo.parent.mkdir(parents=True, exist_ok=True)
    for pose, r in zip(poses, recortes):
        destino = prefijo.parent / f"{prefijo.name}_{pose}.png"
        if pose == "-" or (pose == "Normal" and existente.exists() and not reemplazar):
            continue
        if abs(k - 1.0) > 1e-3:
            r = r.resize((max(1, round(r.width * k)), max(1, round(r.height * k))), Image.LANCZOS)
        r.save(destino)
        print("listo:", destino, r.size)
    return 0


if __name__ == "__main__":
    sys.exit(main())
