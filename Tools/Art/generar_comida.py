"""
Dibuja la comida flotante del Estómago en estilo caricatura (tipo Hora de Aventura):
colores planos, contorno grueso oscuro, brillos blancos simples y mordidas.

Uso:  python generar_comida.py     -> escribe Assets/_Project/Art/Comida/Comida_<Nombre>.png
Se dibuja 4 veces más grande y se reduce (bordes suaves). 256 píxeles = 1 unidad en Unity.
Requiere: numpy, pillow.
"""
import math
import os
import numpy as np
from PIL import Image, ImageDraw

AQUI = os.path.dirname(os.path.abspath(__file__))
SALIDA = os.path.join(AQUI, "..", "..", "Assets", "_Project", "Art", "Comida")

SS = 4
OL = 13 * SS                 # grosor del contorno
LINEA = (62, 10, 38, 255)    # mismo contorno que el tejido del estómago
BLANCO = (255, 248, 240, 255)


def nuevo(w, h):
    img = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def S(*v):
    return [x * SS for x in v]


def poligono_suave(cx, cy, radio, n=180):
    """Puntos alrededor de un centro con radio variable radio(ángulo)."""
    return [(cx + radio(a) * math.cos(a), cy + radio(a) * math.sin(a)) for a in np.linspace(0, 2 * math.pi, n, endpoint=False)]


def mordida(img, circulos, pan):
    """Muerde la comida: quita círculos y deja una banda de migajón claro en la orilla de la mordida.
    Después hay que pasar contorno_exterior para que la orilla mordida también lleve contorno."""
    from scipy import ndimage
    a = np.array(img)
    h, w = a.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w]
    hueco = np.zeros((h, w), bool)
    for cx, cy, r in circulos:
        hueco |= (xx - cx) ** 2 + (yy - cy) ** 2 <= r * r
    antes = a[..., 3] > 0
    a[hueco] = 0
    queda = antes & ~hueco
    cerca = ndimage.distance_transform_edt(~(hueco & antes)) <= 34 * SS + OL
    a[queda & cerca] = pan
    return Image.fromarray(a)


def contorno_exterior(img):
    """Contorno grueso alrededor de toda la silueta (por dentro), para que la orilla sea pareja."""
    from scipy import ndimage
    a = np.array(img)
    m = a[..., 3] > 128
    dist = ndimage.distance_transform_edt(m)
    borde = m & (dist <= OL)
    a[borde] = LINEA
    return Image.fromarray(a)


