"""
AMBIENTE de cada nivel de Healing Knight: un bucle estéreo que suena bajito de fondo (además de la música).
Cada órgano suena distinto: el estómago burbujea con ácido, la nariz respira, la médula zumba con electricidad...

Sale en Assets/_Project/Audio/Resources/SFX/Ambiente_<NombreDeLaEscena>.ogg. El juego (Gameplay/Audio/Ambience.cs)
usa ese archivo si existe; si no, el del sistema (Ambiente_Digestivo, _Respiratorio, _Nervioso).
Casi todo es ruido filtrado y "eventos" sueltos (burbujas, gotas, chispas), sin notas musicales: así no choca
con la tonalidad de la música.

Uso:
    python generar_ambientes.py                       -> todos
    python generar_ambientes.py Nervioso_Medula       -> solo ese
Necesita numpy, scipy y ffmpeg. Usa las herramientas de generar_musica_adaptativa.py (misma carpeta).
"""
import shutil
import sys

import numpy as np

from generar_musica_adaptativa import (SR, campana, en_bucle, env, estereo, filtro, filtro_bucle, filtro_movil,
                                       guardar, reverb, AQUI)

SALIDA = AQUI.parent.parent / "Assets" / "_Project" / "Audio" / "Resources" / "SFX"
RMS_OBJETIVO = -22.0     # dB (los de sistema andan entre -26 y -17)
PICO_MAXIMO = 0.7


class Bucle:
    """Lienzo estéreo de 'segundos' que da la vuelta (lo que se pasa del final aparece al inicio)."""

    def __init__(self, segundos, semilla):
        self.n = int(segundos * SR)
        self.T = self.n / SR
        self.t = np.arange(self.n) / SR
        self.rng = np.random.default_rng(semilla)
        self.seco = np.zeros((self.n, 2))
        self.envio = np.zeros((self.n, 2))     # a la reverb

    def ciclo(self, veces, fase=0.0, m=None):
        """0..1 que sube y baja 'veces' en el bucle (veces enteras = empata al repetir)."""
        t = np.arange(m if m is not None else self.n) / SR
        return 0.5 - 0.5 * np.cos(2 * np.pi * (veces * t / self.T + fase))

    def textura(self, hacer, g=1.0, pan=0.0, rev=0.0):
        """hacer(m) -> señal de m muestras (mono o estéreo); se funde para que el bucle empate."""
        x = estereo(en_bucle(hacer, self.n), pan) * g
        self.seco += x
        self.envio += x * rev

    def evento(self, senal, segundo, g=1.0, pan=0.0, rev=0.0):
        x = estereo(np.asarray(senal, dtype=float), pan) * g
        i = int(segundo * SR) % self.n
        while len(x):
            k = min(len(x), self.n - i)
            self.seco[i:i + k] += x[:k]
            self.envio[i:i + k] += x[:k] * rev
            x = x[k:]
            i = 0

    def eventos(self, cuantos, hacer, g=(0.5, 1.0), rev=0.3, ancho=0.8):
        for _ in range(cuantos):
            self.evento(hacer(self.rng), self.rng.uniform(0, self.T), self.rng.uniform(*g),
                        self.rng.uniform(-ancho, ancho), rev)

    def terminar(self, sala=2.5, brillo=5000, rms=RMS_OBJETIVO):
        x = self.seco + reverb(self.envio, sala, brillo)
        x = filtro_bucle(x, 30.0, "highpass")
        x *= 10 ** ((rms - 20 * np.log10(np.sqrt(np.mean(x ** 2)) + 1e-12)) / 20)
        pico = np.max(np.abs(x))
        if pico > PICO_MAXIMO:
            x *= PICO_MAXIMO / pico
        return x.astype(np.float32)


# ---------------------------------------------------------------- sonidos sueltos

def burbuja(rng, f0=(110, 220), f1=(320, 700), largo=(0.06, 0.16)):
    """Burbuja que sube de tono y revienta."""
    d = rng.uniform(*largo)
    m = int(d * SR)
    t = np.arange(m) / SR
    a, b = rng.uniform(*f0), rng.uniform(*f1)
    return np.sin(2 * np.pi * np.cumsum(a + (b - a) * (t / d) ** 2) / SR) * np.exp(-t * 18) * env(m, 0.004, 0.02)


