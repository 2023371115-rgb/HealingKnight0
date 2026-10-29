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
#                      "golpes": [cada s, caída] -> la capa suena a golpes (latido, pasos, retumbo)
#                      "desde": s               -> la capa empieza más tarde (p. ej. el trueno después del chasquido)
# Opciones del sonido: "dur", "caida", "ataque", "vol", "bucle": True, "forma": "sube"
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
    s = onda(c["onda"], 2 * np.pi * np.cumsum(f) / SR)
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
        hz, prof = c["tremolo"]
        s = s * (1 - prof * (0.5 - 0.5 * np.cos(2 * np.pi * hz * k)))
    if "golpes" in c:
        cada, caida = c["golpes"]
        fase = (k % cada) / cada
        s = s * np.exp(-fase * caida)
    if "desde" in c:
        s = s * (k >= c["desde"])
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
