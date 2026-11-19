"""
Música ADAPTATIVA de Healing Knight (sintetizador + secuenciador hecho con numpy y scipy).
Versión 5: cada sistema con SU género (el que quedó en el LEEME) y una producción más pulida.

    Digestivo     -> ACID HOUSE: bajo "acid" (TB-303) con acentos y slides = jugos gástricos que burbujean.
                     Boca 124 (rebota), Estómago 128 (clásico), Delgado 126 (groove), Grueso 130 (oscuro), Jefe 134.
    Respiratorio  -> TRANCE: pads que se abren y se cierran como una respiración, arpegios y bajo rodante.
                     Nariz 136 (aire), Tráquea 138, Pulmones 138 (respira), Amígdalas 136 (aireado),
                     Jefe 145 en PSYTRANCE.
    Nervioso      -> NEUROFUNK (drum & bass): breakbeat, bajo reese que se mueve, campanas eléctricas.
                     Médula 172 (rodante), Cerebro 174, Hipocampo 174 (memoria), Jefe 176;
                     Ojo (secreto, sigilo) 160 en LIQUID drum & bass.
    Circulatorio  -> TRIBAL en los órganos (el corazón es un tambor: taikos, djembes, marimba, coro y gritos)
                     e INDUSTRIAL en los tubos (los vasos son tuberías: bombo saturado, metales, vapor, bajo EBM).
                     Bazo 112 (tribal, aldea), Arterias y venas 124 (industrial, tubería), Corazón 136 (tribal,
                     latido: el taiko hace lub-dub cada 2 tiempos = 68 por minuto), Riñón 108 (industrial, planta
                     de agua, secreto), Gran trombo 130 (industrial, máquina) y Jefe 140 (tribal + industrial).
                     (El género "synth" —synthwave— sigue disponible en el generador, pero ningún nivel lo usa.)

Cada nivel tiene su canción, separada en 6 CAPAS que duran exactamente lo mismo y se tocan a la vez.
El juego (Scripts/Gameplay/Audio/AdaptiveMusic.cs) sube y baja cada capa según lo que pasa:

    Ambiente   -> siempre suena: zumbido del órgano y su textura (burbujas, aire, chispas, gotas).
    Armonia    -> acordes (pads, órgano de house, rhodes). Casi siempre suena.
    Melodia    -> la melodía principal / arpegio. Suave al explorar, fuerte al pelear.
    Bajo       -> entra cuando hay enemigos cerca (acid, bajo rodante, reese + sub).
    Percusion  -> la batería del género: entra cuando empieza la pelea.
    Combate    -> lo más intenso: segundo acid, lead de trance, "growl" de neurofunk, redobles y subidas.

Además, por nivel:
    Victoria   -> remate corto que suena al limpiar una arena (no se repite).
    Entrada    -> (solo en las de jefe) subida de 1 compás que termina en un golpe justo cuando entra el jefe.
Y uno general: Latido (corazón) con poca vida. Ya existe; solo se rehace con --latido.

Producción (v5):
  - Arreglo de 32 compases en 4 partes (A, A', B, B'): la melodía cambia (sube, se invierte, responde),
    la batería suma piezas, hay redobles al final de cada 8 compases, subidas de ruido antes de cada parte
    nueva y platillo al empezarla.
  - Reverb y eco ping-pong que dan la vuelta al loop: el corte del loop no se nota.
  - Bombeo (sidechain) con el bombo, swing en el house, saturación suave y un limitador suave en la batería.
  - Osciladores sin "aliasing" (polyBLEP) y filtros con resonancia que se mueven en el tiempo.

Sale en: Assets/_Project/Audio/Resources/MusicAdaptive/<NombreDeLaEscena>/<Capa>.ogg
con un manifest.json (BPM, compases y largo en muestras: con eso Unity sincroniza las capas
y cambia de canción justo al terminar un compás).

Uso:
    python generar_musica_adaptativa.py --nivel Digestivo_Estomago --preview
    python generar_musica_adaptativa.py --todos
Necesita: numpy, scipy y ffmpeg (para .ogg). Tarda ~1 min por nivel.

Por qué .ogg y no .wav: en WAV las 6 capas de los 15 niveles pesarían cientos de MB en GitHub;
en OGG ~60 MB. Unity los vuelve a comprimir igual al importarlos.
"""
import argparse
import json
import shutil
import subprocess
import wave
import zlib
from dataclasses import dataclass
from functools import lru_cache
from multiprocessing import Pool
from pathlib import Path

import numpy as np
from scipy.signal import butter, fftconvolve, lfilter, sosfilt

SR = 44100                       # muestras por segundo
AQUI = Path(__file__).resolve().parent
SALIDA = AQUI.parent.parent / "Assets" / "_Project" / "Audio" / "Resources" / "MusicAdaptive"
CAPAS = ("Ambiente", "Armonia", "Melodia", "Bajo", "Percusion", "Combate")

# Volumen de cada capa (RMS en dB): así ninguna tapa a las demás y cada nivel de intensidad suena completo.
VOLUMEN_CAPA = {"Ambiente": -25, "Armonia": -24, "Melodia": -25, "Bajo": -22, "Percusion": -21, "Combate": -25}

# Reverb de cada capa: (segundos que tarda en apagarse, qué tan brillante en Hz).
REVERB = {"Ambiente": (4.0, 4500), "Armonia": (3.2, 6000), "Melodia": (2.3, 7500),
          "Bajo": (0.8, 1500), "Percusion": (1.1, 8000), "Combate": (1.8, 7000)}

# ---------------------------------------------------------------- escalas (semitonos desde la tónica)
MENOR = [0, 2, 3, 5, 7, 8, 10]
FRIGIO = [0, 1, 3, 5, 7, 8, 10]        # oscuro, "árabe": acidez
DORICO = [0, 2, 3, 5, 7, 9, 10]        # menor pero luminoso
LOCRIO = [0, 1, 3, 5, 6, 8, 10]        # muy tenso
LIDIO = [0, 2, 4, 6, 7, 9, 11]         # flotante, aire
MENOR_ARMONICA = [0, 2, 3, 5, 7, 8, 11]
FRIGIO_DOMINANTE = [0, 1, 3, 5, 7, 8, 11]
LOCRIO_TENSO = [0, 1, 3, 5, 6, 8, 11]


@dataclass
class Nivel:
    bpm: int
    tonica: int          # nota MIDI de la tónica (solo cuenta la nota; el registro lo pone cada instrumento)
    escala: list
    acordes: list        # grados de la escala para los acordes (cambian cada 2 compases)
    motivo: list         # grados de la melodía (sobre el acorde que suena)
    ritmo: list          # en qué dieciseisavos del compás cae cada nota del motivo
    genero: str          # acid, trance, psy, neuro, liquid, synth, tribal, industrial
    sabor: str = ""      # variante dentro del género (ver NIVELES)
    swing: float = 0.0   # cuánto se atrasan los dieciseisavos impares (0 recto, 0.2 = shuffle de house)


# Las claves son los nombres de las escenas (así el juego encuentra la carpeta sola).
# Las de jefe son "<Sistema>_Jefe".
NIVELES = {
    # ---- Digestivo: ACID HOUSE
    "Digestivo_Boca":             Nivel(124, 45, DORICO,           [0, 3, 6, 3], [0, 2, 4, 2, 5, 4], [0, 3, 6, 8, 11, 14], "acid", "rebote", 0.20),
    "Digestivo_Estomago":         Nivel(128, 38, FRIGIO,           [0, 1, 0, 6], [0, 2, 1, 4, 3, 1], [0, 3, 7, 10, 13, 15], "acid", "clasico", 0.10),
    "Digestivo_IntestinoDelgado": Nivel(126, 40, DORICO,           [0, 3, 1, 4], [0, 4, 5, 2, 3, 6], [1, 4, 6, 9, 12, 14], "acid", "groove", 0.16),
    "Digestivo_IntestinoGrueso":  Nivel(130, 36, LOCRIO,           [0, 5, 1, 4], [0, 4, 1, 0, 5, 2], [0, 5, 8, 11, 14, 15], "acid", "oscuro"),
    "Digestivo_Jefe":             Nivel(134, 38, FRIGIO_DOMINANTE, [0, 1, 0, 4], [0, 1, 4, 6, 5, 1], [0, 2, 5, 8, 10, 14], "acid", "jefe", 0.06),
    # ---- Respiratorio: TRANCE (jefe en psytrance)
    "Respiratorio_Nariz":         Nivel(136, 50, LIDIO,            [0, 1, 5, 4], [4, 5, 6, 3, 2, 1], [0, 4, 7, 9, 12, 15], "trance", "aire"),
    "Respiratorio_Traquea":       Nivel(138, 43, DORICO,           [0, 6, 3, 4], [0, 2, 4, 5, 4, 2], [0, 3, 6, 8, 11, 14], "trance", "clasico"),
    "Respiratorio_Pulmones":      Nivel(138, 45, MENOR,            [0, 5, 2, 6], [0, 4, 7, 6, 4, 2], [0, 2, 6, 9, 12, 14], "trance", "respira"),
    "Respiratorio_Amigdalas":     Nivel(136, 45, DORICO,           [0, 3, 6, 4], [2, 4, 5, 4, 2, 0], [0, 3, 6, 10, 12, 14], "trance", "aireado"),
    "Respiratorio_Jefe":          Nivel(145, 41, FRIGIO_DOMINANTE, [0, 1, 0, 6], [0, 4, 1, 6, 3, 1], [0, 1, 4, 7, 10, 13], "psy", "jefe"),
    # ---- Nervioso: NEUROFUNK (Ojo en liquid drum & bass)
    "Nervioso_Medula":            Nivel(172, 40, DORICO,           [0, 3, 0, 6], [0, 2, 0, 4, 3, 5], [0, 3, 5, 8, 11, 14], "neuro", "rodante"),
    "Nervioso_Cerebro":           Nivel(174, 40, FRIGIO,           [0, 5, 2, 4], [0, 4, 1, 5, 3, 6], [0, 2, 7, 9, 11, 15], "neuro", "clasico"),
    "Nervioso_Hipocampo":         Nivel(174, 46, MENOR,            [0, 2, 5, 3], [4, 7, 6, 2, 3, 1], [0, 4, 6, 10, 12, 15], "neuro", "memoria"),
    "Nervioso_Ojo":               Nivel(160, 42, MENOR_ARMONICA,   [0, 5, 3, 4], [4, 2, 1, 0, 1, 2], [0, 6, 8, 12, 14, 15], "liquid", "sigilo", 0.08),
    "Nervioso_Jefe":              Nivel(176, 40, LOCRIO_TENSO,     [0, 1, 4, 6], [0, 4, 6, 1, 5, 0], [0, 1, 5, 8, 12, 14], "neuro", "jefe"),
    # ---- Circulatorio: TRIBAL (órganos) e INDUSTRIAL (vasos); el jefe mezcla los dos
    "Circulatorio_Bazo":          Nivel(112, 43, DORICO,           [0, 3, 0, 6], [0, 2, 3, 4, 2, 0], [0, 3, 6, 8, 12, 14], "tribal", "aldea"),
    "Circulatorio_ArteriasVenas": Nivel(124, 45, FRIGIO,           [0, 1, 0, 5], [0, 1, 4, 0, 6, 4], [0, 3, 6, 8, 11, 14], "industrial", "tuberia"),
    "Circulatorio_Corazon":       Nivel(136, 40, MENOR,            [0, 5, 6, 4], [4, 3, 2, 0, 2, 3], [0, 4, 6, 8, 12, 14], "tribal", "latido"),
    "Circulatorio_Rinon":         Nivel(108, 47, DORICO,           [0, 3, 5, 4], [4, 2, 0, 2, 4, 6], [0, 4, 7, 8, 11, 14], "industrial", "planta"),
    "Circulatorio_GranTrombo":    Nivel(130, 38, LOCRIO,           [0, 1, 5, 4], [0, 1, 4, 3, 1, 0], [0, 3, 6, 8, 11, 14], "industrial", "maquina"),
    "Circulatorio_Jefe":          Nivel(140, 40, FRIGIO_DOMINANTE, [0, 1, 0, 6], [0, 4, 1, 6, 4, 1], [0, 2, 6, 8, 10, 14], "tribal", "guerra"),
}


# ================================================================ utilidades de sonido

def hz(midi):
    """Nota MIDI -> frecuencia (69 = La 440 Hz)."""
    return 440.0 * 2 ** ((np.asarray(midi, dtype=float) - 69) / 12)


def _sos(corte, tipo, orden):
    return butter(orden, corte, fs=SR, btype=tipo, output="sos")


def filtro(x, corte, tipo="lowpass", orden=2):
    """Filtro fijo para un sonido suelto. tipo: lowpass, highpass o bandpass (corte = [bajo, alto])."""
    return sosfilt(_sos(corte, tipo, orden), x, axis=0)


def filtro_bucle(x, corte, tipo="lowpass", orden=2):
    """Igual, pero para una capa en loop: se "calienta" con el último segundo, así el inicio empata con el final."""
    m = min(len(x), SR)
    return sosfilt(_sos(corte, tipo, orden), np.concatenate([x[-m:], x]), axis=0)[m:]