def racimo(rng, cuantas=(2, 5), **kw):
    """Varias burbujas seguidas (gorgoteo)."""
    partes = [burbuja(rng, **kw) for _ in range(int(rng.integers(*cuantas)))]
    m = sum(len(p) for p in partes) + int(0.4 * SR)
    x = np.zeros(m)
    i = 0
    for p in partes:
        x[i:i + len(p)] += p * rng.uniform(0.5, 1)
        i += int(len(p) * rng.uniform(0.4, 1.1))
    return x


def gota(rng, alto=(900, 1700), base=380):
    """Gota que cae: un "plic" que baja de tono muy rápido."""
    m = int(0.12 * SR)
    t = np.arange(m) / SR
    f0 = rng.uniform(*alto)
    return np.sin(2 * np.pi * np.cumsum(f0 * np.exp(-t * 14) + base) / SR) * np.exp(-t * 25)


def chispa(rng):
    """Chasquido eléctrico corto."""
    m = int(rng.uniform(0.01, 0.05) * SR)
    x = rng.normal(0, 1, m) * (rng.random(m) > 0.6)
    return filtro(x, [1500, 8000], "bandpass") * np.exp(-np.arange(m) / SR * 60)


def blip(rng, f=(1400, 3200)):
    m = int(0.04 * SR)
    t = np.arange(m) / SR
    return np.sin(2 * np.pi * rng.uniform(*f) * t) * env(m, 0.002, 0.03)


def zumbido(m, f, brillo=400.0, armonicos=(1, 2, 3)):
    t = np.arange(m) / SR
    return filtro(sum(np.sin(2 * np.pi * f * k * t) / k for k in armonicos), brillo)


def latido(b, cada=1.0, g=0.6, fase=0.0):
    """Corazón lejano (lub-dub) cada 'cada' segundos."""
    m = int(0.2 * SR)
    t = np.arange(m) / SR
    golpe = np.sin(2 * np.pi * (48 * t + 30 * (1 - np.exp(-t * 25)) / 25)) * np.exp(-t * 18)
    golpe = filtro(golpe, 160.0)
    k = fase
    while k < b.T:
        b.evento(golpe, k, g)
        b.evento(golpe * 0.7, k + 0.24, g)
        k += cada


def aire(b, m, abre, cierra, forma, q=0.9, alto=2600):
    """Aire que entra y sale: ruido con un filtro que se abre con 'forma' (0..1)."""
    r = b.rng.normal(0, 1, m)
    return filtro_movil(r, abre + (alto - abre) * forma ** 1.5, q) * (cierra + (1 - cierra) * forma)


# ---------------------------------------------------------------- recetas por nivel

def estomago(b):
    b.textura(lambda m: filtro(b.rng.normal(0, 1, m), 90.0) * 7 * (0.5 + 0.5 * b.ciclo(2, 0, m)), 0.6)   # el estómago revuelve
    b.textura(lambda m: filtro(b.rng.normal(0, 1, m) * (b.rng.random(m) > 0.93), [2500, 9000], "bandpass") * 1.2,
              0.35, rev=0.1)                                                                              # el ácido chisporrotea
    b.eventos(10, lambda r: racimo(r), (0.3, 0.7), rev=0.5)                                               # gorgoteo
    b.eventos(3, lambda r: racimo(r, (3, 6), f0=(55, 90), f1=(120, 220), largo=(0.15, 0.3)), (0.6, 1), rev=0.6)   # burbujón grave
    return b.terminar(3.0, 3500)


def intestino_delgado(b):
    # Apretones de la peristalsis (uno cada 4 s), nutrientes que corren y algún gorgoteo.
    b.textura(lambda m: filtro_movil(b.rng.normal(0, 1, m), 220 + 550 * b.ciclo(4, 0, m), 2.0, "bp") * b.ciclo(4, 0, m) ** 2,
              0.7, rev=0.3)
    b.eventos(26, lambda r: burbuja(r, (300, 500), (700, 1400), (0.03, 0.06)), (0.15, 0.35), rev=0.4)      # nutrientes que corren
    b.eventos(6, lambda r: racimo(r), (0.3, 0.6), rev=0.5)
    return b.terminar(2.2, 4000)


def intestino_grueso(b):
    b.textura(lambda m: filtro(b.rng.normal(0, 1, m), 70.0) * 10 * (0.6 + 0.4 * b.ciclo(1, 0, m)), 0.8)     # retumbo oscuro
    b.eventos(5, lambda r: racimo(r, (2, 4), f0=(50, 80), f1=(100, 180), largo=(0.2, 0.35)), (0.6, 1), rev=0.7)   # gases
    b.eventos(7, lambda r: gota(r, (500, 900), 200), (0.2, 0.4), rev=0.8)                                   # goteo en una cueva
    return b.terminar(4.0, 2500)


