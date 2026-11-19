"""
Generador de sonidos de ciencia ficción para Healing Knight.

Cómo funciona (la "receta" de cada sonido):
  1. Un TONO que cambia de frecuencia (barrido): de f0 Hz a f1 Hz.
     - Agudo que baja  = "pew" de láser.     - Grave que baja = golpe pesado.
     - Que sube        = burbuja / carga.
  2. La FORMA de la onda le da el carácter:
     - "sine"   (seno):     limpio, suave, cristalino.
     - "square" (cuadrada): retro, de videojuego clásico.
     - "saw"    (sierra):   zumbido áspero, agresivo.
     - "tri"    (triángulo): entre seno y cuadrada, suave pero con cuerpo.
  3. RUIDO (opcional) mezclado: da "aire", chispa o suciedad (impactos, electricidad).
  4. Una ENVOLVENTE: el volumen arranca rápido y se apaga; "caida" más alta = se apaga más rápido.
  5. Extras: vibrato (tiembla), pulsos (burbujea), chispas (crujido eléctrico),
     tremolo (el volumen sube y baja, como respirar), golpes (latidos / pasos) y burbujas sueltas.
  6. Sonidos en BUCLE ("bucle": True): no se apagan y el final empata con el principio
     (ambiente de cada sistema, charco de ácido...). Usa frecuencias de tremolo/golpes que "quepan"
     exactas en la duración (p. ej. dur 8 s con tremolo 0.25 Hz = 2 respiraciones completas).
  7. "forma": "sube" = el volumen crece en vez de apagarse (cargas, avisos).

Dónde suena cada uno: el juego busca el archivo POR NOMBRE en Assets/_Project/Audio/Resources/SFX.
  - Medicamentos:  Disparo_<Medicamento>, Impacto_<Medicamento>
  - Enemigos:      <archivo del enemigo>_Disparo / _Muere  (p. ej. Virus_Rinovirus_Muere)
                   si no existe, prueba <tipo>_Disparo / _Muere (Virus_, Bacteria_, Jefe_, Bloqueo_...)
                   y si tampoco, Enemigo_Disparo / Enemigo_Muere.
  - Todo lo demás: la lista completa está en LEEME.md (sección Sonido).
Para cambiar un sonido basta con reemplazar el archivo con el MISMO nombre (también sirve un .wav o .mp3 de Victor).

Uso (necesitas Python con numpy; y ffmpeg si quieres .ogg):
  python generar_sonidos.py                 -> genera TODOS en Assets/_Project/Audio/Resources/SFX
  python generar_sonidos.py Disparo_Rabia   -> genera solo ese

Para agregar uno nuevo: copia una entrada de SONIDOS, cámbiale el nombre y los números,
corre el script y escúchalo. Después asígnalo en Unity (arrastra el .ogg al campo del medicamento o enemigo).
"""
import os, sys, wave, shutil, subprocess
import numpy as np
from scipy.signal import butter, sosfilt, fftconvolve

SR = 44100
AQUI = os.path.dirname(os.path.abspath(__file__))
SALIDA = os.path.normpath(os.path.join(AQUI, "..", "..", "Assets", "_Project", "Audio", "Resources", "SFX"))