def filtro_movil(x, corte, q=0.707, tipo="lp", bloque=32):
    """Filtro con resonancia cuyo corte cambia en el tiempo ('corte' y 'q': número o Hz por muestra). Mono.
    tipo "lp" = pasa bajos (el "wah" del acid), "bp" = pasa banda (formantes, viento)."""
    x = np.asarray(x, dtype=float)
    n = len(x)
    medio = np.minimum(np.arange(0, n, bloque) + bloque // 2, n - 1)
    f = np.clip(np.broadcast_to(np.asarray(corte, dtype=float), (n,))[medio], 25, 18000)
    q = np.broadcast_to(np.asarray(q, dtype=float), (n,))[medio]
    w = 2 * np.pi * f / SR
    c, alfa = np.cos(w), np.sin(w) / (2 * q)
    if tipo == "lp":
        b = np.stack([(1 - c) / 2, 1 - c, (1 - c) / 2], 1)
    else:
        b = np.stack([alfa, np.zeros_like(alfa), -alfa], 1)
    a = np.stack([1 + alfa, -2 * c, 1 - alfa], 1)
    b, a = b / a[:, :1], a / a[:, :1]
    y = np.empty(n)
    z = np.zeros(2)
    for k, i in enumerate(range(0, n, bloque)):
        y[i:i + bloque], z = lfilter(b[k], a[k], x[i:i + bloque], zi=z)
    return y


def en_bucle(hacer, n, cruce=0.05):
    """hacer(m) da una señal de m muestras; se corta a n y lo que sobra se funde con el inicio (loop sin corte)."""
    x = int(cruce * SR)
    y = hacer(n + x)
    out = y[:n].copy()
    w = np.linspace(0, 1, x)
    if out.ndim == 2:
        w = w[:, None]
    out[:x] = out[:x] * w + y[n:n + x] * (1 - w)
    return out


def estereo(x, pan=0.0):
    if x.ndim == 2:
        return x
    return np.stack([x * np.sqrt((1 - pan) / 2), x * np.sqrt((1 + pan) / 2)], 1)


def satura(x, k=2.0):
    """Saturación suave (como pasar por un amplificador): engorda el sonido sin pasarse de 1."""
    return np.tanh(k * x) / np.tanh(k)


def limitador_suave(x, umbral=0.5):
    """Deja igual lo bajito y redondea los picos: la batería suena más llena con el mismo volumen máximo."""
    pico = np.max(np.abs(x)) + 1e-12
    y = x / pico
    a = np.abs(y)
    sobre = a > umbral
    y[sobre] = np.sign(y[sobre]) * (umbral + (1 - umbral) * np.tanh((a[sobre] - umbral) / (1 - umbral)))
    return y * pico


def rms_db(x):
    return 20 * np.log10(np.sqrt(np.mean(np.square(x, dtype=np.float64))) + 1e-12)


# ---------------------------------------------------------------- osciladores

def _blep(fase, dt):
    """Corrección polyBLEP: redondea el salto de la sierra para que no haga "aliasing" (zumbidos falsos)."""
    y = np.zeros_like(fase)
    m = fase < dt
    x = fase[m] / dt[m]
    y[m] = x + x - x * x - 1
    m = fase > 1 - dt
    x = (fase[m] - 1) / dt[m]
    y[m] = x * x + x + x + 1
    return y


def _frecuencias(f, n):
    return np.full(n, float(f)) if np.ndim(f) == 0 else np.asarray(f, dtype=float)


def sierra(f, n=None, fase0=0.0):
    """Sierra limpia. f: Hz (número, con n muestras) o un arreglo de Hz por muestra (para slides)."""
    f = _frecuencias(f, n)
    dt = np.clip(f / SR, 1e-7, 0.45)
    fase = (fase0 + np.cumsum(dt)) % 1.0
    return 2 * fase - 1 - _blep(fase, dt)


def cuadrada(f, n=None, fase0=0.0):
    return 0.5 * (sierra(f, n, fase0) - sierra(f, n, fase0 + 0.5))


def seno(f, n=None, fase0=0.0):
    f = _frecuencias(f, n)
    return np.sin(2 * np.pi * (fase0 + np.cumsum(f / SR)))


def env(n, ataque=0.005, salida=0.05, caida=0.0, sosten=1.0):
    """Envolvente: sube en 'ataque' s, baja a 'sosten' en 'caida' s y se apaga en 'salida' s al final."""
    t = np.arange(n) / SR
    e = np.minimum(1.0, t / max(ataque, 1e-4))
    if caida > 0:
        e = e * (sosten + (1 - sosten) * np.exp(-t / caida))
    return e * np.clip((t[-1] - t) / max(salida, 1e-4), 0, 1)


# ---------------------------------------------------------------- instrumentos (se guardan en memoria)

@lru_cache(maxsize=768)
def supersaw(midi, dur, voces=7, ancho=0.18, brillo=3000.0, ataque=0.2, salida=0.3, semilla=0):
    """Varias sierras un poquito desafinadas entre sí y abiertas en estéreo: el sonido del trance."""
    n = max(2, int(dur * SR))
    rng = np.random.default_rng(semilla + int(midi) * 7)
    L, R = np.zeros(n), np.zeros(n)
    for d in np.linspace(-1, 1, voces):
        s = sierra(hz(midi + d * ancho), n, rng.random())
        pan = d * 0.9
        L += s * np.sqrt((1 - pan) / 2)
        R += s * np.sqrt((1 + pan) / 2)
    x = filtro(np.stack([L, R], 1) / np.sqrt(voces), brillo)
    return (x * env(n, ataque, salida)[:, None]).astype(np.float32)


@lru_cache(maxsize=768)
def pluck_trance(midi, dur, brillo=1.0):
    """Pluck de arpegio de trance: supersaw corto con un brillo que se apaga rápido."""
    n = max(2, int(dur * SR))
    t = np.arange(n) / SR
    x = supersaw(midi, dur, 3, 0.12, 7000.0 * brillo, 0.002, 0.04, 3).astype(float)
    oscuro = filtro(x, 900.0)
    return ((x * np.exp(-t * 16)[:, None] * 0.8 + oscuro * np.exp(-t * 5)[:, None] * 0.5)).astype(np.float32)


@lru_cache(maxsize=1024)
def pluck(midi, dur):
    """Cuerda pulsada sintética: un ataque brillante y un cuerpo oscuro que dura más."""
    n = max(2, int(dur * SR))
    t = np.arange(n) / SR
    f = float(hz(midi))
    x = sierra(f, n, 0.1) * 0.7 + cuadrada(f * 1.003, n, 0.3) * 0.5
    claro = filtro(x, min(9000.0, 2600 + f * 4)) * np.exp(-t * 18)
    oscuro = filtro(x, min(4000.0, 700 + f * 1.5)) * np.exp(-t * 6)
    return ((claro * 0.6 + oscuro * 0.5) * env(n, 0.002, 0.03)).astype(np.float32)


@lru_cache(maxsize=512)
def lead_synth(midi, dur, oscuro=False):
    """Lead de synthwave: dos sierras y una cuadrada un poco desafinadas, con vibrato que entra despacio."""
    n = max(2, int(dur * SR))
    t = np.arange(n) / SR
    vib = 1 + 0.006 * np.sin(2 * np.pi * 5.5 * t) * np.clip((t - 0.15) / 0.25, 0, 1)
    f = float(hz(midi)) * vib
    x = sierra(f * 0.997) * 0.45 + sierra(f * 1.003) * 0.45 + cuadrada(f * 0.5) * 0.3
    x = filtro(x, 1800.0 if oscuro else 3200.0, orden=2)
    return (satura(x, 1.4) * env(n, 0.01, 0.12, 0.3, 0.75)).astype(np.float32)


@lru_cache(maxsize=1024)
def burbuja(midi, dur):
    """Pluck que "burbujea": la nota arranca un poco más aguda y cae (melodía del digestivo)."""
    n = max(2, int(dur * SR))
    t = np.arange(n) / SR
    f = float(hz(midi)) * (1 + 0.35 * np.exp(-t * 70))
    x = sierra(f) * 0.6 + seno(f * 2) * 0.3
    x = filtro(x, 3400.0) * np.exp(-t * 8)
    return (x * env(n, 0.002, 0.03)).astype(np.float32)


@lru_cache(maxsize=512)
def organo(midi, dur):
    """Órgano de house (los acordes cortos "stab" de los 90)."""
    n = max(2, int(dur * SR))
    t = np.arange(n) / SR
    f = float(hz(midi))
    x = sum(a * np.sin(2 * np.pi * f * k * t) for k, a in ((1, 1), (2, 0.55), (3, 0.35), (4, 0.2), (6, 0.12)))
    x = x + filtro(np.random.default_rng(int(midi)).normal(0, 1, n), 2500.0, "highpass") * np.exp(-t * 400) * 0.25
    return (x * np.exp(-t * 7) * env(n, 0.003, 0.05)).astype(np.float32)


@lru_cache(maxsize=1024)
def campana(midi, dur, razon=3.5, indice=2.5, caida=2.4):
    """FM: una onda modula a otra y suena metálica, como campanita (razón 3.5) o vidrio (razón 2)."""
    n = max(2, int(dur * SR))
    t = np.arange(n) / SR
    f = float(hz(midi))
    mod = indice * np.exp(-t * 5) * np.sin(2 * np.pi * f * razon * t)
    x = np.sin(2 * np.pi * f * t + mod) * np.exp(-t * caida)
    x = x + 0.25 * np.sin(2 * np.pi * f * 2 * t) * np.exp(-t * caida * 2)
    return (x * env(n, 0.002, 0.06)).astype(np.float32)


@lru_cache(maxsize=512)
def rhodes(midi, dur):
    """Piano eléctrico (FM) con un trémolo que va de un oído al otro: típico del liquid."""
    n = max(2, int(dur * SR))
    t = np.arange(n) / SR
    f = float(hz(midi))
    x = np.sin(2 * np.pi * f * t + 1.6 * np.exp(-t * 3.5) * np.sin(2 * np.pi * f * t))
    x = x + 0.18 * np.sin(2 * np.pi * f * 14 * t) * np.exp(-t * 25)
    x = x * np.exp(-t * 1.3) * env(n, 0.003, 0.12)
    pan = 0.35 * np.sin(2 * np.pi * 3.2 * t)
    return np.stack([x * np.sqrt((1 - pan) / 2), x * np.sqrt((1 + pan) / 2)], 1).astype(np.float32)


@lru_cache(maxsize=512)
def stab(midi, dur, oscuro=False):
    """Acorde corto agresivo (neurofunk / acid techno): cuadradas desafinadas con filtro que se cierra."""
    n = max(2, int(dur * SR))
    t = np.arange(n) / SR
    f = float(hz(midi))
    x = cuadrada(f * 0.995, n, 0.1) + cuadrada(f * 1.005, n, 0.6) + sierra(f / 2, n) * 0.5
    corte = (220 if oscuro else 320) + (2200 if oscuro else 3600) * np.exp(-t * 22)
    x = filtro_movil(x, corte, 4.0)
    return (satura(x * 1.5, 2) * env(n, 0.002, 0.04)).astype(np.float32)


@lru_cache(maxsize=512)
def bajo_pluck(midi, dur, brillo=1.0, caida=10.0):
    """Bajo de trance / psy: sierra con un filtro que se abre al tocar y se cierra rápido, más un sub."""
    n = max(2, int(dur * SR))
    t = np.arange(n) / SR
    f = float(hz(midi))
    x = sierra(f, n) + 0.5 * sierra(f * 1.004, n, 0.5)
    y = filtro_movil(x, 160 + 1500 * brillo * np.exp(-t * caida), 1.4)
    y = satura(y * 1.5, 1.5) + 0.55 * np.sin(2 * np.pi * f * t)
    return (y * env(n, 0.002, 0.015)).astype(np.float32)


@lru_cache(maxsize=64)
def zap(variante=0):
    """Rayito láser (FM que cae rápido): chispas del nervioso."""
    n = int(0.18 * SR)
    t = np.arange(n) / SR
    f = 3000 * np.exp(-t * (18 + variante * 5)) + 140
    x = np.sin(2 * np.pi * np.cumsum(f) / SR + 2 * np.sin(2 * np.pi * np.cumsum(f * 1.5) / SR))
    return (x * np.exp(-t * 14) * env(n, 0.001, 0.02)).astype(np.float32)


# ---------------------------------------------------------------- batería

@lru_cache(maxsize=16)
def bombo(tipo="house"):
    """Bombo: una onda que baja de tono muy rápido + un chasquido. Cada género tiene el suyo."""
    dur, f0, salto, rapidez, caida, click = {
        "house": (0.42, 52, 125, 36, 7.5, 0.35), "techno": (0.6, 46, 110, 30, 4.5, 0.3),
        "trance": (0.38, 50, 150, 42, 8.5, 0.4), "psy": (0.3, 54, 260, 55, 12, 0.5),
        "dnb": (0.34, 55, 170, 45, 10, 0.45), "liquid": (0.3, 52, 120, 40, 11, 0.2),
        "synth": (0.5, 49, 115, 32, 6.5, 0.32), "tribal": (0.7, 50, 60, 20, 4.0, 0.15),
        "industrial": (0.55, 44, 150, 28, 5.0, 0.55)}[tipo]
    n = int(dur * SR)
    t = np.arange(n) / SR
    f = f0 + salto * np.exp(-t * rapidez)
    cuerpo = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * caida)
    golpe = filtro(np.random.default_rng(5).normal(0, 1, n), 1500.0, "highpass") * np.exp(-t * 500) * click
    x = satura(cuerpo + golpe, 1.6)
    if tipo == "industrial":   # saturado como un martillo
        x = satura(x * 2.2, 3.0)
    if tipo == "techno":   # retumbo grave detrás (el "rumble" del techno oscuro)
        retumbo = filtro(np.random.default_rng(6).normal(0, 1, n), 110.0) * np.exp(-t * 4) * 6 * (t > 0.05)
        x = x + satura(retumbo, 2) * 0.25
    return (x * env(n, 0.0005, 0.02)).astype(np.float32)


@lru_cache(maxsize=16)
def palmada(variante=0):
    """Palmada: 3 golpecitos de ruido muy juntos y una colita."""
    n = int(0.32 * SR)
    t = np.arange(n) / SR
    x = filtro(np.random.default_rng(20 + variante).normal(0, 1, n), [900, 3200], "bandpass")
    e = sum(np.exp(-np.maximum(0, t - k * 0.009) * 220) * (t >= k * 0.009) for k in range(3))
    e = e + np.exp(-np.maximum(0, t - 0.027) * 20) * (t >= 0.027) * 0.9
    return (satura(x * e * 2.2, 1.5) * env(n, 0.0005, 0.03)).astype(np.float32)


@lru_cache(maxsize=16)
def caja(tipo="dnb", variante=0):
    """Caja (tarola): cuerpo con tono + ruido. La de drum & bass es más seca y golpeada."""
    n = int(0.3 * SR)
    t = np.arange(n) / SR
    tono, caida, caida_ruido, brillo, dureza = {"dnb": (185, 28, 15, 1600, 1.8), "house": (200, 30, 22, 2200, 1.3),
                                                "liquid": (210, 32, 20, 2500, 1.2), "synth": (180, 20, 9, 1400, 1.5),
                                                "industrial": (165, 24, 11, 900, 2.8)}[tipo]
    f = tono + 70 * np.exp(-t * 70)
    cuerpo = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * caida)
    cuerpo = cuerpo + 0.4 * np.sin(2 * np.pi * tono * 1.75 * t) * np.exp(-t * caida * 1.5)
    ruido = filtro(np.random.default_rng(30 + variante).normal(0, 1, n), float(brillo), "highpass") * np.exp(-t * caida_ruido)
    return (satura(cuerpo * 0.9 + ruido * 0.8, dureza) * env(n, 0.0005, 0.03)).astype(np.float32)


@lru_cache(maxsize=4)
def caja_cortada(variante=0):
    """La caja de los 80: reverb enorme que se CORTA de golpe ("gated reverb"). Estéreo."""
    seca = caja("synth", variante).astype(np.float64)
    m = int(0.42 * SR)
    x = np.zeros((m, 2))
    x[:len(seca)] = estereo(seca, 0.0)[:m]
    cola = reverb(x, 1.6, 6500, semilla=11 + variante, lineal=True)
    puerta = np.clip((0.3 - np.arange(m) / SR) / 0.04, 0, 1)
    y = x + cola * 1.6 * puerta[:, None]
    return (y / max(np.max(np.abs(y)), 1e-9) * 0.9).astype(np.float32)


# ---------------------------------------------------------------- tribal e industrial

@lru_cache(maxsize=48)
def tambor(tipo="taiko", variante=0):
    """Tambores. taiko: grave y largo (el corazón); surdo: grave seco; djembe_bajo / _tono / _golpe (el slap);
    palo: clave de madera."""
    rng = np.random.default_rng(60 + variante + len(tipo))
    if tipo == "palo":
        n = int(0.08 * SR)
        t = np.arange(n) / SR
        x = np.sin(2 * np.pi * 2300 * (1 + 0.02 * variante) * t) * np.exp(-t * 70) + 0.3 * np.sin(2 * np.pi * 5100 * t) * np.exp(-t * 120)
        return (x * env(n, 0.0005, 0.01)).astype(np.float32)
    dur, f0, salto, rapidez, caida, piel, brillo = {
        "taiko": (1.1, 58, 40, 18, 3.2, 0.35, 900.0), "surdo": (0.7, 52, 25, 25, 5.5, 0.25, 700.0),
        "djembe_bajo": (0.45, 78, 30, 30, 11, 0.3, 1200.0), "djembe_tono": (0.3, 210, 25, 40, 16, 0.35, 2600.0),
        "djembe_golpe": (0.18, 380, 60, 60, 30, 1.0, 4500.0)}[tipo]
    n = int(dur * SR)
    t = np.arange(n) / SR
    f = f0 * (1 + 0.03 * variante) + salto * np.exp(-t * rapidez)
    cuerpo = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * caida)
    cuerpo = cuerpo + 0.3 * np.sin(2 * np.pi * np.cumsum(f * 1.58) / SR) * np.exp(-t * caida * 1.6)   # 2o modo de la piel
    golpe = filtro(rng.normal(0, 1, n), brillo) * np.exp(-t * (35 if tipo == "djembe_golpe" else 80)) * piel
    return (satura(cuerpo + golpe, 1.4) * env(n, 0.0008, 0.03)).astype(np.float32)


@lru_cache(maxsize=1024)
def marimba(midi, dur):
    """Marimba: el tono, su parcial de ~4 veces (así suenan las teclas de madera) y un golpecito de la baqueta."""
    n = max(2, int(dur * SR))
    t = np.arange(n) / SR
    f = float(hz(midi))
    x = np.sin(2 * np.pi * f * t) * np.exp(-t * 4.5) + 0.35 * np.sin(2 * np.pi * f * 3.93 * t) * np.exp(-t * 14)
    x = x + 0.12 * np.sin(2 * np.pi * f * 9.2 * t) * np.exp(-t * 40)
    x = x + filtro(np.random.default_rng(int(midi)).normal(0, 1, n), 3000.0) * np.exp(-t * 300) * 0.2
    return (x * env(n, 0.001, 0.05)).astype(np.float32)


