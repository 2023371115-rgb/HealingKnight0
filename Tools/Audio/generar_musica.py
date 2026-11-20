"""
Pistas de música de ejemplo para Healing Knight (secuenciador + sintetizador hecho con numpy).

Cada sistema tiene un género electrónico que "suena" a lo que pasa en ese órgano:
  - Digestivo:    ACID HOUSE   (128 BPM): bajo "acid" burbujeante (estilo TB-303) = jugos gástricos.
  - Respiratorio: TRANCE       (138 BPM): pads que suben y bajan con el bombo = inhalar y exhalar.
  - Nervioso:     NEUROFUNK    (174 BPM): drum & bass con bajo "Reese" eléctrico y chispazos = neuronas.
Y una versión de JEFE de cada uno, más rápida y pesada.

Son loops: el final empata con el principio (los ecos y colas se "doblan" al inicio).
Son de ejemplo para escuchar la idea; Víctor puede rehacerlas en su programa de música
y basta con guardar su archivo con el mismo nombre en Assets/_Project/Audio/Resources/Music.

Uso:  python generar_musica.py                      -> todas
      python generar_musica.py Musica_Nervioso      -> solo esa
Necesita numpy, scipy y ffmpeg (para .ogg).

Cómo está armado (para jugarle):
  - Pista: un lienzo de N compases a cierto BPM; cada instrumento se "pega" en un tiempo (en pasos de 1/16).
  - Buses: drums, bass, pad, lead, fx. El pad y el bajo se "agachan" con cada bombo (sidechain = efecto de bombeo).
  - Instrumentos: kick, clap, snare, hats, acid (303), supersaw (pads de trance), pluck (arpegio),
    reese (bajo de neurofunk), zap (chispazo), burbuja (gorgoteo), respiro (ruido que sube y baja).
"""
import os, sys, wave, shutil, subprocess
import numpy as np
from scipy.signal import lfilter

SR = 44100
AQUI = os.path.dirname(os.path.abspath(__file__))
SALIDA = os.path.normpath(os.path.join(AQUI, "..", "..", "Assets", "_Project", "Audio", "Resources", "Music"))
rng = np.random.default_rng(11)


def mtof(m):
    return 440.0 * 2 ** ((m - 69) / 12.0)


# ------------------------------------------------------------------ filtros (biquad RBJ)

def biquad(kind, f, q):
    f = min(max(f, 20.0), SR * 0.45)
    w = 2 * np.pi * f / SR
    cw, sw = np.cos(w), np.sin(w)
    alpha = sw / (2 * q)
    if kind == "lp":
        b = [(1 - cw) / 2, 1 - cw, (1 - cw) / 2]
    elif kind == "hp":
        b = [(1 + cw) / 2, -(1 + cw), (1 + cw) / 2]
    else:  # bp
        b = [alpha, 0, -alpha]
    a = [1 + alpha, -2 * cw, 1 - alpha]
    return np.array(b) / a[0], np.array(a) / a[0]


def filtro(x, kind, f, q=0.707):
    b, a = biquad(kind, f, q)
    return lfilter(b, a, x)