def boca(b):
    b.textura(lambda m: aire(b, m, 300, 0.2, b.ciclo(4, 0, m), 0.7, 1800), 0.5, rev=0.2)                  # respiración por la boca
    b.eventos(14, lambda r: gota(r, (1200, 2200), 500), (0.2, 0.45), rev=0.5)                                # gotas de saliva
    b.eventos(10, lambda r: burbuja(r, (400, 700), (900, 1600), (0.02, 0.05)), (0.1, 0.3), rev=0.3)          # burbujitas
    b.eventos(4, lambda r: filtro(r.normal(0, 1, int(0.03 * SR)), [800, 3000], "bandpass") * np.exp(-np.arange(int(0.03 * SR)) / SR * 90),
              (0.3, 0.5), rev=0.2)                                                                           # chasquido de la lengua
    return b.terminar(1.4, 5000)


def nariz(b):
    # 4 respiraciones: entra el aire (sube) y sale (baja), con un silbidito por los pelos de la nariz.
    b.textura(lambda m: aire(b, m, 700, 0.1, b.ciclo(4, 0, m), 0.8, 4000), 0.8, rev=0.15)
    b.textura(lambda m: filtro_movil(b.rng.normal(0, 1, m), 2400 + 300 * b.ciclo(4, 0, m), 14.0, "bp") * b.ciclo(4, 0, m) ** 3,
              0.15, rev=0.2)
    b.eventos(8, lambda r: chispa(r) * 0.4, (0.2, 0.5), rev=0.2)                                             # polvito que pega
    return b.terminar(1.5, 6000)


def traquea(b):
    # Un tubo: el aire resuena en sus graves (como soplar una botella) y los cilios se mueven en olas.
    for f, g in ((280, 0.6), (850, 0.35)):
        b.textura(lambda m, f=f: filtro_movil(b.rng.normal(0, 1, m), f * (1 + 0.1 * b.ciclo(4, 0, m)), 6.0, "bp") * (0.3 + 0.7 * b.ciclo(4, 0, m)),
                  g, rev=0.3)
    b.textura(lambda m: filtro(b.rng.normal(0, 1, m) * (b.rng.random(m) > 0.85), [3000, 9000], "bandpass") * (0.3 + 0.7 * b.ciclo(8, 0.25, m)),
              0.3)                                                                                           # cilios
    return b.terminar(2.5, 4000)


def pulmones(b):
    # Respira con el nivel: ciclo de 5.5 s (inhala 3.5, exhala 2) como BreathingCycle; el bucle tiene 3 ciclos.
    def respiracion(m):
        t = (np.arange(m) / SR) % 5.5
        forma = np.where(t < 3.5, np.sin(np.pi * t / 3.5 / 2) ** 2, np.cos(np.pi * (t - 3.5) / 2.0 / 2) ** 2)
        return aire(b, m, 250, 0.15, forma, 0.7, 2200) * 1.2
    b.textura(respiracion, 0.9, rev=0.25)
    for k in range(3):   # crujiditos de los alvéolos al llenarse
        for _ in range(9):
            b.evento(chispa(b.rng) * 0.5, k * 5.5 + b.rng.uniform(0.8, 3.3), b.rng.uniform(0.2, 0.5), b.rng.uniform(-0.8, 0.8), 0.3)
    latido(b, 0.917, 0.35)   # el corazón está al lado (65 latidos por minuto)
    return b.terminar(2.0, 3000)


def amigdalas(b):
    latido(b, 0.8, 0.5)     # inflamadas: laten
    b.textura(lambda m: aire(b, m, 200, 0.3, b.ciclo(4, 0, m), 0.6, 1100), 0.6, rev=0.2)                  # respiración ahogada
    b.eventos(10, lambda r: racimo(r, (1, 3), f0=(250, 400), f1=(600, 1100), largo=(0.03, 0.07)), (0.2, 0.4), rev=0.5)
    b.eventos(6, lambda r: gota(r, (800, 1400), 300), (0.2, 0.35), rev=0.6)
    return b.terminar(1.8, 3500)