@lru_cache(maxsize=512)
def flauta(midi, dur):
    """Flauta de madera: seno con soplido y un vibrato que entra despacio."""
    n = max(2, int(dur * SR))
    t = np.arange(n) / SR
    f0 = float(hz(midi))
    f = f0 * (1 + 0.008 * np.sin(2 * np.pi * 5 * t) * np.clip((t - 0.2) / 0.3, 0, 1))
    fase = 2 * np.pi * np.cumsum(f) / SR
    x = np.sin(fase) + 0.25 * np.sin(2 * fase)
    soplo = filtro(np.random.default_rng(int(midi) + 9).normal(0, 1, n), [f0 * 1.5, min(f0 * 4, 16000)], "bandpass") * 0.35
    return ((x * 0.8 + soplo) * env(n, 0.06, 0.15, 0.2, 0.8)).astype(np.float32)


VOCALES = {"a": (800, 1150), "o": (450, 800), "u": (325, 700), "e": (400, 2000)}


@lru_cache(maxsize=256)
def canto(midi, dur, vocal="o", voces=3, ataque=0.25):
    """Coro que canta una vocal: ondas desafinadas por dos FORMANTES (lo que hace que suene a "o" o a "a"). Estéreo."""
    n = max(2, int(dur * SR))
    t = np.arange(n) / SR
    rng = np.random.default_rng(int(midi) * 3 + voces)
    f1, f2 = VOCALES[vocal]
    L, R = np.zeros(n), np.zeros(n)
    for k, d in enumerate(np.linspace(-1, 1, voces)):
        vib = 1 + 0.006 * np.sin(2 * np.pi * (4.6 + 0.5 * k) * t + rng.random() * 6)
        fuente = sierra(float(hz(midi + d * 0.12)) * vib, None, rng.random())
        v = filtro(fuente, [f1 * 0.8, f1 * 1.25], "bandpass") + 0.6 * filtro(fuente, [f2 * 0.85, f2 * 1.2], "bandpass")
        pan = d * 0.7
        L += v * np.sqrt((1 - pan) / 2)
        R += v * np.sqrt((1 + pan) / 2)
    x = np.stack([L, R], 1) / np.sqrt(voces) * 3
    return (x * env(n, ataque, 0.3)[:, None]).astype(np.float32)


@lru_cache(maxsize=16)
def grito(vocal="a", variante=0):
    """¡HEY! / ¡HA!: grito corto de guerra de la tribu (varias voces, sube un poquito y se corta)."""
    x = canto(57 + (0, 2, -2)[variante % 3], 0.32, vocal, 4, 0.006).astype(float)
    t = np.arange(len(x)) / SR
    return (satura(x * 1.5, 1.6) * np.exp(-t * 7)[:, None]).astype(np.float32)


@lru_cache(maxsize=48)
def metal(f0=420.0, caida=7.0, variante=0):
    """Golpe de METAL (yunque, tubo, lámina): parciales que no son armónicos, como una pieza de fierro."""
    n = int(min(2.5, 6.0 / caida) * SR)
    t = np.arange(n) / SR
    rng = np.random.default_rng(70 + variante)
    x = np.zeros(n)
    for k, (r, g) in enumerate(((1, 1.0), (2.76, 0.6), (5.40, 0.4), (8.93, 0.25), (13.3, 0.12))):
        x += g * np.sin(2 * np.pi * f0 * r * (1 + rng.uniform(-0.01, 0.01)) * t + rng.random() * 6) * np.exp(-t * caida * (1 + k * 0.5))
    x += filtro(rng.normal(0, 1, n), 2500.0, "highpass") * np.exp(-t * 200) * 0.5
    return (satura(x * 0.6, 1.3) * env(n, 0.0005, 0.03)).astype(np.float32)


@lru_cache(maxsize=16)
def vapor(largo=0.25, variante=0):
    """Chorro de VAPOR / pistón hidráulico: ruido agudo que sale de golpe y se apaga."""
    n = int(largo * SR)
    t = np.arange(n) / SR
    x = filtro(np.random.default_rng(80 + variante).normal(0, 1, n), [1800, 9000], "bandpass")
    return (x * np.minimum(1, t / 0.004) * np.exp(-t * (4 / largo)) * env(n, 0.001, 0.02)).astype(np.float32)


def bajo_saturado(midi, dur, brillo=1.2, caida=14.0):
    """Bajo EBM / industrial: el bajo pulsado pasado por un amplificador al máximo."""
    return satura(bajo_pluck(midi, dur, brillo, caida).astype(float) * 2.2, 2.4).astype(np.float32)


def golpes_lub_dub(p, c, g=0.5):
    """Un lub-dub grave muy bajito cada medio compás (a 136 BPM son 68 latidos por minuto)."""
    for compas in range(c.compases):
        for s in (0, 8):
            for d, fuerza in ((0, 1.0), (1.6, 0.6)):
                m = int(0.18 * SR)
                tt = np.arange(m) / SR
                golpe = np.sin(2 * np.pi * (50 * tt + 30 * (1 - np.exp(-tt * 25)) / 25)) * np.exp(-tt * 18) * fuerza
                p.poner("Ambiente", golpe, compas * 16 + s + d, g, 0.0, rev=0.2)


_METAL = (205.3, 304.4, 369.6, 522.7, 540.0, 800.0)   # frecuencias de los platillos de la caja de ritmos 808


@lru_cache(maxsize=16)
def platos(tipo):
    """Hi-hats y platillos: 6 cuadradas desafinadas (sonido metálico) + ruido, sin graves."""
    dur, corte, caida, metal = {"cerrado": (0.09, 7500, 60, 0.55), "abierto": (0.42, 6500, 8, 0.6),
                                "ride": (1.3, 3500, 2.8, 0.8), "crash": (2.4, 4200, 1.9, 0.45),
                                "shaker": (0.1, 5500, 38, 0.0)}[tipo]
    n = int(dur * SR)
    t = np.arange(n) / SR
    rng = np.random.default_rng(40 + len(tipo))
    k = 1.6 if tipo == "ride" else 1.0
    metalico = sum(np.sign(np.sin(2 * np.pi * f * k * t + rng.random() * 6.28)) for f in _METAL) / 6
    x = filtro(metalico * metal + rng.normal(0, 1, n) * 0.5 * (1 - metal * 0.6), float(corte), "highpass", 4)
    e = np.exp(-t * caida)
    if tipo == "shaker":
        e = e * np.minimum(1, t / 0.012)
        x = filtro(x, 11000.0)
    if tipo == "ride":
        x = x + 0.25 * np.sin(2 * np.pi * 2950 * t) * np.exp(-t * 4)
    return (x * e * env(n, 0.0008, 0.03)).astype(np.float32)


@lru_cache(maxsize=4)
def cencerro():
    """Cencerro (cowbell) de 808: el toque juguetón del acid house."""
    n = int(0.25 * SR)
    t = np.arange(n) / SR
    x = filtro(cuadrada(540.0, n) + cuadrada(800.0, n), [600, 2500], "bandpass")
    return (x * np.exp(-t * 13) * env(n, 0.001, 0.03) * 2).astype(np.float32)


@lru_cache(maxsize=4)
def aro():
    """Golpe de aro (rimshot): clic seco."""
    n = int(0.06 * SR)
    t = np.arange(n) / SR
    x = np.sin(2 * np.pi * 1750 * t) * np.exp(-t * 90)
    x = x + filtro(np.random.default_rng(8).normal(0, 1, n), 3000.0, "highpass") * np.exp(-t * 250) * 0.5
    return (x * env(n, 0.0005, 0.01)).astype(np.float32)


@lru_cache(maxsize=16)
def tom(altura=0):
    n = int(0.35 * SR)
    t = np.arange(n) / SR
    base = 95 * 2 ** (altura / 12)
    f = base + base * 0.6 * np.exp(-t * 20)
    return (satura(np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 9), 1.3) * env(n, 0.001, 0.03)).astype(np.float32)


def subida(segundos, semilla=0, brillo=1.0):
    """Ruido que sube de tono y de volumen: anuncia que viene una parte nueva."""
    n = int(segundos * SR)
    t = np.arange(n) / n
    r = np.random.default_rng(semilla).normal(0, 1, n)
    x = filtro_movil(r, 300 * (25 * brillo) ** t, 1.3, "bp")
    return (x * t ** 2.2 * env(n, 0.01, 0.01)).astype(np.float32)


def platillo_al_reves(segundos):
    """Platillo tocado al revés (crece y se corta): otra forma de anunciar el golpe que sigue."""
    c = platos("crash")[::-1]
    n = min(len(c), int(segundos * SR))
    return (c[-n:] * np.linspace(0.2, 1, n)).astype(np.float32)


def golpe_grave(midi, segundos=1.2, semilla=0):
    """Golpe de cine: sub que cae de tono + ruido grave. Para el inicio de una parte o la entrada del jefe."""
    n = int(segundos * SR)
    t = np.arange(n) / SR
    f = float(hz(midi)) * (1 + 1.5 * np.exp(-t * 12))
    x = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 2.2)
    x = x + filtro(np.random.default_rng(semilla).normal(0, 1, n), 400.0) * np.exp(-t * 6) * 1.5
    return (satura(x, 1.5) * env(n, 0.001, 0.2)).astype(np.float32)


# ================================================================ líneas continuas (acid, reese, growl)

@dataclass
class NotaLinea:
    inicio: float        # en dieciseisavos desde el inicio del loop
    largo: float         # en dieciseisavos
    midi: int
    acento: bool = False
    slide: bool = False  # se liga y resbala hacia la siguiente (como la 303)
    mov: int = 0         # movimiento del filtro en el reese (0 abre, 1 vaivén 1/8, 2 vaivén 1/16, 3 cierra, 4 tresillo)


def render_linea(p, notas, sintetizar, puerta=0.55, suavizar=250.0):
    """Toca una lista de notas como UNA voz continua (así puede haber slides) del largo del loop.
    sintetizar(d) recibe arreglos por muestra (f, compuerta, desde, acento, mov) y devuelve la señal mono.
    Se toca desde un compás antes (con las notas del final) y se funde el inicio: el loop empata."""
    n, W, X = p.n, int(16 * p.paso * SR), int(0.012 * SR)
    largo_loop = p.compases * 16
    total = W + n + X
    todas = sorted(((p.muestra(nt.inicio + d) + W, nt) for d in (-largo_loop, 0, largo_loop) for nt in notas),
                   key=lambda e: e[0])
    todas = [(i, nt) for i, nt in todas if -int(8 * p.paso * SR) < i < total]
    f = np.zeros(total)
    compuerta = np.zeros(total)
    cual = np.full(total, -1)
    disparo = np.zeros(total, dtype=int)
    previa = None
    for k, (i, nt) in enumerate(todas):
        a = max(0, i)
        cual[a] = k
        if previa is None or not previa.slide:
            disparo[a] = a
        siguiente = todas[k + 1][0] if k + 1 < len(todas) else total
        fin = siguiente + int(0.004 * SR) if nt.slide else i + int(nt.largo * puerta * p.paso * SR)
        compuerta[a:max(a, min(total, fin))] = 1
        previa = nt
    cual = np.maximum.accumulate(cual)
    cual[cual < 0] = 0
    frec = np.array([float(hz(nt.midi)) for _, nt in todas])
    f = frec[cual]
    # Slides: la nota resbala desde la anterior en 60 ms.
    g = int(0.06 * SR)
    for k in range(1, len(todas)):
        if todas[k - 1][1].slide:
            i = max(0, todas[k][0])
            m = min(g, total - i)
            if m > 0:
                f[i:i + m] = frec[k - 1] * (frec[k] / frec[k - 1]) ** (np.arange(m) / g)
    ultimo = np.maximum.accumulate(disparo)
    desde = (np.arange(total) - ultimo) / SR
    acento = np.array([1.0 if nt.acento else 0.0 for _, nt in todas])[cual[ultimo]]
    mov = np.array([nt.mov for _, nt in todas])[cual]
    compuerta = sosfilt(_sos(suavizar, "lowpass", 2), compuerta)
    y = sintetizar({"f": f, "compuerta": compuerta, "desde": desde, "acento": acento, "mov": mov, "W": W})
    out = y[W:W + n].copy()
    w = np.linspace(0, 1, X)
    out[:X] = out[:X] * w + y[W + n:W + n + X] * (1 - w)
    return out.astype(np.float32)


def curva_loop(p, total, W, forma):
    """Valor 0..1 que recorre el loop (para abrir y cerrar el filtro del acid a lo largo de la canción)."""
    fase = ((np.arange(total) - W) / p.n) % 1.0
    return forma(fase)


# ================================================================ la pista

class Pista:
    """Los lienzos estéreo de cada capa. Lo que se pasa del final se dobla al inicio (loop).
    Cada capa tiene buses: seco, envío a reverb y envío a eco; y los mismos "con bombeo" (sidechain)."""

    def __init__(self, nivel, compases):
        self.paso = 60 / nivel.bpm / 4                    # duración de un dieciseisavo (segundos)
        self.compases = compases
        self.n = round(compases * 16 * self.paso * SR)    # largo total en muestras
        self.swing = nivel.swing
        self.buses = {}
        self.bombos = []                                  # muestras donde cae el bombo (para el bombeo)
        self.despues = {}                                 # capa -> función que procesa sus buses al final

    def muestra(self, paso):
        """Dieciseisavo -> muestra. Los dieciseisavos impares se atrasan un poco si hay swing."""
        if self.swing and float(paso).is_integer() and int(paso) % 2 == 1:
            paso = paso + self.swing
        return int(round(paso * self.paso * SR))

    def bus(self, capa, tipo):
        if (capa, tipo) not in self.buses:
            self.buses[(capa, tipo)] = np.zeros((self.n, 2), np.float32)
        return self.buses[(capa, tipo)]

    def poner(self, capa, senal, paso, g=1.0, pan=0.0, rev=0.0, eco=0.0, bombeo=False, recto=False):
        """Pega una señal en la capa en el dieciseisavo 'paso'. pan: -1 izquierda, +1 derecha.
        rev / eco: cuánto se manda a la reverb / al eco. bombeo: baja con cada bombo."""
        x = estereo(np.asarray(senal, np.float32) * np.float32(g), pan)
        i = (int(round(paso * self.paso * SR)) if recto else self.muestra(paso)) % self.n
        pre = "b" if bombeo else ""
        _sumar(self.bus(capa, pre + "seco"), x, i)
        if rev:
            _sumar(self.bus(capa, pre + "rev"), x * np.float32(rev), i)
        if eco:
            _sumar(self.bus(capa, pre + "eco"), x * np.float32(eco), i)

    def continua(self, capa, senal, g=1.0, pan=0.0, rev=0.0, eco=0.0, bombeo=False):
        """Pega una señal que ya mide todo el loop (líneas de bajo, texturas)."""
        self.poner(capa, senal, 0, g, pan, rev, eco, bombeo, recto=True)

    def bombo(self, capa, senal, paso, g):
        self.poner(capa, senal, paso, g)
        self.bombos.append(self.muestra(paso) % self.n)


def _sumar(buf, x, i):
    n = len(buf)
    while len(x):
        k = min(len(x), n - i)
        buf[i:i + k] += x[:k]
        x = x[k:]
        i = 0


def reverb(x, segundos, oscuro, semilla=0, lineal=False):
    """Reverb de sala: ruido que se apaga (distinto en cada oído). En el loop da la vuelta (convolución circular)."""
    m = int(segundos * SR)
    t = np.arange(m) / SR
    rng = np.random.default_rng(semilla + int(segundos * 100))
    salida = []
    for lado in range(2):
        r = rng.normal(0, 1, m) * np.exp(-t * 6.9 / segundos)
        r = filtro(filtro(r, float(oscuro)), 180.0, "highpass")
        r = r * (1 - 0.5 * t / segundos) + filtro(r, oscuro / 3) * (0.5 * t / segundos)   # se oscurece al apagarse
        r[: int(0.012 * SR)] = 0                                                          # 12 ms antes de empezar
        r /= np.sqrt(np.sum(r ** 2)) + 1e-12
        if lineal:
            salida.append(fftconvolve(x[:, lado], r)[:len(x)])
        else:
            n = len(x)
            salida.append(np.fft.irfft(np.fft.rfft(x[:, lado].astype(np.float64)) * np.fft.rfft(r, n), n))
    return np.stack(salida, 1)


