"""
Genera el arte pixel de las superficies del Estómago (piso, relleno, plataformas) y las sombras suaves.
Todo se repite sin cortes (tileable) para que Unity lo estire con drawMode = Tiled.

Uso:  python generar_tejido.py            -> escribe en Assets/_Project/Art/Tejidos/
Para otro órgano: copia una paleta en PALETAS y llama generar("Nombre", paleta).
Requiere: numpy, pillow.
"""
import os
import numpy as np
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
SALIDA = os.path.join(AQUI, "..", "..", "Assets", "_Project", "Art", "Tejidos")

# Paleta: del más oscuro al más claro. Mismo tono que el fondo, un poco más claro y saturado para resaltar.
PALETAS = {
    "Estomago": {
        "contorno": (32, 6, 15),
        "rampa": [(62, 13, 28), (88, 21, 38), (116, 30, 50), (146, 42, 63), (178, 58, 77), (212, 84, 97)],
        "brillo": (246, 172, 172),
        "moco": (234, 132, 138),
        "relleno": [(40, 8, 20), (53, 12, 26), (66, 16, 32)],
    }
}

BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0


def ruido(w, h, semilla, octavas=((2, 1.0), (4, 0.5), (8, 0.25)), periodo_x=None, periodo_y=None):
    """Ruido suave que se repite en X (y en Y si se pide): suma de senos con frecuencias enteras."""
    rng = np.random.default_rng(semilla)
    px = periodo_x or w
    py = periodo_y or h
    x = np.arange(w)[None, :]
    y = np.arange(h)[:, None]
    total = np.zeros((h, w))
    for f, amp in octavas:
        for _ in range(3):
            fx = rng.integers(1, f + 1)
            fy = rng.integers(0, f + 1) if periodo_y else rng.uniform(0, f)
            fase = rng.uniform(0, 2 * np.pi)
            total += amp * np.sin(2 * np.pi * (fx * x / px + fy * y / py) + fase)
    total -= total.min()
    return total / max(total.max(), 1e-6)


def a_rampa(valor, rampa, x0=0, y0=0):
    """Valor 0..1 -> color de la rampa con tramado ordenado (dithering), estilo pixel art."""
    h, w = valor.shape
    n = len(rampa)
    yy, xx = np.mgrid[0:h, 0:w]
    umbral = BAYER[(yy + y0) % 4, (xx + x0) % 4]
    idx = np.clip(np.floor(valor * (n - 1) + umbral * 0.9), 0, n - 1).astype(int)
    pal = np.array(rampa, dtype=np.uint8)
    return pal[idx]


def poros(img, mascara, rng, cantidad, rampa, y_min=0, y_max=None):
    """Poros como los del fondo: un punto oscuro con un pixel claro abajo (brillo en el borde)."""
    h, w = mascara.shape
    y_max = y_max or h - 1
    for _ in range(cantidad):
        x, y = int(rng.integers(0, w)), int(rng.integers(y_min, y_max))
        if mascara[y, x] and y + 1 < h and mascara[y + 1, x]:
            img[y, x, :3] = rampa[0]
            if rng.random() < 0.6 and x + 1 < w and mascara[y, x + 1]:
                img[y, x + 1, :3] = rampa[0]
            img[y + 1, x, :3] = rampa[min(len(rampa) - 1, 4)]


def bultos(w, h, centros, rx, ry, cy, periodo):
    """Sombreado de bultos redondos (como los tubos del fondo): 1 = cresta iluminada, 0 = hendidura.
    'centros' puede ser una lista de x, o de (x, rx, ry, cy) para bultos de distinto tamaño."""
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    mejor = np.zeros((h, w))
    for c in centros:
        cx, rxi, ryi, cyi = c if isinstance(c, tuple) else (c, rx, ry, cy)
        for k in (-1, 0, 1):
            dx = (xx - (cx + k * periodo)) / rxi
            dy = (yy - cyi) / ryi
            d = np.sqrt(dx ** 2 + dy ** 2)
            # Luz desde arriba a la izquierda.
            luz = np.clip(1 - d, 0, 1) * (0.75 + 0.25 * np.clip(-(dx + dy) * 0.7 + 0.3, 0, 1))
            mejor = np.maximum(mejor, luz)
    return mejor