# --------------------------------------------------------------------------------------------
# Lista de sonidos. Cada capa ("capas") se suma; luego se aplica la envolvente.
#   tono:   {"onda": "sine|square|saw|tri", "f0": Hz inicio, "f1": Hz final, "curva": 1 = recta,
#            <1 = cambia rápido al principio, >1 = cambia al final, "vol": 0..1,
#            "vibrato": [Hz, profundidad Hz] (opcional), "pulsos": Hz (opcional, burbujeo)}
#   ruido:  {"ruido": True, "suave": 0..1 (bajo = sordo, 1 = siseo), "vol": 0..1,
#            "chispas": 0..1 (opcional: deja solo chispazos sueltos, eléctrico),
#            "polos": 1..4 (opcional: filtra varias veces; para fondos usa 3 o 4, si no suena a estática)}
#   burbujas: {"burbujas": por segundo, "f0": Hz, "f1": Hz, "largo": s, "vol": 0..1}  (blips sueltos al azar)
#   En cualquier capa: "tremolo": [Hz, 0..1]   -> el volumen ondula (respirar, zumbido que late)
#                                [Hz, 0..1, Hz final] -> y el ondular acelera o frena (rueda que agarra vuelo)
#                      "golpes": [cada s, caída] -> la capa suena a golpes (latido, pasos, retumbo)
#                      "desde": s / "hasta": s  -> la capa empieza más tarde / se calla antes (sonidos en 2 partes)
#                      "decae": rapidez         -> la capa se apaga sola desde que empieza (más alto = más corto)
#                      "crece": True            -> la capa crece hasta que termina (la "a..." de un estornudo)
#                      "filtro": ["lowpass" | "highpass" | "bandpass", Hz o [Hz bajo, Hz alto]]
#   En tonos: "fm": [razón, índice, caída] -> otra onda modula a esta: suena metálico, a campana o a vidrio
# Opciones del sonido: "dur", "caida", "ataque", "vol", "bucle": True, "forma": "sube",
#                      "satura": k (lo engorda), "reverb": [segundos, mezcla 0..1], "eco": [cada s, 0..1, veces]
# --------------------------------------------------------------------------------------------
SONIDOS = {
    # ---------------- Disparos (cortos, menos de 0.2 s) ----------------
    "Disparo_Antibiotico":      {"dur": 0.16, "caida": 1.6, "capas": [
        {"onda": "square", "f0": 1800, "f1": 380, "curva": 0.6, "vol": 1}]},
    "Disparo_Antiviral":        {"dur": 0.18, "caida": 2.2, "capas": [
        {"onda": "sine", "f0": 2600, "f1": 1500, "curva": 0.5, "vol": 1},
        {"onda": "sine", "f0": 3900, "f1": 2250, "curva": 0.5, "vol": 0.6}]},
    "Disparo_Antiinflamatorio": {"dur": 0.20, "caida": 2.5, "ataque": 0.01, "capas": [
        {"ruido": True, "suave": 0.08, "vol": 0.6},
        {"onda": "tri", "f0": 700, "f1": 300, "curva": 0.7, "vol": 1}]},
    "Disparo_Antihistaminico":  {"dur": 0.20, "caida": 1.8, "capas": [
        {"onda": "sine", "f0": 1100, "f1": 600, "curva": 1, "vol": 1, "vibrato": [38, 160]}]},
    "Disparo_Antiparasitario":  {"dur": 0.18, "caida": 1.4, "capas": [
        {"onda": "saw", "f0": 420, "f1": 120, "curva": 0.8, "vol": 1},
        {"onda": "square", "f0": 840, "f1": 240, "curva": 0.8, "vol": 0.4}]},
    "Disparo_Mucolitico":       {"dur": 0.20, "caida": 1.5, "capas": [
        {"onda": "sine", "f0": 500, "f1": 1400, "curva": 1, "vol": 1, "pulsos": 45}]},
    "Disparo_Anticonvulsivo":   {"dur": 0.17, "caida": 1.8, "capas": [
        {"ruido": True, "suave": 1, "vol": 0.8, "chispas": 0.82},
        {"onda": "square", "f0": 3200, "f1": 1200, "curva": 0.4, "vol": 0.5}]},

    # ---------------- Impactos (un poco más graves/sucios que su disparo) ----------------
    "Impacto_Antibiotico":      {"dur": 0.14, "caida": 2.5, "capas": [
        {"onda": "square", "f0": 900, "f1": 200, "curva": 0.3, "vol": 1}, {"ruido": True, "suave": 0.25, "vol": 0.6}]},
    "Impacto_Antiviral":        {"dur": 0.16, "caida": 2.5, "capas": [
        {"onda": "sine", "f0": 2200, "f1": 900, "curva": 0.3, "vol": 1}, {"ruido": True, "suave": 0.5, "vol": 0.3}]},
    "Impacto_Antiinflamatorio": {"dur": 0.18, "caida": 3.0, "capas": [
        {"onda": "tri", "f0": 500, "f1": 180, "curva": 0.3, "vol": 1}, {"ruido": True, "suave": 0.06, "vol": 0.8}]},
    "Impacto_Antihistaminico":  {"dur": 0.15, "caida": 2.5, "capas": [
        {"onda": "sine", "f0": 1400, "f1": 500, "curva": 0.3, "vol": 1}, {"ruido": True, "suave": 0.2, "vol": 0.5}]},
    "Impacto_Antiparasitario":  {"dur": 0.20, "caida": 2.0, "capas": [
        {"onda": "saw", "f0": 260, "f1": 70, "curva": 0.3, "vol": 1}, {"ruido": True, "suave": 0.12, "vol": 0.7}]},
    "Impacto_Mucolitico":       {"dur": 0.16, "caida": 2.5, "capas": [
        {"onda": "sine", "f0": 700, "f1": 1300, "curva": 0.3, "vol": 1}, {"ruido": True, "suave": 0.1, "vol": 0.5}]},
    "Impacto_Anticonvulsivo":   {"dur": 0.15, "caida": 2.0, "capas": [
        {"onda": "square", "f0": 2800, "f1": 600, "curva": 0.3, "vol": 1}, {"ruido": True, "suave": 0.9, "vol": 0.9}]},

    # ---------------- Generales ----------------
    "Rebote_Ineficaz":          {"dur": 0.12, "caida": 4.0, "vol": 0.7, "capas": [
        {"onda": "square", "f0": 320, "f1": 300, "curva": 1, "vol": 1},
        {"onda": "sine", "f0": 1210, "f1": 1180, "curva": 1, "vol": 0.7},
        {"ruido": True, "suave": 0.3, "vol": 0.3}]},

    # ---------------- Jugador ----------------
    "Jugador_Salto":            {"dur": 0.12, "caida": 1.5, "vol": 0.6, "capas": [
        {"onda": "square", "f0": 260, "f1": 620, "curva": 0.7, "vol": 1}]},
    "Jugador_SaltoDoble":       {"dur": 0.16, "caida": 1.5, "vol": 0.6, "capas": [
        {"onda": "square", "f0": 420, "f1": 980, "curva": 0.7, "vol": 1},
        {"ruido": True, "suave": 0.6, "vol": 0.3}]},
    "Jugador_Dash":             {"dur": 0.26, "caida": 1.3, "ataque": 0.02, "vol": 0.55, "capas": [   # zumbido de aire (dash de Cuphead)
        {"ruido": True, "suave": 0.35, "polos": 2, "vol": 1},
        {"onda": "sine", "f0": 900, "f1": 260, "curva": 0.6, "vol": 0.35}]},
    "Jugador_Herido":           {"dur": 0.30, "caida": 1.6, "capas": [
        {"onda": "saw", "f0": 520, "f1": 140, "curva": 0.5, "vol": 1, "vibrato": [30, 40]},
        {"ruido": True, "suave": 0.3, "vol": 0.5}]},
    "Jugador_Muere":            {"dur": 1.10, "caida": 1.2, "capas": [
        {"onda": "square", "f0": 700, "f1": 60, "curva": 0.8, "vol": 1, "vibrato": [8, 50]},
        {"ruido": True, "suave": 0.1, "vol": 0.4}]},
    "Escudo_Bloquea":           {"dur": 0.14, "caida": 2.5, "capas": [
        {"onda": "sine", "f0": 900, "f1": 850, "vol": 1}, {"onda": "sine", "f0": 1350, "f1": 1300, "vol": 0.5},
        {"ruido": True, "suave": 0.4, "vol": 0.4}]},
    "Escudo_Parry":             {"dur": 0.45, "caida": 1.8, "capas": [
        {"onda": "sine", "f0": 1760, "f1": 1760, "vol": 1}, {"onda": "sine", "f0": 2640, "f1": 2640, "vol": 0.6},
        {"onda": "sine", "f0": 3520, "f1": 3600, "vol": 0.3, "vibrato": [12, 30]}]},
    "Escudo_Rompe":             {"dur": 0.50, "caida": 1.5, "capas": [
        {"ruido": True, "suave": 0.8, "vol": 1, "chispas": 0.6}, {"onda": "saw", "f0": 400, "f1": 90, "vol": 0.6}]},
    "Bomba_Lanza":              {"dur": 0.22, "caida": 1.2, "vol": 0.6, "capas": [
        {"ruido": True, "suave": 0.5, "vol": 0.6}, {"onda": "sine", "f0": 300, "f1": 700, "vol": 0.6}]},
    "Bomba_Explota":            {"dur": 0.90, "caida": 2.2, "ataque": 0.003, "capas": [
        {"ruido": True, "suave": 0.03, "vol": 1}, {"onda": "sine", "f0": 140, "f1": 35, "curva": 0.4, "vol": 1},
        {"ruido": True, "suave": 0.7, "vol": 0.4, "chispas": 0.7}]},
    "Bomba_Ganada":             {"dur": 0.35, "caida": 1.2, "vol": 0.6, "capas": [
        {"onda": "square", "f0": 660, "f1": 660, "vol": 1, "golpes": [0.09, 2]},
        {"onda": "square", "f0": 990, "f1": 1320, "vol": 0.6, "desde": 0.18}]},

    # ---------------- Enemigos (por tipo; puedes hacer uno por enemigo con su nombre de archivo) ----------------
    "Enemigo_Disparo":          {"dur": 0.18, "caida": 1.6, "vol": 0.7, "capas": [
        {"onda": "tri", "f0": 500, "f1": 220, "curva": 0.6, "vol": 1}]},
    "Virus_Disparo":            {"dur": 0.22, "caida": 1.6, "vol": 0.7, "capas": [
        {"onda": "tri", "f0": 300, "f1": 900, "curva": 0.6, "vol": 1, "vibrato": [20, 60]}]},
    "Bacteria_Disparo":         {"dur": 0.18, "caida": 1.8, "vol": 0.7, "capas": [
        {"onda": "sine", "f0": 260, "f1": 160, "vol": 1, "pulsos": 60}, {"ruido": True, "suave": 0.1, "vol": 0.4}]},
    "Enemigo_Muere":            {"dur": 0.35, "caida": 1.4, "capas": [
        {"onda": "saw", "f0": 600, "f1": 60, "curva": 0.5, "vol": 0.7}, {"ruido": True, "suave": 0.15, "vol": 0.8}]},
    "Bloqueo_Muere":            {"dur": 0.80, "caida": 1.3, "capas": [
        {"burbujas": 40, "f0": 200, "f1": 500, "largo": 0.06, "vol": 1}, {"ruido": True, "suave": 0.05, "vol": 0.5}]},
    "Jefe_Muere":               {"dur": 1.80, "caida": 1.6, "capas": [
        {"ruido": True, "suave": 0.02, "vol": 1}, {"onda": "saw", "f0": 220, "f1": 30, "curva": 0.6, "vol": 0.8},
        {"ruido": True, "suave": 0.8, "vol": 0.3, "chispas": 0.85, "golpes": [0.25, 4]}]},
    "Jefe_Rabia_Virion_Muere":  {"dur": 0.25, "caida": 2.0, "capas": [
        {"onda": "square", "f0": 1200, "f1": 200, "curva": 0.4, "vol": 1}, {"ruido": True, "suave": 0.9, "vol": 0.6, "chispas": 0.5}]},
    "Neurona_Hiperexcitada_Muere": {"dur": 0.70, "caida": 1.4, "capas": [   # al calmarla: vuelve a ser sana
        {"onda": "sine", "f0": 523, "f1": 523, "vol": 1}, {"onda": "sine", "f0": 659, "f1": 659, "vol": 0.8, "desde": 0.08},
        {"onda": "sine", "f0": 784, "f1": 784, "vol": 0.7, "desde": 0.16}, {"onda": "sine", "f0": 1047, "f1": 1047, "vol": 0.5, "desde": 0.24}]},

    # ---------------- Digestivo ----------------
    "Acido_Burbujeo":           {"dur": 3.0, "bucle": True, "vol": 0.6, "capas": [   # bucle cerca del charco
        {"burbujas": 14, "f0": 180, "f1": 600, "largo": 0.07, "vol": 1}, {"ruido": True, "suave": 0.03, "vol": 0.25}]},
    "Acido_Quema":              {"dur": 0.30, "caida": 1.5, "vol": 0.7, "capas": [
        {"ruido": True, "suave": 1, "vol": 1}, {"burbujas": 30, "f0": 400, "f1": 900, "largo": 0.04, "vol": 0.6}]},
    "Lombriz_Retumbo":          {"dur": 0.90, "forma": "sube", "caida": 0.7, "capas": [   # aviso: viene por debajo
        {"ruido": True, "suave": 0.01, "vol": 1}, {"onda": "sine", "f0": 45, "f1": 60, "vol": 0.8, "golpes": [0.15, 3]}]},
    "Lombriz_Sale":             {"dur": 0.60, "caida": 1.5, "capas": [
        {"ruido": True, "suave": 0.08, "vol": 1}, {"onda": "saw", "f0": 90, "f1": 260, "curva": 0.5, "vol": 0.7},
        {"burbujas": 30, "f0": 150, "f1": 300, "largo": 0.05, "vol": 0.5}]},
    "Lombriz_Entra":            {"dur": 0.50, "caida": 1.4, "capas": [
        {"ruido": True, "suave": 0.05, "vol": 1}, {"onda": "saw", "f0": 220, "f1": 70, "vol": 0.6}]},
    "Lombriz_Ruge":             {"dur": 1.00, "caida": 1.2, "ataque": 0.05, "capas": [
        {"onda": "saw", "f0": 110, "f1": 75, "vol": 1, "vibrato": [14, 12]}, {"onda": "saw", "f0": 166, "f1": 110, "vol": 0.6, "vibrato": [11, 15]},
        {"ruido": True, "suave": 0.15, "vol": 0.6, "tremolo": [14, 0.6]}]},

    # ---------------- Respiratorio ----------------
    "Alveolo_Infla":            {"dur": 0.50, "forma": "sube", "caida": 0.8, "vol": 0.5, "capas": [
        {"ruido": True, "suave": 0.2, "vol": 1}, {"onda": "sine", "f0": 200, "f1": 340, "vol": 0.4}]},
    "Alveolo_Desinfla":         {"dur": 0.55, "caida": 1.2, "vol": 0.5, "capas": [
        {"ruido": True, "suave": 0.15, "vol": 1}, {"onda": "sine", "f0": 320, "f1": 160, "vol": 0.4}]},
    "Alveolo_Rebote":           {"dur": 0.35, "caida": 1.6, "capas": [   # trampolín
        {"onda": "sine", "f0": 140, "f1": 520, "curva": 0.5, "vol": 1, "vibrato": [22, 25]},
        {"onda": "tri", "f0": 280, "f1": 1040, "curva": 0.5, "vol": 0.3}]},
    "Colmena_Enjambre":         {"dur": 1.20, "caida": 1.0, "ataque": 0.1, "capas": [
        {"onda": "saw", "f0": 210, "f1": 230, "vol": 1, "vibrato": [37, 18]}, {"onda": "saw", "f0": 317, "f1": 340, "vol": 0.6, "vibrato": [29, 22]},
        {"ruido": True, "suave": 0.3, "vol": 0.3}]},

    # ---------------- Nervioso ----------------
    "Rayo_Aviso":               {"dur": 1.00, "forma": "sube", "caida": 1.5, "vol": 0.6, "capas": [   # dura lo mismo que el aviso
        {"ruido": True, "suave": 1, "vol": 1, "chispas": 0.9}, {"onda": "saw", "f0": 120, "f1": 480, "curva": 1.5, "vol": 0.5}]},
    "Rayo_Cae":                 {"dur": 1.60, "caida": 1.8, "ataque": 0.001, "capas": [
        {"ruido": True, "suave": 1, "vol": 1, "chispas": 0.3, "golpes": [0.07, 2]},
        {"ruido": True, "suave": 0.015, "vol": 1, "desde": 0.04}, {"onda": "sine", "f0": 80, "f1": 30, "vol": 0.7}]},
    "Neurona_Carga":            {"dur": 0.90, "forma": "sube", "caida": 1.2, "vol": 0.5, "capas": [
        {"onda": "square", "f0": 300, "f1": 1400, "curva": 1.3, "vol": 1, "tremolo": [18, 0.7]}]},
    "Neurona_Descarga":         {"dur": 0.40, "caida": 2.0, "capas": [
        {"ruido": True, "suave": 1, "vol": 1, "chispas": 0.4}, {"onda": "square", "f0": 1800, "f1": 200, "curva": 0.3, "vol": 0.7}]},
    "Neurona_Golpe":            {"dur": 0.20, "caida": 2.0, "vol": 0.6, "capas": [
        {"onda": "sine", "f0": 700, "f1": 300, "vol": 1}, {"onda": "sine", "f0": 740, "f1": 320, "vol": 0.8}]},
    "Neurona_Infecta":          {"dur": 0.80, "caida": 1.2, "capas": [
        {"onda": "saw", "f0": 600, "f1": 180, "curva": 0.7, "vol": 1, "vibrato": [9, 40]},
        {"ruido": True, "suave": 1, "vol": 0.5, "chispas": 0.8}]},
    "Diente_Secreto":           {"dur": 1.30, "caida": 1.1, "capas": [   # easter egg: ¡ding! + risita
        {"onda": "sine", "f0": 1318, "f1": 1318, "vol": 0.8}, {"onda": "sine", "f0": 1976, "f1": 1976, "vol": 0.5, "desde": 0.07},
        {"onda": "square", "f0": 520, "f1": 700, "vol": 0.35, "golpes": [0.11, 6], "desde": 0.35, "vibrato": [9, 30]}]},
    "Campo_Neuroprotector":     {"dur": 1.20, "caida": 1.3, "ataque": 0.05, "capas": [
        {"onda": "sine", "f0": 880, "f1": 880, "vol": 1, "tremolo": [6, 0.5]}, {"onda": "sine", "f0": 1320, "f1": 1330, "vol": 0.6},
        {"onda": "sine", "f0": 440, "f1": 440, "vol": 0.6}]},
    "Rabia_Chillido":           {"dur": 0.45, "caida": 1.4, "capas": [   # la cadena da la vuelta
        {"onda": "saw", "f0": 1300, "f1": 900, "vol": 1, "vibrato": [45, 120]}, {"ruido": True, "suave": 0.7, "vol": 0.4}]},

    # ---------------- Ambiente de cada sistema (bucles de 8 s, suenan bajito de fondo) ----------------
    "Ambiente_Digestivo":       {"dur": 8.25, "bucle": True, "vol": 0.35, "capas": [
        {"onda": "sine", "f0": 55, "f1": 55, "vol": 1, "golpes": [1.0, 10]},              # latido, 60 por minuto (lub)
        {"onda": "sine", "f0": 48, "f1": 48, "vol": 0.7, "golpes": [1.0, 14], "desde": 0},
        {"onda": "sine", "f0": 38, "f1": 38, "vol": 0.25, "tremolo": [0.125, 0.8]},       # retumbo de tripas, muy grave
        {"burbujas": 1.2, "f0": 70, "f1": 160, "largo": 0.35, "vol": 0.3}]},              # gorgoteo de vez en cuando
    "Ambiente_Respiratorio":    {"dur": 8.25, "bucle": True, "vol": 0.3, "capas": [
        {"ruido": True, "suave": 0.03, "polos": 4, "vol": 1, "tremolo": [0.25, 1]},       # aire suave: 2 respiraciones en 8 s
        {"onda": "sine", "f0": 70, "f1": 70, "vol": 0.25, "tremolo": [0.25, 1]}]},        # cuerpo grave de la respiración
    "Ambiente_Nervioso":        {"dur": 8.25, "bucle": True, "vol": 0.6, "capas": [
        {"onda": "saw", "f0": 60, "f1": 60, "vol": 0.25, "tremolo": [0.5, 0.5]},          # zumbido eléctrico
        {"onda": "sine", "f0": 120, "f1": 120, "vol": 0.4},
        {"ruido": True, "suave": 1, "vol": 0.6, "chispas": 0.997},                        # chispitas sueltas
        {"burbujas": 1.5, "f0": 1800, "f1": 2600, "largo": 0.04, "vol": 0.3}]},           # sinapsis

    # ======================= v2 (se agregan al final para no cambiar los de arriba al regenerar) =======================
    # ---------------- Enemigos por tipo: muere / dispara ----------------
    "Bacteria_Muere":           {"dur": 0.45, "caida": 1.5, "satura": 1.5, "reverb": [0.4, 0.15], "capas": [   # "bloop" que se desinfla
        {"onda": "sine", "f0": 420, "f1": 90, "curva": 0.5, "vol": 1},
        {"ruido": True, "suave": 1, "vol": 0.7, "filtro": ["bandpass", [300, 2200]], "decae": 14},
        {"burbujas": 25, "f0": 300, "f1": 800, "largo": 0.04, "vol": 0.5, "desde": 0.05}]},
    "Virus_Muere":              {"dur": 0.50, "caida": 1.6, "reverb": [0.6, 0.2], "capas": [   # cápside de cristal que se rompe
        {"onda": "sine", "f0": 1400, "f1": 1400, "fm": [2.76, 4, 10], "vol": 0.8, "decae": 9},
        {"onda": "square", "f0": 900, "f1": 180, "curva": 0.4, "vol": 0.5},
        {"ruido": True, "suave": 1, "vol": 0.6, "chispas": 0.6, "filtro": ["highpass", 3000]}]},
    "Parasito_Muere":           {"dur": 0.55, "caida": 1.3, "satura": 2, "capas": [   # gusano que se aplasta: "squish"
        {"onda": "saw", "f0": 190, "f1": 55, "curva": 0.6, "vol": 0.8, "vibrato": [16, 25], "filtro": ["lowpass", 900]},
        {"ruido": True, "suave": 1, "vol": 0.8, "filtro": ["bandpass", [250, 1400]], "tremolo": [22, 0.8]},
        {"burbujas": 18, "f0": 150, "f1": 400, "largo": 0.06, "vol": 0.4}]},
    "Alergeno_Muere":           {"dur": 0.40, "caida": 1.6, "reverb": [0.5, 0.25], "capas": [   # nubecita de polvo
        {"ruido": True, "suave": 1, "vol": 1, "filtro": ["bandpass", [1500, 6000]], "decae": 9},
        {"onda": "sine", "f0": 2200, "f1": 2600, "vol": 0.3, "fm": [3.1, 1.5, 12]}]},
    "Parasito_Disparo":         {"dur": 0.20, "caida": 1.8, "vol": 0.7, "capas": [   # escupitajo
        {"ruido": True, "suave": 1, "vol": 1, "filtro": ["bandpass", [600, 2500]], "decae": 25},
        {"onda": "sine", "f0": 380, "f1": 140, "curva": 0.5, "vol": 0.7}]},
    "Alergeno_Disparo":         {"dur": 0.35, "caida": 1.4, "vol": 0.7, "reverb": [0.4, 0.2], "capas": [   # ¡puf! de polen
        {"ruido": True, "suave": 1, "vol": 1, "filtro": ["bandpass", [800, 5000]], "decae": 10},
        {"onda": "sine", "f0": 160, "f1": 90, "vol": 0.5, "decae": 20}]},
    "Jefe_Disparo":             {"dur": 0.30, "caida": 1.5, "vol": 0.8, "satura": 1.8, "capas": [   # bola pesada
        {"onda": "sine", "f0": 220, "f1": 70, "curva": 0.5, "vol": 1},
        {"ruido": True, "suave": 1, "vol": 0.6, "filtro": ["bandpass", [300, 1800]], "decae": 18}]},

    # ---------------- Enemigos: aviso antes de atacar (_Prepara) y ataque cuerpo a cuerpo (_Ataca) ----------------
    "Enemigo_Prepara":          {"dur": 0.32, "forma": "sube", "caida": 1.3, "vol": 0.45, "capas": [
        {"onda": "tri", "f0": 260, "f1": 620, "curva": 1.3, "vol": 1, "tremolo": [24, 0.5]}]},
    "Bacteria_Prepara":         {"dur": 0.35, "forma": "sube", "caida": 1.2, "vol": 0.45, "capas": [   # se infla, viscoso
        {"onda": "sine", "f0": 150, "f1": 330, "curva": 1.2, "vol": 1, "vibrato": [12, 15]},
        {"ruido": True, "suave": 1, "vol": 0.3, "filtro": ["bandpass", [400, 1200]]}]},
    "Virus_Prepara":            {"dur": 0.32, "forma": "sube", "caida": 1.2, "vol": 0.45, "capas": [   # carga eléctrica
        {"onda": "square", "f0": 400, "f1": 1300, "curva": 1.4, "vol": 0.8, "tremolo": [30, 0.6], "filtro": ["lowpass", 5000]}]},
    "Parasito_Prepara":         {"dur": 0.35, "forma": "sube", "caida": 1.0, "vol": 0.45, "capas": [   # se retuerce
        {"onda": "saw", "f0": 140, "f1": 210, "vol": 0.7, "vibrato": [9, 30], "filtro": ["lowpass", 1200]},
        {"ruido": True, "suave": 1, "vol": 0.5, "filtro": ["bandpass", [300, 1500]], "tremolo": [14, 0.8]}]},
    "Enemigo_Ataca":            {"dur": 0.25, "caida": 1.6, "vol": 0.6, "capas": [   # embestida: "fuush" + golpe
        {"ruido": True, "suave": 1, "vol": 1, "filtro": ["bandpass", [500, 3000]]},
        {"onda": "sine", "f0": 160, "f1": 60, "vol": 0.8, "decae": 18}]},
    "Bacteria_Ataca":           {"dur": 0.28, "caida": 1.5, "vol": 0.6, "satura": 1.5, "capas": [   # embestida blandita
        {"ruido": True, "suave": 1, "vol": 0.9, "filtro": ["bandpass", [300, 1600]], "decae": 12},
        {"onda": "sine", "f0": 200, "f1": 70, "vol": 0.9, "decae": 14}]},
    "Virus_Ataca":              {"dur": 0.25, "caida": 1.6, "vol": 0.6, "capas": [   # embestida eléctrica
        {"onda": "square", "f0": 1200, "f1": 300, "curva": 0.5, "vol": 0.6, "filtro": ["lowpass", 4000]},
        {"ruido": True, "suave": 1, "vol": 0.6, "chispas": 0.6, "filtro": ["highpass", 2500]}]},
    "Parasito_Ataca":           {"dur": 0.30, "caida": 1.4, "vol": 0.6, "capas": [   # se lanza, baboso
        {"onda": "saw", "f0": 260, "f1": 120, "vol": 0.6, "vibrato": [12, 25], "filtro": ["lowpass", 1400]},
        {"ruido": True, "suave": 1, "vol": 0.7, "filtro": ["bandpass", [300, 1800]], "decae": 10}]},
    "Enemigo_Eclosiona":        {"dur": 0.50, "caida": 1.3, "capas": [   # el cascarón del huevo se rompe
        {"ruido": True, "suave": 1, "vol": 0.9, "chispas": 0.8, "filtro": ["bandpass", [1500, 6000]], "golpes": [0.06, 5], "hasta": 0.2},
        {"onda": "sine", "f0": 300, "f1": 800, "curva": 0.5, "vol": 0.7, "desde": 0.15, "decae": 12},
        {"burbujas": 25, "f0": 300, "f1": 700, "largo": 0.05, "vol": 0.4, "desde": 0.15}]},

    # ---------------- Enemigos en particular (habilidad especial = _Especial) ----------------
    "Bacteria_HPylori_Especial": {"dur": 0.60, "caida": 1.2, "vol": 0.6, "capas": [   # se entierra en el moco
        {"ruido": True, "suave": 1, "vol": 1, "filtro": ["lowpass", 700], "tremolo": [18, 0.7]},
        {"burbujas": 30, "f0": 120, "f1": 260, "largo": 0.06, "vol": 0.6},
        {"onda": "sine", "f0": 220, "f1": 70, "vol": 0.5}]},
    "Bacteria_HPylori_Ataca":   {"dur": 0.45, "caida": 1.5, "satura": 1.6, "capas": [   # ¡sale por debajo!
        {"onda": "sine", "f0": 70, "f1": 260, "curva": 0.4, "vol": 1},
        {"ruido": True, "suave": 1, "vol": 0.8, "filtro": ["bandpass", [300, 2500]], "decae": 10},
        {"burbujas": 30, "f0": 250, "f1": 600, "largo": 0.05, "vol": 0.5}]},
    "Virus_Rotavirus_Prepara":  {"dur": 0.60, "forma": "sube", "caida": 1.1, "vol": 0.5, "capas": [   # la rueda agarra vuelo
        {"onda": "saw", "f0": 90, "f1": 420, "curva": 1.3, "vol": 0.7, "filtro": ["lowpass", 2500]},
        {"ruido": True, "suave": 1, "vol": 0.5, "filtro": ["bandpass", [1000, 4000]], "tremolo": [8, 0.9, 40]}]},
    "Virus_Rotavirus_Ataca":    {"dur": 0.40, "caida": 1.4, "vol": 0.7, "capas": [   # ¡se lanza como sierra!
        {"onda": "saw", "f0": 520, "f1": 300, "vol": 0.6, "filtro": ["lowpass", 3000], "tremolo": [45, 0.7]},
        {"ruido": True, "suave": 1, "vol": 0.7, "filtro": ["bandpass", [1500, 6000]]}]},
    "Virus_Rotavirus_Muere":    {"dur": 0.70, "caida": 1.3, "reverb": [0.4, 0.2], "capas": [   # la rueda se desarma
        {"onda": "sine", "f0": 900, "f1": 900, "fm": [3.7, 5, 6], "vol": 0.6, "golpes": [0.11, 5]},
        {"onda": "saw", "f0": 380, "f1": 50, "curva": 0.6, "vol": 0.6, "tremolo": [30, 0.8, 6], "filtro": ["lowpass", 2000]},
        {"ruido": True, "suave": 1, "vol": 0.4, "chispas": 0.7, "filtro": ["highpass", 2500]}]},
    "Parasito_Giardia_Ataca":   {"dur": 0.40, "caida": 1.4, "vol": 0.7, "capas": [   # brinca y ¡chup! se pega
        {"onda": "sine", "f0": 200, "f1": 600, "curva": 0.6, "vol": 0.8, "hasta": 0.2},
        {"onda": "sine", "f0": 900, "f1": 200, "curva": 0.3, "vol": 0.9, "desde": 0.2, "decae": 20},
        {"ruido": True, "suave": 1, "vol": 0.6, "filtro": ["bandpass", [500, 2500]], "desde": 0.2, "decae": 25}]},
    "Parasito_Giardia_Muere":   {"dur": 0.45, "caida": 1.5, "capas": [   # se despega: ¡plop!
        {"onda": "sine", "f0": 300, "f1": 1100, "curva": 0.5, "vol": 0.9, "decae": 9},
        {"ruido": True, "suave": 1, "vol": 0.6, "filtro": ["bandpass", [400, 2000]], "decae": 15}]},
    "Parasito_Entamoeba_Ataca": {"dur": 0.50, "caida": 1.0, "vol": 0.7, "capas": [   # el seudópodo se estira
        {"onda": "saw", "f0": 110, "f1": 330, "curva": 0.8, "vol": 0.6, "vibrato": [7, 20], "filtro": ["lowpass", 1100]},
        {"ruido": True, "suave": 1, "vol": 0.5, "filtro": ["bandpass", [250, 900]], "tremolo": [11, 0.7]}]},
    "Parasito_Entamoeba_Muere": {"dur": 0.60, "caida": 1.3, "satura": 1.5, "capas": [   # la ameba se deshace
        {"onda": "sine", "f0": 260, "f1": 60, "curva": 0.6, "vol": 0.9, "vibrato": [9, 20]},
        {"ruido": True, "suave": 1, "vol": 0.8, "filtro": ["bandpass", [200, 1200]], "tremolo": [16, 0.8]},
        {"burbujas": 30, "f0": 120, "f1": 400, "largo": 0.07, "vol": 0.5}]},
    "Bacteria_CDifficile_Especial": {"dur": 0.60, "caida": 1.4, "reverb": [0.5, 0.25], "capas": [   # se vuelve espora: ¡crac!
        {"ruido": True, "suave": 1, "vol": 0.8, "chispas": 0.85, "filtro": ["highpass", 1500], "golpes": [0.07, 4]},
        {"onda": "sine", "f0": 700, "f1": 1400, "fm": [1.5, 2, 4], "vol": 0.5}]},
    "Parasito_Oxiuro_Muere":    {"dur": 0.50, "caida": 1.4, "capas": [   # se retuerce y suelta el huevo (¡plop!)
        {"onda": "saw", "f0": 260, "f1": 90, "vol": 0.5, "vibrato": [14, 40], "filtro": ["lowpass", 1500], "hasta": 0.3},
        {"onda": "sine", "f0": 250, "f1": 700, "curva": 0.5, "vol": 0.8, "desde": 0.28, "decae": 18}]},
    "Parasito_Oxiuro_Huevo_Especial": {"dur": 0.25, "caida": 1.6, "vol": 0.5, "capas": [   # aparece el huevo
        {"onda": "sine", "f0": 180, "f1": 420, "curva": 0.5, "vol": 1, "decae": 14},
        {"ruido": True, "suave": 1, "vol": 0.4, "filtro": ["bandpass", [300, 1500]], "decae": 20}]},
    "Bacteria_SMutans_Disparo": {"dur": 0.30, "caida": 1.5, "vol": 0.7, "capas": [   # escupe ácido que chisporrotea
        {"onda": "sine", "f0": 500, "f1": 220, "curva": 0.5, "vol": 0.7, "decae": 15},
        {"ruido": True, "suave": 1, "vol": 0.6, "chispas": 0.5, "filtro": ["highpass", 3000]},
        {"burbujas": 20, "f0": 400, "f1": 900, "largo": 0.04, "vol": 0.4}]},
    "Virus_Rinovirus_Disparo":  {"dur": 0.50, "caida": 1.0, "vol": 0.7, "capas": [   # estornudo: "a... ¡chís!"
        {"onda": "saw", "f0": 260, "f1": 380, "vol": 0.5, "filtro": ["bandpass", [500, 1500]], "hasta": 0.22, "crece": True},
        {"ruido": True, "suave": 1, "vol": 1, "filtro": ["bandpass", [2500, 8000]], "desde": 0.22, "decae": 12}]},
    "Virus_Rinovirus_Muere":    {"dur": 0.40, "caida": 1.5, "reverb": [0.4, 0.2], "capas": [   # ¡chís! chiquito y brillos
        {"ruido": True, "suave": 1, "vol": 0.8, "filtro": ["bandpass", [3000, 9000]], "decae": 15},
        {"onda": "sine", "f0": 1800, "f1": 900, "fm": [2.76, 3, 8], "vol": 0.6}]},
    "Bacteria_SAureus_Muere":   {"dur": 0.45, "caida": 1.3, "capas": [   # el racimo se separa: ¡plip-plop!
        {"onda": "sine", "f0": 350, "f1": 900, "curva": 0.5, "vol": 0.9, "hasta": 0.15, "decae": 12},
        {"onda": "sine", "f0": 280, "f1": 750, "curva": 0.5, "vol": 0.9, "desde": 0.14, "decae": 12},
        {"ruido": True, "suave": 1, "vol": 0.5, "filtro": ["bandpass", [400, 2000]], "decae": 10}]},
    "Bacteria_SAureus_Coco_Muere": {"dur": 0.20, "caida": 1.8, "vol": 0.6, "capas": [   # cocos chiquitos: ¡pip!
        {"onda": "sine", "f0": 500, "f1": 1200, "curva": 0.5, "vol": 1, "decae": 25}]},
    "Bacteria_Pertussis_Prepara": {"dur": 0.70, "forma": "sube", "caida": 0.9, "vol": 0.55, "capas": [   # toma aire con silbido ("whoop")
        {"ruido": True, "suave": 1, "vol": 0.8, "filtro": ["bandpass", [700, 3000]]},
        {"onda": "sine", "f0": 650, "f1": 1250, "curva": 0.8, "vol": 0.5, "vibrato": [6, 15]}]},
    "Bacteria_Pertussis_Disparo": {"dur": 0.22, "caida": 1.6, "vol": 0.75, "satura": 2, "capas": [   # ¡kof!
        {"ruido": True, "suave": 1, "vol": 1, "filtro": ["bandpass", [250, 2200]], "decae": 14},
        {"onda": "saw", "f0": 170, "f1": 110, "vol": 0.6, "filtro": ["lowpass", 900], "decae": 18}]},
    "Bacteria_Neumococo_Ataca": {"dur": 0.35, "caida": 1.5, "vol": 0.7, "capas": [   # brinco: ¡boing!
        {"onda": "sine", "f0": 160, "f1": 480, "curva": 0.5, "vol": 1, "vibrato": [18, 30]},
        {"onda": "tri", "f0": 320, "f1": 960, "curva": 0.5, "vol": 0.3}]},
    "Virus_Influenza_Prepara":  {"dur": 0.55, "forma": "sube", "caida": 1.3, "vol": 0.5, "capas": [   # apunta: chillido que sube
        {"onda": "square", "f0": 700, "f1": 1600, "curva": 1.2, "vol": 0.6, "vibrato": [20, 40], "filtro": ["lowpass", 4000]}]},
    "Virus_Influenza_Ataca":    {"dur": 0.80, "caida": 1.0, "vol": 0.6, "capas": [   # en picada: silbido que baja
        {"onda": "sine", "f0": 1900, "f1": 420, "vol": 0.8, "vibrato": [8, 20]},
        {"ruido": True, "suave": 1, "vol": 0.5, "filtro": ["bandpass", [1000, 4000]]}]},
    "Bacteria_Tuberculosis_Disparo": {"dur": 0.30, "caida": 1.5, "vol": 0.8, "satura": 2.2, "capas": [   # tos grave: ¡KOF!
        {"ruido": True, "suave": 1, "vol": 1, "filtro": ["bandpass", [150, 1200]], "decae": 10},
        {"onda": "saw", "f0": 120, "f1": 80, "vol": 0.7, "filtro": ["lowpass", 600], "decae": 12}]},
    "Bacteria_Tuberculosis_Especial": {"dur": 0.80, "caida": 1.2, "vol": 0.7, "capas": [   # se encierra en granuloma
        {"ruido": True, "suave": 1, "vol": 0.8, "chispas": 0.9, "filtro": ["bandpass", [600, 4000]], "golpes": [0.09, 3]},
        {"ruido": True, "suave": 1, "vol": 0.7, "filtro": ["lowpass", 300]},
        {"onda": "sine", "f0": 90, "f1": 60, "vol": 0.6}]},
    "Bacteria_Tuberculosis_Muere": {"dur": 0.90, "caida": 1.3, "satura": 1.5, "reverb": [0.6, 0.2], "capas": [   # la cera se quiebra
        {"ruido": True, "suave": 1, "vol": 0.9, "chispas": 0.85, "filtro": ["bandpass", [800, 5000]], "golpes": [0.12, 3]},
        {"onda": "sine", "f0": 240, "f1": 50, "curva": 0.6, "vol": 0.9},
        {"ruido": True, "suave": 1, "vol": 0.6, "filtro": ["lowpass", 400], "decae": 4}]},
    "Virus_Poliovirus_Disparo": {"dur": 0.30, "caida": 1.5, "vol": 0.6, "capas": [   # rayo que paraliza: zumbido
        {"onda": "square", "f0": 900, "f1": 500, "vol": 0.6, "tremolo": [40, 0.9], "filtro": ["lowpass", 3500]},
        {"onda": "sine", "f0": 1800, "f1": 1000, "vol": 0.4}]},
    "Bacteria_Meningococo_Prepara": {"dur": 0.50, "forma": "sube", "caida": 1.5, "vol": 0.5, "capas": [   # se agazapa: tensión
        {"onda": "saw", "f0": 200, "f1": 520, "curva": 1.5, "vol": 0.6, "filtro": ["lowpass", 2500], "tremolo": [12, 0.5, 40]}]},
    "Bacteria_Meningococo_Ataca": {"dur": 0.45, "caida": 1.2, "vol": 0.7, "capas": [   # ¡zum! cruza a toda velocidad
        {"ruido": True, "suave": 1, "vol": 1, "filtro": ["bandpass", [700, 4000]]},
        {"onda": "saw", "f0": 600, "f1": 200, "curva": 0.6, "vol": 0.4, "filtro": ["lowpass", 2500]}]},
    "Virus_Herpes_Especial":    {"dur": 0.80, "caida": 1.0, "vol": 0.5, "reverb": [0.8, 0.35], "capas": [   # se vuelve latente (invisible)
        {"onda": "sine", "f0": 1500, "f1": 400, "curva": 0.8, "vol": 0.6, "fm": [2, 1.5, 3]},
        {"ruido": True, "suave": 1, "vol": 0.5, "filtro": ["bandpass", [2000, 7000]], "tremolo": [8, 0.6]}]},
    "Virus_Herpes_Disparo":     {"dur": 0.18, "caida": 1.8, "vol": 0.6, "capas": [   # ráfaga brillante
        {"onda": "square", "f0": 1600, "f1": 500, "curva": 0.5, "vol": 0.7, "filtro": ["lowpass", 5000]},
        {"onda": "sine", "f0": 2400, "f1": 900, "vol": 0.4}]},
    "Virus_Rabia_Prepara":      {"dur": 0.30, "forma": "sube", "caida": 1.2, "vol": 0.6, "capas": [   # gruñido
        {"onda": "saw", "f0": 120, "f1": 160, "vol": 0.8, "vibrato": [28, 18], "filtro": ["lowpass", 1400]},
        {"ruido": True, "suave": 1, "vol": 0.5, "filtro": ["bandpass", [300, 1500]], "tremolo": [28, 0.8]}]},
    "Virus_Rabia_Ataca":        {"dur": 0.20, "caida": 2.0, "vol": 0.8, "satura": 2, "capas": [   # ¡ñam! mordida
        {"ruido": True, "suave": 1, "vol": 1, "filtro": ["bandpass", [1000, 5000]], "decae": 40},
        {"onda": "sine", "f0": 300, "f1": 90, "vol": 0.8, "decae": 25}]},
    "Virus_Rabia_Muere":        {"dur": 0.60, "caida": 1.3, "reverb": [0.5, 0.2], "capas": [   # gruñido que se apaga
        {"onda": "saw", "f0": 150, "f1": 55, "curva": 0.7, "vol": 0.7, "vibrato": [24, 20], "filtro": ["lowpass", 1200]},
        {"onda": "sine", "f0": 1200, "f1": 1200, "fm": [2.76, 3, 9], "vol": 0.5, "decae": 10}]},

    # ---------------- Mecánicas de los niveles ----------------
    "Gas_Revienta":             {"dur": 0.35, "caida": 1.4, "vol": 0.7, "capas": [   # burbuja de gas: ¡plop! grave
        {"onda": "sine", "f0": 140, "f1": 420, "curva": 0.4, "vol": 1, "decae": 12},
        {"ruido": True, "suave": 1, "vol": 0.6, "filtro": ["bandpass", [200, 1200]], "decae": 16}]},
    "Moco_Dispara":             {"dur": 0.35, "caida": 1.3, "vol": 0.75, "satura": 1.6, "capas": [   # cañón de moco: ¡bluf!
        {"onda": "sine", "f0": 150, "f1": 60, "curva": 0.5, "vol": 1},
        {"ruido": True, "suave": 1, "vol": 0.7, "filtro": ["bandpass", [250, 1500]], "tremolo": [30, 0.6], "decae": 8}]},
    "Moco_Splat":               {"dur": 0.30, "caida": 1.6, "vol": 0.7, "capas": [   # ¡splat!
        {"ruido": True, "suave": 1, "vol": 1, "filtro": ["bandpass", [400, 3000]], "decae": 18},
        {"onda": "sine", "f0": 260, "f1": 90, "vol": 0.6, "decae": 14},
        {"burbujas": 30, "f0": 300, "f1": 700, "largo": 0.04, "vol": 0.4}]},
    "Grasa_Revienta":           {"dur": 0.40, "caida": 1.4, "vol": 0.7, "capas": [   # gota de grasa: ¡ploc! aceitoso
        {"onda": "sine", "f0": 700, "f1": 220, "curva": 0.4, "vol": 0.9, "decae": 10},
        {"burbujas": 35, "f0": 200, "f1": 500, "largo": 0.05, "vol": 0.6},
        {"ruido": True, "suave": 1, "vol": 0.4, "filtro": ["lowpass", 1200], "decae": 12}]},
    "Saliva_Ola":               {"dur": 1.40, "caida": 0.9, "ataque": 0.25, "vol": 0.6, "capas": [   # ola de saliva
        {"ruido": True, "suave": 1, "vol": 1, "filtro": ["bandpass", [300, 2500]], "tremolo": [3, 0.4]},
        {"burbujas": 25, "f0": 300, "f1": 900, "largo": 0.05, "vol": 0.5}]},
    "Estornudo_Aviso":          {"dur": 1.50, "forma": "sube", "caida": 1.2, "vol": 0.55, "capas": [   # "a... a... ¡a...!"
        {"onda": "saw", "f0": 220, "f1": 330, "vol": 0.6, "filtro": ["bandpass", [500, 1700]], "golpes": [0.5, 1.2]},
        {"ruido": True, "suave": 1, "vol": 0.6, "filtro": ["bandpass", [600, 2500]], "golpes": [0.5, 1.2]}]},
    "Estornudo":                {"dur": 0.90, "caida": 1.2, "ataque": 0.01, "vol": 0.85, "satura": 1.5, "reverb": [0.6, 0.2], "capas": [   # ¡ACHÚ!
        {"ruido": True, "suave": 1, "vol": 1, "filtro": ["bandpass", [1500, 7000]], "decae": 4},
        {"ruido": True, "suave": 1, "vol": 0.8, "filtro": ["lowpass", 500], "decae": 6},
        {"onda": "saw", "f0": 300, "f1": 180, "vol": 0.4, "filtro": ["bandpass", [400, 1400]], "decae": 10}]},
    "Tos_Aviso":                {"dur": 1.50, "forma": "sube", "caida": 1.0, "vol": 0.5, "capas": [   # la tráquea se irrita (carraspeo)
        {"ruido": True, "suave": 1, "vol": 0.8, "filtro": ["bandpass", [300, 1500]], "tremolo": [18, 0.7]},
        {"onda": "saw", "f0": 110, "f1": 140, "vol": 0.4, "filtro": ["lowpass", 700]}]},
    "Tos_Rafaga":               {"dur": 1.00, "caida": 1.2, "vol": 0.85, "satura": 2, "reverb": [0.7, 0.25], "capas": [   # ¡KOF-KOF!
        {"ruido": True, "suave": 1, "vol": 1, "filtro": ["bandpass", [200, 2000]], "golpes": [0.3, 5]},
        {"onda": "saw", "f0": 140, "f1": 100, "vol": 0.6, "filtro": ["lowpass", 800], "golpes": [0.3, 6]},
        {"ruido": True, "suave": 1, "vol": 0.5, "filtro": ["bandpass", [800, 4000]], "desde": 0.15}]},
    "Tos_Inhala":               {"dur": 1.00, "forma": "sube", "caida": 0.9, "vol": 0.6, "capas": [   # la reina toma aire ("whoop")
        {"ruido": True, "suave": 1, "vol": 0.8, "filtro": ["bandpass", [600, 3000]]},
        {"onda": "sine", "f0": 500, "f1": 1300, "curva": 0.7, "vol": 0.6, "vibrato": [5, 20]}]},
    "Tos_Fuerte":               {"dur": 0.60, "caida": 1.4, "vol": 0.9, "satura": 2.2, "reverb": [0.8, 0.25], "capas": [   # ¡KOF! de jefe
        {"ruido": True, "suave": 1, "vol": 1, "filtro": ["bandpass", [150, 1800]], "decae": 6},
        {"onda": "saw", "f0": 110, "f1": 70, "vol": 0.8, "filtro": ["lowpass", 700], "decae": 7},
        {"onda": "sine", "f0": 60, "f1": 40, "vol": 0.6}]},
    "Ojo_Parpadeo":             {"dur": 0.30, "caida": 1.5, "vol": 0.4, "capas": [   # parpadeo húmedo y suave
        {"ruido": True, "suave": 1, "vol": 0.8, "filtro": ["bandpass", [400, 2000]], "decae": 12},
        {"onda": "sine", "f0": 180, "f1": 120, "vol": 0.4, "desde": 0.12, "decae": 25}]},
    "Memoria_Desvanece":        {"dur": 0.90, "caida": 1.2, "vol": 0.55, "reverb": [1.0, 0.35], "capas": [   # burbuja de memoria que se borra
        {"onda": "sine", "f0": 1760, "f1": 880, "curva": 0.7, "vol": 0.6, "fm": [2, 2, 4]},
        {"onda": "sine", "f0": 1320, "f1": 660, "curva": 0.7, "vol": 0.4, "fm": [2, 1.5, 4], "desde": 0.08},
        {"ruido": True, "suave": 1, "vol": 0.3, "filtro": ["highpass", 4000], "tremolo": [12, 0.7]}]},
    "Piso_Ruidoso":             {"dur": 0.25, "caida": 1.8, "vol": 0.6, "capas": [   # piso que cruje: ¡te pueden oír!
        {"onda": "sine", "f0": 820, "f1": 760, "vol": 0.7, "decae": 30},
        {"ruido": True, "suave": 1, "vol": 0.6, "chispas": 0.8, "filtro": ["bandpass", [800, 4000]], "decae": 10},
        {"onda": "saw", "f0": 140, "f1": 100, "vol": 0.4, "vibrato": [25, 20], "filtro": ["lowpass", 900], "desde": 0.04}]},
    "Pulso_Ritmo":              {"dur": 0.22, "caida": 1.8, "vol": 0.55, "capas": [   # plataformas al ritmo: "bip" con golpe
        {"onda": "sine", "f0": 880, "f1": 880, "vol": 0.6, "decae": 18},
        {"onda": "sine", "f0": 150, "f1": 55, "vol": 0.9, "decae": 20}]},
    "Secuencia_Nota_0":         {"dur": 0.60, "caida": 1.5, "vol": 0.6, "reverb": [0.6, 0.25], "capas": [   # neuronas del lóbulo frontal:
        {"onda": "sine", "f0": 523, "f1": 523, "fm": [3.5, 1.5, 6], "vol": 1}]},                            # cada una su nota
    "Secuencia_Nota_1":         {"dur": 0.60, "caida": 1.5, "vol": 0.6, "reverb": [0.6, 0.25], "capas": [
        {"onda": "sine", "f0": 587, "f1": 587, "fm": [3.5, 1.5, 6], "vol": 1}]},
    "Secuencia_Nota_2":         {"dur": 0.60, "caida": 1.5, "vol": 0.6, "reverb": [0.6, 0.25], "capas": [
        {"onda": "sine", "f0": 659, "f1": 659, "fm": [3.5, 1.5, 6], "vol": 1}]},
    "Secuencia_Nota_3":         {"dur": 0.60, "caida": 1.5, "vol": 0.6, "reverb": [0.6, 0.25], "capas": [
        {"onda": "sine", "f0": 784, "f1": 784, "fm": [3.5, 1.5, 6], "vol": 1}]},
    "Secuencia_Nota_4":         {"dur": 0.60, "caida": 1.5, "vol": 0.6, "reverb": [0.6, 0.25], "capas": [
        {"onda": "sine", "f0": 880, "f1": 880, "fm": [3.5, 1.5, 6], "vol": 1}]},
    "Secuencia_Error":          {"dur": 0.50, "caida": 1.2, "vol": 0.6, "capas": [   # ¡mal! zumbido que baja
        {"onda": "square", "f0": 220, "f1": 110, "vol": 0.6, "filtro": ["lowpass", 2000], "golpes": [0.16, 2]},
        {"onda": "square", "f0": 233, "f1": 116, "vol": 0.5, "filtro": ["lowpass", 2000], "golpes": [0.16, 2]}]},
    "Secuencia_Correcta":       {"dur": 1.00, "caida": 1.2, "vol": 0.6, "reverb": [0.8, 0.3], "capas": [   # ¡abre! arpegio
        {"onda": "sine", "f0": 523, "f1": 523, "fm": [3.5, 1.2, 6], "vol": 0.8},
        {"onda": "sine", "f0": 659, "f1": 659, "fm": [3.5, 1.2, 6], "vol": 0.8, "desde": 0.1},
        {"onda": "sine", "f0": 784, "f1": 784, "fm": [3.5, 1.2, 6], "vol": 0.8, "desde": 0.2},
        {"onda": "sine", "f0": 1047, "f1": 1047, "fm": [3.5, 1.2, 6], "vol": 0.8, "desde": 0.3}]},
    "Reflejo_Arma":             {"dur": 0.35, "forma": "sube", "caida": 1.4, "vol": 0.5, "capas": [   # el resorte se carga
        {"onda": "tri", "f0": 300, "f1": 900, "curva": 1.4, "vol": 0.8, "tremolo": [20, 0.5, 45]}]},
    "Reflejo_Salta":            {"dur": 0.40, "caida": 1.4, "vol": 0.8, "capas": [   # ¡toc! martillito de reflejo y ¡boing!
        {"onda": "sine", "f0": 1200, "f1": 1100, "vol": 0.6, "decae": 60},
        {"onda": "sine", "f0": 140, "f1": 560, "curva": 0.5, "vol": 0.9, "vibrato": [22, 25], "desde": 0.02}]},
    "Sigilo_Detectado":         {"dur": 0.70, "caida": 1.2, "vol": 0.75, "reverb": [0.5, 0.2], "capas": [   # "¡!" te vieron
        {"onda": "square", "f0": 988, "f1": 988, "vol": 0.6, "filtro": ["lowpass", 5000], "hasta": 0.12},
        {"onda": "square", "f0": 1319, "f1": 1319, "vol": 0.6, "filtro": ["lowpass", 5000], "desde": 0.1},
        {"onda": "saw", "f0": 80, "f1": 60, "vol": 0.5, "filtro": ["lowpass", 500]}]},
    "Recoger_Vida":             {"dur": 0.80, "caida": 1.3, "vol": 0.6, "reverb": [0.6, 0.25], "capas": [   # vida: acorde que sube
        {"onda": "tri", "f0": 523, "f1": 523, "vol": 0.7, "hasta": 0.4},
        {"onda": "tri", "f0": 659, "f1": 659, "vol": 0.7, "desde": 0.08},
        {"onda": "tri", "f0": 784, "f1": 784, "vol": 0.7, "desde": 0.16},
        {"onda": "sine", "f0": 1047, "f1": 1047, "vol": 0.5, "desde": 0.24, "vibrato": [6, 6]}]},
    "Recoger_Capsula":          {"dur": 0.40, "caida": 1.4, "vol": 0.6, "capas": [   # cápsula: ¡pop-tlin!
        {"onda": "sine", "f0": 400, "f1": 900, "curva": 0.4, "vol": 0.8, "hasta": 0.1},
        {"onda": "sine", "f0": 1568, "f1": 1568, "fm": [3.5, 1, 8], "vol": 0.6, "desde": 0.08}]},
    "Memoria_Recoge":           {"dur": 1.20, "caida": 1.1, "vol": 0.55, "reverb": [1.2, 0.4], "capas": [   # fragmento de memoria
        {"onda": "sine", "f0": 1047, "f1": 1047, "fm": [2, 1.2, 4], "vol": 0.6},
        {"onda": "sine", "f0": 1175, "f1": 1175, "fm": [2, 1.2, 4], "vol": 0.6, "desde": 0.07},
        {"onda": "sine", "f0": 1397, "f1": 1397, "fm": [2, 1.2, 4], "vol": 0.6, "desde": 0.14},
        {"onda": "sine", "f0": 1568, "f1": 1568, "fm": [2, 1.2, 4], "vol": 0.6, "desde": 0.21},
        {"onda": "sine", "f0": 2093, "f1": 2093, "fm": [2, 1.2, 4], "vol": 0.5, "desde": 0.28},
        {"ruido": True, "suave": 1, "vol": 0.25, "filtro": ["highpass", 6000], "tremolo": [14, 0.8]}]},
    "Placa_Rompe":              {"dur": 0.70, "caida": 1.4, "vol": 0.8, "satura": 1.5, "capas": [   # placa / sarro que se rompe
        {"ruido": True, "suave": 1, "vol": 1, "chispas": 0.8, "filtro": ["bandpass", [1000, 6000]], "golpes": [0.08, 4]},
        {"onda": "sine", "f0": 180, "f1": 60, "vol": 0.8, "decae": 8},
        {"ruido": True, "suave": 1, "vol": 0.5, "filtro": ["lowpass", 800], "decae": 9}]},
    "Inmune_Activa":            {"dur": 1.00, "caida": 1.1, "ataque": 0.02, "vol": 0.6, "reverb": [0.8, 0.3], "capas": [   # las defensas se activan
        {"onda": "saw", "f0": 220, "f1": 440, "curva": 0.5, "vol": 0.4, "filtro": ["lowpass", 2500]},
        {"onda": "saw", "f0": 277, "f1": 554, "curva": 0.5, "vol": 0.35, "filtro": ["lowpass", 2500]},
        {"onda": "saw", "f0": 330, "f1": 660, "curva": 0.5, "vol": 0.35, "filtro": ["lowpass", 2500]},
        {"onda": "sine", "f0": 1320, "f1": 1320, "vol": 0.3, "desde": 0.3, "tremolo": [8, 0.5]}]},
    "Muela_Mastica":            {"dur": 0.45, "caida": 1.5, "vol": 0.85, "satura": 1.8, "capas": [   # ¡CRUNCH!
        {"ruido": True, "suave": 1, "vol": 1, "chispas": 0.6, "filtro": ["bandpass", [600, 5000]], "decae": 8},
        {"onda": "sine", "f0": 110, "f1": 45, "vol": 1, "decae": 10}]},
    "Aire_Impulso":             {"dur": 0.50, "caida": 1.2, "ataque": 0.03, "vol": 0.7, "capas": [   # corriente de aire hacia arriba
        {"ruido": True, "suave": 1, "vol": 1, "filtro": ["bandpass", [500, 3000]]},
        {"onda": "sine", "f0": 200, "f1": 600, "curva": 0.6, "vol": 0.3}]},
    "Peristalsis_Ola":          {"dur": 1.40, "caida": 1.0, "ataque": 0.2, "vol": 0.8, "capas": [   # el intestino se aprieta
        {"ruido": True, "suave": 1, "vol": 1, "filtro": ["lowpass", 500], "tremolo": [9, 0.6]},
        {"onda": "saw", "f0": 70, "f1": 110, "vol": 0.5, "vibrato": [6, 10], "filtro": ["lowpass", 600]},
        {"burbujas": 12, "f0": 90, "f1": 260, "largo": 0.12, "vol": 0.6}]},
    "Oxigeno_Recoge":           {"dur": 0.55, "caida": 1.3, "vol": 0.6, "reverb": [0.5, 0.2], "capas": [   # burbuja de oxígeno: ¡aaah!
        {"onda": "sine", "f0": 300, "f1": 900, "curva": 0.5, "vol": 0.7, "hasta": 0.15},
        {"ruido": True, "suave": 1, "vol": 0.6, "filtro": ["bandpass", [800, 4000]], "desde": 0.05},
        {"onda": "sine", "f0": 1175, "f1": 1175, "vol": 0.4, "desde": 0.12, "fm": [2, 0.8, 5]}]},
    "Hilo_Engancha":            {"dur": 0.40, "caida": 1.6, "vol": 0.7, "capas": [   # el hilo dental se tensa: ¡tuing!
        {"onda": "saw", "f0": 330, "f1": 310, "vol": 0.6, "filtro": ["lowpass", 3000], "decae": 9, "vibrato": [30, 6]},
        {"onda": "sine", "f0": 1500, "f1": 1400, "vol": 0.4, "decae": 40}]},
    "Puerta_Abre":              {"dur": 1.00, "caida": 1.1, "ataque": 0.05, "vol": 0.7, "capas": [   # esfínter que se abre
        {"onda": "saw", "f0": 80, "f1": 140, "vol": 0.5, "filtro": ["lowpass", 500], "vibrato": [5, 6]},
        {"ruido": True, "suave": 1, "vol": 0.8, "filtro": ["bandpass", [200, 1200]], "tremolo": [7, 0.5]},
        {"burbujas": 10, "f0": 150, "f1": 350, "largo": 0.08, "vol": 0.4}]},
    "Azucar_Rompe":             {"dur": 0.35, "caida": 1.6, "vol": 0.6, "reverb": [0.4, 0.2], "capas": [   # cubito de azúcar
        {"ruido": True, "suave": 1, "vol": 0.8, "chispas": 0.85, "filtro": ["highpass", 2500]},
        {"onda": "sine", "f0": 2600, "f1": 2600, "fm": [2.7, 2, 15], "vol": 0.5}]},
    "Quiste_Sale":              {"dur": 0.50, "caida": 1.3, "vol": 0.7, "capas": [   # el toxoplasma sale de su quiste
        {"onda": "sine", "f0": 120, "f1": 380, "curva": 0.5, "vol": 0.9, "decae": 6},
        {"ruido": True, "suave": 1, "vol": 0.6, "filtro": ["bandpass", [300, 1500]], "decae": 10},
        {"burbujas": 25, "f0": 200, "f1": 600, "largo": 0.05, "vol": 0.5}]},
    "Quiste_Entra":             {"dur": 0.50, "caida": 1.2, "vol": 0.6, "capas": [   # se esconde: ¡shlup!
        {"onda": "sine", "f0": 400, "f1": 110, "curva": 0.6, "vol": 0.9},
        {"ruido": True, "suave": 1, "vol": 0.6, "filtro": ["bandpass", [250, 1200]], "decae": 8}]},
    "Nodo_Mensaje":             {"dur": 0.25, "caida": 1.6, "vol": 0.35, "capas": [   # llega un mensaje de NODO
        {"onda": "sine", "f0": 1319, "f1": 1319, "vol": 0.7, "hasta": 0.08},
        {"onda": "sine", "f0": 1760, "f1": 1760, "vol": 0.7, "desde": 0.07}]},
    "Nodo_Alerta":              {"dur": 0.35, "caida": 1.4, "vol": 0.4, "capas": [   # mensaje urgente de NODO
        {"onda": "square", "f0": 880, "f1": 880, "vol": 0.5, "filtro": ["lowpass", 4000], "golpes": [0.09, 3]},
        {"onda": "square", "f0": 1175, "f1": 1175, "vol": 0.4, "filtro": ["lowpass", 4000], "golpes": [0.09, 3]}]},
    "Bestiario_Nuevo":          {"dur": 0.70, "caida": 1.3, "vol": 0.45, "reverb": [0.6, 0.25], "capas": [   # patógeno nuevo en el bestiario
        {"onda": "sine", "f0": 784, "f1": 784, "fm": [3.5, 1, 6], "vol": 0.7},
        {"onda": "sine", "f0": 1175, "f1": 1175, "fm": [3.5, 1, 6], "vol": 0.7, "desde": 0.1}]},
}