def eco(x, paso_s, pasos=3, retro=0.42, veces=6, oscuro=5000.0):
    """Eco ping-pong (rebota de un oído al otro) cada 'pasos' dieciseisavos; también da la vuelta al loop."""
    d = int(round(pasos * paso_s * SR))
    y = np.zeros(x.shape)
    g = 1.0
    for k in range(1, veces + 1):
        g *= retro
        s = np.roll(x, k * d, axis=0)
        y += (s[:, ::-1] if k % 2 else s) * g
    return filtro_bucle(filtro_bucle(y, oscuro), 280.0, "highpass")


def envolvente_bombeo(p, profundidad, regreso):
    """Volumen que baja de golpe con cada bombo y regresa en 'regreso' segundos (el "sidechain" del dance)."""
    e = np.ones(p.n, np.float32)
    if not p.bombos or profundidad <= 0:
        return e
    largo = int(regreso * 4 * SR)
    t = np.arange(largo) / SR
    curva = 1 - profundidad * np.exp(-t / regreso)
    a = int(0.003 * SR)
    curva[:a] = np.linspace(1, curva[a], a)
    for i in sorted(set(p.bombos)):
        idx = (i + np.arange(largo)) % p.n
        e[idx] = np.minimum(e[idx], curva)
    return e


# ================================================================ composición