def rollos(w, h, lista, periodo):
    """Rollos de tejido estilo pixel art (como los del fondo): cada rollo tiene borde de arriba claro,
    cuerpo medio, parte de abajo oscura y una hendidura oscura que lo separa del de al lado.
    Los rollos que van después en la lista quedan ENFRENTE. Devuelve (valor 0..1, hendidura bool, dentro bool)."""
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    valor = np.zeros((h, w))
    hend = np.zeros((h, w), bool)
    dentro = np.zeros((h, w), bool)
    for cx, rx, ry, cy in lista:
        for k in (-1, 0, 1):
            dx = (xx - (cx + k * periodo)) / rx
            dy = (yy - cy) / ry
            d = np.sqrt(dx ** 2 + dy ** 2)
            m = d <= 1.0
            # De arriba (dy=-1) a abajo (dy=+1): claro -> oscuro, con luz extra arriba a la izquierda.
            v = 0.92 - 0.55 * (dy + 1) / 2 - 0.12 * np.clip(dx, 0, 1)
            v = np.where(d > 0.86, v - 0.35, v)          # orilla del rollo más oscura (volumen)
            valor = np.where(m, v, valor)
            hend = np.where(m, d > 0.95, hend)
            dentro |= m
    return np.clip(valor, 0, 1), hend, dentro


def tubo(alto_px, filas):
    """Perfil de un tubo acostado (como los rollos del fondo): claro en el tercio de arriba, oscuro abajo."""
    t = np.clip(filas / max(alto_px, 1), 0, 1)
    return np.clip(0.55 + 0.45 * np.cos(np.pi * (t * 1.15 - 0.18)) * (1 - t * 0.35) - t * 0.25, 0, 1)


def poros_densos(img, mascara, rng, cantidad, p, y_min, y_max):
    """Poros del tejido (como en el fondo): hoyito oscuro de 1-2 px con un pixel claro debajo."""
    h, w = mascara.shape
    for _ in range(cantidad):
        x, y = int(rng.integers(0, w)), int(rng.integers(y_min, y_max))
        if not (mascara[y, x] and y + 1 < h and mascara[y + 1, x]):
            continue
        oscuro = p["rampa"][0] if rng.random() < 0.7 else p["contorno"]
        img[y, x, :3] = oscuro
        if rng.random() < 0.45:
            img[y, (x + 1) % w, :3] = oscuro
        claro = img[y + 1, x, :3].astype(int)
        img[y + 1, x, :3] = np.clip(claro + 28, 0, 255)