rng = np.random.default_rng(7)

def onda(tipo, fase):
    if tipo == "sine": return np.sin(fase)
    if tipo == "square": return np.sign(np.sin(fase)) * 0.6
    if tipo == "saw": return 2 * ((fase / (2 * np.pi)) % 1) - 1
    if tipo == "tri": return 2 * np.abs(2 * ((fase / (2 * np.pi)) % 1) - 1) - 1
    raise ValueError("onda desconocida: " + tipo)

def capa_tono(c, n):
    t = np.linspace(0, 1, n)
    f = c["f0"] + (c["f1"] - c["f0"]) * t ** c.get("curva", 1)
    if "vibrato" in c:
        hz, prof = c["vibrato"]
        f = f + prof * np.sin(2 * np.pi * hz * np.arange(n) / SR)
    fase = 2 * np.pi * np.cumsum(f) / SR
    if "fm" in c:
        razon, indice, caida = (list(c["fm"]) + [0])[:3]
        s = np.sin(fase + indice * np.exp(-np.arange(n) / SR * caida) * np.sin(fase * razon))
    else:
        s = onda(c["onda"], fase)
    if "pulsos" in c:
        s = s * (np.sin(2 * np.pi * c["pulsos"] * np.arange(n) / SR) > -0.2)
    return s * c.get("vol", 1)

