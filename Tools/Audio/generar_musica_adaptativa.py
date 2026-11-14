"""
Música ADAPTATIVA de Healing Knight (sintetizador + secuenciador hecho con numpy y scipy).
Versión 4: la estructura de la v2 integrada al juego + el motor de sonido de la v3 del equipo
(sierras de verdad, filtro resonante del acid, pads supersaw, lead de trance, pluck y reese con filtro).

Cada nivel tiene su canción, separada en 6 CAPAS que duran exactamente lo mismo y se tocan a la vez.
El juego (Scripts/Gameplay/Audio/AdaptiveMusic.cs) sube y baja cada capa según lo que pasa:

    Ambiente   -> siempre suena: zumbido grave del órgano, textura de aire/líquido y gotitas.
    Armonia    -> acordes suaves (pads). Casi siempre suena.
    Melodia    -> la melodía principal (campanitas). Suave al explorar, fuerte al pelear.
    Bajo       -> entra cuando hay enemigos cerca.
    Percusion  -> bombo, caja y hi-hats: entra cuando empieza la pelea.
    Combate    -> riff agresivo, golpes extra y redobles: solo en peleas fuertes o con el jefe.

Además, por nivel:
    Victoria   -> remate corto que suena al limpiar una arena (no se repite).
    Entrada    -> (solo en las de jefe) golpe con subida que suena cuando aparece el jefe.
Y uno general: Latido (corazón) que suena cuando al jugador le queda poca vida.

Sale en: Assets/_Project/Audio/Resources/MusicAdaptive/<NombreDeLaEscena>/<Capa>.ogg
con un manifest.json (BPM, compases y largo en muestras: con eso Unity sincroniza las capas
y cambia de canción justo al terminar un compás).

Uso:
    python generar_musica_adaptativa.py --nivel Digestivo_Estomago --preview
    python generar_musica_adaptativa.py --todos
Necesita: numpy, scipy y ffmpeg (para .ogg).

Por qué .ogg y no .wav: en WAV las 6 capas de los 12 niveles pesarían ~770 MB en GitHub;
en OGG ~50 MB. Unity los vuelve a comprimir igual al importarlos.
"""
import argparse
import json
import shutil
import subprocess
import wave
from dataclasses import dataclass
from pathlib import Path

import numpy as np
from functools import lru_cache

from scipy.signal import butter, lfilter, sosfilt

SR = 44100                       # muestras por segundo
AQUI = Path(__file__).resolve().parent
SALIDA = AQUI.parent.parent / "Assets" / "_Project" / "Audio" / "Resources" / "MusicAdaptive"
CAPAS = ("Ambiente", "Armonia", "Melodia", "Bajo", "Percusion", "Combate")

# Volumen de cada capa (RMS en dB): así ninguna tapa a las demás y cada nivel de intensidad suena completo.
VOLUMEN_CAPA = {"Ambiente": -25, "Armonia": -24, "Melodia": -26, "Bajo": -23, "Percusion": -22, "Combate": -26}

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
    tonica: int          # nota MIDI de la tónica (38 = Re grave)
    escala: list
    acordes: list        # grados de la escala para los acordes (cambian cada 2 compases)
    motivo: list         # grados de la melodía
    ritmo: list          # en qué dieciseisavos del compás cae la melodía
    estilo: str          # acid, minimal, industrial, air, trance, psy, break, neuro, liquid


