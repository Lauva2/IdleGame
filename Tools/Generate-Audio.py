"""Original synthesized score and sound design; no external recordings or melodies."""
import math
import random
import wave
from pathlib import Path
import struct
import sys

ROOT = Path(__file__).resolve().parents[1] / 'Assets/Resources/Audio'
ROOT.mkdir(parents=True, exist_ok=True)
RATE = 22050

def write(name, data):
    peak = max(max(map(abs, data)), 0.01)
    scale = min(0.84 / peak, 1.0)
    with wave.open(str(ROOT / (name + '.wav')), 'wb') as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes(b''.join(struct.pack('<h', int(max(-1, min(1, x * scale)) * 32767)) for x in data))

def hz(midi):
    return 440 * 2 ** ((midi - 69) / 12)

def note(buf, start, duration, midi, volume=0.12):
    begin, end = int(start * RATE), min(len(buf), int((start + duration) * RATE))
    freq = hz(midi)
    for i in range(begin, end):
        t = (i - begin) / RATE
        env = (1 - math.exp(-t * 30)) * math.exp(-t * 2.3) * min(1, (duration - t) * 10)
        s = math.sin(2 * math.pi * freq * t) + .22 * math.sin(2 * math.pi * freq * 2 * t) + .07 * math.sin(2 * math.pi * freq * 3 * t)
        buf[i] += volume * env * s

# 64-second cyclic original motif with airy pads and sparse pentatonic music-box notes.
length = 64
score = [0.] * (RATE * length)
chords = [(48, 55, 60, 64), (45, 52, 57, 60), (53, 60, 65, 69), (55, 62, 67, 71)]
melody = [72, 76, 79, 76, 74, 72, 69, 72, 76, 79, 81, 79, 76, 74, 72, 67]
for bar in range(16):
    chord = chords[(bar // 2) % 4]
    for k in range(4):
        note(score, bar * 4 + k, 2.8, chord[k] + 12, .075)
    note(score, bar * 4 + .5, 2.5, melody[bar], .10)
    note(score, bar * 4 + 2.5, 1.7, melody[(bar + 3) % 16], .055)
    for k in range(4):
        f = hz(chord[k])
        for j in range(RATE * 4):
            t = j / RATE
            score[bar * RATE * 4 + j] += .013 * math.sin(math.pi * t / 4) ** 2 * math.sin(2 * math.pi * f * t)
rng = random.Random(41)
filtered = 0.
ambience = [0.] * len(score)
for i in range(len(score)):
    filtered = .985 * filtered + .015 * rng.uniform(-1, 1)
    # Constant low woodland breeze; fade loop endpoints to eliminate clicks.
    ambience[i] = filtered * .026
    fade = min(1., i / (RATE * .4), (len(score) - 1 - i) / (RATE * .4))
    score[i] *= max(0., fade)
    ambience[i] *= max(0., fade)
for start in (7.5, 19., 34.5, 50.):
    for chirp in range(3):
        begin = int((start + chirp * .22) * RATE)
        for j in range(int(.15 * RATE)):
            t = j / RATE
            envelope = math.sin(math.pi * t / .15) ** 2
            # Original bird-like rising chirps, deliberately quiet behind the score.
            ambience[begin + j] += .014 * envelope * math.sin(2 * math.pi * (1900 * t + 3500 * t * t))
if '--legacy-music' in sys.argv:
    write('woodland', score)
else:
    print('Keeping licensed woodland background music; --legacy-music is only for historical reproduction.')
write('ambience', ambience)

for name, notes in {
    'tap': [(0, .14, 79, .11)],
    'confirm': [(0, .55, 72, .17), (.10, .55, 76, .15)],
    'sale': [(0, .45, 84, .15), (.09, .5, 88, .12)],
    'harvest': [(0, .35, 76, .13), (.075, .4, 81, .11)],
    'unlock': [(0, .8, 72, .14), (.14, .8, 76, .14), (.28, .8, 79, .14), (.5, 1.2, 84, .12)],
    'door': [(0, .8, 79, .12), (.11, .9, 86, .10)],
    'cauldron': [(0, .25, 48, .09), (.13, .35, 55, .07)],
    'voice': [(0, .13, 72, .07), (.14, .13, 74, .06)],
}.items():
    n = max(t + d for t, d, _, _ in notes)
    audio = [0.] * int((n + .05) * RATE)
    for args in notes:
        note(audio, *args)
    write(name, audio)
print('Generated separate original breeze/birds ambience and 8 sound effects; background music is preserved unless explicitly reproducing the legacy score.')
