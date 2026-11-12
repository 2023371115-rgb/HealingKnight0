"""
Genera el arte de las superficies del Estómago (piso, relleno, plataformas) y las sombras suaves,
en estilo caricatura (tipo Hora de Aventura): colores planos, contorno grueso oscuro, formas redondas,
brillos ovalados y manchitas, como el fondo pintado.

Todo se repite sin cortes (tileable) para que Unity lo estire con drawMode = Tiled.
Se dibuja a 3x y se reduce (bordes suaves, sin pixeles).

Uso:  python generar_tejido.py            -> escribe en Assets/_Project/Art/Tejidos/
Para otro órgano: agrega una paleta en PALETAS.
Requiere: numpy, pillow, scipy.
"""
import os
import numpy as np
from PIL import Image
from scipy import ndimage

AQUI = os.path.dirname(os.path.abspath(__file__))
SALIDA = os.path.join(AQUI, "..", "..", "Assets", "_Project", "Art", "Tejidos")

SS = 3            # sobremuestreo (se dibuja 3 veces más grande y se reduce)
PPU = 256         # píxeles por unidad en Unity (SceneSetup.TissuePixelsPerUnit)

PALETAS = {
    "Estomago": {
        "contorno": (62, 10, 38),
        "cuerpo": (196, 52, 88),
        "labio": (236, 108, 126),      # borde de arriba, más claro
        "brillo": (255, 196, 198),
        "mancha": (150, 34, 70),
        "sombra": (158, 38, 74),       # parte de abajo del cuerpo
        "relleno": (122, 26, 60),
        "relleno_mancha": (100, 19, 50),
        "moco": (250, 170, 182),
    },
    # Intestino delgado: durazno y coral (con bilis amarilla).
    "Delgado": {
        "contorno": (72, 20, 22),
        "cuerpo": (226, 106, 74),
        "labio": (250, 160, 112),
        "brillo": (255, 224, 176),
        "mancha": (186, 72, 54),
        "sombra": (198, 82, 60),
        "relleno": (150, 50, 40),
        "relleno_mancha": (126, 40, 32),
        "moco": (255, 204, 150),
    },
    # Intestino grueso: malva apagado con moco dorado.
    "Grueso": {
        "contorno": (50, 18, 42),
        "cuerpo": (158, 90, 130),
        "labio": (198, 134, 168),
        "brillo": (238, 200, 216),
        "mancha": (124, 66, 102),
        "sombra": (132, 72, 110),
        "relleno": (94, 48, 78),
        "relleno_mancha": (78, 38, 64),
        "moco": (214, 186, 104),
    },
    # Nariz: ciruela y magenta, con moco amarillo lima.
    "Nariz": {
        "contorno": (40, 10, 38),
        "cuerpo": (150, 56, 132),
        "labio": (196, 96, 176),
        "brillo": (240, 172, 226),
        "mancha": (112, 38, 98),
        "sombra": (122, 42, 108),
        "relleno": (84, 28, 78),
        "relleno_mancha": (66, 20, 62),
        "moco": (206, 226, 120),
    },
    # Tráquea: índigo y violeta, con moco claro azulado.
    "Traquea": {
        "contorno": (26, 16, 56),
        "cuerpo": (112, 80, 168),
        "labio": (156, 132, 220),
        "brillo": (206, 196, 246),
        "mancha": (84, 58, 134),
        "sombra": (92, 64, 144),
        "relleno": (64, 44, 108),
        "relleno_mancha": (50, 34, 90),
        "moco": (170, 230, 242),
    },
    # Pulmones: índigo profundo con alvéolos lavanda y brillo turquesa.
    "Pulmones": {
        "contorno": (20, 14, 48),
        "cuerpo": (104, 74, 156),
        "labio": (178, 146, 224),
        "brillo": (224, 204, 248),
        "mancha": (78, 54, 124),
        "sombra": (86, 60, 132),
        "relleno": (54, 38, 96),
        "relleno_mancha": (42, 30, 80),
        "moco": (150, 240, 236),
    },
}


def lienzo(w, h, color=None):
    img = np.zeros((h, w, 4), np.float32)
    if color is not None:
        img[..., :3] = color
        img[..., 3] = 255
    return img


def pintar(img, mascara, color, alpha=255):
    img[mascara, :3] = color
    img[mascara, 3] = alpha