# Las claves son los nombres de las escenas (así el juego encuentra la carpeta sola).
# Las de jefe son "<Sistema>_Jefe".
NIVELES = {
    "Digestivo_Boca":             Nivel(120, 45, DORICO,           [0, 3, 4, 0], [0, 2, 4, 2, 1, 0], [0, 4, 7, 10, 12, 14], "minimal"),
    "Digestivo_Estomago":         Nivel(126, 38, FRIGIO,           [0, 1, 5, 4], [0, 2, 1, 4, 3, 1], [0, 3, 7, 10, 13, 15], "acid"),
    "Digestivo_IntestinoDelgado": Nivel(125, 45, DORICO,           [0, 3, 1, 4], [0, 4, 5, 2, 3, 6], [1, 4, 6, 9, 12, 14], "minimal"),
    "Digestivo_IntestinoGrueso":  Nivel(132, 36, LOCRIO,           [0, 5, 1, 4], [0, 4, 1, 0, 5, 2], [0, 5, 8, 11, 14, 15], "industrial"),
    "Digestivo_Jefe":             Nivel(142, 38, FRIGIO_DOMINANTE, [0, 1, 0, 4], [0, 1, 4, 6, 5, 1], [0, 2, 5, 8, 10, 14], "industrial"),
    "Respiratorio_Nariz":         Nivel(132, 50, LIDIO,            [0, 1, 4, 3], [4, 5, 6, 3, 2, 1], [0, 4, 7, 9, 12, 15], "air"),
    "Respiratorio_Traquea":       Nivel(134, 43, DORICO,           [0, 4, 3, 1], [0, 2, 4, 5, 4, 2], [0, 3, 6, 8, 11, 14], "trance"),
    "Respiratorio_Pulmones":      Nivel(138, 45, MENOR,            [0, 5, 2, 6], [0, 4, 7, 6, 4, 2], [0, 2, 6, 9, 12, 14], "trance"),
    "Respiratorio_Jefe":          Nivel(148, 41, FRIGIO_DOMINANTE, [0, 4, 1, 6], [0, 4, 1, 6, 3, 1], [0, 1, 4, 7, 10, 13], "psy"),
    "Nervioso_Medula":            Nivel(168, 40, DORICO,           [0, 3, 0, 6], [0, 2, 0, 4, 3, 5], [0, 3, 5, 8, 11, 14], "break"),
    "Nervioso_Hipocampo":         Nivel(172, 46, MENOR,            [0, 2, 5, 3], [4, 7, 6, 2, 3, 1], [0, 4, 6, 10, 12, 15], "liquid"),
    "Nervioso_Cerebro":           Nivel(174, 40, FRIGIO,           [0, 5, 2, 4], [0, 4, 1, 5, 3, 6], [0, 2, 7, 9, 11, 15], "neuro"),
    "Nervioso_Jefe":              Nivel(180, 40, LOCRIO_TENSO,     [0, 1, 4, 6], [0, 4, 6, 1, 5, 0], [0, 1, 5, 8, 12, 14], "neuro"),
}


# ---------------------------------------------------------------- utilidades de sonido

def hz(midi):
    """Nota MIDI -> frecuencia (69 = La 440 Hz)."""
    return 440.0 * 2 ** ((midi - 69) / 12)


def filtro(x, corte, tipo="lowpass"):
    """Filtro de 2 polos: 'lowpass' deja los graves, 'highpass' los agudos."""
    return sosfilt(butter(2, corte, fs=SR, btype=tipo, output="sos"), x)


def envolvente(t, ataque=0.012, salida=0.12):
    """Sube en 'ataque' segundos y baja en 'salida' segundos al final (sin chasquidos)."""
    return np.minimum(1, t / ataque) * np.minimum(1, np.maximum(0, (t[-1] - t) / salida))


def sierra(fase, f):
    """Onda de sierra sin "aliasing": suma de armónicos (1, 1/2, 1/3...) hasta antes de lo que el oído escucha."""
    armonicos = max(1, min(40, int(19000 / f)))
    return sum(np.sin(k * fase) / k for k in range(1, armonicos + 1)) * (-2 / np.pi)


def filtro_resonante(x, corte, q):
    """Pasa bajos con resonancia cuyo corte cambia en el tiempo ('corte' es un arreglo por muestra).
    Se recalcula cada 128 muestras: así se hace el "wah" del bajo acid y el temblor del reese."""
    y = np.zeros_like(x)
    estado = np.zeros(2)
    for i in range(0, len(x), 128):
        w = 2 * np.pi * np.clip(corte[min(i + 64, len(x) - 1)], 40, 16000) / SR
        c = np.cos(w)
        alfa = np.sin(w) / (2 * q)
        b = np.array([(1 - c) / 2, 1 - c, (1 - c) / 2]) / (1 + alfa)
        a = np.array([1, -2 * c / (1 + alfa), (1 - alfa) / (1 + alfa)])
        y[i:i + 128], estado = lfilter(b, a, x[i:i + 128], zi=estado)
    return y