def capa_ruido(c, n):
    x = rng.uniform(-1, 1, n)
    a = c.get("suave", 1)
    if a < 1:  # filtro: menos agudos = ruido más sordo. "polos" = cuántas veces se filtra (más = más suave, sin siseo)
        for _ in range(c.get("polos", 1)):
            y = np.zeros(n)
            for i in range(1, n): y[i] = y[i - 1] + a * (x[i] - y[i - 1])
            x = y
        x = x / (np.max(np.abs(x)) + 1e-9)
    if "chispas" in c:
        x = x * (rng.uniform(0, 1, n) > c["chispas"])
    return x * c.get("vol", 1)

def capa_burbujas(c, n):
    s = np.zeros(n)
    largo = int(SR * c.get("largo", 0.05))
    cantidad = int(c["burbujas"] * n / SR)
    for _ in range(cantidad):
        i = int(rng.uniform(0, max(1, n - largo)))
        f0 = c["f0"] * rng.uniform(0.7, 1.4)
        f1 = c["f1"] * rng.uniform(0.7, 1.4)
        f = np.linspace(f0, f1, largo)
        blip = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.linspace(1, 0, largo) ** 2
        s[i:i + largo] += blip * rng.uniform(0.4, 1)
    return s * c.get("vol", 1)

def modular(c, s):
    n = len(s)
    k = np.arange(n) / SR
    if "tremolo" in c:
        hz, prof = c["tremolo"][:2]
        hz_fin = c["tremolo"][2] if len(c["tremolo"]) > 2 else hz
        fase = 2 * np.pi * np.cumsum(np.linspace(hz, hz_fin, n)) / SR
        s = s * (1 - prof * (0.5 - 0.5 * np.cos(fase)))
    if "golpes" in c:
        cada, caida = c["golpes"]
        fase = (k % cada) / cada
        s = s * np.exp(-fase * caida)
    if "filtro" in c:
        tipo, corte = c["filtro"]
        s = sosfilt(butter(2, corte, fs=SR, btype=tipo, output="sos"), s)
    desde, hasta = c.get("desde", 0.0), c.get("hasta", n / SR)
    if "decae" in c:
        s = s * np.exp(-np.maximum(0, k - desde) * c["decae"])
    if c.get("crece"):
        s = s * np.clip((k - desde) / max(1e-3, hasta - desde), 0, 1) ** 1.5
    if "desde" in c or "hasta" in c:
        rampa = 0.003   # 3 ms para entrar y salir sin chasquido
        s = s * np.clip((k - desde) / rampa, 0, 1) * np.clip((hasta - k) / rampa, 0, 1)
    return s