def medula(b):
    b.textura(lambda m: zumbido(m, 50.0, 500.0, (1, 2, 3, 4)) * (0.7 + 0.3 * b.ciclo(4, 0, m)), 0.5)     # zumbido eléctrico
    b.textura(lambda m: filtro(b.rng.normal(0, 1, m) * (b.rng.random(m) > 0.97), [2000, 8000], "bandpass") * 2, 0.3)   # chispitas
    for _ in range(8):   # impulsos nerviosos que viajan de un lado al otro
        m = int(0.45 * SR)
        t = np.arange(m) / SR
        x = filtro_movil(b.rng.normal(0, 1, m), 900 + 2400 * t / t[-1], 4.0, "bp") * np.sin(np.pi * t / t[-1]) ** 2
        lado = b.rng.choice([-1, 1])
        pan = lado * np.linspace(-0.9, 0.9, m)
        b.evento(np.stack([x * np.sqrt((1 - pan) / 2), x * np.sqrt((1 + pan) / 2)], 1), b.rng.uniform(0, b.T), 0.5, rev=0.3)
    return b.terminar(1.6, 6000)


def cerebro(b):
    b.textura(lambda m: filtro(b.rng.normal(0, 1, m), 120.0) * 6 * (0.7 + 0.3 * b.ciclo(2, 0, m)), 0.6)    # zumbido profundo
    b.eventos(70, lambda r: blip(r) * 0.6, (0.1, 0.35), rev=0.35)                                            # las neuronas "platican"
    b.eventos(6, lambda r: chispa(r) * 2, (0.4, 0.8), rev=0.4)                                               # chispazo
    return b.terminar(2.4, 5000)


def hipocampo(b):
    b.textura(lambda m: filtro_movil(b.rng.normal(0, 1, m), 600 + 900 * b.ciclo(2, 0, m), 1.2, "bp"), 0.35, rev=0.4)   # viento suave
    escala = [72, 74, 77, 79, 81, 84, 86, 89]   # pentatónica aguda: campanitas de recuerdos (no chocan casi con nada)
    for _ in range(14):
        nota = int(b.rng.choice(escala))
        b.evento(campana(nota, 1.6, 2.0, 1.2, 2.0), b.rng.uniform(0, b.T), b.rng.uniform(0.08, 0.2), b.rng.uniform(-0.8, 0.8), 0.7)
    for _ in range(3):   # recuerdos al revés que crecen
        nota = int(b.rng.choice(escala))
        b.evento(campana(nota, 1.5, 2.0, 1.5, 1.5)[::-1], b.rng.uniform(0, b.T), 0.12, b.rng.uniform(-0.5, 0.5), 0.6)
    return b.terminar(3.5, 5000, rms=-24)


def ojo(b):
    # Sigilo: casi silencio. Un zumbido húmedo, un tono finito que va y viene y moscas volantes que pasan.
    b.textura(lambda m: filtro(b.rng.normal(0, 1, m), 90.0) * 6, 0.4)
    b.textura(lambda m: filtro_movil(b.rng.normal(0, 1, m), 5200, 25.0, "bp") * b.ciclo(2, 0, m) ** 2, 0.08, rev=0.3)
    b.textura(lambda m: filtro_movil(b.rng.normal(0, 1, m), 400 + 500 * b.ciclo(3, 0.3, m), 1.5, "bp"), 0.25, rev=0.3)
    b.eventos(4, lambda r: gota(r, (600, 1000), 250), (0.15, 0.3), rev=0.8)
    return b.terminar(3.0, 4000, rms=-27)   # más bajito: es un nivel de sigilo


AMBIENTES = {   # escena: (segundos del bucle, receta)
    "Digestivo_Estomago": (16.0, estomago),
    "Digestivo_IntestinoDelgado": (16.0, intestino_delgado),
    "Digestivo_IntestinoGrueso": (16.0, intestino_grueso),
    "Digestivo_Boca": (16.0, boca),
    "Respiratorio_Nariz": (16.0, nariz),
    "Respiratorio_Traquea": (16.0, traquea),
    "Respiratorio_Pulmones": (16.5, pulmones),
    "Respiratorio_Amigdalas": (16.0, amigdalas),
    "Nervioso_Medula": (16.0, medula),
    "Nervioso_Cerebro": (16.0, cerebro),
    "Nervioso_Hipocampo": (16.0, hipocampo),
    "Nervioso_Ojo": (16.0, ojo),
}


def main():
    nombres = sys.argv[1:] or list(AMBIENTES)
    usar_ogg = shutil.which("ffmpeg") is not None
    for k, nombre in enumerate(AMBIENTES):
        if nombre not in nombres:
            continue
        segundos, receta = AMBIENTES[nombre]
        x = receta(Bucle(segundos, 500 + k))
        guardar(SALIDA / ("Ambiente_" + nombre), x, usar_ogg)
        print("Ambiente_" + nombre, "listo", flush=True)


if __name__ == "__main__":
    main()