def elipse(h, w, cx, cy, rx, ry, periodo=None):
    """Máscara de una elipse; con 'periodo' se repite a los lados (para que el tile no se corte)."""
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    m = np.zeros((h, w), bool)
    desplaz = (-periodo, 0, periodo) if periodo else (0,)
    for d in desplaz:
        m |= ((xx - cx - d) / rx) ** 2 + ((yy - cy) / ry) ** 2 <= 1
    return m


def reducir(img):
    """De 3x al tamaño final, con el color premultiplicado para que no salgan bordes claros."""
    h, w = img.shape[:2]
    a = img[..., 3:4] / 255
    pm = np.concatenate([img[..., :3] * a, img[..., 3:4]], axis=2)
    pm = pm.reshape(h // SS, SS, w // SS, SS, 4).mean(axis=(1, 3))
    a2 = pm[..., 3:4] / 255
    rgb = np.where(a2 > 0, pm[..., :3] / np.maximum(a2, 1e-6), 0)
    return Image.fromarray(np.clip(np.concatenate([rgb, pm[..., 3:4]], axis=2), 0, 255).astype(np.uint8), "RGBA")


def onda(x, periodo, partes):
    t = 2 * np.pi * x / periodo
    return sum(a * np.sin(t * f + fase) for f, a, fase in partes)


def manchas(img, mascara, h, w, rng, n, color, rmin, rmax, y0, y1, periodo):
    for _ in range(n):
        cx, cy = rng.uniform(0, w), rng.uniform(y0, y1)
        r = rng.uniform(rmin, rmax)
        m = elipse(h, w, cx, cy, r, r * rng.uniform(0.6, 0.9), periodo) & mascara
        pintar(img, m, color)


# ---------------- Piso ----------------

def piso(p, w=512, h=256):
    """Franja de arriba del piso: contorno grueso arriba, labio claro con borde ondulado, cuerpo con
    manchas y brillos, y abajo pasa al color del relleno con una onda."""
    W, H = w * SS, h * SS
    rng = np.random.default_rng(3)
    img = lienzo(W, H, p["cuerpo"])
    xs = np.arange(W)
    yy = np.arange(H)[:, None]
    todo = np.ones((H, W), bool)

    # Abajo: relleno (con borde ondulado) y una línea de contorno delgada entre los dos.
    borde = 150 * SS + onda(xs, W, [(2, 10 * SS, 0.4), (3, 6 * SS, 1.3)])
    pintar(img, yy > borde[None, :] + 4 * SS, p["relleno"])
    pintar(img, (yy > borde[None, :]) & (yy <= borde[None, :] + 4 * SS), p["contorno"])
    # Sombra del cuerpo, encima de esa línea.
    sombra = (yy > borde[None, :] - 26 * SS) & (yy <= borde[None, :])
    pintar(img, sombra, p["sombra"])

    # Manchitas como las del fondo.
    manchas(img, yy < borde[None, :] - 30 * SS, H, W, rng, 10, p["mancha"], 6 * SS, 14 * SS, 60 * SS, 130 * SS, W)
    manchas(img, yy > borde[None, :] + 10 * SS, H, W, rng, 6, p["relleno_mancha"], 8 * SS, 16 * SS, 175 * SS, 245 * SS, W)

    # Labio claro arriba (borde de abajo ondulado) y brillos ovalados.
    labio = 14 * SS + 22 * SS + onda(xs, W, [(2, 6 * SS, 0.0), (5, 3 * SS, 2.0)])
    pintar(img, yy < labio[None, :], p["labio"])
    for cx in rng.uniform(0, W, 5):
        pintar(img, elipse(H, W, cx, 24 * SS, rng.uniform(18, 34) * SS, 5 * SS, W), p["brillo"])

    # Contorno grueso de arriba (donde se pisa).
    pintar(img, np.broadcast_to(yy < 16 * SS, (H, W)), p["contorno"])

    # El relleno al final se repite con el tile de relleno: deja las últimas filas iguales a él.
    return reducir(img)


def relleno(p, w=256, h=256):
    W, H = w * SS, h * SS
    rng = np.random.default_rng(11)
    img = lienzo(W, H, p["relleno"])
    yy, xx = np.mgrid[0:H, 0:W]
    for _ in range(7):
        cx, cy = rng.uniform(0, W), rng.uniform(0, H)
        r = rng.uniform(8, 18) * SS
        m = np.zeros((H, W), bool)
        for dx in (-W, 0, W):
            for dy in (-H, 0, H):
                m |= ((xx - cx - dx) / r) ** 2 + ((yy - cy - dy) / (r * 0.7)) ** 2 <= 1
        pintar(img, m, p["relleno_mancha"])
    return reducir(img)


# ---------------- Plataforma ----------------

def plataforma(p, tapa=96, centro=256, h=176):
    """Masa de tejido redonda: arriba plana (se pisa), puntas redondas, abajo ondulada con gotas de moco.
    Las tapas no se repiten; el centro sí (por eso se calcula con el centro repetido 3 veces)."""
    T, C, H = tapa * SS, centro * SS, h * SS
    W = T * 2 + C * 3                         # tapa + 3 centros + tapa (luego se queda el del medio)
    xs = np.arange(W).astype(np.float32)
    yy = np.arange(H)[:, None].astype(np.float32)

    arriba = np.full(W, 4.0 * SS)
    abajo = 112 * SS + onda(xs - T, C, [(2, 9 * SS, 0.6), (3, 5 * SS, 2.0)])
    # Puntas redondas: el contorno sube y se cierra como media elipse.
    for lado in (0, 1):
        x0 = T if lado == 0 else W - T
        k = np.clip((x0 - xs) / T if lado == 0 else (xs - x0) / T, 0, 1)
        curva = np.sqrt(np.clip(1 - k ** 2, 0, 1))
        medio = 60 * SS
        zona = k > 0
        arriba = np.where(zona, np.minimum(np.maximum(arriba, medio - (medio - 4 * SS) * curva), medio), arriba)
        abajo = np.where(zona, np.maximum(medio + (abajo - medio) * curva, medio), abajo)

    mascara = (yy >= arriba[None, :]) & (yy <= abajo[None, :])
    # Gotas de moco (dos por tramo del centro).
    gotas = np.zeros_like(mascara)
    for rep in range(3):
        for gx, largo in ((0.3, 34), (0.72, 50)):
            cx = T + rep * C + gx * C
            base = 112 * SS + onda(np.array([cx - T]), C, [(2, 9 * SS, 0.6), (3, 5 * SS, 2.0)])[0]
            gotas |= elipse(H, W, cx, base + largo * SS * 0.5, 11 * SS, largo * SS * 0.5)
            gotas |= elipse(H, W, cx, base + largo * SS - 8 * SS, 15 * SS, 15 * SS)
    todo = mascara | gotas

    img = lienzo(W, H)
    pintar(img, todo, p["cuerpo"])
    pintar(img, gotas & ~mascara, p["moco"])
    # Sombra en la parte de abajo y labio claro arriba.
    pintar(img, mascara & (yy > abajo[None, :] - 24 * SS), p["sombra"])
    labio = arriba + 30 * SS + onda(xs - T, C, [(3, 4 * SS, 0.0)])
    pintar(img, mascara & (yy < labio[None, :]), p["labio"])

    rng = np.random.default_rng(21)
    for rep in range(3):
        for fx, fy, r in ((0.2, 70, 9), (0.55, 82, 12), (0.85, 66, 7)):
            cx = T + rep * C + fx * C
            pintar(img, elipse(H, W, cx, fy * SS, r * SS, r * SS * 0.7) & mascara, p["mancha"])
        for fx, ancho in ((0.15, 30), (0.62, 22)):
            cx = T + rep * C + fx * C
            pintar(img, elipse(H, W, cx, 18 * SS, ancho * SS, 5 * SS) & mascara, p["brillo"])
    # Brillos en las puntas.
    pintar(img, elipse(H, W, T * 0.75, 26 * SS, 18 * SS, 6 * SS) & mascara, p["brillo"])

    # Contorno grueso por dentro de la orilla.
    dist = ndimage.distance_transform_edt(todo)
    pintar(img, todo & (dist <= 14 * SS), p["contorno"])
    # Las gotas llevan contorno más delgado.
    dist_g = ndimage.distance_transform_edt(gotas)
    fino = gotas & ~mascara & (dist_g > 8 * SS)
    pintar(img, fino, p["moco"])

    # Se queda con: tapa izquierda + centro del medio + tapa derecha.
    final = np.concatenate([img[:, :T], img[:, T + C:T + 2 * C], img[:, W - T:]], axis=1)
    return reducir(final)


# ---------------- Sombras ----------------

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