class Ctx:
    """Lo que todas las capas necesitan saber del nivel."""

    def __init__(self, nombre, nivel, compases):
        self.nombre, self.nv, self.compases = nombre, nivel, compases
        self.g, self.s = nivel.genero, nivel.sabor
        self.rng = np.random.default_rng(zlib.crc32(nombre.encode()))
        self.raiz = 33 + (nivel.tonica - 33) % 12          # la tónica entre La1 y Sol#2 (55 a 104 Hz)
        self.semilla = zlib.crc32(nombre.encode()) % 1000

    def grado(self, g):
        e = self.nv.escala
        return e[g % 7] + 12 * (g // 7)

    def acorde(self, compas):
        return self.nv.acordes[(compas // 2) % len(self.nv.acordes)]

    def seccion(self, compas):
        return (compas // 8) % 4        # 0 = A, 1 = A', 2 = B, 3 = B'


def frase(c, compas):
    """Notas de la melodía en un compás: [(paso, grado sobre el acorde, largo en pasos, fuerza)].
    Frases de 4 compases: motivo, motivo movido, motivo una tercera arriba y respuesta que vuelve a casa.
    En la parte B el motivo va en espejo y más arriba. En la A deja huecos (se oye más al explorar)."""
    mot, rit = c.nv.motivo, c.nv.ritmo
    k, sec = compas % 4, c.seccion(compas)
    notas = []
    for j, s in enumerate(rit):
        if k == 3 and j >= 3:
            break
        g = mot[(j + (1 if k == 1 else 0)) % len(mot)] + (2 if k == 2 else 0)
        if sec >= 2:
            g = 2 * mot[0] - g + 4
        siguiente = rit[j + 1] if j + 1 < len(rit) else 16 + rit[0]
        if sec == 0 and j % 3 == 2:
            continue
        notas.append((s, g, siguiente - s, 1.0 if j == 0 else 0.8))
    if k == 3:
        s = rit[3] if len(rit) > 3 else 8
        notas.append((s, 0 if sec < 2 else 4, 16 - s + rit[0], 1.0))
    return notas


def lfo_loop(p, ciclos, fase=0.0, m=None):
    """0..1 que sube y baja 'ciclos' veces en el loop (ciclos enteros = el loop empata)."""
    t = np.arange(m if m is not None else p.n) / SR
    return 0.5 - 0.5 * np.cos(2 * np.pi * (ciclos * t / (p.n / SR) + fase))


def f_bucle(p, f):
    """Ajusta una frecuencia para que quepa un número entero de vueltas en el loop (sin chasquido al repetir)."""
    T = p.n / SR
    return max(1, round(f * T)) / T


# ---------------------------------------------------------------- Ambiente

def ambiente(p, c):
    rng = c.rng
    n = p.n
    t = np.arange(n) / SR
    # Zumbido grave del órgano: tónica y quinta, con un vaivén muy lento.
    for grado, g, ciclos in ((0, 0.5, 2), (4, 0.3, 3)):
        f = f_bucle(p, float(hz(c.raiz + 12 + c.grado(grado))))
        z = (np.sin(2 * np.pi * f * t) + 0.3 * np.sin(4 * np.pi * f * t)) * (0.5 + 0.5 * lfo_loop(p, ciclos, 0.25 * grado))
        suave = {"trance": 0.3, "psy": 0.35, "liquid": 0.3}.get(c.g, 0.45)   # bajito: que se oigan las texturas
        p.continua("Ambiente", z, g * suave, pan=-0.2 if grado else 0.2, rev=0.3)

    if c.g == "acid":
        # Gorgoteo: racimos de burbujas que suben, con mucha reverb (el estómago "trabajando").
        for _ in range(int(c.compases * (2.2 if c.s == "rebote" else 1.4))):
            paso = rng.uniform(0, c.compases * 16)
            for b in range(int(rng.integers(1, 4))):
                largo = rng.uniform(0.06, 0.16)
                m = int(largo * SR)
                tt = np.arange(m) / SR
                f0, f1 = rng.uniform(110, 220), rng.uniform(320, 700)
                burb = np.sin(2 * np.pi * np.cumsum(f0 + (f1 - f0) * (tt / largo) ** 2) / SR) * np.exp(-tt * 18) * env(m, 0.004, 0.02)
                p.poner("Ambiente", burb, paso + b * 0.6, rng.uniform(0.15, 0.35), rng.uniform(-0.8, 0.8), rev=0.6, recto=True)
        # Peristalsis: una oleada grave cada 4 compases.
        ola = en_bucle(lambda m: filtro(rng.normal(0, 1, m), 170.0) * lfo_loop(p, c.compases / 4, 0, m) ** 2 * 4, n)
        p.continua("Ambiente", ola, 0.7 if c.s == "oscuro" else 0.4, rev=0.2)
        if c.s == "rebote":   # Boca: gotas de saliva que caen
            for _ in range(c.compases * 2):
                m = int(0.09 * SR)
                tt = np.arange(m) / SR
                gota = np.sin(2 * np.pi * np.cumsum(1500 * np.exp(-tt * 20) + 500) / SR) * np.exp(-tt * 30)
                p.poner("Ambiente", gota, rng.uniform(0, c.compases * 16), 0.12, rng.uniform(-0.7, 0.7), rev=0.5, recto=True)

    elif c.g in ("trance", "psy"):
        # Respiración: aire que se abre al inhalar y se cierra al exhalar (una respiración cada 4 compases).
        ciclos = c.compases / 4
        for lado, fase in ((-0.6, 0.0), (0.6, 0.03)):
            def aire(m, fase=fase):
                resp = lfo_loop(p, ciclos, fase, m)
                abre = 2600 if c.g == "trance" else 1500
                return filtro_movil(rng.normal(0, 1, m), 350 + abre * resp ** 1.5, 0.9) * (0.2 + 0.8 * resp)
            p.continua("Ambiente", en_bucle(aire, n), 0.5, pan=lado, rev=0.3)
        # Brillo: senos muy agudos que titilan (aire limpio).
        if c.g == "trance":
            for k, grado in enumerate((0, 4, 7)):
                f = f_bucle(p, float(hz(c.raiz + 48 + c.grado(grado))))
                p.continua("Ambiente", np.sin(2 * np.pi * f * t) * lfo_loop(p, 3 + k, k * 0.3), 0.05, pan=(k - 1) * 0.6, rev=0.8)
        else:   # psy: un zumbido grave que vibra
            f = f_bucle(p, float(hz(c.raiz)))
            z = en_bucle(lambda m: filtro(sierra(f, m), 260.0) * (0.6 + 0.4 * lfo_loop(p, 16, 0, m)), n)
            p.continua("Ambiente", z, 0.5, rev=0.2)

    elif c.g == "neuro":
        # Zumbido eléctrico: sierra grave con un filtro que "respira".
        f = f_bucle(p, float(hz(c.raiz + 12)))
        z = en_bucle(lambda m: filtro_movil(sierra(f, m), 260 + 520 * lfo_loop(p, c.compases / 8, 0, m), 2.2), n)
        p.continua("Ambiente", z, 0.5, rev=0.25)
        # Chispas: chasquiditos sueltos al azar.
        def chispas(m):
            x = np.zeros(m)
            for i in rng.integers(0, m, int(m / SR * 14)):
                x[i] = rng.uniform(-1, 1) * rng.uniform(0.2, 1) ** 2
            return filtro(x, [2000, 7000], "bandpass") * 9
        for lado in (-0.5, 0.5):
            p.continua("Ambiente", en_bucle(chispas, n), 0.5, pan=lado, rev=0.2)
        # Sinapsis: blips agudos con eco (las neuronas "hablando").
        for _ in range(c.compases * 3):
            m = int(0.035 * SR)
            blip = np.sin(2 * np.pi * rng.uniform(1500, 3200) * np.arange(m) / SR) * env(m, 0.002, 0.025)
            p.poner("Ambiente", blip, float(rng.integers(0, c.compases * 16)), 0.25, rng.uniform(-0.8, 0.8), rev=0.4, eco=0.5)
        if c.s == "memoria":   # campanas al revés que crecen: recuerdos que vuelven
            for compas in range(2, c.compases, 4):
                nota = c.raiz + 48 + c.grado(c.acorde(compas) + int(rng.choice([0, 2, 4])))
                p.poner("Ambiente", campana(nota, p.paso * 16, 2.0, 1.5, 1.0)[::-1], compas * 16, 0.35, rng.uniform(-0.5, 0.5), rev=0.7)
        if c.s == "jefe":
            ret = en_bucle(lambda m: satura(filtro(rng.normal(0, 1, m), 120.0) * 12, 2) * (0.4 + 0.6 * lfo_loop(p, 8, 0, m)), n)
            p.continua("Ambiente", ret, 0.35, rev=0.2)

    elif c.g == "synth":
        # El torrente: ruido filtrado que se abre con cada latido (la sangre empujada por el corazón).
        latidos = c.compases * 2   # dos "empujones" por compás
        def torrente(m):
            pulso = lfo_loop(p, latidos, 0.0, m) ** 3
            return filtro_movil(rng.normal(0, 1, m), 260 + 900 * pulso, 1.2) * (0.35 + 0.65 * pulso)
        for lado in (-0.5, 0.5):
            p.continua("Ambiente", en_bucle(torrente, n), 0.45, pan=lado, rev=0.3)
        # Brillo de neón: senos agudos que titilan muy despacio.
        for k, grado in enumerate((0, 4, 9)):
            f = f_bucle(p, float(hz(c.raiz + 48 + c.grado(grado))))
            p.continua("Ambiente", np.sin(2 * np.pi * f * t) * lfo_loop(p, 2 + k, k * 0.3), 0.04, pan=(k - 1) * 0.7, rev=0.8)
        if c.s == "agua":   # Riñón: gotas y agua que corre
            for _ in range(c.compases * 2):
                m = int(0.12 * SR)
                tt = np.arange(m) / SR
                gota = np.sin(2 * np.pi * np.cumsum(rng.uniform(900, 1600) * np.exp(-tt * 14) + 380) / SR) * np.exp(-tt * 25)
                p.poner("Ambiente", gota, rng.uniform(0, c.compases * 16), rng.uniform(0.08, 0.18), rng.uniform(-0.8, 0.8), rev=0.8, recto=True)
        if c.s == "latido":   # Corazón: un "lub-dub" grave muy bajito en cada latido (cada 2 tiempos)
            for compas in range(c.compases):
                for s in (0, 8):
                    for k, (d, g) in enumerate(((0, 1.0), (1.6, 0.6))):
                        m = int(0.18 * SR)
                        tt = np.arange(m) / SR
                        golpe = np.sin(2 * np.pi * (50 * tt + 30 * (1 - np.exp(-tt * 25)) / 25)) * np.exp(-tt * 18) * g
                        p.poner("Ambiente", golpe, compas * 16 + s + d, 0.5, 0.0, rev=0.2)
        if c.s in ("oscuro", "jefe"):   # retumbo grave que amenaza
            ret = en_bucle(lambda m: satura(filtro(rng.normal(0, 1, m), 110.0) * 12, 2) * (0.4 + 0.6 * lfo_loop(p, 8, 0, m)), n)
            p.continua("Ambiente", ret, 0.3, rev=0.2)

    elif c.g == "tribal":
        # El "didgeridoo": un zumbido grave cuya boca se abre y se cierra (un formante que se mueve).
        f = f_bucle(p, float(hz(c.raiz)))
        ciclos = c.compases * (2 if c.s == "latido" else 1)
        didge = en_bucle(lambda m: filtro_movil(sierra(f, m), 300 + 500 * lfo_loop(p, ciclos, 0, m) ** 2, 4.0, "bp"), n)
        p.continua("Ambiente", didge, 0.4, rev=0.3)
        # Palo de lluvia cada 4 compases: semillitas que caen.
        for compas in range(0, c.compases, 4):
            m = int(p.paso * 16 * SR)
            tt = np.arange(m) / SR
            granos = np.zeros(m)
            idx = rng.integers(0, m, int(m / SR * 300))
            granos[idx] = rng.uniform(-1, 1, len(idx))
            lluvia = filtro(granos, [2500, 9000], "bandpass") * np.sin(np.pi * tt / tt[-1]) ** 2 * 6
            p.poner("Ambiente", lluvia, compas * 16 + 8, 0.35, float(rng.uniform(-0.6, 0.6)), rev=0.5)
        if c.s == "latido":
            golpes_lub_dub(p, c)
        if c.s == "guerra":   # retumbo grave y fierros lejanos: la tribu contra la máquina
            ret = en_bucle(lambda m: satura(filtro(rng.normal(0, 1, m), 110.0) * 12, 2) * (0.4 + 0.6 * lfo_loop(p, 8, 0, m)), n)
            p.continua("Ambiente", ret, 0.3, rev=0.2)
            for _ in range(c.compases // 2):
                p.poner("Ambiente", metal(float(rng.uniform(180, 420)), 3.0, int(rng.integers(0, 6))),
                        float(rng.integers(0, c.compases * 16)), 0.15, float(rng.uniform(-0.8, 0.8)), rev=0.8)

    elif c.g == "industrial":
        # Zumbido de fábrica (la corriente con sus armónicos, un poco saturada) que sube y baja.
        f = f_bucle(p, float(hz(c.raiz + 12)))
        zumbido = satura(sum(np.sin(2 * np.pi * f * k * t) / k for k in (1, 2, 3, 5)) * 0.7, 2.0) * (0.7 + 0.3 * lfo_loop(p, 4))
        p.continua("Ambiente", zumbido, 0.25 if c.s != "planta" else 0.15, rev=0.2)
        # Golpes de metal lejanos con mucha reverb y vapor que se escapa.
        for _ in range(c.compases):
            p.poner("Ambiente", metal(float(rng.uniform(180, 520)), 3.0, int(rng.integers(0, 6))),
                    float(rng.integers(0, c.compases * 16)), 0.18, float(rng.uniform(-0.8, 0.8)), rev=0.8)
        for _ in range(c.compases // 2):
            p.poner("Ambiente", vapor(0.6, int(rng.integers(0, 4))), float(rng.integers(0, c.compases * 16)), 0.12,
                    float(rng.uniform(-0.8, 0.8)), rev=0.5)
        if c.s == "planta":   # Riñón: agua que corre por los tubos de la planta y gotas
            flujo = en_bucle(lambda m: filtro_movil(rng.normal(0, 1, m), 700 + 400 * lfo_loop(p, c.compases / 2, 0, m), 0.8, "bp"), n)
            p.continua("Ambiente", flujo, 0.35, pan=-0.3, rev=0.4)
            for _ in range(c.compases * 2):
                m = int(0.12 * SR)
                tt = np.arange(m) / SR
                gota = np.sin(2 * np.pi * np.cumsum(rng.uniform(900, 1600) * np.exp(-tt * 14) + 380) / SR) * np.exp(-tt * 25)
                p.poner("Ambiente", gota, rng.uniform(0, c.compases * 16), rng.uniform(0.08, 0.18), rng.uniform(-0.8, 0.8), rev=0.8, recto=True)
        if c.s == "maquina":   # retumbo de máquina grande
            ret = en_bucle(lambda m: satura(filtro(rng.normal(0, 1, m), 110.0) * 12, 2) * (0.4 + 0.6 * lfo_loop(p, 8, 0, m)), n)
            p.continua("Ambiente", ret, 0.35, rev=0.2)

    else:   # liquid (Ojo): gotas que caen en un lugar húmedo y silencioso
        for _ in range(c.compases * 2):
            m = int(0.12 * SR)
            tt = np.arange(m) / SR
            f0 = rng.uniform(900, 1700)
            gota = np.sin(2 * np.pi * np.cumsum(f0 * np.exp(-tt * 14) + 380) / SR) * np.exp(-tt * 25)
            p.poner("Ambiente", gota, rng.uniform(0, c.compases * 16), rng.uniform(0.08, 0.2), rng.uniform(-0.8, 0.8), rev=0.8, recto=True)
        for k, grado in enumerate((0, 2)):   # dos notas agudas que "laten" muy despacio: inquietante
            f = f_bucle(p, float(hz(c.raiz + 36 + c.grado(grado))))
            df = f_bucle(p, f + 0.35) - f
            s = np.sin(2 * np.pi * f * t) + np.sin(2 * np.pi * (f + df) * t)
            p.continua("Ambiente", s * lfo_loop(p, 2, k * 0.5), 0.08, pan=(k - 0.5), rev=0.7)
        viento = en_bucle(lambda m: filtro_movil(rng.normal(0, 1, m), 500 + 600 * lfo_loop(p, 4, 0, m), 1.5, "bp"), n)
        p.continua("Ambiente", viento, 0.25, rev=0.3)


# ---------------------------------------------------------------- Armonía

PUERTA_TRANCE = [1, 0, 1, 1, 0, 1, 1, 0, 1, 0, 1, 1, 0, 1, 1, 1]   # el "trancegate": el pad picado en dieciseisavos


def compuerta_ritmica(p, m, patron, ancho=0.7):
    """Arreglo de volumen que prende y apaga en dieciseisavos según el patrón (con rampitas, sin chasquidos)."""
    t = np.arange(m) / SR
    paso = (t / p.paso).astype(int)
    dentro = (t / p.paso) % 1.0
    abierto = np.array(patron)[paso % len(patron)] * (dentro < ancho)
    return sosfilt(_sos(300.0, "lowpass", 2), abierto.astype(float))


def armonia(p, c):
    rng = c.rng
    pa = p.paso
    pad_base = c.raiz + 24
    for compas in range(c.compases):
        ac, sec, ini = c.acorde(compas), c.seccion(compas), compas * 16
        nuevo = compas % 2 == 0
        if c.g == "acid":
            if nuevo:   # pad cálido de fondo
                for j in (0, 2, 4):
                    p.poner("Armonia", supersaw(pad_base + c.grado(ac + j), pa * 32 + 0.4, 5, 0.12, 1300.0, 0.35, 0.5, c.semilla),
                            ini, 0.22, (j - 2) * 0.25, rev=0.35)
            if c.s == "oscuro":
                if compas % 2 == 1 or sec >= 2:
                    p.poner("Armonia", stab(pad_base + 12 + c.grado(ac), pa * 3, True), ini + 6, 0.3, -0.3, rev=0.3, eco=0.5)
            elif c.s == "jefe":
                for j in (0, 2, 4):
                    p.poner("Armonia", stab(pad_base + 12 + c.grado(ac + j), pa * 2, True), ini + (0 if nuevo else 10), 0.22, (j - 2) * 0.3, rev=0.3, eco=0.3)
            else:   # stabs de órgano de house en el contratiempo
                golpes = [6, 14] if sec == 0 else [3, 6, 11, 14] if c.s == "groove" else [2, 6, 10, 14]
                for s in golpes:
                    for j in (0, 2, 4, 6) if c.s == "groove" else (0, 2, 4):
                        p.poner("Armonia", organo(pad_base + 12 + c.grado(ac + j), pa * 1.5), ini + s, 0.13, (j - 2) * 0.2, rev=0.25, eco=0.25)

        elif c.g == "trance":
            if nuevo:   # pad supersaw con bombeo; el filtro que "respira" se pone al final (despues)
                voces = (0, 2, 4, 7) if c.s != "aireado" else (0, 4, 7, 9)
                for j in voces:
                    p.poner("Armonia", supersaw(pad_base + c.grado(ac + j), pa * 32 + 0.5, 7, 0.2, 5000.0, 0.25, 0.5, c.semilla),
                            ini, 0.16, (j % 3 - 1) * 0.3, rev=0.3, bombeo=True)
                p.poner("Armonia", supersaw(c.raiz + 12 + c.grado(ac), pa * 32 + 0.5, 5, 0.15, 1500.0, 0.25, 0.5, c.semilla),
                        ini, 0.14, rev=0.1, bombeo=True)
                if sec >= 2 and c.s in ("clasico", "aire"):   # en la parte B el pad picado ("trancegate") arriba
                    m = int(pa * 32 * SR)
                    for j in (0, 2, 4):
                        x = supersaw(pad_base + 12 + c.grado(ac + j), pa * 32, 5, 0.18, 7000.0, 0.005, 0.05, 9)
                        p.poner("Armonia", x * compuerta_ritmica(p, m, PUERTA_TRANCE)[:, None], ini, 0.1, (j - 2) * 0.4, rev=0.25, eco=0.25)

        elif c.g == "psy":
            if compas % 4 == 0:   # pad oscuro y largo
                for j in (0, 4):
                    p.poner("Armonia", supersaw(pad_base + c.grado(ac + j), pa * 64 + 0.6, 5, 0.25, 900.0, 0.8, 0.8, c.semilla),
                            ini, 0.2, (j - 2) * 0.3, rev=0.5)
            if sec >= 2 and compas % 2 == 1:   # gritito psicodélico en la parte B
                x = supersaw(pad_base + 24 + c.grado(ac + 4), pa * 6, 3, 0.4, 6000.0, 0.01, 0.2, 5)
                p.poner("Armonia", x, ini + 10, 0.12, rng.uniform(-0.6, 0.6), rev=0.4, eco=0.5)

        elif c.g == "neuro":
            if nuevo:   # pad oscuro de fondo (cuerdas sintéticas)
                for j in (0, 2, 4):
                    brillo = 1600.0 if c.s != "jefe" else 1100.0
                    p.poner("Armonia", supersaw(pad_base + c.grado(ac + j), pa * 32 + 0.4, 5, 0.15, brillo, 0.4, 0.5, c.semilla),
                            ini, 0.2, (j - 2) * 0.3, rev=0.45)
            if c.s in ("clasico", "jefe") and sec >= 1 and compas % 2 == 1:   # stab de neuro con eco
                for j in (0, 2):
                    p.poner("Armonia", stab(pad_base + 12 + c.grado(ac + j), pa * 1.5), ini + 7, 0.16, (j - 1) * 0.4, rev=0.2, eco=0.45)
            if c.s == "memoria" and sec >= 2 and nuevo:   # en la parte B un coro brillante arriba
                for j in (4, 7):
                    p.poner("Armonia", supersaw(pad_base + 12 + c.grado(ac + j), pa * 32 + 0.4, 5, 0.1, 4500.0, 0.8, 0.6, 2),
                            ini, 0.08, (j - 5) * 0.4, rev=0.6)

        elif c.g == "synth":
            if nuevo:   # pad ancho y cálido (como un Juno de los 80), con ataque lento
                brillo = {"oscuro": 1400.0, "jefe": 1700.0, "noche": 1900.0}.get(c.s, 2600.0)
                for j in (0, 2, 4, 6) if c.s in ("agua", "noche") else (0, 2, 4):
                    p.poner("Armonia", supersaw(pad_base + c.grado(ac + j), pa * 32 + 0.6, 6, 0.16, brillo, 0.45, 0.6, c.semilla),
                            ini, 0.17, (j - 2) * 0.3, rev=0.45, bombeo=True)
            if sec >= 1 and c.s in ("outrun", "latido", "jefe"):   # acordes picados en corcheas (el "brass" del outrun)
                m = int(pa * 16 * SR)
                patron = [1, 0] * 8 if c.s != "jefe" else [1, 0, 1, 1] * 4
                for j in (0, 2, 4):
                    x = supersaw(pad_base + 12 + c.grado(ac + j), pa * 16, 4, 0.12, 4200.0, 0.004, 0.04, 6)
                    p.poner("Armonia", x * compuerta_ritmica(p, m, patron, 0.55)[:, None], ini, 0.08, (j - 2) * 0.35, rev=0.3, eco=0.2)
            if c.s == "oscuro" and compas % 2 == 1:   # golpe de sintetizador oscuro con eco
                p.poner("Armonia", stab(pad_base + 12 + c.grado(ac), pa * 3, True), ini + 6, 0.25, -0.3, rev=0.3, eco=0.5)

        elif c.g == "tribal":
            if nuevo:   # el coro de la tribu: una vocal larga en el acorde
                vocal = "u" if c.s == "latido" else "o"
                for j in ((0, 2, 4) if c.s == "aldea" else (0, 4)):
                    p.poner("Armonia", canto(pad_base + c.grado(ac + j), pa * 32 + 0.5, vocal, 3, 0.6), ini, 0.2, (j - 2) * 0.25, rev=0.5)
            if sec >= 1 and compas % 4 == 3:   # ¡HEY! al final de cada frase
                p.poner("Armonia", grito("e" if c.s == "aldea" else "a", compas % 3), ini + 12, 0.25, float(rng.uniform(-0.4, 0.4)), rev=0.4, eco=0.3)
            if c.s == "guerra" and compas % 2 == 1:   # golpe de máquina oscuro con eco
                p.poner("Armonia", stab(pad_base + 12 + c.grado(ac), pa * 3, True), ini + 6, 0.22, -0.3, rev=0.3, eco=0.4)

        elif c.g == "industrial":
            if nuevo:   # pad oscuro de fábrica (el de la planta de agua, más claro)
                brillo = 2200.0 if c.s == "planta" else 1100.0
                for j in (0, 2, 4):
                    p.poner("Armonia", supersaw(pad_base + c.grado(ac + j), pa * 32 + 0.5, 5, 0.2, brillo, 0.6, 0.6, c.semilla),
                            ini, 0.18, (j - 2) * 0.3, rev=0.45)
            if sec >= 1 and c.s != "planta":   # golpes metálicos en el contratiempo
                for s in ((6, 14) if compas % 2 else (6,)):
                    p.poner("Armonia", stab(pad_base + 12 + c.grado(ac), pa * 1.5, True), ini + s, 0.2, 0.3 if s == 6 else -0.3, rev=0.25, eco=0.35)

        else:   # liquid: rhodes con acordes de 4 notas y un pad suave
            for s, largo in ((0, 10), (10, 6)) if sec >= 1 else ((0, 16),):
                for j in (0, 2, 4, 6):
                    p.poner("Armonia", rhodes(pad_base + c.grado(ac + j), pa * largo), ini + s, 0.13 if s == 0 else 0.09, rev=0.4)
            if nuevo:
                for j in (0, 4):
                    p.poner("Armonia", supersaw(pad_base + c.grado(ac + j), pa * 32 + 0.4, 5, 0.12, 1200.0, 0.6, 0.6, c.semilla),
                            ini, 0.12, (j - 2) * 0.3, rev=0.5)

    if c.g == "trance":
        # La respiración: el filtro de los pads se abre (inhala) y se cierra (exhala) cada 4 compases.
        ciclos = c.compases / 4
        def respirar(x):
            m = len(x)
            r = lfo_loop(p, ciclos, 0.0, m)
            hondo = 3600 if c.s == "respira" else 2600
            corte = 600 + hondo * r ** 1.4
            ex = min(m, SR)
            y = np.stack([filtro_movil(np.concatenate([x[-ex:, k], x[:, k]]), np.concatenate([corte[-ex:], corte]), 1.1)[ex:]
                          for k in range(2)], 1)
            if c.s == "respira":   # en Pulmones también crece el volumen al inhalar
                y = y * (0.55 + 0.45 * r)[:, None]
            return y
        p.despues["Armonia"] = respirar


# ---------------------------------------------------------------- Melodía

ARPEGIO = [0, 2, 4, 7, 4, 2, 4, 7, 0, 2, 4, 9, 7, 4, 2, 4]          # grados sobre el acorde (trance)
ARPEGIO_PSY = [0, 0, 1, 0, 4, 0, 1, 7, 0, 0, 1, 0, 4, 7, 1, 0]      # psy: tónica insistente con la 2a menor
ARPEGIO_SYNTH = [0, 4, 7, 9, 7, 4, 2, 4, 0, 4, 7, 11, 9, 7, 4, 2]   # synthwave: sube y baja como luces de neón
ARPEGIO_TRIBAL = [0, 2, 3, 4, 2, 0, 3, 4, 0, 2, 4, 6, 4, 3, 2, 0]   # pentatónica (grados 0, 2, 3, 4 y 6)
ARPEGIO_MAQUINA = [0, 0, 7, 0, 1, 0, 4, 0, 0, 7, 0, 1, 4, 0, 6, 0]  # industrial: insistente como una máquina


def melodia(p, c):
    rng = c.rng
    pa = p.paso
    base = c.raiz + 36
    for compas in range(c.compases):
        ac, sec, ini = c.acorde(compas), c.seccion(compas), compas * 16
        if c.g == "trance":
            lento = c.s in ("respira", "aireado")
            for s in range(16):
                if (sec == 0 or lento) and s % 2:
                    continue
                if sec == 1 and s % 4 == 3:
                    continue
                g = ARPEGIO[s] + (7 if sec == 3 and s >= 8 else 0)
                fuerza = 1.0 if s % 4 == 0 else 0.7
                largo = pa * (2.2 if lento else 1.1)
                nota = base + c.grado(ac + g)
                if c.s == "aireado":
                    x = campana(nota, largo + 0.3, 2.0, 1.2, 3.0)
                else:
                    x = pluck_trance(nota, largo + 0.1, 1.3 if c.s == "aire" else 1.0)
                p.poner("Melodia", x, ini + s, 0.22 * fuerza, (0.35 if s % 4 == 2 else -0.15), rev=0.25, eco=0.35, bombeo=True)
            continue
        if c.g == "tribal":
            for s in range(16):
                if s % 2 and (sec == 0 or c.s == "latido"):
                    continue
                if sec == 0 and s % 4 == 2:
                    continue
                g = ARPEGIO_TRIBAL[s] + (7 if sec == 3 and s >= 8 else 0)
                p.poner("Melodia", marimba(base + c.grado(ac + g), pa * 3), ini + s, 0.26 if s % 4 == 0 else 0.18,
                        0.5 * np.sin(s * 0.9), rev=0.25, eco=0.2)
            if sec >= 2 and c.s in ("aldea", "guerra"):   # la flauta canta la frase arriba
                for s, g, largo, fuerza in frase(c, compas):
                    p.poner("Melodia", flauta(base + 12 + c.grado(ac + g), pa * min(largo, 4) + 0.1), ini + s, 0.18 * fuerza, 0.0, rev=0.45, eco=0.3)
            continue
        if c.g == "industrial":
            for s in range(16):
                if s % 2 and sec < 2:
                    continue
                if c.s == "planta" and s % 4:
                    continue
                g = ARPEGIO_MAQUINA[s]
                x = campana(base + c.grado(ac + g), pa * 2.5, 1.41 if c.s != "planta" else 2.0, 2.2, 3.0)
                p.poner("Melodia", x, ini + s, 0.22 if s % 4 == 0 else 0.15, 0.45 * np.sin(s * 1.3), rev=0.3, eco=0.35, bombeo=True)
            continue
        if c.g == "synth":
            lento = c.s in ("noche", "agua")
            for s in range(16):
                if (sec == 0 or lento) and s % 2:
                    continue
                g = ARPEGIO_SYNTH[s] + (7 if sec == 3 and s >= 8 else 0)
                nota = base + c.grado(ac + g)
                largo = pa * (2.4 if lento else 1.2)
                x = campana(nota + 12, largo + 0.4, 2.0, 1.2, 2.6) if c.s == "agua" else pluck(nota, largo + 0.15)
                p.poner("Melodia", x, ini + s, (0.24 if s % 4 == 0 else 0.17), 0.4 * np.sin(s * 0.7), rev=0.3, eco=0.45, bombeo=True)
            continue
        if c.g == "psy":
            for s in range(16):
                if sec == 0 and s % 4 not in (0, 3):
                    continue
                g = ARPEGIO_PSY[s] + (7 if sec == 3 and s % 8 == 7 else 0)
                x = bajo_pluck(base + c.grado(ac + g), pa * 0.9, 2.5, 22.0)
                p.poner("Melodia", x, ini + s, 0.16 if s % 4 == 0 else 0.11, 0.4 * np.sin(s * 0.8), rev=0.2, eco=0.4)
            continue

        for s, g, largo, fuerza in frase(c, compas):
            nota = base + c.grado(ac + g)
            if c.g == "acid":
                if c.s == "oscuro" and (compas % 2 or s > 8):
                    continue
                x = burbuja(nota, pa * min(largo, 2.5) * 0.9 + 0.05) if c.s != "oscuro" else stab(nota - 12, pa * 1.2, True)
                p.poner("Melodia", x, ini + s, 0.3 * fuerza, -0.3 if s % 2 else 0.3, rev=0.2, eco=0.3)
            elif c.g == "neuro":
                if c.s == "memoria":
                    x = campana(nota + 12, pa * largo + 0.6, 2.0, 1.8, 1.6)
                    p.poner("Melodia", x, ini + s, 0.3 * fuerza, -0.3 if s % 2 else 0.3, rev=0.45, eco=0.5)
                elif c.s == "jefe":
                    x = campana(nota, pa * largo + 0.3, 1.41, 3.0, 3.0)    # razón rara = disonante
                    p.poner("Melodia", x, ini + s, 0.3 * fuerza, -0.3 if s % 2 else 0.3, rev=0.3, eco=0.3)
                else:
                    x = campana(nota + (12 if c.s == "rodante" else 0), pa * largo + 0.3, 3.5, 2.5, 2.6)
                    p.poner("Melodia", x, ini + s, 0.3 * fuerza, -0.3 if s % 2 else 0.3, rev=0.3, eco=0.35)
            else:   # liquid
                if sec == 0 and compas % 2:
                    continue
                x = campana(nota, pa * largo + 0.8, 2.0, 1.1, 1.4)
                p.poner("Melodia", x, ini + s, 0.3 * fuerza, -0.25 if s % 2 else 0.25, rev=0.55, eco=0.4)


# ---------------------------------------------------------------- Bajo

ACID = {   # densidad, resonancia, distorsión, caída del filtro, corte bajo y alto, onda, octava, slides
    "rebote":  (0.55, 5.5, 1.8, 0.12, 300, 1900, "cuadrada", 0.3, 0.2),
    "clasico": (0.70, 7.5, 2.5, 0.16, 220, 2300, "sierra", 0.22, 0.22),
    "groove":  (0.75, 6.5, 2.0, 0.20, 250, 2000, "sierra", 0.2, 0.3),
    "oscuro":  (0.65, 9.0, 4.0, 0.10, 150, 1600, "sierra", 0.15, 0.18),
    "jefe":    (0.85, 8.5, 4.5, 0.12, 260, 2700, "sierra", 0.25, 0.2),
}


def patron_acid(rng, densidad, octava, slides):
    """Un compás de 303: (paso, grado, octava, acento, slide)."""
    pasos = []
    for s in range(16):
        if s % 4 == 0 or rng.random() < densidad:
            g = int(rng.choice([0, 0, 0, 0, 2, 4, 6, 7, -3]))
            pasos.append([s, g, 12 if rng.random() < octava else 0, rng.random() < (0.45 if s % 4 == 2 else 0.2), rng.random() < slides])
    hay = {e[0] for e in pasos}
    for e in pasos:
        e[4] = e[4] and (e[0] + 1) in hay
    return pasos


def mutar(rng, patron, cambios, densidad, octava, slides):
    nuevo = [list(e) for e in patron]
    otro = patron_acid(rng, densidad, octava, slides)
    por_paso = {e[0]: e for e in otro}
    for s in rng.choice(16, cambios, replace=False):
        nuevo = [e for e in nuevo if e[0] != s]
        if s in por_paso:
            nuevo.append(list(por_paso[s]))
    nuevo.sort()
    hay = {e[0] for e in nuevo}
    for e in nuevo:
        e[4] = e[4] and (e[0] + 1) in hay
    return nuevo


def linea_acid(p, c, base, patrones, q, distorsion, caida, abajo, arriba, onda, puerta=0.55):
    """La 303: una voz con slides y acentos; el filtro se abre poco a poco a lo largo del loop y se vuelve a cerrar."""
    notas = []
    for compas in range(c.compases):
        ac, sec = c.acorde(compas), c.seccion(compas)
        patron = patrones[min(len(patrones) - 1, sec // 2 * 2 + (1 if compas % 4 == 3 else 0))]
        for s, g, octv, acc, sl in patron:
            notas.append(NotaLinea(compas * 16 + s, 1, base + c.grado(ac + g) + octv, acc, sl))

    def sintetizar(d):
        total = len(d["f"])
        barrido = curva_loop(p, total, d["W"], lambda ph: (0.5 - 0.5 * np.cos(2 * np.pi * ph)) ** 1.3 + 0.12 * np.sin(2 * np.pi * ph * 16))
        corte_base = abajo * (arriba / abajo) ** np.clip(barrido, 0, 1)
        acc = d["acento"]
        envf = np.exp(-d["desde"] / (caida * (1 - 0.4 * acc)))
        corte = np.minimum(corte_base * (1 + (2.5 + 2.5 * acc) * envf), 12000)
        osc = sierra(d["f"]) if onda == "sierra" else cuadrada(d["f"]) * 1.4
        y = filtro_movil(osc, corte, q + 3 * acc)
        y = filtro_movil(y, corte, 0.6)
        y = y * d["compuerta"] * (1 + 0.45 * acc)
        return filtro(filtro(satura(y * distorsion, distorsion), 35.0, "highpass"), 8500.0)

    return render_linea(p, notas, sintetizar, puerta)


RITMO_REESE = {   # (paso, largo) en cada compás
    "rodante": [(0, 3), (3, 1), (4, 2), (7, 1), (8, 3), (11, 1), (12, 2), (14, 2)],
    "clasico": [(0, 4), (6, 2), (10, 3), (14, 2)],
    "memoria": [(0, 6), (7, 3), (10, 6)],
    "jefe":    [(0, 2), (3, 2), (6, 1), (7, 3), (10, 2), (12, 1), (13, 3)],
    "sigilo":  [(0, 8), (10, 6)],
}


def notas_reese(c, variar=True):
    rng = np.random.default_rng(c.semilla + 11)
    notas = []
    movs = {"rodante": [0, 1, 3], "clasico": [0, 1, 2, 4], "memoria": [0, 3], "jefe": [1, 2, 4, 2], "sigilo": [0, 3]}[c.s]
    for compas in range(c.compases):
        ac, sec = c.acorde(compas), c.seccion(compas)
        for k, (s, largo) in enumerate(RITMO_REESE[c.s]):
            g = ac
            if variar and sec >= 2 and k == len(RITMO_REESE[c.s]) - 1:
                g = ac + 4                       # la parte B termina el compás en la quinta
            salto = -12 if (c.s == "jefe" and k % 3 == 1) else 0
            notas.append(NotaLinea(compas * 16 + s, largo, c.raiz + c.grado(g) + salto, mov=int(rng.choice(movs)), slide=(c.s == "rodante" and k == 2)))
    return notas


def sint_reese(p, abajo, rango, q, distorsion, techo=None):
    """Reese (neurofunk): 3 sierras desafinadas con un filtro que se mueve al ritmo (abre, vaivén, cierra...).
    techo: corta lo más agudo (para un reese tibio, como el del liquid)."""
    def sintetizar(d):
        f, desde, mov = d["f"], d["desde"], d["mov"]
        x = sierra(f * 0.993) + sierra(f * 1.007, fase0=0.37) + 0.6 * sierra(f * 2.002, fase0=0.71)
        paso = p.paso
        movimiento = np.select(
            [mov == 0, mov == 1, mov == 2, mov == 3],
            [np.minimum(1, desde / (2 * paso)),
             0.5 - 0.5 * np.cos(2 * np.pi * desde / (2 * paso)),
             0.5 - 0.5 * np.cos(2 * np.pi * desde / paso),
             np.exp(-desde / 0.12)],
            0.5 - 0.5 * np.cos(2 * np.pi * desde / (4 * paso / 3)))
        y = filtro_movil(x, abajo + rango * movimiento, q)
        y = satura(y * distorsion, distorsion)
        if techo:
            y = filtro(y, float(techo), orden=4)
        return filtro(y * d["compuerta"], 90.0, "highpass")
    return sintetizar


def sint_sub(d):
    return filtro(np.sin(2 * np.pi * np.cumsum(d["f"]) / SR) * d["compuerta"], 160.0)


def bajo(p, c):
    pa = p.paso
    if c.g == "acid":
        densidad, q, dist, caida, abajo, arriba, onda, octava, slides = ACID[c.s]
        rng = np.random.default_rng(c.semilla + 3)
        a = patron_acid(rng, densidad, octava, slides)
        a2 = mutar(rng, a, 4, densidad, octava, slides)        # final de frase
        b = mutar(rng, a, 7, densidad, octava + 0.1, slides)   # parte B
        b2 = mutar(rng, b, 4, densidad, octava + 0.15, slides)
        base = c.raiz + (12 if c.s == "rebote" else 0)
        x = linea_acid(p, c, base, [a, a2, b, b2], q, dist, caida, abajo, arriba, onda)
        p.continua("Bajo", x, 0.5, bombeo=True, eco=0.08 if c.s == "groove" else 0)

    elif c.g == "trance":
        patron = {"aire": [2, 3, 6, 7, 10, 11, 14, 15], "clasico": [1, 2, 3, 5, 6, 7, 9, 10, 11, 13, 14, 15],
                  "respira": [2, 6, 10, 14], "aireado": [2, 3, 6, 7, 10, 11, 14, 15]}[c.s]
        largo = 1.8 if c.s == "respira" else 0.85
        for compas in range(c.compases):
            ac, sec = c.acorde(compas), c.seccion(compas)
            for s in patron:
                octv = 12 if (sec >= 2 and s % 4 == 3) else 0
                x = bajo_pluck(c.raiz + c.grado(ac) + octv, pa * largo, 0.9 if c.s != "aireado" else 0.6, 9.0)
                p.poner("Bajo", x, compas * 16 + s, 0.42, bombeo=True)

    elif c.g == "psy":
        for compas in range(c.compases):
            ac, sec = c.acorde(compas), c.seccion(compas)
            for s in range(16):
                if s % 4 == 0:
                    continue   # en el psy el bajo nunca pisa al bombo: "bum-ba-ba-ba"
                g = ac
                if s == 15 and compas % 2:
                    g = ac + 1          # la 2a menor al final: tensión típica del psy
                if sec >= 2 and s in (7, 11) and compas % 4 == 3:
                    g = ac + 7
                x = bajo_pluck(c.raiz + c.grado(g), pa * 0.75, 1.1, 24.0)
                p.poner("Bajo", x, compas * 16 + s, 0.45)

    elif c.g == "synth":
        # Corcheas que saltan de octava (el bajo del outrun); el oscuro y el jefe en dieciseisavos.
        rapido = c.s in ("oscuro", "jefe", "latido")
        for compas in range(c.compases):
            ac, sec = c.acorde(compas), c.seccion(compas)
            for s in range(0, 16, 1 if rapido else 2):
                octv = 12 if (s // (1 if rapido else 2)) % 2 else 0
                if c.s == "noche" and octv:
                    continue
                if c.s == "agua" and s % 4:
                    continue
                largo = pa * (0.9 if rapido else 1.7 if c.s != "agua" else 3.6)
                x = bajo_pluck(c.raiz + c.grado(ac) + octv, largo, 0.8 if c.s != "jefe" else 1.1, 7.0 if c.s != "agua" else 3.0)
                p.poner("Bajo", x, compas * 16 + s, 0.42 if not octv else 0.34, bombeo=True)

    elif c.g == "tribal":
        # Bajo de madera grave en el ritmo 3-3-2 que lleva la danza (en el Corazón, al ritmo del latido).
        golpes = (0, 2, 8, 10) if c.s == "latido" else (0, 3, 6, 8, 11, 14)
        for compas in range(c.compases):
            ac, sec = c.acorde(compas), c.seccion(compas)
            for k, s in enumerate(golpes):
                octv = 12 if (sec >= 2 and k == 2) else 0
                x = bajo_pluck(c.raiz + c.grado(ac) + octv, pa * 2.2, 0.5, 6.0)
                p.poner("Bajo", x, compas * 16 + s, 0.45 if k == 0 else 0.36, bombeo=True)
            if c.s == "guerra" and sec >= 1:   # y el bajo de máquina en los huecos
                for s in (1, 5, 9, 13):
                    p.poner("Bajo", bajo_saturado(c.raiz + c.grado(ac) + 12, pa * 0.8), compas * 16 + s, 0.22, bombeo=True)

    elif c.g == "industrial":
        # EBM: dieciseisavos saturados que golpean fuera del bombo (en la planta, corcheas más tranquilas).
        for compas in range(c.compases):
            ac, sec = c.acorde(compas), c.seccion(compas)
            for s in range(16):
                if c.s == "planta":
                    if s % 2 or s in (0, 10):
                        continue
                elif s % 4 == 0:
                    continue
                octv = 12 if (s % 8 == 7 and sec >= 1) else 0
                if c.s == "planta":   # la planta de agua: bajo limpio y redondo
                    x = bajo_pluck(c.raiz + c.grado(ac) + octv, pa * 1.6, 0.7, 8.0)
                else:
                    x = bajo_saturado(c.raiz + c.grado(ac) + octv, pa * 0.85)
                p.poner("Bajo", x, compas * 16 + s, 0.38, bombeo=True)

    else:   # neuro y liquid: sub limpio + reese que se mueve
        notas = notas_reese(c)
        sub = render_linea(p, notas, sint_sub, puerta=0.92, suavizar=60.0)
        p.continua("Bajo", sub, 0.55, bombeo=True)
        if c.g == "liquid":
            x = render_linea(p, notas, sint_reese(p, 200, 320, 1.2, 1.2, 900), puerta=0.92, suavizar=40.0)
            p.continua("Bajo", x, 0.22, bombeo=True)
        else:
            x = render_linea(p, notas, sint_reese(p, 240, 700 if c.s != "jefe" else 1000, 2.2, 2.4), puerta=0.9, suavizar=60.0)
            p.continua("Bajo", x, 0.3, bombeo=True)


# ---------------------------------------------------------------- Percusión

def percusion(p, c):
    rng = c.rng
    for compas in range(c.compases):
        sec, ini = c.seccion(compas), compas * 16
        fin8 = compas % 8 == 7
        if c.g == "acid":
            tipo = "techno" if c.s == "oscuro" else "house"
            for s in (0, 4, 8, 12):
                if fin8 and s == 12 and c.s != "jefe":
                    continue                           # se quita el último bombo antes del cambio de parte
                p.bombo("Percusion", bombo(tipo), ini + s, 0.55)
            if c.s == "oscuro":
                for s in (4, 12):
                    p.poner("Percusion", caja("house", 1), ini + s, 0.18, rev=0.3)
                for s in range(16):
                    p.poner("Percusion", platos("cerrado"), ini + s, 0.09 if s % 4 == 2 else 0.05, 0.3)
            else:
                for s in (4, 12):
                    p.poner("Percusion", palmada(), ini + s, 0.3, rev=0.25)
                if c.s == "jefe" and sec == 3:
                    for s in (0, 8):
                        p.poner("Percusion", palmada(1), ini + s, 0.18, rev=0.2)
            for s in (2, 6, 10, 14):
                p.poner("Percusion", platos("abierto"), ini + s, 0.13, 0.25)
            if sec >= 1:
                for s in range(16):
                    if s % 4 != 2:
                        p.poner("Percusion", platos("shaker" if c.s in ("groove", "rebote") else "cerrado"), ini + s,
                                0.06 if s % 2 else 0.08, -0.35)
            if sec == 3:
                for s in (0, 4, 8, 12):
                    p.poner("Percusion", platos("ride"), ini + s + 2, 0.05, 0.5)
            if fin8:   # fill: palmadas que se juntan
                for s in (13, 14, 15):
                    p.poner("Percusion", palmada(2), ini + s, 0.1 + 0.05 * (s - 13), rev=0.3)

        elif c.g in ("trance", "psy"):
            tipo = "psy" if c.g == "psy" else "trance"
            for s in (0, 4, 8, 12):
                if fin8 and s >= 8 and c.g == "trance":
                    continue
                p.bombo("Percusion", bombo(tipo), ini + s, 0.55)
            for s in (4, 12):
                p.poner("Percusion", palmada(), ini + s, 0.26 if c.g == "trance" else 0.18, rev=0.3)
            for s in (2, 6, 10, 14):
                p.poner("Percusion", platos("abierto"), ini + s, 0.13, 0.25)
            if sec >= 1 or c.g == "psy":
                for s in range(16):
                    if s % 4 != 2:
                        p.poner("Percusion", platos("cerrado"), ini + s, 0.07 if s % 4 == 0 else 0.045, -0.3)
            if sec >= 2:
                for s in (0, 4, 8, 12):
                    p.poner("Percusion", platos("ride"), ini + s + 2, 0.05, 0.45)
            if compas % 8 == 0:
                p.poner("Percusion", platos("crash"), ini, 0.12, rev=0.3)

        elif c.g == "neuro":
            # Breakbeat de dos pasos: bombo en 1 y en el "y" del 3, caja en 2 y 4 (a 174 suena "partido").
            bombos = [0, 10] if compas % 4 != 3 else [0, 7, 10]
            if c.s == "rodante" and sec >= 1:
                bombos = [0, 6, 10] if compas % 2 else [0, 10, 11]
            if c.s == "jefe" and sec >= 2:
                bombos = [0, 3, 10, 13]
            for s in bombos:
                p.bombo("Percusion", bombo("dnb"), ini + s, 0.5)
            for s in (4, 12):
                p.poner("Percusion", caja("dnb"), ini + s, 0.42, rev=0.22)
            fantasmas = [7, 15] if compas % 2 else [9, 14]
            for s in fantasmas:
                p.poner("Percusion", caja("dnb", 2), ini + s, 0.08, rev=0.1)
            for s in range(0, 16, 2):
                p.poner("Percusion", platos("cerrado"), ini + s, 0.09 if s % 4 == 2 else 0.06, 0.3)
            if sec >= 1:
                for s in range(1, 16, 2):
                    p.poner("Percusion", platos("shaker"), ini + s, 0.05, -0.3)
            if sec >= 2:
                for s in (2, 6, 10, 14):
                    p.poner("Percusion", platos("ride"), ini + s, 0.045, 0.5)
            if fin8:   # redoble de caja que sube
                for s in (12, 13, 14, 15):
                    p.poner("Percusion", caja("dnb", 3), ini + s + 0.5, 0.1 + 0.04 * (s - 12), rev=0.2)

        elif c.g == "synth":
            # Caja de ritmos de los 80: bombo, caja con reverb cortada en 2 y 4, hi-hats en corcheas.
            bombos = (0, 4, 8, 12) if c.s in ("outrun", "oscuro", "jefe", "latido") else (0, 8) if c.s == "noche" else (0, 7, 8)
            for s in bombos:
                if fin8 and s == 12 and c.s != "jefe":
                    continue
                p.bombo("Percusion", bombo("synth"), ini + s, 0.55)
            for s in (4, 12):
                p.poner("Percusion", caja_cortada(compas % 2), ini + s, 0.4 if c.s != "agua" else 0.28)
            for s in range(0, 16, 2):
                p.poner("Percusion", platos("cerrado"), ini + s, 0.08 if s % 4 == 2 else 0.05, 0.3)
            if sec >= 1:
                for s in (2, 6, 10, 14):
                    p.poner("Percusion", platos("abierto"), ini + s, 0.07, -0.3)
            if sec >= 2 and c.s in ("outrun", "jefe", "latido"):
                for s in range(1, 16, 2):
                    p.poner("Percusion", platos("shaker"), ini + s, 0.04, -0.4)
            if compas % 8 == 0:
                p.poner("Percusion", platos("crash"), ini, 0.12, rev=0.3)
            if fin8:   # redoble de toms que baja (marca de los 80)
                for k, s in enumerate((12, 13, 14, 15)):
                    p.poner("Percusion", tom(-k * 3), ini + s, 0.22, (k - 1.5) * 0.5, rev=0.35)

        elif c.g == "tribal":
            # Taiko: el golpe grande. En el Corazón hace "lub-dub" cada medio compás (un latido).
            if c.s == "latido":
                for s in (0, 8):
                    p.bombo("Percusion", tambor("taiko"), ini + s, 0.6)
                    p.poner("Percusion", tambor("taiko", 2), ini + s + 1.5, 0.38)
            else:
                for s in ((0, 10) if compas % 2 == 0 else (0, 6, 10)):
                    p.bombo("Percusion", tambor("taiko" if s == 0 else "surdo"), ini + s, 0.55 if s == 0 else 0.4)
            # Djembe: bajo, tonos y golpes (slaps).
            patron = {0: "djembe_bajo", 2: "djembe_tono", 3: "djembe_tono", 4: "djembe_golpe", 7: "djembe_tono",
                      10: "djembe_tono", 11: "djembe_tono", 12: "djembe_golpe", 14: "djembe_golpe", 15: "djembe_tono"}
            for s, tipo in patron.items():
                if sec == 0 and s in (3, 11, 15):
                    continue
                p.poner("Percusion", tambor(tipo, s % 3), ini + s, 0.3 if "golpe" in tipo else 0.24,
                        -0.35 if "tono" in tipo else 0.25, rev=0.15)
            for s in range(16):   # sonajas
                p.poner("Percusion", platos("shaker"), ini + s, 0.06 if s % 4 == 2 else 0.035, 0.45)
            if sec >= 1:   # clave 3-2 (de dos compases)
                for s in ((0, 6, 12) if compas % 2 == 0 else (4, 8)):
                    p.poner("Percusion", tambor("palo", 1), ini + s, 0.12, -0.5, rev=0.2)
            if c.s == "guerra":   # la máquina: caja industrial con metal en 2 y 4
                for s in (4, 12):
                    p.poner("Percusion", caja("industrial", compas % 2), ini + s, 0.25, rev=0.25)
                    p.poner("Percusion", metal(620.0, 9.0, 1), ini + s, 0.1, 0.2, rev=0.3)
            if fin8:   # redoble de tambores que crece
                for k, s in enumerate((8, 10, 12, 13, 14, 15)):
                    p.poner("Percusion", tambor("djembe_bajo" if k < 3 else "taiko", k % 3), ini + s, 0.18 + 0.05 * k,
                            (k % 2 - 0.5) * 0.6, rev=0.25)

        elif c.g == "industrial":
            planta = c.s == "planta"
            for s in ((0, 10) if planta else (0, 4, 8, 12)):
                if fin8 and s == 12:
                    continue
                p.bombo("Percusion", bombo("industrial"), ini + s, 0.55)
            for s in (4, 12):   # caja con un golpe de lámina encima
                p.poner("Percusion", caja("industrial", compas % 2), ini + s, 0.32 if not planta else 0.24, rev=0.25)
                p.poner("Percusion", metal(620.0, 9.0, 1), ini + s, 0.12, 0.2, rev=0.3)
            for s in range(0, 16, 1 if (sec >= 1 and not planta) else 2):
                p.poner("Percusion", platos("cerrado"), ini + s, 0.06 if s % 4 == 2 else 0.035, 0.3)
            for s in (2, 6, 10, 14):   # pistones de vapor en el contratiempo
                if planta and s in (6, 14):
                    continue
                p.poner("Percusion", vapor(0.12, s % 3), ini + s, 0.12, -0.3)
            if sec >= 2:   # yunque
                for s in (3, 7, 11):
                    if rng.random() < 0.6:
                        p.poner("Percusion", metal(880.0, 12.0, 2), ini + s, 0.08, 0.5, rev=0.2)
            if compas % 8 == 0:
                p.poner("Percusion", platos("crash"), ini, 0.1, rev=0.3)
            if fin8:   # metales que bajan
                for k, s in enumerate((12, 13, 14, 15)):
                    p.poner("Percusion", metal(700.0 - 120 * k, 8.0, k), ini + s, 0.16, (k - 1.5) * 0.4, rev=0.3)

        else:   # liquid: más suave y con shaker
            for s in ([0, 10] if compas % 4 != 3 else [0, 10, 13]):
                p.bombo("Percusion", bombo("liquid"), ini + s, 0.45)
            for s in (4, 12):
                p.poner("Percusion", caja("liquid"), ini + s, 0.3, rev=0.35)
            for s in (7, 9) if compas % 2 else (14,):
                p.poner("Percusion", caja("liquid", 2), ini + s, 0.05, rev=0.2)
            for s in range(16):
                p.poner("Percusion", platos("shaker"), ini + s, 0.06 if s % 2 == 0 else 0.035, -0.3)
            if sec >= 2:
                for s in (2, 6, 10, 14):
                    p.poner("Percusion", platos("ride"), ini + s, 0.04, 0.5)


# ---------------------------------------------------------------- Combate

def subidas(p, c, capa, g=0.25):
    """Antes de cada parte nueva: ruido que sube 2 compases, platillo al revés y platillo + golpe al llegar."""
    pa = p.paso
    for compas in range(8, c.compases + 1, 8):
        sube = subida(pa * 32, c.semilla + compas, 1.0)
        p.poner(capa, sube, compas * 16 - 32, g, 0.0, rev=0.3)
        reves = platillo_al_reves(pa * 8)
        p.poner(capa, reves, compas * 16 - len(reves) / SR / pa, g * 0.5, rev=0.2)
        p.poner(capa, platos("crash"), (compas % c.compases) * 16, 0.18, rev=0.3)
        p.poner(capa, golpe_grave(c.raiz, 1.0, compas), (compas % c.compases) * 16, 0.3, rev=0.3)


def combate(p, c):
    rng = c.rng
    pa = p.paso
    if c.g == "acid":
        # Segunda 303, una octava arriba, en dieciseisavos seguidos y con más resonancia.
        densidad, q, dist, caida, abajo, arriba, onda, octava, slides = ACID[c.s]
        r = np.random.default_rng(c.semilla + 5)
        a = patron_acid(r, 0.95, 0.35, 0.3)
        b = mutar(r, a, 6, 0.95, 0.35, 0.3)
        x = linea_acid(p, c, c.raiz + 12 + (12 if c.s == "rebote" else 0), [a, a, b, b], q + 2, dist + 1, caida * 0.8,
                       abajo * 1.5, arriba * 1.6, "sierra", 0.5)
        p.continua("Combate", x, 0.32, pan=0.2, eco=0.25, bombeo=True)
        for compas in range(c.compases):
            ini = compas * 16
            if c.s in ("rebote", "groove", "clasico"):
                for s in ((3, 6, 11, 14) if compas % 2 else (3, 6, 10)):
                    p.poner("Combate", cencerro(), ini + s, 0.09, 0.45, rev=0.2)
            else:
                for s in (3, 7, 11, 15):
                    p.poner("Combate", aro(), ini + s, 0.12, 0.4, rev=0.2, eco=0.2)
            for s in (4, 12):
                p.poner("Combate", palmada(3), ini + s + 0.1, 0.16, -0.3, rev=0.35)
            if c.s == "jefe":
                for s in (2, 6, 10, 14):
                    p.bombo("Combate", bombo("house"), ini + s + 1.5, 0.2)
            if compas % 8 == 3:
                for k, s in enumerate((12, 13, 14, 15)):
                    p.poner("Combate", tom(-k * 3), ini + s, 0.2, (k - 1.5) * 0.4, rev=0.2)
        subidas(p, c, "Combate")

    elif c.g == "trance":
        # El "himno": lead supersaw con la melodía en notas largas, y redobles que crecen antes de cada parte.
        for compas in range(c.compases):
            ac, sec, ini = c.acorde(compas), c.seccion(compas), compas * 16
            for s, g, largo, fuerza in frase(c, compas):
                nota = c.raiz + 36 + c.grado(ac + g) + (12 if sec >= 2 else 0)
                x = supersaw(nota, pa * min(largo, 4) + 0.12, 7, 0.22, 6500.0, 0.01, 0.12, 4)
                p.poner("Combate", x, ini + s, 0.2 * fuerza, 0.0, rev=0.35, eco=0.3, bombeo=True)
            if compas % 8 >= 4:   # redoble: negras, luego corcheas, luego dieciseisavos
                fase = compas % 8 - 4
                cada = {0: 4, 1: 4, 2: 2, 3: 1}[fase]
                for s in range(0, 16, cada):
                    fuerza = (fase * 16 + s) / 64
                    p.poner("Combate", caja("house", 4), ini + s, 0.04 + 0.16 * fuerza, rev=0.4)
            for s in (2, 6, 10, 14):
                p.poner("Combate", platos("abierto"), ini + s, 0.07, -0.4)
        subidas(p, c, "Combate", 0.3)

    elif c.g == "psy":
        r = np.random.default_rng(c.semilla + 5)
        a = patron_acid(r, 0.9, 0.4, 0.35)
        b = mutar(r, a, 8, 0.9, 0.4, 0.35)
        x = linea_acid(p, c, c.raiz + 24, [a, a, b, b], 10.0, 4.0, 0.1, 400, 4200, "sierra", 0.5)
        p.continua("Combate", x, 0.25, pan=-0.2, eco=0.4)
        for compas in range(c.compases):
            ini = compas * 16
            for s in range(16):
                p.poner("Combate", platos("cerrado"), ini + s + 0.5, 0.05, 0.5)
            if compas % 2:
                p.poner("Combate", zap(compas % 3), ini + 14, 0.2, rng.uniform(-0.7, 0.7), rev=0.3, eco=0.4)
            if compas % 8 == 7:
                for s in range(8, 16):
                    p.poner("Combate", caja("house", 5), ini + s, 0.05 + 0.02 * (s - 8), rev=0.3)
        subidas(p, c, "Combate", 0.3)

    elif c.g == "neuro":
        # "Growl": bajo medio que "habla" (formantes que van de "o" a "a"), con FM.
        notas = notas_reese(c)

        def growl(d):
            f, desde, mov = d["f"], d["desde"], d["mov"]
            ciclo = np.where(mov % 2 == 0, 2 * p.paso, p.paso)
            m = 0.5 - 0.5 * np.cos(2 * np.pi * desde / ciclo)
            x = np.sin(2 * np.pi * np.cumsum(f * 2) / SR + (1.5 + 2.5 * m) * np.sin(2 * np.pi * np.cumsum(f) / SR)) + 0.5 * sierra(f * 2)
            y = filtro_movil(x, 380 + 450 * m, 6.0, "bp") + 0.8 * filtro_movil(x, 800 + 700 * m, 7.0, "bp")
            return filtro(satura(y * 3, 3) * d["compuerta"], 150.0, "highpass")

        x = render_linea(p, notas, growl, puerta=0.85, suavizar=80.0)
        p.continua("Combate", x, 0.35, bombeo=True)
        for compas in range(c.compases):
            ini, sec = compas * 16, c.seccion(compas)
            for s in range(16):   # dieciseisavos y cajas fantasma (estilo "amen")
                if s % 2:
                    p.poner("Combate", platos("cerrado"), ini + s, 0.06, -0.5)
            for s in ((6, 9, 15) if compas % 2 else (3, 9, 11)):
                p.poner("Combate", caja("dnb", 6), ini + s, 0.1, 0.2, rev=0.1)
            if rng.random() < 0.5:
                p.poner("Combate", zap(int(rng.integers(0, 4))), ini + float(rng.choice([2, 7, 13])), 0.18, rng.uniform(-0.8, 0.8), rev=0.3, eco=0.4)
            if c.s == "jefe" and sec >= 2:
                p.bombo("Combate", bombo("dnb"), ini + 13, 0.25)
        subidas(p, c, "Combate")

    elif c.g == "tribal":
        # Ensamble de guerra: tambores en corcheas (dieciseisavos en la parte B), gritos y la flauta (o el lead) arriba.
        for compas in range(c.compases):
            ac, sec, ini = c.acorde(compas), c.seccion(compas), compas * 16
            for s in range(16):
                if s % 2 and sec < 2:
                    continue
                p.poner("Combate", tambor("djembe_tono" if s % 4 else "surdo", s % 3), ini + s, 0.12 if s % 4 else 0.2,
                        0.5 * np.sin(s), rev=0.2)
            if compas % 2 == 1:
                p.poner("Combate", grito("a", compas % 3), ini + 4, 0.22, -0.3, rev=0.35)
                p.poner("Combate", grito("e", (compas + 1) % 3), ini + 12, 0.18, 0.3, rev=0.35)
            for s, g, largo, fuerza in frase(c, compas):
                nota = c.raiz + 36 + c.grado(ac + g) + (12 if sec >= 2 else 0)
                dur = pa * min(largo, 4) + 0.1
                if c.s == "guerra":
                    x = satura(lead_synth(nota, dur, True).astype(float) * 2.5, 3.0)
                else:
                    x = flauta(nota, dur)
                p.poner("Combate", x, ini + s, 0.2 * fuerza, 0.0, rev=0.4, eco=0.3)
            if c.s == "guerra":
                for s in range(2, 16, 4):
                    p.poner("Combate", metal(1100.0, 16.0, s % 4), ini + s, 0.06, 0.5 * np.sin(s), rev=0.15)
        subidas(p, c, "Combate", 0.25)

    elif c.g == "industrial":
        # La máquina a todo: un lead saturado que grita la melodía, metales en dieciseisavos y vapor.
        planta = c.s == "planta"
        for compas in range(c.compases):
            ac, sec, ini = c.acorde(compas), c.seccion(compas), compas * 16
            for s, g, largo, fuerza in frase(c, compas):
                nota = c.raiz + 36 + c.grado(ac + g) + (12 if sec >= 2 else 0)
                x = lead_synth(nota, pa * min(largo, 4) + 0.1, not planta).astype(float)
                if not planta:
                    x = satura(x * 2.5, 3.0)
                p.poner("Combate", x, ini + s, 0.18 * fuerza, 0.0, rev=0.3, eco=0.3, bombeo=True)
            for s in range(1, 16, 2):
                p.poner("Combate", metal(1200.0 + 200 * (s % 3), 18.0, s % 4), ini + s, 0.05, 0.5 * np.sin(s), rev=0.1)
            if compas % 2:
                p.poner("Combate", vapor(0.35, compas % 4), ini + 14, 0.14, float(rng.uniform(-0.6, 0.6)), rev=0.3)
            if compas % 8 == 7:
                for s in range(8, 16):
                    p.poner("Combate", caja("industrial", 5), ini + s, 0.05 + 0.025 * (s - 8), rev=0.3)
        subidas(p, c, "Combate", 0.25)

    elif c.g == "synth":
        # El lead de los 80 cantando la melodía (una octava arriba en la parte B), y dieciseisavos de hi-hat.
        oscuro = c.s in ("oscuro", "jefe")
        for compas in range(c.compases):
            ac, sec, ini = c.acorde(compas), c.seccion(compas), compas * 16
            for s, g, largo, fuerza in frase(c, compas):
                nota = c.raiz + 36 + c.grado(ac + g) + (12 if sec >= 2 else 0)
                p.poner("Combate", lead_synth(nota, pa * min(largo, 4) + 0.1, oscuro), ini + s, 0.24 * fuerza, 0.0,
                        rev=0.35, eco=0.35, bombeo=True)
            for s in range(1, 16, 2):
                p.poner("Combate", platos("cerrado"), ini + s, 0.05, 0.45)
            if oscuro and compas % 2:
                p.poner("Combate", stab(c.raiz + 24 + c.grado(ac), pa * 1.5, True), ini + 14, 0.18, -0.4, rev=0.2, eco=0.4)
            if compas % 8 == 7:
                for s in range(8, 16):
                    p.poner("Combate", caja("synth", 5), ini + s, 0.05 + 0.025 * (s - 8), rev=0.3)
        subidas(p, c, "Combate", 0.25)

    else:   # liquid
        notas = notas_reese(c)
        x = render_linea(p, notas, sint_reese(p, 300, 900, 2.0, 1.8), puerta=0.9, suavizar=40.0)
        p.continua("Combate", x, 0.3, bombeo=True)
        for compas in range(c.compases):
            ini = compas * 16
            for s in range(0, 16, 2):
                p.poner("Combate", platos("ride"), ini + s, 0.04, 0.5)
            for s in (3, 11):
                p.poner("Combate", aro(), ini + s, 0.08, -0.4, rev=0.3, eco=0.3)
        subidas(p, c, "Combate", 0.18)


BOMBEO = {"acid": (0.32, 0.12), "trance": (0.55, 0.16), "psy": (0.3, 0.1), "neuro": (0.25, 0.1), "liquid": (0.18, 0.12),
          "synth": (0.35, 0.15), "tribal": (0.22, 0.14), "industrial": (0.4, 0.12)}
ECO = {"acid": 3, "trance": 3, "psy": 3, "neuro": 3, "liquid": 6, "synth": 3, "tribal": 3, "industrial": 3}   # en dieciseisavos (3 = corchea con puntillo)


def componer(nombre, nivel, compases):
    p = Pista(nivel, compases)
    c = Ctx(nombre, nivel, compases)
    for capa in (ambiente, armonia, melodia, bajo, percusion, combate):
        capa(p, c)

    bombeo = envolvente_bombeo(p, *BOMBEO[c.g])
    capas = {}
    for capa in CAPAS:
        total = np.zeros((p.n, 2))
        for pre in ("", "b"):
            seco = p.buses.get((capa, pre + "seco"))
            if seco is None:
                continue
            y = seco.astype(np.float64)
            rv = p.buses.get((capa, pre + "rev"))
            if rv is not None:
                y += reverb(rv, *REVERB[capa], semilla=c.semilla)
            ec = p.buses.get((capa, pre + "eco"))
            if ec is not None:
                e = eco(ec, p.paso, ECO[c.g] if capa != "Ambiente" else 6)
                y += e + 0.3 * reverb(e, *REVERB[capa], semilla=c.semilla + 1)
            if capa in p.despues:
                y = p.despues[capa](y)
            if pre == "b":
                y *= bombeo[:, None]
            total += y
        corte = {"Bajo": 28.0, "Percusion": 30.0, "Ambiente": 50.0, "Combate": 40.0}.get(capa, 120.0)
        total = filtro_bucle(total, corte, "highpass")
        if capa in ("Percusion", "Combate"):
            total = limitador_suave(total, 0.45)
        capas[capa] = total

    # Balance: cada capa a su volumen; luego todo junto (con todas encendidas) que no pase de 0.9.
    for capa, objetivo in VOLUMEN_CAPA.items():
        capas[capa] *= 10 ** ((objetivo - rms_db(capas[capa])) / 20)
    pico = np.max(np.abs(sum(capas.values())))
    for capa in capas:
        capas[capa] = (capas[capa] * min(1.0, 0.9 / max(pico, 1e-9))).astype(np.float32)
    p.capas = capas
    return p


# ---------------------------------------------------------------- remates (no se repiten)

def _mezcla_remate(n, eventos, rev_s=2.2):
    """eventos: (segundo, señal, ganancia, paneo, envío a reverb). Devuelve estéreo normalizado a 0.85."""
    x = np.zeros((n, 2))
    envio = np.zeros((n, 2))
    for seg, sig, g, pan, rv in eventos:
        i = int(seg * SR)
        s = estereo(np.asarray(sig, dtype=float) * g, pan)[: max(0, n - i)]
        x[i:i + len(s)] += s
        envio[i:i + len(s)] += s * rv
    x = x + reverb(envio, rev_s, 6000, lineal=True)
    x = filtro(x, 30.0, "highpass")
    x[-int(0.3 * SR):] *= np.linspace(1, 0, int(0.3 * SR))[:, None]
    return x * (0.85 / max(np.max(np.abs(x)), 1e-9))


def remate_victoria(c, nivel):
    """Arpegio que sube sobre la tónica con el instrumento del género, platillo y acorde final (2 compases)."""
    paso = 60 / nivel.bpm / 4
    n = int(32 * paso * SR + 1.6 * SR)
    base = c.raiz + 36
    ev = []
    for k, g in enumerate((0, 2, 4, 7, 9, 11, 14)):
        nota = base + c.grado(g)
        if c.g == "acid":
            sig = burbuja(nota, paso * 2.5)
        elif c.g == "trance":
            sig = pluck_trance(nota, paso * 3, 1.2)
        elif c.g == "psy":
            sig = bajo_pluck(nota, paso * 2, 2.5, 18.0)
        elif c.g == "synth":
            sig = pluck(nota, paso * 3)
        elif c.g == "tribal":
            sig = marimba(nota, paso * 4)
        elif c.g == "industrial":
            sig = campana(nota, paso * 4, 1.41, 2.0, 3.0)
        else:
            sig = campana(nota + 12, paso * 6, 2.0 if c.g == "liquid" else 3.5, 2.0, 2.0)
        ev.append((k * 2 * paso, sig, 0.5, (k % 2 - 0.5) * 0.6, 0.3))
    golpe = 14 * paso
    for j in (0, 4, 7):
        if c.g == "tribal":
            acorde = canto(c.raiz + 36 + c.grado(j), paso * 18, "a", 3, 0.02)
        elif c.g in ("trance", "psy", "synth", "industrial"):
            acorde = supersaw(c.raiz + 24 + c.grado(j), paso * 18, 7, 0.2, 5000.0, 0.01, 0.6, 1)
        elif c.g == "acid":
            acorde = organo(c.raiz + 36 + c.grado(j), paso * 12)
        else:
            acorde = rhodes(c.raiz + 24 + c.grado(j), paso * 18) if c.g == "liquid" else supersaw(
                c.raiz + 24 + c.grado(j), paso * 18, 5, 0.15, 2500.0, 0.01, 0.6, 1)
        ev.append((golpe, acorde, 0.35, (j - 3) * 0.15, 0.5))
    ev.append((golpe, platos("crash"), 0.4, 0.0, 0.3))
    tipo = {"acid": "house", "trance": "trance", "psy": "psy", "neuro": "dnb", "liquid": "liquid", "synth": "synth",
            "tribal": "tribal", "industrial": "industrial"}[c.g]
    ev.append((golpe, bombo(tipo), 0.8, 0.0, 0.0))
    remate = {"synth": caja_cortada, "tribal": lambda: tambor("taiko"), "industrial": lambda: caja("industrial")}.get(
        c.g, (lambda: caja("dnb")) if c.g in ("neuro", "liquid") else palmada)()
    ev.append((golpe, remate, 0.5, 0.0, 0.4))
    return _mezcla_remate(n, ev)


def remate_entrada(c, nivel):
    """Subida de 1 compás (ruido, redoble y platillo al revés) que termina en un golpe: anuncia al jefe.
    El golpe cae justo en el dieciseisavo 16, cuando empieza la canción del jefe."""
    paso = 60 / nivel.bpm / 4
    golpe = 16 * paso
    n = int(golpe * SR + 1.8 * SR)
    ev = [(0.0, subida(golpe, 7, 1.2), 0.6, 0.0, 0.3)]
    reves = platillo_al_reves(golpe)
    ev.append((golpe - len(reves) / SR, reves, 0.5, 0.0, 0.2))
    for s in range(16):
        if s >= 8 or s % 2 == 0:
            ev.append((s * paso, caja("dnb" if c.g in ("neuro", "liquid") else "house", 7), 0.1 + 0.3 * s / 16, 0.0, 0.3))
    ev.append((golpe, golpe_grave(c.raiz, 1.6, 3), 1.0, 0.0, 0.4))
    ev.append((golpe, platos("crash"), 0.5, 0.0, 0.3))
    return _mezcla_remate(n, ev, 2.6)


def latido():
    """Corazón (lub-dub) a ~70 por minuto, en loop."""
    n = int(SR * .86)
    x = np.zeros(n, np.float32)
    for inicio, g in ((0.0, 1.0), (0.24, .7)):
        t = np.arange(int(SR * .2)) / SR
        golpe = np.sin(2 * np.pi * (52 * t + 30 * (1 - np.exp(-t * 25)) / 25)) * np.exp(-t * 18) * g
        i = int(inicio * SR)
        x[i:i + len(golpe)] += golpe
    x = filtro(x, 180)
    x = np.stack([x, x], axis=1)
    return x * (.8 / np.max(np.abs(x)))


# ---------------------------------------------------------------- guardar

def guardar(ruta_sin_ext, x, usar_ogg):
    wav = ruta_sin_ext.with_suffix(".wav")
    with wave.open(str(wav), "wb") as w:
        w.setparams((2, 2, SR, 0, "NONE", "not compressed"))
        w.writeframes(np.round(np.clip(x, -1, 1) * 32767).astype("<i2").tobytes())
    if usar_ogg:
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(wav), "-c:a", "libvorbis", "-q:a", "4",
                        str(ruta_sin_ext.with_suffix(".ogg"))], check=True)
        wav.unlink()   # solo queda el .ogg (dos archivos con el mismo nombre confunden a Unity)


def generar_nivel(trabajo):
    nombre, compases, preview, usar_ogg = trabajo
    nivel = NIVELES[nombre]
    pista = componer(nombre, nivel, compases)
    carpeta = SALIDA / nombre
    carpeta.mkdir(parents=True, exist_ok=True)
    for capa, x in pista.capas.items():
        guardar(carpeta / capa, x, usar_ogg)
    c = Ctx(nombre, nivel, compases)
    guardar(carpeta / "Victoria", remate_victoria(c, nivel), usar_ogg)
    if nombre.endswith("_Jefe"):
        guardar(carpeta / "Entrada", remate_entrada(c, nivel), usar_ogg)
    (carpeta / "manifest.json").write_text(json.dumps({
        "level": nombre, "bpm": nivel.bpm, "bars": compases, "sampleRate": SR,
        "samples": pista.n, "layers": list(CAPAS)}, indent=2), encoding="utf-8")
    if preview:
        (AQUI / "preview").mkdir(exist_ok=True)
        guardar(AQUI / "preview" / nombre, sum(pista.capas.values()), usar_ogg)
    return nombre


def main():
    ap = argparse.ArgumentParser(description="Música adaptativa por capas de Healing Knight")
    ap.add_argument("--nivel", choices=NIVELES, default="Digestivo_Estomago")
    ap.add_argument("--todos", action="store_true", help="genera todos los niveles")
    ap.add_argument("--compases", type=int, default=32, help="largo del loop (múltiplo de 8)")
    ap.add_argument("--preview", action="store_true", help="además guarda la mezcla completa en Tools/Audio/preview")
    ap.add_argument("--latido", action="store_true", help="vuelve a hacer el Latido (corazón)")
    ap.add_argument("--procesos", type=int, default=2, help="cuántos niveles a la vez")
    args = ap.parse_args()
    if args.compases < 8 or args.compases % 8:
        ap.error("--compases debe ser múltiplo de 8")
    usar_ogg = shutil.which("ffmpeg") is not None
    if not usar_ogg:
        print("AVISO: no hay ffmpeg; se guardan .wav (pesan mucho más).")

    SALIDA.mkdir(parents=True, exist_ok=True)
    if args.latido or not any(SALIDA.glob("Latido.*")):
        guardar(SALIDA / "Latido", latido(), usar_ogg)

    nombres = list(NIVELES) if args.todos else [args.nivel]
    trabajos = [(n, args.compases, args.preview, usar_ogg) for n in nombres]
    if len(trabajos) > 1 and args.procesos > 1:
        with Pool(args.procesos) as pool:
            for nombre in pool.imap_unordered(generar_nivel, trabajos):
                print(nombre, "listo", flush=True)
    else:
        for t in trabajos:
            print(generar_nivel(t), "listo", flush=True)


if __name__ == "__main__":
    main()