def filtro_movil(x, cutoff, q, block=64):
    """Pasa bajos cuyo corte cambia en el tiempo (cutoff = arreglo por muestra)."""
    y = np.zeros_like(x)
    zi = np.zeros(2)
    for i in range(0, len(x), block):
        b, a = biquad("lp", float(cutoff[min(i + block // 2, len(x) - 1)]), q)
        y[i:i + block], zi = lfilter(b, a, x[i:i + block], zi=zi)
    return y


def saw(freq_array):
    phase = np.cumsum(freq_array) / SR
    return 2 * (phase % 1.0) - 1


def env_exp(n, decay):
    return np.exp(-np.arange(n) / SR / decay)


# ------------------------------------------------------------------ pista

class Pista:
    def __init__(self, bpm, compases):
        self.bpm = bpm
        self.paso = 60.0 / bpm / 4.0               # duración de 1/16 en segundos
        self.pasos = compases * 16
        self.n = int(round(self.pasos * self.paso * SR))
        self.buses = {k: np.zeros((2, self.n)) for k in ("drums", "bass", "pad", "lead", "fx")}
        self.kicks = []

    def t(self, paso):
        return int(round(paso * self.paso * SR)) % self.n

    def poner(self, bus, sig, paso, vol=1.0, pan=0.0):
        """Suma un sonido en el paso indicado. Lo que se pasa del final da la vuelta (loop perfecto)."""
        start = self.t(paso)
        sig = sig * vol
        l, r = sig * np.sqrt(0.5 * (1 - pan)), sig * np.sqrt(0.5 * (1 + pan))
        for ch, s in ((0, l), (1, r)):
            pos = start
            rest = s
            while len(rest) > 0:
                k = min(len(rest), self.n - pos)
                self.buses[bus][ch, pos:pos + k] += rest[:k]
                rest = rest[k:]
                pos = 0

    def eco(self, bus, pasos, feedback=0.35, mezcla=0.35, ancho=True):
        """Eco circular (no rompe el loop). 'ancho' alterna izquierda/derecha."""
        d = self.t(pasos)
        x = self.buses[bus].copy()
        for k in range(1, 6):
            g = mezcla * feedback ** (k - 1)
            shifted = np.roll(x, k * d, axis=1)
            if ancho and k % 2 == 1:
                shifted = shifted[::-1]
            self.buses[bus] += shifted * g

    def sidechain(self, profundidad=0.6, tiempo=0.14):
        env = np.ones(self.n)
        for p in self.kicks:
            s = self.t(p)
            k = int(tiempo * 4 * SR)
            curva = 1 - profundidad * np.exp(-np.arange(k) / SR / tiempo)
            idx = (s + np.arange(k)) % self.n
            env[idx] = np.minimum(env[idx], curva)
        return env

    def mezclar(self, vols, duck=0.6):
        env = self.sidechain(duck)
        mix = np.zeros((2, self.n))
        for bus, v in vols.items():
            sig = self.buses[bus] * v
            if bus in ("pad", "bass", "lead"):
                sig = sig * env
            mix += sig
        # Quita el retumbo inútil (<35 Hz) y baja un poco los graves para que se oigan los efectos del juego.
        mix = lfilter(*biquad("hp", 35, 0.707), mix, axis=1)
        b, a = biquad("lp", 160, 0.707)
        mix = mix - lfilter(b, a, mix, axis=1) * 0.3
        mix = np.tanh(mix * 1.1) / np.tanh(1.1)          # compresión suave
        mix = mix / (np.max(np.abs(mix)) + 1e-9) * 0.8    # deja espacio para los efectos del juego
        return mix


# ------------------------------------------------------------------ instrumentos

def kick(pesado=False):
    n = int(0.45 * SR)
    t = np.arange(n) / SR
    f = 44 + 120 * np.exp(-t * 32)
    s = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * (7 if pesado else 9))
    s[:200] += rng.uniform(-1, 1, 200) * np.linspace(0.6, 0, 200)   # clic del ataque
    if pesado:
        s = np.tanh(s * 3.2) * 0.8
    return s


def clap():
    n = int(0.25 * SR)
    ruido = filtro(rng.uniform(-1, 1, n), "bp", 1400, 1.2)
    env = np.zeros(n)
    for k, d in enumerate((0, 0.011, 0.022)):
        i = int(d * SR)
        env[i:] += np.exp(-np.arange(n - i) / SR / (0.012 if k < 2 else 0.09))
    return ruido * env * 2.2


def snare(fuerte=1.0):
    n = int(0.25 * SR)
    t = np.arange(n) / SR
    tono = np.sin(2 * np.pi * 185 * t) * np.exp(-t * 28)
    ruido = filtro(rng.uniform(-1, 1, n), "hp", 1800) * np.exp(-t * 16)
    return (tono * 0.8 + ruido * 1.1) * fuerte


def hat(abierto=False):
    n = int((0.22 if abierto else 0.05) * SR)
    ruido = filtro(rng.uniform(-1, 1, n), "hp", 7500)
    return ruido * env_exp(n, 0.07 if abierto else 0.012)


def ride():
    n = int(0.4 * SR)
    t = np.arange(n) / SR
    metal = sum(np.sign(np.sin(2 * np.pi * f * t)) for f in (523, 811, 1183, 1609))
    return filtro(metal / 4 + rng.uniform(-0.3, 0.3, n), "hp", 5000) * np.exp(-t * 6)


def supersaw(nota, dur, voces=7, detune=0.012, corte=3200, ataque=0.08):
    n = int(dur * SR)
    s = np.zeros(n)
    for v in range(voces):
        d = 1 + detune * (v - (voces - 1) / 2) / ((voces - 1) / 2)
        f = np.full(n, mtof(nota) * d)
        s += np.roll(saw(f), int(rng.uniform(0, SR / mtof(nota))))
    s = filtro(s / voces, "lp", corte, 0.8)
    env = np.ones(n)
    a = int(ataque * SR)
    env[:a] = np.linspace(0, 1, a)
    r = int(0.15 * SR)
    env[-r:] *= np.linspace(1, 0, r)
    return s * env


def pluck(nota, dur=0.22, brillo=4000):
    n = int(dur * SR)
    f = np.full(n, mtof(nota))
    s = saw(f) * 0.6 + saw(f * 1.006) * 0.4
    s = filtro_movil(s, brillo * np.exp(-np.arange(n) / SR / 0.06) + 400, 1.4)
    return s * env_exp(n, 0.09)


def zap(f0=2400, f1=90, dur=0.14):
    n = int(dur * SR)
    t = np.arange(n) / SR
    f = f1 + (f0 - f1) * np.exp(-t * 35)
    s = np.sign(np.sin(2 * np.pi * np.cumsum(f) / SR)) * np.exp(-t * 18)
    return s * 0.5 + rng.uniform(-1, 1, n) * (rng.uniform(0, 1, n) > 0.85) * np.exp(-t * 30) * 0.4


def burbuja():
    n = int(rng.uniform(0.06, 0.14) * SR)
    t = np.arange(n) / SR
    f = np.linspace(rng.uniform(150, 260), rng.uniform(500, 900), n)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.sin(np.pi * t / t[-1]) ** 2


def respiro(dur):
    """Ruido de aire que sube (inhala) y baja (exhala)."""
    n = int(dur * SR)
    t = np.linspace(0, 1, n)
    curva = np.sin(np.pi * t) ** 2
    s = filtro_movil(rng.uniform(-1, 1, n), 500 + 2200 * curva, 0.9, block=256)
    return s * curva


def acid(p, notas, corte_base, reso=7.0, drive=2.0, vol=1.0):
    """
    Bajo acid (tipo TB-303): 'notas' = lista de 16 pasos por compás repetida: (midi o None, acento, ligado).
    El corte del filtro sube y baja lento en todo el loop (el clásico "abrir el acid").
    """
    n = p.n
    freq = np.zeros(n)
    gate = np.zeros(n)
    acento = np.zeros(n)
    nota_env = np.zeros(n)
    actual = mtof(33)
    for paso in range(p.pasos):
        m, acc, lig = notas[paso]
        s, e = p.t(paso), p.t(paso + 1) if paso + 1 < p.pasos else n
        if m is None:
            freq[s:e] = actual
            continue
        objetivo = mtof(m)
        k = e - s
        if lig:
            freq[s:e] = actual + (objetivo - actual) * np.minimum(1, np.arange(k) / (0.05 * SR))
        else:
            freq[s:e] = objetivo
            nota_env[s:e] = np.exp(-np.arange(k) / SR / (0.09 if acc else 0.16))
        actual = objetivo
        largo = k if lig else int(k * 0.8)
        gate[s:s + largo] = 1.0
        acento[s:e] = 1.0 if acc else 0.0
    gate = filtro(gate, "lp", 200)
    lfo = 0.5 - 0.5 * np.cos(2 * np.pi * np.arange(n) / n)          # una "abierta" por loop
    corte = corte_base[0] + (corte_base[1] - corte_base[0]) * lfo
    corte = corte * (1 + nota_env * (1.5 + acento * 2.0))
    s = filtro_movil(saw(freq), corte, reso)
    s = np.tanh(s * drive * (1 + acento * 0.6)) * gate * vol
    return s


def reese(p, notas_por_compas, wobble_hz, corte=(180, 1600), drive=2.2):
    """Bajo Reese de neurofunk: dos sierras desafinadas + sub, filtro que se mueve y distorsión."""
    n = p.n
    freq = np.zeros(n)
    for c, m in enumerate(notas_por_compas):
        freq[p.t(c * 16):(p.t((c + 1) * 16) if (c + 1) * 16 < p.pasos else n)] = mtof(m)
    freq = filtro(freq, "lp", 30)  # pequeño glide entre notas
    s = saw(freq * 0.993) + saw(freq * 1.007)
    sub = np.sin(2 * np.pi * np.cumsum(freq) / SR)
    lfo = 0.5 + 0.5 * np.sin(2 * np.pi * wobble_hz * np.arange(n) / SR)
    s = filtro_movil(s, corte[0] + (corte[1] - corte[0]) * lfo ** 2, 2.5)
    return np.tanh(s * drive) * 0.7 + sub * 0.45


def colocar_bajo_continuo(p, sig, bus="bass", gates=None, vol=1.0):
    """Para bajos que se calculan de una pieza (acid, reese): aplica un patrón de silencios por compás."""
    if gates is not None:
        g = np.zeros(p.n)
        for c in range(p.pasos // 16):
            for a, b in gates:
                g[p.t(c * 16 + a):p.t(c * 16 + b) if c * 16 + b < p.pasos else p.n] = 1
        sig = sig * filtro(g, "lp", 120)
    p.buses[bus] += np.vstack([sig, sig]) * vol


# ------------------------------------------------------------------ pistas

def tambores_4x4(p, pesado=False, ride_on=False, clap_vol=0.8):
    for paso in range(p.pasos):
        b = paso % 16
        if b % 4 == 0:
            p.poner("drums", kick(pesado), paso, 1.0)
            p.kicks.append(paso)
        if b % 4 == 2:
            p.poner("drums", hat(True), paso, 0.35, pan=0.2)
        if b in (4, 12):
            p.poner("drums", clap(), paso, clap_vol, pan=-0.05)
        p.poner("drums", hat(), paso, 0.18 if b % 2 else 0.28, pan=-0.3)
        if ride_on and b % 4 == 2:
            p.poner("drums", ride(), paso, 0.18, pan=0.4)


def musica_digestivo(jefe=False):
    p = Pista(134 if jefe else 128, 16)
    tambores_4x4(p, pesado=jefe, ride_on=jefe)

    # Línea acid en La menor; cambia de tono cada 4 compases (La, La, Fa, Sol).
    patron = [(33, 1, 0), (33, 0, 0), (45, 0, 1), (33, 0, 0), (36, 1, 0), (None, 0, 0), (33, 0, 0), (43, 0, 1),
              (33, 1, 0), (33, 0, 0), (40, 0, 0), (33, 0, 1), (48, 1, 0), (46, 0, 1), (43, 0, 0), (36, 0, 0)]
    tonos = [0, 0, 0, 0, 0, 0, 0, 0, -4, -4, -4, -4, -2, -2, -2, -2]
    notas = []
    for c in range(16):
        for m, acc, lig in patron:
            notas.append((None if m is None else m + tonos[c], acc, lig))
    linea = acid(p, notas, (260, 2400) if not jefe else (420, 3600), reso=8 if jefe else 6.5, drive=3.2 if jefe else 2.0)
    colocar_bajo_continuo(p, linea, vol=0.75)

    # Gorgoteos (tripas) de vez en cuando.
    for c in range(16):
        for _ in range(2 if not jefe else 3):
            p.poner("fx", burbuja(), c * 16 + int(rng.integers(0, 16)), 0.25, pan=rng.uniform(-0.7, 0.7))
    p.eco("fx", 3, 0.4, 0.4)

    # Pad grave de fondo (acorde menor) para dar ambiente.
    for c in range(0, 16, 4):
        raiz = 57 + tonos[c]
        for nota in (raiz, raiz + 3, raiz + 7):
            p.poner("pad", supersaw(nota, 4 * 16 * p.paso, voces=5, corte=900, ataque=0.6), c * 16, 0.13)
    if jefe:
        # Alarma: un stab agudo cada 2 compases.
        for c in range(0, 16, 2):
            for nota in (69, 72, 76):
                p.poner("lead", pluck(nota + tonos[c], 0.35, 6000), c * 16 + 14, 0.22)
        p.eco("lead", 3, 0.45, 0.4)
    return p.mezclar({"drums": 0.85, "bass": 0.85, "pad": 1.0, "lead": 0.9, "fx": 0.7}, duck=0.45)


def musica_respiratorio(jefe=False):
    p = Pista(145 if jefe else 138, 16)
    tambores_4x4(p, pesado=jefe, clap_vol=0.7)
    progresion = [57, 53, 48, 55]  # Am, F, C, G (raíces)
    menor = {57: 3, 53: 4, 48: 4, 55: 4}  # tercera de cada acorde

    for c in range(16):
        raiz = progresion[(c // 2) % 4]
        tercera = menor[raiz]
        # Pads: suben y bajan con el bombo (respiración).
        if c % 2 == 0:
            for nota in (raiz, raiz + tercera, raiz + 7, raiz + 12):
                p.poner("pad", supersaw(nota, 2 * 16 * p.paso, corte=2600 if not jefe else 3600, ataque=0.25), c * 16, 0.16)
        # Bajo rodante de trance en las contras.
        for b in range(16):
            if b % 4 != 0:
                n = pluck(raiz - 24, 0.12, 900)
                p.poner("bass", n + np.sin(2 * np.pi * mtof(raiz - 24) * np.arange(len(n)) / SR) * env_exp(len(n), 0.08) * 0.6,
                        c * 16 + b, 0.55)
        # Arpegio en semicorcheas.
        figura = [0, tercera, 7, 12, 7, tercera, 12, 15 if tercera == 3 else 16]
        for b in range(16):
            p.poner("lead", pluck(raiz + 12 + figura[b % 8], 0.18, 5000 if jefe else 3800), c * 16 + b, 0.22 if not jefe else 0.28,
                    pan=0.35 if b % 2 else -0.35)
        # Aire: inhala en el primer compás del par, exhala en el segundo.
        if c % 2 == 0:
            p.poner("fx", respiro(2 * 16 * p.paso), c * 16, 0.35)
    p.eco("lead", 3, 0.45, 0.35)

    if jefe:
        # Melodía líder (hard trance) encima.
        melodia = [(0, 69, 4), (4, 72, 4), (8, 76, 6), (14, 74, 2), (16, 72, 4), (20, 69, 4), (24, 67, 8)]
        for vuelta in range(0, 16, 2):
            for paso, nota, largo in melodia:
                raiz = progresion[(vuelta // 2) % 4]
                p.poner("lead", supersaw(nota + (raiz - 57) % 12 - (12 if (raiz - 57) % 12 > 6 else 0),
                                         largo * p.paso, voces=5, corte=5200, ataque=0.01), vuelta * 16 + paso, 0.2)
        # Redoble de caja antes de cada 8 compases.
        for c in (7, 15):
            for b in range(8, 16):
                p.poner("drums", snare(0.4 + b * 0.05), c * 16 + b, 0.5)
    return p.mezclar({"drums": 0.8, "bass": 0.55, "pad": 1.5, "lead": 1.3, "fx": 0.6}, duck=0.7)


def musica_nervioso(jefe=False):
    p = Pista(176 if jefe else 174, 32)
    # Breakbeat de drum & bass: bombo en 1 y "y" del 3, caja en 2 y 4.
    for paso in range(p.pasos):
        b = paso % 16
        if b in (0, 10) or (jefe and b == 6 and (paso // 16) % 2):
            p.poner("drums", kick(jefe), paso, 1.0)
            p.kicks.append(paso)
        if b in (4, 12):
            p.poner("drums", snare(1.2), paso, 0.9)
        if b in (7, 15) and rng.uniform() < 0.5:
            p.poner("drums", snare(0.4), paso, 0.35)            # caja fantasma
        p.poner("drums", hat(), paso, 0.3 if b % 2 == 0 else 0.14, pan=0.25)
        if b == 14 and (paso // 16) % 4 == 3:
            p.poner("drums", hat(True), paso, 0.3, pan=-0.3)
    # Bajo Reese en Mi menor (dos compases por nota).
    notas = []
    for m in (28, 28, 31, 31, 26, 26, 24, 23):
        notas += [m, m]
    notas = (notas * 2)[:32]
    wob = p.bpm / 60.0 * (4 if jefe else 2)
    bajo = reese(p, notas, wob, corte=(160, 2400) if jefe else (180, 1500), drive=3.0 if jefe else 2.2)
    colocar_bajo_continuo(p, bajo, gates=[(0, 6), (8, 10), (11, 16)], vol=0.7)

    # Chispazos eléctricos (sinapsis) en pasos al azar.
    for c in range(32):
        for _ in range(3 if jefe else 2):
            p.poner("fx", zap(rng.uniform(1500, 4000), rng.uniform(60, 200)), c * 16 + int(rng.integers(0, 16)),
                    0.18, pan=rng.uniform(-0.8, 0.8))
    p.eco("fx", 3, 0.35, 0.3)

    # Pad atmosférico oscuro.
    for c in range(0, 32, 4):
        raiz = 52 if (c // 4) % 2 == 0 else 48
        for nota in (raiz, raiz + 3, raiz + 7, raiz + 14):
            p.poner("pad", supersaw(nota, 4 * 16 * p.paso, voces=5, corte=1400, ataque=1.0), c * 16, 0.1)
    # Pulsos agudos tipo neurona.
    for c in range(32):
        if c % 2 == 1:
            p.poner("lead", pluck(88 + int(rng.choice([0, 3, 7])), 0.1, 8000), c * 16 + 6, 0.12, pan=0.5)
    p.eco("lead", 3, 0.5, 0.45)
    if jefe:
        for c in range(7, 32, 8):
            for b in range(8, 16):
                p.poner("drums", snare(0.5 + b * 0.05), c * 16 + b, 0.55)
    return p.mezclar({"drums": 0.9, "bass": 0.85, "pad": 1.0, "lead": 1.0, "fx": 0.7}, duck=0.5)


PISTAS = {
    "Musica_Digestivo": lambda: musica_digestivo(False),
    "Musica_Digestivo_Jefe": lambda: musica_digestivo(True),
    "Musica_Respiratorio": lambda: musica_respiratorio(False),
    "Musica_Respiratorio_Jefe": lambda: musica_respiratorio(True),
    "Musica_Nervioso": lambda: musica_nervioso(False),
    "Musica_Nervioso_Jefe": lambda: musica_nervioso(True),
}


def guardar(nombre, mix):
    os.makedirs(SALIDA, exist_ok=True)
    wav = os.path.join(SALIDA, nombre + ".wav")
    datos = (np.clip(mix.T, -1, 1) * 32767).astype(np.int16)
    with wave.open(wav, "wb") as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(datos.tobytes())
    if shutil.which("ffmpeg"):
        ogg = wav[:-4] + ".ogg"
        subprocess.run(["ffmpeg", "-loglevel", "error", "-y", "-i", wav, "-c:a", "libvorbis", "-q:a", "5", ogg], check=True)
        os.remove(wav)
        print("ok", os.path.basename(ogg), f"{mix.shape[1] / SR:.1f} s")
    else:
        print("ok", os.path.basename(wav), "(instala ffmpeg para .ogg)")


if __name__ == "__main__":
    for nombre in sys.argv[1:] or list(PISTAS):
        if nombre not in PISTAS:
            print("No existe:", nombre); continue
        guardar(nombre, PISTAS[nombre]())