@lru_cache(maxsize=512)
def _tono(midi, duracion, tipo, variante):
    """Una nota con distintos 'instrumentos'. Se guarda en memoria: las notas repetidas no se recalculan."""
    t = np.arange(max(2, round(duracion * SR))) / SR
    f = hz(midi)
    fase = 2 * np.pi * f * t
    if tipo in ("pad", "lead"):
        # Supersaw: 7 sierras un poquito desafinadas entre sí (el sonido típico del trance)
        x = sum(sierra(fase * (1 + v * .004) + v * .71, f * (1 + v * .004)) for v in range(-3, 4)) / 7
        if tipo == "pad":
            x, e = filtro(x, 2200), envolvente(t, .25, .25)
        else:
            x, e = filtro(x, 6200), envolvente(t, .012, .09)
    elif tipo == "acid":
        # Bajo acid (estilo TB-303): sierra + filtro resonante que se abre al tocar y se cierra rápido
        corte = 180 + (1600 + variante * 360) * np.exp(-t * 15)
        x = np.tanh(filtro_resonante(sierra(fase, f), corte, 5.5) * 3)
        e = envolvente(t, .003, .025)
    elif tipo == "reese":
        # Bajo reese (neurofunk): dos sierras desafinadas con un filtro que "tiembla", más un sub redondo
        x = sierra(fase * .992, f * .992) + sierra(fase * 1.008, f * 1.008)
        corte = 220 + (900 + variante * 180) * (0.5 + 0.5 * np.sin(2 * np.pi * (4 + variante) * t)) ** 2
        x = np.tanh(filtro_resonante(x, corte, 2.4) * 3) * .65 + np.sin(fase) * .4
        e = envolvente(t, .006, .035)
    elif tipo == "campana":
        # FM: una onda modula a otra y suena metálica, como campanita
        x = np.sin(fase + 1.8 * np.exp(-t * 7) * np.sin(fase * 2.01)) * .7
        e = envolvente(t, .005, .08) * np.exp(-t * 3)
    elif tipo == "pluck":
        # Cuerda pulsada: sierra filtrada que se apaga rápido
        x = filtro(sierra(fase, f), 2500)
        e = envolvente(t, .003, .04) * np.exp(-t * 9)
    else:
        # "sub": bajo redondo
        x = np.sin(fase) * .75 + np.sin(fase * 2) * .2
        e = envolvente(t, .008, .06)
    return (x * e).astype("float32")


def tono(midi, duracion, tipo, variante=0):
    return _tono(int(midi), round(float(duracion), 6), tipo, int(variante))


def tambor(tipo, rng):
    """Percusión sintetizada: el bombo es una onda que baja de tono muy rápido; lo demás es ruido filtrado."""
    t = np.arange(int(SR * .32)) / SR
    ruido = rng.normal(0, 1, len(t))
    if tipo == "bombo":
        x = np.sin(2 * np.pi * (48 * t + 110 * (1 - np.exp(-t * 40)) / 40)) * np.exp(-t * 14)
    elif tipo == "caja":
        x = (filtro(ruido, 1300, "highpass") * .6 + np.sin(2 * np.pi * 185 * t) * .5) * np.exp(-t * 20)
    elif tipo == "palmada":
        x = filtro(ruido, 1100, "highpass") * (np.exp(-t * 30) + .6 * np.exp(-np.abs(t - .012) * 300))
    elif tipo == "tom":
        x = np.sin(2 * np.pi * (90 * t + 60 * (1 - np.exp(-t * 18)) / 18)) * np.exp(-t * 9)
    elif tipo == "platillo":
        t = np.arange(int(SR * 1.2)) / SR
        x = filtro(rng.normal(0, 1, len(t)), 5000, "highpass") * np.exp(-t * 3.5) * .6
    else:                      # hi-hat
        x = filtro(ruido, 7000, "highpass") * .5 * np.exp(-t * 65)
    return (x * envolvente(np.arange(len(x)) / SR, .001, .015)).astype("float32")


def rms_db(x):
    return 20 * np.log10(np.sqrt(np.mean(x ** 2)) + 1e-12)


# ---------------------------------------------------------------- composición