def generar(nombre, d):
    n = int(SR * d["dur"])
    s = np.zeros(n)
    for c in d["capas"]:
        if c.get("ruido"): capa = capa_ruido(c, n)
        elif "burbujas" in c: capa = capa_burbujas(c, n)
        else: capa = capa_tono(c, n)
        s += modular(c, capa)
    t = np.linspace(0, 1, n)
    if d.get("bucle"):
        # Bucle: sin envolvente; el final se funde con el principio para que no se note el corte.
        x = int(SR * 0.25)
        mezcla = np.linspace(0, 1, x)
        s[:x] = s[:x] * mezcla + s[-x:] * (1 - mezcla)
        s = s[:-x]
        env = np.ones(len(s))
    elif d.get("forma") == "sube":
        env = t ** d.get("caida", 2)
        env[-int(SR * 0.01):] *= np.linspace(1, 0, int(SR * 0.01))
    else:
        env = (1 - t) ** d.get("caida", 2)
    ataque = int(SR * d.get("ataque", 0.002))
    if ataque > 0 and not d.get("bucle"): env[:ataque] *= np.linspace(0, 1, ataque)
    s = s * env
    if "satura" in d:
        s = s / (np.max(np.abs(s)) + 1e-9)
        s = np.tanh(d["satura"] * s) / np.tanh(d["satura"])
    if "eco" in d:
        cada, retro, veces = d["eco"]
        paso = int(cada * SR)
        s = np.concatenate([s, np.zeros(paso * veces)])
        seco = s.copy()
        for v in range(1, veces + 1):
            s[paso * v:] += seco[:len(s) - paso * v] * retro ** v
    if "reverb" in d:
        segundos, mezcla = d["reverb"]
        m = int(segundos * SR)
        t = np.arange(m) / SR
        sala = np.random.default_rng(len(nombre)).normal(0, 1, m) * np.exp(-t * 6.9 / segundos)
        sala = sosfilt(butter(2, [200, 6000], fs=SR, btype="bandpass", output="sos"), sala)
        sala /= np.sqrt(np.sum(sala ** 2))
        s = s / (np.max(np.abs(s)) + 1e-9)
        cola = fftconvolve(s, sala) * mezcla
        s = np.concatenate([s, np.zeros(m)])
        s[:len(cola)] += cola
        s[-int(0.05 * SR):] *= np.linspace(1, 0, int(0.05 * SR))
    s = s / (np.max(np.abs(s)) + 1e-9) * d.get("vol", 0.8)

    os.makedirs(SALIDA, exist_ok=True)
    wav = os.path.join(SALIDA, nombre + ".wav")
    with wave.open(wav, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((s * 32767).astype(np.int16).tobytes())

    # .ogg para el juego (los .wav van por Git LFS en este repo). Si no hay ffmpeg, se queda el .wav.
    if shutil.which("ffmpeg"):
        ogg = wav[:-4] + ".ogg"
        subprocess.run(["ffmpeg", "-loglevel", "error", "-y", "-i", wav, "-c:a", "libvorbis", "-q:a", "6", ogg], check=True)
        os.remove(wav)
        print("ok", os.path.basename(ogg))
    else:
        print("ok", os.path.basename(wav), "(instala ffmpeg para .ogg)")

if __name__ == "__main__":
    nombres = sys.argv[1:] or list(SONIDOS)
    for nombre in nombres:
        if nombre not in SONIDOS:
            print("No existe:", nombre); continue
        generar(nombre, SONIDOS[nombre])