def piso(p, w=128, h=64):
    """Franja de arriba del piso: un gran pliegue de tejido acostado, con bultos suaves, poros y
    borde de moco húmedo. Abajo se funde con el relleno."""
    rng = np.random.default_rng(3)
    yy, xx = np.mgrid[0:h, 0:w]
    grosor = 40
    perfil = tubo(grosor, yy.astype(float))
    bultos_h = ruido(w, h, 4, ((2, 1.0), (3, 0.6)), periodo_x=w)       # el pliegue engorda y adelgaza
    pliegues = ruido(w, h, 9, ((5, 1.0), (9, 0.4)), periodo_x=w)       # surcos suaves
    v = perfil * 0.85 + (bultos_h - 0.5) * 0.25 * (yy < grosor) + (pliegues - 0.5) * 0.12
    # Sombra donde el pliegue se mete bajo el tejido de abajo.
    borde = grosor + (bultos_h[0] * 6 - 3).astype(int)
    debajo = yy > borde[None, :]
    v = np.where(debajo, 0.2 - (yy - borde[None, :]) / h * 0.4, v)
    v = np.clip(v, 0, 1)

    img = np.zeros((h, w, 4), np.uint8)
    img[..., :3] = a_rampa(v, p["rampa"])
    img[..., 3] = 255
    linea = (yy == borde[None, :]) | (yy == borde[None, :] + 1)
    img[linea, :3] = p["rampa"][0]
    poros_densos(img, ~debajo, rng, 140, p, 4, grosor)

    # Abajo: pasa al relleno con tramado (sin línea de corte).
    rel_tile = np.array(relleno(p))[..., :3]
    rel = np.tile(rel_tile, (h // rel_tile.shape[0] + 1, w // rel_tile.shape[1] + 1, 1))[:h, :w]
    fade = np.clip((yy / (h - 1) - 0.7) / 0.25, 0, 1)
    usar_rel = fade * 1.05 > BAYER[yy % 4, xx % 4]
    img[usar_rel, :3] = rel[usar_rel]

    # Borde de moco brillante y destellos húmedos.
    img[0, :, :3] = p["moco"]
    img[1, :, :3] = p["rampa"][-1]
    for x in rng.choice(w, 10, replace=False):
        img[0, x, :3] = p["brillo"]
        img[1, x, :3] = p["moco"]
        if rng.random() < 0.5:
            img[0, (x + 1) % w, :3] = p["brillo"]
    return Image.fromarray(img, "RGBA")


def relleno(p, w=64, h=64):
    """Tejido profundo: oscuro, con células grandes apenas visibles y algunos poros."""
    rng = np.random.default_rng(11)
    celdas = bultos(w, h, (8, 40), 17, 13, 14, w) * 0.6 + bultos(w, h, (24, 56), 17, 13, 46, w) * 0.6
    celdas = np.maximum(celdas, np.roll(bultos(w, h, (8, 40), 17, 13, 14, w), 32, axis=0) * 0.6)
    v = np.clip(celdas + ruido(w, h, 11, ((2, 1.0), (5, 0.5)), periodo_x=w, periodo_y=h) * 0.25, 0, 1)
    img = np.zeros((h, w, 4), np.uint8)
    img[..., :3] = a_rampa(v, p["relleno"])
    img[..., 3] = 255
    for _ in range(10):
        x, yy = rng.integers(1, w - 1), rng.integers(1, h - 2)
        img[yy, x, :3] = (20, 3, 10)
        img[yy + 1, x, :3] = p["relleno"][-1]
    return Image.fromarray(img, "RGBA")


def plataforma(p, tapa=24, centro=64, h=44):
    """Trozo de pliegue gástrico suelto: arriba plano (se pisa), sombreado de tubo, poros,
    puntas redondeadas y un par de gotas de moco colgando. Las tapas no se repiten; el centro sí."""
    w = tapa * 2 + centro
    rng = np.random.default_rng(21)
    xs = np.arange(w)

    def fondo_centro(x):  # se repite cada 'centro' píxeles
        t = 2 * np.pi * x / centro
        return 27 + 2.0 * np.sin(t * 2 + 0.6) + 1.2 * np.sin(t * 3 + 2.0)

    junta = fondo_centro(0)
    alto = np.zeros(w)
    arriba = np.zeros(w)
    for x in xs:
        if x < tapa or x >= tapa + centro:
            k = (1 - x / (tapa - 1)) if x < tapa else (x - tapa - centro) / (tapa - 1)
            arriba[x] = round(6 * k ** 2.6)
            alto[x] = junta - (junta - 9) * k ** 2.2
        else:
            alto[x] = fondo_centro(x - tapa)

    yy, xx = np.mgrid[0:h, 0:w]
    mascara = (yy >= arriba[None, :]) & (yy <= alto[None, :])
    rel_y = (yy - arriba[None, :]).astype(float)
    grosor = (alto - arriba)[None, :]
    perfil = tubo(1.0, rel_y / np.maximum(grosor, 1))
    bultos_h = ruido(w, h, 4, ((2, 1.0), (4, 0.5)), periodo_x=centro)
    v = np.clip(perfil * 0.9 + (bultos_h - 0.5) * 0.2, 0, 1)
    img = np.zeros((h, w, 4), np.uint8)
    img[..., :3] = a_rampa(v, p["rampa"])
    img[..., 3] = mascara * 255
    poros_densos(img, mascara, rng, 90, p, 3, int(junta) - 3)

    # Gotas de moco colgando: redondas, claras y un poco transparentes.
    for cx, largo in ((tapa + 17, 6), (tapa + 46, 9)):
        base = int(fondo_centro(cx - tapa))
        for y in range(base - 1, min(h - 1, base + largo + 1)):
            k = (y - base) / largo
            # Gota: ancha donde se une al tejido, se adelgaza y termina en una bolita.
            if k < 0.6:
                r = max(0.9, 3.2 * (1 - k / 0.6) ** 1.5)
            else:
                r = 1.9 * np.sqrt(max(0.0, 1 - ((k - 0.8) / 0.22) ** 2))
            for x in range(int(cx - r - 1), int(cx + r + 2)):
                if abs(x - cx) <= r:
                    img[y, x, :3] = p["moco"] if abs(x - cx) < r - 0.8 else p["rampa"][3]
                    img[y, x, 3] = 215
        img[base + largo - 1, cx - 1, :3] = p["brillo"]

    # Borde de arriba húmedo y brillos.
    for x in xs:
        t = int(arriba[x])
        img[t, x, :3] = p["moco"]
        img[t + 1, x, :3] = p["rampa"][-1]
    for x in rng.choice(np.arange(tapa, tapa + centro), 7, replace=False):
        img[int(arriba[x]), x, :3] = p["brillo"]

    # Contorno oscuro suave (no negro) por dentro de la orilla de abajo y de las puntas.
    a = img[..., 3] > 0
    vecinos = np.roll(a, 1, 0) & np.roll(a, -1, 0) & np.roll(a, 1, 1) & np.roll(a, -1, 1)
    borde = a & ~vecinos & (img[..., 3] == 255)
    borde[0, :] = False
    lado = np.zeros_like(borde)
    lado[:, tapa] = lado[:, tapa + centro - 1] = True
    borde &= ~(lado & np.roll(a, 1, 0) & np.roll(a, -1, 0))
    img[borde, :3] = p["contorno"]
    return Image.fromarray(img, "RGBA")


def sombra_suave(n=64):
    yy, xx = np.mgrid[0:n, 0:n]
    d = np.sqrt(((xx + 0.5) / n * 2 - 1) ** 2 + ((yy + 0.5) / n * 2 - 1) ** 2)
    a = np.clip(1 - d, 0, 1) ** 1.6
    img = np.zeros((n, n, 4), np.uint8)
    img[..., 3] = (a * 255).astype(np.uint8)
    return Image.fromarray(img, "RGBA")


def sombra_vertical(h=32):
    """Opaca abajo, transparente arriba (sombra de contacto sobre el fondo, junto al piso)."""
    a = (np.linspace(0, 1, h) ** 1.8)[:, None] * np.ones((1, 4))
    img = np.zeros((h, 4, 4), np.uint8)
    img[..., 3] = (a * 255).astype(np.uint8)
    return Image.fromarray(img, "RGBA")


def generar(nombre, p):
    os.makedirs(SALIDA, exist_ok=True)
    piso(p).save(os.path.join(SALIDA, f"Tejido_{nombre}_Piso.png"))
    relleno(p).save(os.path.join(SALIDA, f"Tejido_{nombre}_Relleno.png"))
    plataforma(p).save(os.path.join(SALIDA, f"Tejido_{nombre}_Plataforma.png"))


if __name__ == "__main__":
    for nombre, paleta in PALETAS.items():
        generar(nombre, paleta)
    sombra_suave().save(os.path.join(SALIDA, "Sombra_Suave.png"))
    sombra_vertical().save(os.path.join(SALIDA, "Sombra_Vertical.png"))
    print("Listo:", os.path.abspath(SALIDA))