def guardar(img, nombre):
    w, h = img.size
    a = np.array(img).astype(np.float32)
    al = a[..., 3:4] / 255
    pm = np.concatenate([a[..., :3] * al, a[..., 3:4]], axis=2)
    pm = pm.reshape(h // SS, SS, w // SS, SS, 4).mean(axis=(1, 3))
    al2 = pm[..., 3:4] / 255
    rgb = np.where(al2 > 0, pm[..., :3] / np.maximum(al2, 1e-6), 0)
    out = np.clip(np.concatenate([rgb, pm[..., 3:4]], axis=2), 0, 255).astype(np.uint8)
    os.makedirs(SALIDA, exist_ok=True)
    Image.fromarray(out).save(os.path.join(SALIDA, f"Comida_{nombre}.png"))


# ---------------------------------------------------------------- Dona

def dona():
    w, h = 640, 600
    img, d = nuevo(w, h)
    cx, cy = w * SS / 2, h * SS * 0.52
    R, r = 300 * SS, 92 * SS
    # Masa
    d.ellipse([cx - R, cy - R * 0.92, cx + R, cy + R * 0.92], fill=(226, 154, 84, 255))
    # Sombra de la masa (parte de abajo)
    d.chord([cx - R, cy - R * 0.92, cx + R, cy + R * 0.92], 20, 160, fill=(196, 120, 62, 255))
    # Glaseado con escurridos
    def rad(a):
        drip = max(0.0, math.sin(a * 7 + 0.8)) ** 3 * 0.1 if 0.2 < a < math.pi - 0.2 else 0.0
        return R * (0.84 + 0.03 * math.sin(a * 5) + drip)
    puntos = [(cx + rad(a) * math.cos(a), cy + rad(a) * math.sin(a) * 0.92) for a in np.linspace(0, 2 * math.pi, 240, endpoint=False)]
    d.polygon(puntos, fill=(246, 128, 172, 255), outline=LINEA, width=int(OL * 0.7))
    # Brillo del glaseado
    d.arc([cx - R * 0.7, cy - R * 0.68, cx + R * 0.55, cy + R * 0.5], 195, 255, fill=(255, 214, 230, 255), width=22 * SS)
    # Chispitas
    rng = np.random.default_rng(4)
    colores = [(255, 230, 90, 255), (110, 200, 255, 255), (255, 255, 255, 255), (140, 230, 120, 255), (255, 110, 90, 255)]
    for _ in range(26):
        a = rng.uniform(0, 2 * math.pi)
        rr = rng.uniform(r * 1.5, R * 0.75)
        x, y = cx + rr * math.cos(a), cy + rr * math.sin(a) * 0.92
        ang = rng.uniform(0, math.pi)
        lx, ly = 16 * SS * math.cos(ang), 16 * SS * math.sin(ang)
        d.line([x - lx, y - ly, x + lx, y + ly], fill=LINEA, width=14 * SS)
        d.line([x - lx, y - ly, x + lx, y + ly], fill=colores[_ % len(colores)], width=8 * SS)
    # Hoyo
    d.ellipse([cx - r - OL, cy - r * 0.85 - OL, cx + r + OL, cy + r * 0.85 + OL], fill=LINEA)
    d.ellipse([cx - r, cy - r * 0.85, cx + r, cy + r * 0.85], fill=(0, 0, 0, 0))
    img = mordida(img, [(cx + R * 0.95, cy - R * 0.25, 88 * SS), (cx + R * 1.0, cy + R * 0.12, 92 * SS), (cx + R * 0.9, cy + R * 0.48, 80 * SS)],
                  (246, 214, 160, 255))
    img = contorno_exterior(img)
    guardar(img, "Dona")


# ---------------------------------------------------------------- Hamburguesa

def hamburguesa():
    w, h = 720, 560
    img, d = nuevo(w, h)
    L, Rr = 40 * SS, (w - 40) * SS
    lw = int(OL * 0.75)
    # Se dibuja de abajo hacia arriba: cada capa tapa a la anterior.
    d.rounded_rectangle([L + 20 * SS, 410 * SS, Rr - 20 * SS, 530 * SS], radius=50 * SS, fill=(226, 146, 66, 255), outline=LINEA, width=lw)
    d.rounded_rectangle([L, 300 * SS, Rr, 425 * SS], radius=60 * SS, fill=(118, 60, 40, 255), outline=LINEA, width=lw)
    for x in (140, 260, 400, 520, 600):
        d.ellipse(S(x - 14, 360, x + 14, 376), fill=(90, 42, 28, 255))
    # Queso: lámina con picos que cuelgan sobre la carne
    queso = [(L + 5 * SS, 285 * SS), (Rr - 5 * SS, 285 * SS)]
    xs = np.linspace(Rr - 5 * SS, L + 5 * SS, 9)
    for i, x in enumerate(xs):
        queso.append((x, (300 if i % 2 == 0 else 345) * SS))
    d.polygon(queso, fill=(255, 200, 50, 255), outline=LINEA, width=lw)
    # Jitomate
    d.rounded_rectangle([L + 25 * SS, 250 * SS, Rr - 25 * SS, 295 * SS], radius=22 * SS, fill=(232, 70, 60, 255), outline=LINEA, width=lw)
    # Lechuga ondulada (sobresale a los lados)
    hoja = [(L - 12 * SS, 225 * SS), (Rr + 12 * SS, 225 * SS)]
    for x in np.linspace(Rr + 12 * SS, L - 12 * SS, 50):
        hoja.append((x, 262 * SS + 14 * SS * math.sin(x / (20 * SS))))
    d.polygon(hoja, fill=(120, 200, 70, 255), outline=LINEA, width=lw)
    # Pan de arriba (domo)
    d.chord([L, 20 * SS, Rr, 470 * SS], 180, 360, fill=(232, 152, 64, 255), outline=LINEA, width=lw)
    d.chord([L + 40 * SS, 50 * SS, Rr - 140 * SS, 330 * SS], 200, 250, fill=(250, 196, 110, 255))   # brillo
    rng = np.random.default_rng(9)
    for _ in range(18):
        x, y = rng.uniform(150, 600), rng.uniform(70, 200)
        if ((x - 360) / 320) ** 2 + ((y - 245) / 225) ** 2 < 0.85:
            d.ellipse(S(x - 12, y - 7, x + 12, y + 7), fill=BLANCO, outline=LINEA, width=3 * SS)
    img = mordida(img, [(Rr + 10 * SS, 110 * SS, 80 * SS), (Rr + 20 * SS, 230 * SS, 84 * SS), (Rr + 5 * SS, 350 * SS, 78 * SS)], (250, 220, 170, 255))
    img = contorno_exterior(img)
    guardar(img, "Hamburguesa")


# ---------------------------------------------------------------- Manzana (corazón mordido)

def manzana():
    w, h = 420, 560
    img, d = nuevo(w, h)
    rojo, rojo_osc, pulpa = (226, 52, 58, 255), (180, 32, 46, 255), (250, 232, 176, 255)
    # Rabito y hoja
    d.line(S(214, 150, 230, 40), fill=LINEA, width=30 * SS)
    d.line(S(214, 150, 230, 40), fill=(120, 72, 40, 255), width=16 * SS)
    # Hoja: forma de gota acostada e inclinada
    hoja = []
    for a in np.linspace(0, 2 * math.pi, 120, endpoint=False):
        x = 75 * math.cos(a)
        y = 32 * math.sin(a) * (1 - 0.5 * math.cos(a))
        ang = -0.45
        hoja.append((SS * (300 + x * math.cos(ang) - y * math.sin(ang)), SS * (75 + x * math.sin(ang) + y * math.cos(ang))))
    d.polygon(hoja, fill=(110, 196, 80, 255), outline=LINEA, width=int(OL * 0.7))
    # Tapa de arriba
    d.chord(S(40, 110, 380, 330), 180, 360, fill=rojo, outline=LINEA, width=OL)
    d.chord(S(80, 130, 260, 250), 200, 260, fill=(255, 130, 120, 255))
    # Corazón (lados mordidos hacia adentro)
    cuerpo = []
    for y in np.linspace(220, 450, 40):
        t = (y - 220) / 230
        cuerpo.append((SS * (110 + 60 * math.sin(math.pi * t)), SS * y))
    for y in np.linspace(450, 220, 40):
        t = (y - 220) / 230
        cuerpo.append((SS * (310 - 60 * math.sin(math.pi * t)), SS * y))
    d.polygon(cuerpo, fill=pulpa, outline=LINEA, width=OL)
    # Tapa de abajo
    d.chord(S(60, 360, 360, 540), 0, 180, fill=rojo, outline=LINEA, width=OL)
    d.chord(S(100, 420, 320, 530), 20, 160, fill=rojo_osc)
    # Semillas
    for x, y, ang in ((185, 300, -0.3), (235, 318, 0.3)):
        d.ellipse(S(x - 13, y - 22, x + 13, y + 22), fill=(80, 40, 30, 255), outline=LINEA, width=4 * SS)
    img = contorno_exterior(img)
    guardar(img, "Manzana")


# ---------------------------------------------------------------- Salchicha

def salchicha():
    w, h = 900, 330
    img, d = nuevo(w, h)
    d.rounded_rectangle(S(30, 50, 870, 290), radius=120 * SS, fill=(200, 84, 52, 255), outline=LINEA, width=OL)
    d.rounded_rectangle(S(70, 200, 830, 280), radius=60 * SS, fill=(170, 62, 42, 255))           # sombra abajo
    d.rounded_rectangle(S(110, 80, 700, 120), radius=20 * SS, fill=(250, 170, 130, 255))          # brillo
    for x in range(200, 760, 110):                                                                  # marcas de parrilla
        d.line(S(x, 110, x + 60, 230), fill=(110, 36, 30, 255), width=16 * SS)
    img = contorno_exterior(img)
    guardar(img, "Salchicha")


if __name__ == "__main__":
    dona()
    hamburguesa()
    manzana()
    salchicha()
    print("Listo:", os.path.abspath(SALIDA))