class Pista:
    """Un lienzo estéreo por capa, de 'compases' de largo. Lo que se pasa del final se dobla al inicio (loop)."""

    def __init__(self, bpm, compases):
        self.paso = 60 / bpm / 4                        # duración de un dieciseisavo (segundos)
        self.n = round(compases * 16 * self.paso * SR)  # largo total en muestras
        self.capas = {c: np.zeros((self.n, 2), np.float32) for c in CAPAS}

    def poner(self, capa, senal, paso, ganancia, paneo=0.0):
        """Pega una señal en la capa, empezando en el dieciseisavo 'paso'. paneo: -1 izquierda, +1 derecha."""
        idx = (round(paso * self.paso * SR) + np.arange(len(senal))) % self.n
        v = senal * ganancia
        np.add.at(self.capas[capa][:, 0], idx, v * np.sqrt((1 - paneo) / 2))
        np.add.at(self.capas[capa][:, 1], idx, v * np.sqrt((1 + paneo) / 2))


def componer(nombre, nivel, compases):
    rng = np.random.default_rng(list(NIVELES).index(nombre) + 700)  # misma canción cada vez que se genera
    p = Pista(nivel.bpm, compases)

    rapido = nivel.estilo in ("break", "neuro", "liquid")
    aireado = nivel.estilo in ("air", "liquid")
    pesado = nivel.estilo in ("industrial", "psy", "neuro")
    acido = nivel.estilo in ("acid", "minimal", "industrial")

    def nota(grado):
        return nivel.tonica + nivel.escala[grado % 7] + 12 * (grado // 7)

    sistema = nombre.split("_")[0]
    # Instrumento de la melodía según el sistema: pluck (digestivo), lead de trance (respiratorio), campana (nervioso)
    melodia = {"Digestivo": "pluck", "Respiratorio": "lead"}.get(sistema, "campana")
    trance = nivel.estilo in ("trance", "psy")

    def variante():
        return int(rng.integers(0, 4))   # cada nota de acid/reese abre el filtro un poco distinto

    bombos_de_todo = []   # para el "bombeo" (sidechain)

    for c in range(compases):
        inicio = c * 16
        acorde = nivel.acordes[(c // 2) % 4]
        seccion = (c // 8) % 4          # 4 secciones de 8 compases: A, A', B, B'

        # --- Ambiente: zumbido grave (tónica y quinta), textura que respira y gotitas
        if c % 4 == 0:
            for grado, g in ((0, .16), (4, .09)):
                p.poner("Ambiente", tono(nota(grado) - 12, p.paso * 64, "pad"), inicio, g, -.2 if grado else .2)
        t = np.arange(round(p.paso * 16 * SR)) / SR
        textura = filtro(rng.normal(0, 1, len(t)), 1800 if rapido else 650) * np.sin(np.pi * t / t[-1]) ** 2
        p.poner("Ambiente", textura, inicio, .12, -.5 if c % 2 else .5)
        for q in range(2 if aireado else 4):
            tt = np.arange(round(SR * .16)) / SR
            f = (1100 if rapido else 240) * (1 + q * .15)
            gota = np.sin(2 * np.pi * (f * tt + (900 if acido else -f * 2) * tt ** 2)) * envolvente(tt, .008, .06) * np.exp(-tt * 8)
            p.poner("Ambiente", gota, inicio + int(rng.integers(0, 16)), .065, float(rng.uniform(-.8, .8)))

        # --- Armonía: acorde de 4 notas que dura 2 compases
        if c % 2 == 0:
            for j in (0, 2, 4, 8 if aireado else 7):
                p.poner("Armonia", tono(nota(acorde + j) + 12, p.paso * 32, "pad"), inicio, .10, -.65 if j % 2 else .55)

        # --- Melodía (y su eco grave en Combate: ahora en TODAS las secciones, variando)
        for j, s in enumerate(nivel.ritmo):
            if seccion == 0 and j % 3 == 2:
                continue        # la primera sección deja huecos: respira
            grado = nivel.motivo[(j + (1 if c % 4 == 3 else 0)) % len(nivel.motivo)]
            m = nota(acorde + grado) + 24
            p.poner("Melodia", tono(m, p.paso * (3.5 if aireado else 2.2), melodia), inicio + s, .13, -.35 if j % 2 else .35)
            if seccion != 1 or j % 2 == 0:
                riff = "reese" if rapido else "acid"
                p.poner("Combate", tono(m - (12 if seccion < 2 else 24), p.paso * 1.6, riff, variante()), inicio + s, .08)

        # --- Bajo
        if rapido:
            pasos_bajo = [0, 3, 6, 10, 13]
        elif aireado:
            pasos_bajo = [2, 5, 9, 13]
        elif acido:
            pasos_bajo = [0, 2, 3, 6, 8, 10, 13, 15]
        else:
            pasos_bajo = [1, 2, 3, 5, 6, 7, 9, 10, 11, 13, 14, 15]   # bajo "rodante" de trance
        for j, s in enumerate(pasos_bajo):
            grado = acorde + (4 if j == len(pasos_bajo) - 1 and c % 2 else 0)
            m = nota(grado) - (12 if nivel.tonica >= 45 else 0)
            instrumento = "reese" if rapido else "acid" if (acido or pesado) else "pluck" if trance else "sub"
            p.poner("Bajo", tono(m, p.paso * (1.7 if rapido or aireado else .85), instrumento, variante()), inicio + s, .28)

        # --- Percusión
        bombos = [0, 10] if rapido else [0, 8] if aireado else [0, 4, 8, 12]
        cajas = [12] if aireado else [4, 12]
        for s in bombos:
            p.poner("Percusion", tambor("bombo", rng), inicio + s, .48)
            bombos_de_todo.append(inicio + s)
        for s in cajas:
            p.poner("Percusion", tambor("caja", rng), inicio + s, .25)
        for s in range(0, 16, 1 if seccion >= 2 else 2):
            p.poner("Percusion", tambor("hat", rng), inicio + s, .12 if s % 4 == 2 else .065, .35)

        # --- Combate: golpes extra, palmadas y un redoble al final de cada 8 compases
        for s in ([6, 14] if rapido else [3, 11]):
            p.poner("Combate", tambor("bombo" if rapido else "caja", rng), inicio + s, .19)
        for s in (4, 12):
            p.poner("Combate", tambor("palmada", rng), inicio + s, .14, -.2)
        if c % 4 == 1:
            for s, tom in ((13, 0), (14, 1), (15, 2)):
                p.poner("Combate", tambor("tom", rng), inicio + s, .16 - tom * .02, (tom - 1) * .4)
        if c % 8 == 7:
            for s in range(10, 16):
                p.poner("Combate", tambor("caja", rng), inicio + s, .07 + .018 * (s - 10))
        if c % 8 == 0:
            p.poner("Combate", tambor("platillo", rng), inicio, .16)

    # --- Ecos (que también dan la vuelta al loop)
    for capa in ("Melodia", "Ambiente", "Armonia"):
        x = p.capas[capa]
        p.capas[capa] = x + np.roll(x[:, ::-1], round(p.paso * 3 * SR), axis=0) * .22 + np.roll(x, round(p.paso * 6 * SR), axis=0) * .10

    # --- Bombeo: bajo, acordes y melodía bajan con cada bombo (más hondo en trance, como en la v3 del equipo)
    if not aireado:
        env = np.ones(p.n, np.float32)
        largo = int(.22 * SR)
        curva = 1 - (.48 if trance else .28) * np.exp(-np.arange(largo) / SR / .065)
        for s in bombos_de_todo:
            i = round(s * p.paso * SR)
            idx = (i + np.arange(largo)) % p.n
            env[idx] = np.minimum(env[idx], curva)
        for capa in ("Bajo", "Armonia", "Melodia"):
            p.capas[capa] *= env[:, None]

    # --- Balance: cada capa a su volumen; luego todo junto que no pase de 0.9 (con todas encendidas)
    for capa, objetivo in VOLUMEN_CAPA.items():
        p.capas[capa] *= 10 ** ((objetivo - rms_db(p.capas[capa])) / 20)
    pico = np.max(np.abs(sum(p.capas.values())))
    for capa in p.capas:
        p.capas[capa] *= min(1.0, .9 / max(pico, 1e-9))
    return p


def remate_victoria(nivel, rng):
    """Arpegio que sube sobre la tónica, platillo y bombo: 2 compases, no se repite."""
    paso = 60 / nivel.bpm / 4
    n = round(32 * paso * SR)
    x = np.zeros((n, 2), np.float32)

    def poner(s, sig, g, pan=0.0):
        i = round(s * paso * SR)
        sig = sig[: max(0, n - i)] * g
        x[i:i + len(sig), 0] += sig * np.sqrt((1 - pan) / 2)
        x[i:i + len(sig), 1] += sig * np.sqrt((1 + pan) / 2)

    def nota(g):
        return nivel.tonica + nivel.escala[g % 7] + 12 * (g // 7)

    for k, g in enumerate((0, 2, 4, 7, 9, 11, 14)):
        poner(k * 2, tono(nota(g) + 24, paso * 6, "campana"), .22, (k % 2 - .5) * .6)
    poner(14, tono(nota(0) + 12, paso * 18, "pad"), .2)
    poner(14, tono(nota(4) + 12, paso * 18, "pad"), .14)
    poner(14, tambor("platillo", rng), .25)
    poner(14, tambor("bombo", rng), .5)
    return x * (.8 / max(np.max(np.abs(x)), 1e-9))


def remate_entrada(nivel, rng):
    """Subida de ruido de 1 compás que termina en un golpe grave: anuncia al jefe."""
    paso = 60 / nivel.bpm / 4
    n = round(20 * paso * SR)
    t = np.arange(round(16 * paso * SR)) / SR
    subida = filtro(rng.normal(0, 1, len(t)), 2500, "highpass") * (t / t[-1]) ** 2 * .5
    golpe = tono(nivel.tonica - 12, paso * 6, "sub") * 1.2
    x = np.zeros(n, np.float32)
    x[:len(subida)] += subida
    i = len(subida)
    golpe = (golpe + np.pad(tambor("bombo", rng), (0, max(0, len(golpe) - int(SR * .32))))[:len(golpe)])[: n - i]
    x[i:i + len(golpe)] += golpe
    x = np.stack([x, x], axis=1)
    return x * (.85 / max(np.max(np.abs(x)), 1e-9))


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
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(wav), "-c:a", "libvorbis", "-q:a", "5",
                        str(ruta_sin_ext.with_suffix(".ogg"))], check=True)
        wav.unlink()   # solo queda el .ogg (dos archivos con el mismo nombre confunden a Unity)


def main():
    ap = argparse.ArgumentParser(description="Música adaptativa por capas de Healing Knight")
    ap.add_argument("--nivel", choices=NIVELES, default="Digestivo_Estomago")
    ap.add_argument("--todos", action="store_true", help="genera todos los niveles")
    ap.add_argument("--compases", type=int, default=32, help="largo del loop (múltiplo de 8)")
    ap.add_argument("--preview", action="store_true", help="además guarda la mezcla completa en Tools/Audio/preview")
    args = ap.parse_args()
    if args.compases < 8 or args.compases % 8:
        ap.error("--compases debe ser múltiplo de 8")
    usar_ogg = shutil.which("ffmpeg") is not None
    if not usar_ogg:
        print("AVISO: no hay ffmpeg; se guardan .wav (pesan mucho más).")

    SALIDA.mkdir(parents=True, exist_ok=True)
    guardar(SALIDA / "Latido", latido(), usar_ogg)

    for nombre in (NIVELES if args.todos else [args.nivel]):
        nivel = NIVELES[nombre]
        pista = componer(nombre, nivel, args.compases)
        carpeta = SALIDA / nombre
        carpeta.mkdir(parents=True, exist_ok=True)
        for capa, x in pista.capas.items():
            guardar(carpeta / capa, x, usar_ogg)
        rng = np.random.default_rng(99)
        guardar(carpeta / "Victoria", remate_victoria(nivel, rng), usar_ogg)
        if nombre.endswith("_Jefe"):
            guardar(carpeta / "Entrada", remate_entrada(nivel, rng), usar_ogg)
        (carpeta / "manifest.json").write_text(json.dumps({
            "level": nombre, "bpm": nivel.bpm, "bars": args.compases, "sampleRate": SR,
            "samples": pista.n, "layers": list(CAPAS)}, indent=2), encoding="utf-8")
        if args.preview:
            (AQUI / "preview").mkdir(exist_ok=True)
            guardar(AQUI / "preview" / nombre, sum(pista.capas.values()), usar_ogg)
        print(nombre, "listo", flush=True)


if __name__ == "__main__":
    main()
